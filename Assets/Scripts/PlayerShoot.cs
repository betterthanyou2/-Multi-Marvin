using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerShoot : Ability
{
    [Header("Shooting Settings")]
    public float damage = 10f;
    public float range = 100f;
    [Tooltip("Layers the shot can hit.")]
    public LayerMask hitMask = ~0;

    void Reset()
    {
        cooldown = 0.2f; 
    }

    public override bool WantsToActivate() =>
        Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;

    protected override void Activate(Pawn owner)
    {
        Transform cam = owner.cameraTransform;
        if (cam == null) return;

        Debug.Log("Fired Weapon!");

        Vector3 origin = cam.position;
        Vector3 direction = cam.forward;

        Debug.DrawRay(origin, direction * range, Color.red, 1f);

        RaycastHit[] hits = Physics.RaycastAll(origin, direction, range, hitMask, QueryTriggerInteraction.Ignore);
        Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (var hit in hits)
        {
            if (hit.transform.IsChildOf(owner.transform)) continue;

            Debug.Log("Hit object: " + hit.transform.name);

            IDamageable target = hit.collider.GetComponentInParent<IDamageable>();
            if (target != null)
            {
                target.TakeDamage(damage);
                Debug.Log("Damaged " + hit.transform.name);
            }
            break; 
        }
    }
}