using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PossessionController : MonoBehaviour
{
    [Header("References")]
    public Pawn startingPawn;     
    [Tooltip("Shared camera for any pawn with no camera of its own (usually your Main Camera for the player).")]
    public Camera playerCamera;

    [Header("Possession Rules")]
    public float possessDistance = 6f;
    [Tooltip("Max angle (degrees) between where the camera looks and the target.")]
    public float lookAngle = 15f;
    public LayerMask lineOfSightMask = ~0;
    public Key interactKey = Key.E;
    public Key releaseKey = Key.Q;

    [Header("Third Person Camera")]
    [Tooltip("Multiplier on mouse delta (pixels). ~0.1 is a good start.")]
    public float mouseSensitivity = 0.1f;
    public float minPitch = -30f;
    public float maxPitch = 70f;
    [Tooltip("Offset from the pivot in camera space. X = over-the-shoulder, Y = raise/lower. 0 = centered.")]
    public Vector2 shoulderOffset = new Vector2(0.5f, 0f);
    public float collisionRadius = 0.25f;
    public float minCameraDistance = 0.4f;
    public LayerMask cameraCollisionMask = ~0;
    [Tooltip("How tightly the camera follows the pawn during normal play.")]
    public float followSmoothTime = 0.05f;
    [Tooltip("How long the new camera takes to glide into place (position, distance and FOV) after possessing.")]
    public float possessTransitionTime = 0.4f;

    public Pawn CurrentPawn { get; private set; }
    public Pawn HomePawn => startingPawn;
    public Pawn LookedAtTarget { get; private set; } 
    public Camera ActiveCamera { get; private set; }

    float yaw, pitch = 15f;
    Vector3 smoothedPivot, pivotVelocity;
    float smoothedDistance, distanceVelocity;
    float transitionStartTime, transitionEndTime;
    float fovFrom, fovTo;

    readonly RaycastHit[] hitBuffer = new RaycastHit[16];
    readonly Dictionary<Camera, float> authoredFov = new Dictionary<Camera, float>();

    void Start()
    {
        if (startingPawn == null)
        {
            Debug.LogError("PossessionController: assign Starting Pawn!");
            enabled = false;
            return;
        }

        Cursor.lockState = CursorLockMode.Locked;
        yaw = startingPawn.transform.eulerAngles.y;

        if (playerCamera != null) Pawn.SetCameraActive(playerCamera, false);

        Possess(startingPawn, instant: true);
        if (CurrentPawn == null) enabled = false;
    }

    void Update()
    {
        LookedAtTarget = FindTarget();

        var kb = Keyboard.current;
        if (kb == null) return;

        if (kb[interactKey].wasPressedThisFrame && LookedAtTarget != null)
            Possess(LookedAtTarget);
        else if (kb[releaseKey].wasPressedThisFrame && CurrentPawn != startingPawn)
            Possess(startingPawn);
    }

    void LateUpdate()
    {
        if (CurrentPawn == null || ActiveCamera == null) return;
        HandleLook();
        UpdateCamera();
    }


    Camera CameraFor(Pawn pawn) => pawn.pawnCamera != null ? pawn.pawnCamera : playerCamera;

    float GetAuthoredFov(Camera cam)
    {
        if (!authoredFov.TryGetValue(cam, out float fov))
            authoredFov[cam] = fov = cam.fieldOfView;
        return fov;
    }

    void HandleLook()
    {
        if (Mouse.current == null || Cursor.lockState != CursorLockMode.Locked) return;

        Vector2 delta = Vector2.ClampMagnitude(Mouse.current.delta.ReadValue(), 100f) * mouseSensitivity;
        yaw += delta.x;
        pitch = Mathf.Clamp(pitch - delta.y, minPitch, maxPitch);
    }

    void UpdateCamera()
    {
        float smooth = Time.time < transitionEndTime ? possessTransitionTime : followSmoothTime;
        smoothedPivot = Vector3.SmoothDamp(smoothedPivot, CurrentPawn.CameraPivot, ref pivotVelocity, smooth);
        smoothedDistance = Mathf.SmoothDamp(smoothedDistance, CurrentPawn.cameraDistance, ref distanceVelocity, 0.3f);

        Quaternion rot = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 offset = rot * new Vector3(shoulderOffset.x, shoulderOffset.y, -smoothedDistance);
        float maxDist = offset.magnitude;
        Vector3 dir = offset / maxDist;
        float dist = maxDist;

        int count = Physics.SphereCastNonAlloc(smoothedPivot, collisionRadius, dir, hitBuffer, maxDist,
                                               cameraCollisionMask, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < count; i++)
        {
            var hit = hitBuffer[i];
            if (hit.collider.transform.IsChildOf(CurrentPawn.transform)) continue;
            if (hit.distance < dist) dist = hit.distance;
        }
        dist = Mathf.Max(dist, minCameraDistance);

        var camT = ActiveCamera.transform;
        camT.position = smoothedPivot + dir * dist;
        camT.rotation = rot;

        float span = transitionEndTime - transitionStartTime;
        float blend = span > 0f ? Mathf.Clamp01((Time.time - transitionStartTime) / span) : 1f;
        ActiveCamera.fieldOfView = Mathf.Lerp(fovFrom, fovTo, Mathf.SmoothStep(0f, 1f, blend));
        Debug.Log($"pivot {CurrentPawn.CameraPivot}, cam {camT.position}");
    }


    Pawn FindTarget()
    {
        if (CurrentPawn == null || ActiveCamera == null) return null;

        Vector3 camPos = ActiveCamera.transform.position;
        Vector3 camForward = ActiveCamera.transform.forward;

        Pawn best = null;
        float bestAngle = lookAngle;

        foreach (var col in Physics.OverlapSphere(CurrentPawn.transform.position, possessDistance))
        {
            var pawn = col.GetComponentInParent<Pawn>();
            if (pawn == null || pawn == CurrentPawn || !pawn.canBePossessed) continue;

            Vector3 toTarget = pawn.CameraPivot - camPos;
            float angle = Vector3.Angle(camForward, toTarget);
            if (angle >= bestAngle) continue;
            if (!HasLineOfSight(camPos, toTarget, pawn)) continue;

            best = pawn;
            bestAngle = angle;
        }
        return best;
    }

    bool HasLineOfSight(Vector3 origin, Vector3 toTarget, Pawn target)
    {
        var hits = Physics.RaycastAll(origin, toTarget.normalized, toTarget.magnitude + 0.5f,
                                      lineOfSightMask, QueryTriggerInteraction.Ignore);
        Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (var hit in hits)
        {
            if (CurrentPawn != null && hit.transform.IsChildOf(CurrentPawn.transform)) continue;
            return hit.transform.IsChildOf(target.transform);
        }
        return false;
    }

    public void Possess(Pawn next, bool instant = false)
    {
        if (next == null || next == CurrentPawn) return;

        Camera newCam = CameraFor(next);
        if (newCam == null)
        {
            Debug.LogError($"PossessionController: '{next.name}' has no camera and no shared Player Camera is assigned.");
            return;
        }

        fovTo = GetAuthoredFov(newCam);
        fovFrom = ActiveCamera != null ? ActiveCamera.fieldOfView : fovTo;

        if (CurrentPawn != null) CurrentPawn.SetPossessed(false);

        if (ActiveCamera != null && ActiveCamera != newCam) Pawn.SetCameraActive(ActiveCamera, false);
        Pawn.SetCameraActive(newCam, true);
        ActiveCamera = newCam;

        CurrentPawn = next;
        CurrentPawn.cameraTransform = newCam.transform;
        CurrentPawn.SetPossessed(true);

        transitionStartTime = Time.time;
        if (instant)
        {
            smoothedPivot = next.CameraPivot;
            smoothedDistance = next.cameraDistance;
            pivotVelocity = Vector3.zero;
            transitionEndTime = 0f;
        }
        else
        {
            transitionEndTime = Time.time + possessTransitionTime;
        }
    }
}