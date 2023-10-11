using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class RoomController : MonoBehaviour
{
    private Room room;
    public bool isPlayer;

    public RoomType roomType;
    // Start is called before the first frame update
    void Start()
    {
        isPlayer = transform.parent.GetComponent<MonoBehaviour>().GetType() == typeof(PlayerSpaceship);

        room = createRoom(roomType);
        GameManager.Instance.RegisterRoom(room, isPlayer);
        room.spriteRenderer = GetComponent<SpriteRenderer>();

        Transform canvas = transform.Find("Canvas");

        room.Title  = canvas.Find("Title").GetComponent<TextMeshProUGUI>();
        room.Attack = canvas.Find("Attack").GetComponent<TextMeshProUGUI>();
        room.Shield = canvas.Find("Shield").GetComponent<TextMeshProUGUI>();
        room.Health = canvas.Find("Health").GetComponent<TextMeshProUGUI>();

        room.Title.text = roomType.ToString();
        room.Attack.text = "0";
        room.Shield.text = "0";
        room.Health.text = room.getMaxHealth().ToString();
        room.parent = this;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    void OnMouseDown()
    {
        if (GameManager.Instance.selectedCard != null)
        {
            GameManager.Instance.RoomClicked(Camera.main.ScreenToWorldPoint(Input.mousePosition), room);
        }
        else
        {
            UnityEngine.Debug.Log("If its you turn: Play a card to do an Action, else hit enter");
        }
    }

    Room createRoom(RoomType roomType)
    {
        if (roomType == RoomType.Laser)
        {
            return new LaserRoom();
        }
        else if (roomType == RoomType.Missile)
        {
            return new MissileRoom();
        }
        else if (roomType == RoomType.CargoHold)
        {
            return new CargoHoldRoom();
        }
        else if (roomType == RoomType.Shield)
        {
            return new ShieldRoom();
        }
        else if (roomType == RoomType.Engine)
        {
            return new EngineRoom();
        }
        else if (roomType == RoomType.Reactor)
        {
            return new ReactorRoom();
        }
        else
        {
            return new ReactorRoom();
        }
    }         

}

public abstract class Room
{
    public RoomController parent;
    public List<RoomAction> roomActions;
    public RoomType roomType;
    protected float maxHealth;
    public float health;
    public float cooldown;
    public float defence = 0;
    public float turnsUntilReady = 0;
    public float cost;
    public string description;
    public bool targetSelf;
    public SpriteRenderer spriteRenderer;
    public TextMeshProUGUI Health;
    public TextMeshProUGUI Title;
    public TextMeshProUGUI Attack;
    public TextMeshProUGUI Shield;

    public float getHealth()
    {
        return health;
    }
    public float getMaxHealth()
    {
        return maxHealth;
    }
 
    public void setHealth(float newHealth)
    {
        health = newHealth;
        Health.text = newHealth.ToString();
    }
    public void takeDamage(float damage)
    {
        if (defence >= damage)
        {
            defence -= damage;
            return;
        }

        damage = damage - defence;
        health = health < damage ? 0 : health - damage;
    }
    public void updateHealthGraphics()
    {
        Health.text = health.ToString();
        if (health==0)
        {
            onDestroy();
        }
        UpdateHealthBar();
    }

    public void increaseDefence(float adjustment)
    {
        defence += adjustment;
        Shield.text = defence.ToString();
    }

    public void decreaseDefence(float adjustment)
    {
        defence += adjustment;
        Shield.text = defence.ToString();
    }

    public void heal(float healing)
    {
        if (health == maxHealth){return;}

        float newHealth = health + healing > maxHealth ? maxHealth : health + healing;
        setHealth(newHealth);
        UpdateHealthBar();
    }

    public virtual void onDestroy()
    {
        UnityEngine.Debug.Log("Room Destroyed");
    }

    void UpdateHealthBar()
    {
        if (getHealth() == 0)
        {
            spriteRenderer.color = new Color(0.0f, 0.0f, 0.0f); // Black
            return;
        }
        // Increase the red component of the room
        Color targetColour = new Color(1.0f, 0.0f, 0.0f); // Red
        Color currentColour = spriteRenderer.color;
        Color colourDifference = targetColour - currentColour;

        float redComponentIncrease = 1f - (getHealth() / getMaxHealth());

        spriteRenderer.color = currentColour + (colourDifference * redComponentIncrease);
        
    }
}

public class LaserRoom : Room
{
    public LaserRoom()
    {
        roomType = RoomType.Laser;
        maxHealth = 5;
        cooldown = 0;
        cost = 1;
        description = "Deal 1 Damage";
        health = maxHealth;
        targetSelf = false;
    }
}
public class MissileRoom : Room
{
    public MissileRoom()
    {
        roomType = RoomType.Missile;
        maxHealth = 2;
        cooldown = 2;
        cost = 1;
        description = "Deal 1 Damage";
        health = maxHealth;
        targetSelf = false;
    }
}
public class CargoHoldRoom : Room
{
    public CargoHoldRoom()
    {
        roomType = RoomType.CargoHold;
        maxHealth = 3;
        cooldown = 1;
        cost = 1;
        description = "Heal 1 HP";
        health = maxHealth;
        targetSelf = true;
    }
}
public class ShieldRoom : Room
{
    public ShieldRoom()
    {
        roomType = RoomType.Shield;
        maxHealth = 3;
        cooldown = 0;
        cost = 1;
        description = " + 1 Defence";
        health = maxHealth;
        targetSelf = true;
    }
}
public class EngineRoom : Room
{
    public EngineRoom()
    {
        roomType = RoomType.Engine;
        maxHealth = 3;
        cooldown = 0;
        cost = 1;
        description = "+ 1 Speed";
        health = maxHealth;
        targetSelf = true;
    }
}
public class ReactorRoom : Room
{

    public ReactorRoom()
    {
        roomType = RoomType.Reactor;
        maxHealth = 8;
        cooldown = 3;
        cost = 0;
        description = "+ 1 AP";
        health = maxHealth;
        targetSelf = true;
    }

    public override void onDestroy()
    {
        base.onDestroy();
        UnityEngine.Debug.Log("Ship gone");
    }

}


// --- Room Action ---


public abstract class RoomAction { 


    public string roomName;
    public Room affectedRoom;
    public Stat affectedStat;
    public float statAdjustment;
    public bool affectsSelf;
    public Room sourceRoom;

    public void activate()
    {
        affectedRoom.turnsUntilReady = affectedRoom.cooldown;
    }
}

public class LaserAction : RoomAction
{
    public LaserAction()
    {
        affectedStat = Stat.Health;
        statAdjustment = -1;
        affectsSelf = false;
        roomName = "Laser";
    }
}

public class ShieldAction : RoomAction
{
    public ShieldAction()
    {
        affectedStat = Stat.Defence;
        statAdjustment = 1;
        affectsSelf = true;
        roomName = "Shield";
    }
}

public class ReactorAction : RoomAction
{
    public ReactorAction()
    {
        affectedStat = Stat.AP;
        statAdjustment = 1;
        affectsSelf = true;
        roomName = "Reactor";
    }
}

public class EngineAction : RoomAction
{
    public EngineAction()
    {
        affectedStat = Stat.Speed;
        statAdjustment = 1;
        affectsSelf = true;
        roomName = "Engine";
    }
}

public class CargoHoldAction : RoomAction
{
    public CargoHoldAction()
    {
        affectedStat = Stat.Health;
        statAdjustment = 1;
        affectsSelf = true;
        roomName = "CargoHold";
    }
}

public class MissileAction : RoomAction
{
    public MissileAction()
    {
        affectedStat = Stat.Health;
        statAdjustment = -2;
        affectsSelf = false;
        roomName = "Missile";
    }
}

public enum RoomType
{
    Laser,
    Missile,
    CargoHold,
    Shield,
    Engine,
    Reactor
}

public enum Stat
{
    Attack,
    Defence,
    Health,
    Speed,
    AP
}