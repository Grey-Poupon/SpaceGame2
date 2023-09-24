using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemySpaceshipController : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        GameManager.Instance.RegisterEnemyShip(gameObject);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    private void OnDestroy()
    {
        // Remove this player ship from the GameManager when destroyed.
        GameManager.Instance.RegisterEnemyShip(gameObject);
    }
}