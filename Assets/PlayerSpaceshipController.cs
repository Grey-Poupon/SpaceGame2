using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerSpaceshipController : MonoBehaviour
{
    public float moveSpeed = 5.0f;

    // Start is called before the first frame update
    void Start()
    {
        GameManager.Instance.RegisterPlayerShip(gameObject);
    }

    // Update is called once per frame
    private void Update()
    {
        // Get player input for movement
        float horizontalInput = Input.GetAxis("Horizontal");
        float verticalInput = Input.GetAxis("Vertical");

        // Calculate movement vector
        Vector2 movement = new Vector2(horizontalInput, verticalInput) * moveSpeed * Time.deltaTime;

        // Apply movement
        transform.Translate(movement);
    }

    private void OnDestroy()
    {
        // Remove this player ship from the GameManager when destroyed.
        GameManager.Instance.RemovePlayerShip(gameObject);
    }
}
