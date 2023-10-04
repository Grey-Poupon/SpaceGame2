using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RoomController : MonoBehaviour
{
    private Room room;

    // Start is called before the first frame update
    void Start()
    {
        room = new ReactorRoom();
        UnityEngine.Debug.Log(room.roomType);
        room.ListActions();
        room.spriteRenderer = GetComponent<SpriteRenderer>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    void OnMouseDown()
    {
        GameManager.Instance.FireLaserAtTarget(Camera.main.ScreenToWorldPoint(Input.mousePosition), room);
    }
}

public abstract class RoomAction { 

    public enum Stat
    {
        Attack,
        Defence
    }

    public Room roomAffected;
    protected Stat statAffected;
    protected float statAdjustment;

    public Stat getStat()
    {
        return statAffected;
    }

    public float getStatAdjustment()
    {
        return statAdjustment;
    }

    public abstract string getInfo();

    public virtual void activate()
    {
        switch (statAffected)
        {
            case Stat.Defence:
                roomAffected.defence += statAdjustment;
                break;
            case Stat.Attack:
                roomAffected.takeDamage(statAdjustment);
                break;
            default:
                break;

        }   
        
    }   

}

public class AttackAction : RoomAction
{
    public AttackAction()
    {
        statAffected = RoomAction.Stat.Attack;
        statAdjustment = 15;
    }
    public override string getInfo()
    {
        return "Attack action dealing " + statAdjustment.ToString() + " damage";
    }
}

public class DefenceAction : RoomAction
{
    public DefenceAction()
    {
        statAffected = RoomAction.Stat.Defence;
        statAdjustment = 2;
    }
    public override string getInfo()
    {
        return "Defence action increasing shield by " + statAdjustment.ToString() + " points";
    }
}

public abstract class Room
{
    public List<RoomAction> roomActions;
    public RoomType roomType;
    private float maxHealth = 100;
    public  float defence = 10;
    private float health = 100;
    public SpriteRenderer spriteRenderer;

    public float getHealth()
    {
        return health;
    }
    public float getMaxHealth()
    {
        return maxHealth;
    }
    public float getDefence()
    {
        return defence;
    }
    
    public void takeDamage(float rawDamage)
    {
        float damage = rawDamage - defence;
        health = health < damage ? 0 : health - damage;
        UnityEngine.Debug.Log("Health: " + health.ToString());
        if (health==0)
        {
            onDestroy();
        }
        UpdateHealthBar();
    }

    public virtual void onDestroy()
    {
        UnityEngine.Debug.Log("Room Destroyed");
    }

    public void ListActions()
    {
        UnityEngine.Debug.Log("Actions available:");
        foreach(RoomAction ra in roomActions)
        {
            UnityEngine.Debug.Log("\t" + ra.getInfo());
        }
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

public class WeaponsRoom : Room
{

    public WeaponsRoom()
    {
        roomType = RoomType.Weapons;
    }

}

public class ReactorRoom : Room
{

    public ReactorRoom()
    {
        roomType = RoomType.Reactor;
        roomActions = new List<RoomAction>();
        roomActions.Add(new AttackAction());
        roomActions.Add(new DefenceAction());
    }

    public override void onDestroy()
    {
        base.onDestroy();
        UnityEngine.Debug.Log("Ship gone");
    }

}

public enum RoomType
{
    Weapons,
    Sheilds,
    Engines,
    Reactor
}
