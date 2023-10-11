using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerSpaceship : SpaceShip
{
    public float moveSpeed = 5.0f;

    // Start is called before the first frame update
    void Awake()
    {
        GameManager.Instance.RegisterPlayerShip(this);
    }

    // Update is called once per frame
    private void Update()
    {

    }

    private void OnDestroy()
    {
        // Remove this player ship from the GameManager when destroyed.
        GameManager.Instance.RemovePlayerShip();
    }
}

public abstract class SpaceShip: MonoBehaviour
{
    public float defaultAP = 3;
    public float AP;
    public float defaultSpeed = 0;
    public float speed;
    public void resetAP(){AP = defaultAP;}
    public void resetSpeed(){speed = defaultSpeed;}

    protected SpaceShip()
    {
        resetAP();
        resetSpeed();
    }
}