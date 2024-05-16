using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Linq;
using System;

public class Spaceship
{
    public bool isPlayer;
    public float defaultAP = 3;
    public float AP;
    public float defaultSpeed = 0;
    public float speed;
    public void ResetAP(bool IsSimulation=false)
    {
        AP = defaultAP;
        if (IsSimulation == false && GameManagerController.Instance != null){ GameManagerController.Instance.gameManagerController.UpdateAPGraphics(this.isPlayer);}
    }
    public void ResetSpeed(bool IsSimulation=false)
    {
        speed = defaultSpeed;
        if (IsSimulation == false && GameManagerController.Instance != null){ GameManagerController.Instance.gameManagerController.UpdateSpeedGraphics(this.isPlayer);}
    }
    public void AdjustAP(float change, bool IsSimulation=false)
    {
        AP += change;
        if (IsSimulation == false && GameManagerController.Instance != null){ GameManagerController.Instance.gameManagerController.UpdateAPGraphics(this.isPlayer);}
    }
    public void AdjustSpeed(float change, bool IsSimulation=false)
    {
        speed += change;
        if (IsSimulation == false && GameManagerController.Instance != null){ GameManagerController.Instance.gameManagerController.UpdateSpeedGraphics(this.isPlayer);}
    }
    
    public void ResetTempRoomStats()
    {
        foreach(Room room in GetRoomList())
        {
            room.defence = 0;
            room.incomingDamage = 0;
            room.UpdateHealthBar();
        }
    }
    public Spaceship(float defaultAP, float defaultSpeed, bool isPlayer)
    {
        this.defaultAP = defaultAP;
        this.defaultSpeed = defaultSpeed;
        this.isPlayer = isPlayer;
    }

    public Dictionary<RoomType, List<Room>> GetRooms()
    {
        if (this.isPlayer){return GameManagerController.Instance.playerRooms;}
        return GameManagerController.Instance.enemyRooms;
    }
    public List<Room> GetRoomList()
    {
        return GetRooms().Values.SelectMany(x => x).ToList();
    }
    public void onDestroy(){
        
    }
    public Spaceship Clone(){
        Spaceship clone = new Spaceship(defaultAP, defaultSpeed, isPlayer);
        clone.AP = AP;
        clone.speed = speed;
        return clone;
    }
}

public class SpaceshipController : MonoBehaviour{
    public Spaceship spaceship;
    void Awake()
    {
        init();
    }
    public void init(){
        if (spaceship is not null) return;
        if (gameObject.tag.Contains("player")){
            this.spaceship = new Spaceship(3, 0, true);
            GameManagerController.Instance.RegisterPlayerShip(this.spaceship);
        }
        if (gameObject.tag.Contains("enemy")){
            this.spaceship = new Spaceship(3, 0, false);
            this.spaceship.ResetTempRoomStats();
            GameManagerController.Instance.RegisterEnemyShip(this.spaceship);
        }
    }
}

