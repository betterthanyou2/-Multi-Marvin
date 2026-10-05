using UnityEngine;

public class PlayerShoot : MonoBehaviour
{
    public GameObject bulletPrefab;
    public float bulletSpeed = 10f;

    private GameObject bulletSpawn;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        bulletSpawn = GameObject.Find("BulletSpawn");
    }

    // Update is called once per frame
    void Update()
    {
        if(Input.GetButtonDown("Fire1"))
        {
            GameObject tempBullet = Instantiate(bulletPrefab, bulletSpawn.transform.position, Quaternion.identity);
            tempBullet.GetComponent<Rigidbody>().linearVelocity = bulletSpawn.transform.up * bulletSpeed;
        }
    }
}
