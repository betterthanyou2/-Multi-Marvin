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
    public Transform firePoint; 

    private float nextTimeToFire = 0f;

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

        RaycastHit hit;

        if (Physics.Raycast(playerCamera.transform.position, playerCamera.transform.forward, out hit, range))
        {
            Debug.Log("Hit object: " + hit.transform.name);
        }
    }
}
