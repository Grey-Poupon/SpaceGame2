using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LaserShot : MonoBehaviour
{
    public float speed = 11f; // Adjust the speed as needed.
    private Vector3 targetPosition; // The position the laser should move towards.
    public GameObject explosion;
    private float start;
    public Room target;
    private bool isMoving = false; // Flag to track if the laser is moving.

    private void Start()
    {
        start = Time.fixedTime;
    }

    public void StartMoving(Vector3 target_Position)
    {
        targetPosition = target_Position;
        this.isMoving = true;
        // Calculate the direction from the laser's current position to the target position.
        Vector3 moveDirection = (targetPosition - gameObject.transform.position).normalized;
        
        // Set the laser's velocity to move it towards the target position.
        GetComponent<Rigidbody2D>().velocity = moveDirection * speed;
    }
 
    private void Update()
    {
        // Check if the laser has reached the target position.
        if (isMoving)
        {
            float distance = Mathf.Sqrt(
            Mathf.Pow(gameObject.transform.position.x - targetPosition.x, 2f) +
            Mathf.Pow(gameObject.transform.position.y - targetPosition.y, 2f));
 
            if (distance < 0.1f)
            {
                // If the laser is close to the target position, stop moving.
                isMoving = false;

                // Modify the z position
                Vector3 newPosition = gameObject.transform.position;
                newPosition.z = -1f; 

                // Trigger the animation when the laser arrives.
                Instantiate(explosion, newPosition, Quaternion.identity);

                // Tell the game manager you hit
                GameManager.Instance.RegisterAttackComplete(target, "Laser");
                
                // Destroy the laser
                Destroy(gameObject);
            }
        }
    }
}
