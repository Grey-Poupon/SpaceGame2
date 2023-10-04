using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;
    public LaserShot laserPrefab;
    public enum TurnTypes {Player, Enemy, Resolve}
    public TurnTypes turn = TurnTypes.Player;

    private GameObject playerShips = null;
    private List<GameObject> enemyShips = new List<GameObject>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void RegisterPlayerShip(GameObject playerShip)
    {
        playerShips=playerShip;
    }

    public void RegisterEnemyShip(GameObject enemyShip)
    {
        enemyShips.Add(enemyShip);
    }

    public void RemovePlayerShip(GameObject playerShip)
    {
        playerShips=null;
    }

    public void RemoveEnemyShip(GameObject enemyShip)
    {
        enemyShips.Remove(enemyShip);
    }

    public void FireLaserAtTarget(Vector3 targetPosition, Room target)
    {
        if (playerShips != null)
        {
            // Calculate the direction to the target position.
            Vector3 direction = (targetPosition - playerShips.transform.position).normalized;
            // Instantiate the laser at the spaceship's position.
            LaserShot laser = Instantiate(laserPrefab, playerShips.transform.position, Quaternion.identity);
            
            // Rotate the laser to face the target direction.
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            laser.transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
            laser.StartMoving(targetPosition);

            // Pass the room/target so it can call GM to do damage when it hits
            laser.target = target;
        }
    }

    public void FinishTurn()
    {
        if (turn == TurnTypes.Player)
        {
            turn = TurnTypes.Enemy;
        }
        else if (turn == TurnTypes.Enemy)
        {
            turn = TurnTypes.Resolve;
        }
        else if (turn == TurnTypes.Resolve)
        {
            turn = TurnTypes.Player;
        }
        UnityEngine.Debug.Log("Turn is now:" + turn.ToString());
    }

    public void RegisterAttack(Room RoomHit,string AttackType)
    {
        if (AttackType == "Laser")
        {
            RoomHit.takeDamage(33f);
        }
    }


}
