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
}


