using UnityEngine;

public class EnemyPossession : MonoBehaviour, IPossessable
{
    private bool isPossessed = false;
    private Vector3 inputDirection;
    [SerializeField] private float speed = 5f;

    // References to turn off when possessed
    private MonoBehaviour enemyAIBehaviorScript;

    void Start() => enemyAIBehaviorScript = GetComponent<EnemyAI>(); // Your patrol/chase logic

    public void OnPossess()
    {
        isPossessed = true;
        if (enemyAIBehaviorScript != null) enemyAIBehaviorScript.enabled = false; // Stop regular AI
    }

    public void OnUnpossess()
    {
        isPossessed = false;
        if (enemyAIBehaviorScript != null) enemyAIBehaviorScript.enabled = true; // Return to regular AI
    }

    public void ReceiveInput(float horizontal, float vertical, bool actionPressed)
    {
        inputDirection = new Vector3(horizontal, 0, vertical).normalized;
    }

    void Update()
    {
        if (isPossessed)
        {
            // Move using the injected player input
            transform.Translate(inputDirection * speed * Time.deltaTime, Space.World);
        }
    }
}
