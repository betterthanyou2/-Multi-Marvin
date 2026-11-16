
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerDash : MonoBehaviour
{
    public float dashForce = 15f;
    public float doubleTapTime = 0.3f;
    public float dashCooldown = 1f;

    private Rigidbody rb;

    private float lastPressTime = -1f;
    private float nextDashTime = 0f;

    void Start()
    {
        rb = GetComponent<Rigidbody>();

        if (rb == null)
        {
            Debug.LogError("Player needs a Rigidbody!");
        }
    }

    void Update()
    {
        if (Keyboard.current != null &&
            Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            if (lastPressTime >= 0f &&
                Time.time - lastPressTime <= doubleTapTime)
            {
                if (Time.time >= nextDashTime)
                {
                    Dash();
                }

                lastPressTime = -1f;
            }
            else
            {
                lastPressTime = Time.time;
            }
        }
    }

    void Dash()
    {
        if (rb == null) return;

        Debug.Log("DASH!");

        Vector3 direction = transform.forward;

        rb.AddForce(direction * dashForce, ForceMode.Impulse);

        nextDashTime = Time.time + dashCooldown;
    }
}
