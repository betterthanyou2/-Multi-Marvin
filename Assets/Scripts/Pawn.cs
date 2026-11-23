using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class Pawn : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Empty child at head/chest height. The third-person camera orbits around this point.")]
    public Transform cameraAnchor;
    [Tooltip("Anything that drives this body on its own (AI scripts, NavMeshAgent...). Disabled while possessed.")]
    public Behaviour[] aiBehaviours;
    [Tooltip("If the list above is empty, disable every script (and NavMeshAgent) on this object while possessed. Untick on the player.")]
    public bool autoDetectAI = true;

    [Header("Camera")]
    [Tooltip("This pawn's own camera (a child Camera). Leave empty to use the controller's shared camera. Auto-found in children if empty.")]
    public Camera pawnCamera;
    [Tooltip("How far the third-person camera sits behind this pawn. Use bigger values for bigger enemies.")]
    public float cameraDistance = 4f;

    [Header("Movement")]
    public float moveSpeed = 5f;
    public float sprintMultiplier = 1.5f;
    public float jumpHeight = 1.2f;
    public float gravity = -20f;
    [Tooltip("Degrees per second the body turns toward the movement direction.")]
    public float turnSpeed = 720f;
    public float impulseDamping = 8f;

    [Header("Possession")]
    public bool canBePossessed = true;
    public UnityEvent onPossessed;
    public UnityEvent onReleased;

    public bool IsPossessed { get; private set; }

    [HideInInspector] public Transform cameraTransform;

    public Vector3 CameraPivot =>
        cameraAnchor != null ? cameraAnchor.position : transform.position + Vector3.up * 1.6f;

    CharacterController cc;
    Ability[] abilities;
    float verticalVelocity;
    Vector3 impulse;

    void Awake()
    {
        cc = GetComponent<CharacterController>();
        abilities = GetComponents<Ability>();

        if (pawnCamera == null) pawnCamera = GetComponentInChildren<Camera>(true);
        if (pawnCamera != null) SetCameraActive(pawnCamera, false);

        if (autoDetectAI && (aiBehaviours == null || aiBehaviours.Length == 0))
        {
            var found = new System.Collections.Generic.List<Behaviour>();
            foreach (var mb in GetComponents<MonoBehaviour>())
                if (mb != this && !(mb is Ability) && !(mb is PossessionController)) found.Add(mb);

            var agent = GetComponent<NavMeshAgent>();
            if (agent != null) found.Add(agent);

            aiBehaviours = found.ToArray();
        }
    }

    public void SetPossessed(bool possessed)
    {
        IsPossessed = possessed;

        foreach (var b in aiBehaviours)
            if (b != null) b.enabled = !possessed;

        if (possessed)
        {
            verticalVelocity = 0f;
            impulse = Vector3.zero;
            onPossessed?.Invoke();
        }
        else
        {
            cameraTransform = null;
            onReleased?.Invoke();
        }
    }

    public static void SetCameraActive(Camera cam, bool active)
    {
        if (cam == null) return;
        cam.enabled = active;
        var listener = cam.GetComponent<AudioListener>();
        if (listener != null) listener.enabled = active;
    }

    public void AddImpulse(Vector3 force) => impulse += force;

    void Update()
    {
        if (!IsPossessed) return;

        HandleMovement();
        HandleAbilities();
    }

    void HandleMovement()
    {
        var kb = Keyboard.current;
        if (kb == null) return;

        Vector2 input = new Vector2(
            (kb.dKey.isPressed ? 1f : 0f) - (kb.aKey.isPressed ? 1f : 0f),
            (kb.wKey.isPressed ? 1f : 0f) - (kb.sKey.isPressed ? 1f : 0f));
        input = Vector2.ClampMagnitude(input, 1f);

        float yaw = (cameraTransform != null ? cameraTransform : transform).eulerAngles.y;
        Vector3 moveDir = Quaternion.Euler(0f, yaw, 0f) * new Vector3(input.x, 0f, input.y);

        if (moveDir.sqrMagnitude > 0.001f)
        {
            Quaternion target = Quaternion.LookRotation(moveDir);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, target, turnSpeed * Time.deltaTime);
        }

        float speed = moveSpeed * (kb.leftShiftKey.isPressed ? sprintMultiplier : 1f);
        Vector3 move = moveDir * speed;

        if (cc.isGrounded && verticalVelocity < 0f) verticalVelocity = -2f;
        if (cc.isGrounded && kb.spaceKey.wasPressedThisFrame)
            verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
        verticalVelocity += gravity * Time.deltaTime;

        impulse = Vector3.Lerp(impulse, Vector3.zero, impulseDamping * Time.deltaTime);

        cc.Move((move + impulse + Vector3.up * verticalVelocity) * Time.deltaTime);
    }

    void HandleAbilities()
    {
        foreach (var a in abilities)
            if (a.WantsToActivate()) a.TryUse(this);
    }
}