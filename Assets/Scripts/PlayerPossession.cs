using UnityEditor;
using UnityEngine;

public class PlayerPossession : MonoBehaviour
{
    public GameObject playerGhostForm;
    private IPossessable currentBody;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // Start by possessing the default player body
        currentBody = playerGhostForm.GetComponent<IPossessable>();
        currentBody.OnPossess();
    }

    // Update is called once per frame
    void Update()
    {
        // Pass down standard inputs to whoever the player controls
        float h = Input.GetAxis("xaxis");
        float v = Input.GetAxis("yaxis");
        bool action = Input.GetButtonDown("Jump");

        currentBody?.ReceiveInput(h, v, action);

        //Looks for new target
        if (Input.GetKeyDown(KeyCode.E))
        {
            TryPossessEnemy();
        }
    }

    void TryPossessEnemy()
    {
        // Fire a raycast forward from the camera to find the enemy
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, 20))
        {
            IPossessable targetEnemy = hit.collider.GetComponent<IPossessable>();

            if (targetEnemy != null)
            {
                // Unpossess the current body
                currentBody.OnUnpossess();

                // If leaving an enemy, you might need to spawn a player ghost

                // Possess the new body
                currentBody = targetEnemy;
                currentBody.OnPossess();

                // Update camera target here
                // vCam.Follow = hit.transform;
            }
        }
    }
}
