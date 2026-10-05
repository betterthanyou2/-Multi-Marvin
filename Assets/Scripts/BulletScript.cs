using UnityEngine;

public class BulletScript
    : MonoBehaviour
{
    private void OnCollisionEnter(Collision collision)
    {
        Destroy(this.gameObject);

        if (collision.gameObject.CompareTag("Target"))
        {
            Destroy(collision.gameObject);
        }
    }
}
