using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerDash : Ability
{
    public float dashForce = 25f;
    public float doubleTapTime = 0.3f;
    [Tooltip("On: dash along the ground in the camera's facing direction. Off: also dash up/down with the camera's pitch.")]
    public bool flattenToGround = true;
    [Tooltip("Turn the body to face the dash direction.")]
    public bool faceDashDirection = true;

    float lastPressTime = -1f;

    void Reset()
    {
        cooldown = 1f;
    }

    public override bool WantsToActivate()
    {
        if (Keyboard.current == null || !Keyboard.current.spaceKey.wasPressedThisFrame)
            return false;

        if (lastPressTime >= 0f && Time.time - lastPressTime <= doubleTapTime)
        {
            lastPressTime = -1f;
            return true;
        }

        lastPressTime = Time.time;
        return false;
    }

    protected override void Activate(Pawn owner)
    {
        Transform cam = owner.cameraTransform != null ? owner.cameraTransform : owner.transform;

        Vector3 flat = cam.forward;
        flat.y = 0f;
        if (flat.sqrMagnitude < 0.001f) flat = owner.transform.forward;
        flat.Normalize();

        Vector3 dashDir = flattenToGround ? flat : cam.forward.normalized;

        if (faceDashDirection)
            owner.transform.rotation = Quaternion.LookRotation(flat);

        Debug.Log("DASH!");
        owner.AddImpulse(dashDir * dashForce);
    }
}