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
    public GameObject player;

    private float nextTimeToFire = 0f;
    private void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player");
    }

    void Update()
    {
        firePoint = player.transform;
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
        Debug.DrawRay(firePoint.position + firePoint.transform.forward * 2, firePoint.transform.forward * range, Color.red, 1f);
        if (Physics.Raycast(firePoint.position, firePoint.transform.forward, out hit, range))
        {
            Debug.Log("Hit object: " + hit.transform.name);
        }
    }
}
