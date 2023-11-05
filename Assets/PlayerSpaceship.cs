using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

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
        //GameManager.Instance.RemovePlayerShip();
    }
}

public abstract class SpaceShip: MonoBehaviour
{
    public float defaultAP = 3;
    public float AP;
    public float defaultSpeed = 0;
    public float speed;
    public void ResetAP()
    {
        AP = defaultAP;
        if (GameManager.Instance != null){ GameManager.Instance.UpdateAPGraphics(this is PlayerSpaceship);}
    }
    public void ResetSpeed()
    {
        speed = defaultSpeed;
        if (GameManager.Instance != null){ GameManager.Instance.UpdateSpeedGraphics(this is PlayerSpaceship);}
    }
    public void AdjustAP(float change)
    {
        AP += change;
        if (GameManager.Instance != null){ GameManager.Instance.UpdateAPGraphics(this is PlayerSpaceship);}
    }
    public void AdjustSpeed(float change)
    {
        speed += change;
        if (GameManager.Instance != null){ GameManager.Instance.UpdateSpeedGraphics(this is PlayerSpaceship);}
    }
    
    public void ResetTempRoomStats()
    {
        foreach(Room room in GetRoomList())
        {
            room.defence = 0;
            room.incomingDamage = 0;
        }
    }
    protected SpaceShip()
    {

    }

    public Dictionary<RoomType, List<Room>> GetRooms()
    {
        if (this is PlayerSpaceship){return GameManager.Instance.playerRooms;}
        return GameManager.Instance.enemyRooms;
    }
    public List<Room> GetRoomList()
    {
        return GetRooms().Values.SelectMany(x => x).ToList();
    }
}