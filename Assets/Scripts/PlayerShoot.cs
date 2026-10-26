using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerShoot : MonoBehaviour
{
    [Header("Shooting Settings")]
    public float damage = 10f;
    public float range = 100f;
    public float fireRate = 0.2f;

    [Header("References")]
    public Camera playerCamera;

    private float nextTimeToFire = 0f;

    void Start()
    {
        if (playerCamera == null)
            playerCamera = Camera.main;
    }

    void Update()
    {
        if (Mouse.current == null) return;

        if (Mouse.current.leftButton.wasPressedThisFrame && Time.time >= nextTimeToFire)
        {
            nextTimeToFire = Time.time + fireRate;
            Shoot();
        }
    }

    void Shoot()
    {
        Debug.Log("Fired Weapon!");

        Vector3 origin = playerCamera.transform.position;
        Vector3 direction = playerCamera.transform.forward;

        Debug.DrawRay(origin, direction * range, Color.red, 1f);

        if (Physics.Raycast(origin, direction, out RaycastHit hit, range))
        {
            Debug.Log("Hit object: " + hit.transform.name);

            IDamageable target = hit.collider.GetComponentInParent<IDamageable>();
            if (target != null)
            {
                target.TakeDamage(damage);
                Debug.Log("Damaged " + hit.transform.name);
            }
        }
    }
}