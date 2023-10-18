using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemySpaceship : SpaceShip
{

    void Awake()
    {
        GameManager.Instance.RegisterEnemyShip(this);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    private void OnDestroy()
    {
        // Remove this player ship from the GameManager when destroyed.
        //GameManager.Instance.RemoveEnemyShip();
    }
}