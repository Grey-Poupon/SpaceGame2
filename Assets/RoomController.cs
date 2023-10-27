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
        isPlayer = transform.parent.GetComponent<MonoBehaviour>() is PlayerSpaceship;

        if (room == null){room = createRoom(roomType);}
        GameManager.Instance.RegisterRoom(room, isPlayer);
        room.spriteRenderer = GetComponent<SpriteRenderer>();

        Transform canvas = transform.Find("Canvas");

        room.Title  = canvas.Find("Title").GetComponent<TextMeshProUGUI>();
        room.Attack = canvas.Find("Attack").GetComponent<TextMeshProUGUI>();
        room.Shield = canvas.Find("Shield").GetComponent<TextMeshProUGUI>();
        room.Health = canvas.Find("Health").GetComponent<TextMeshProUGUI>();

        room.UpdateTextGraphics();
        room.parent = this;
        room.isPlayer = isPlayer;
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
            //UnityEngine.Debug.Log("If its you turn: Play a card to do an Action, else hit enter");
        }
    }


    Room createRoom(RoomType roomType)
    {
        /*List<CardAction> weaponActions  = new List<CardAction> { new LaserAction(),         new MissileAction(),          new FirebombAction(),         new ShieldPiercerAction()       };
        List<CardAction> shieldActions  = new List<CardAction> { new FocusedShieldAction(), new GeneralShieldAction(),    new BigBoyShieldAction(),     new SemiPermanentShieldAction() };
        List<CardAction> engineActions  = new List<CardAction> { new SpeedUpAction(),       new BigBoySpeedUpAction(),    new EvasiveManeouvreAction(), new OverHeatAction()            };
        List<CardAction> reactorActions = new List<CardAction> { new OverdriveAction(),     new BuffEnergyWeaponAction(), new ChargeBatteriesAction(),  new EMPAction()                 };*/


        List<CardAction> weaponActions = new List<CardAction> { new LaserAction(), new MissileAction() };
        List<CardAction> shieldActions = new List<CardAction> { new FocusedShieldAction(), new GeneralShieldAction() };
        List<CardAction> engineActions = new List<CardAction> { new SpeedUpAction(), new BigBoySpeedUpAction() };
        List<CardAction> reactorActions = new List<CardAction> { new OverdriveAction(), new BuffEnergyWeaponAction() };

        if      (roomType == RoomType.Weapons) { return new WeaponsRoom(weaponActions);  }
        else if (roomType == RoomType.Shield)  { return new ShieldRoom(shieldActions);   }
        else if (roomType == RoomType.Engine)  { return new EngineRoom(engineActions);   }
        else if (roomType == RoomType.Reactor) { return new ReactorRoom(reactorActions); }
        else                                   { return new ReactorRoom(reactorActions); }
    }   


}

public abstract class Room
{
    public bool isPlayer;
    public RoomController parent;
    public List<CardAction> actions;
    public RoomType roomType;
    protected float maxHealth;
    public float health;
    public float defence = 0;
    public float incomingDamage = 0;
    public bool disabled = false;
    public bool destroyed = false;

    public SpriteRenderer spriteRenderer;
    public TextMeshProUGUI Health;
    public TextMeshProUGUI Title;
    public TextMeshProUGUI Attack;
    public TextMeshProUGUI Shield;
    public List<CombatEffect> effectsApplied = new List<CombatEffect>();

    public SpaceShip getParentShip(bool invert = false)
    {
        if (parent.isPlayer != invert){return GameManager.Instance.playerShip;}
        return GameManager.Instance.enemyShip;
    }

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

    public void IncreaseDefence(float adjustment)
    {
        defence += adjustment;
        Shield.text = defence.ToString();
    }

    public void DecreaseDefence(float adjustment)
    {
        defence += adjustment;
        Shield.text = defence.ToString();
    }

    public void IncreaseAttackIntent(float damage)
    {
        incomingDamage += damage;
        Attack.text = incomingDamage.ToString();
    }

    public void heal(float healing)
    {
        if (health == maxHealth){return;}

        float newHealth = health + healing > maxHealth ? maxHealth : health + healing;
        
        // Actions are added back to player when room is brought back up
        if (health <= 0 && healing > 0)
        {
            destroyed = false;
            foreach(CardAction action in actions)
            {
                if (action.IsReady()){action.card.cardController.gameObject.SetActive(true);}
            }
        }
        setHealth(newHealth);
        UpdateHealthBar();
    }

    public virtual void onDestroy()
    {
        destroyed = true;
        foreach(CardAction action in actions)
        {
            action.card.cardController.gameObject.SetActive(false);
        }
        //UnityEngine.Debug.Log("Room Destroyed");
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

    protected void AttachRoomToAction()
    {
        foreach(CardAction action in this.actions)
        {
            action.sourceRoom = this;
        }
    }
    public void UpdateTextGraphics()
    {
        Title.text = roomType.ToString();
        Attack.text = incomingDamage.ToString();
        Shield.text = defence.ToString();
        Health.text = getHealth().ToString();
    }
}

public class WeaponsRoom : Room
{
    public WeaponsRoom(List<CardAction> actions)
    {
        this.actions = actions;
        AttachRoomToAction();
        roomType = RoomType.Weapons;
        maxHealth = 5;
        health = maxHealth;
    }
}

public class ShieldRoom : Room
{
    public ShieldRoom(List<CardAction> actions)
    {
        this.actions = actions;
        AttachRoomToAction();
        roomType = RoomType.Shield;
        maxHealth = 3;
        health = maxHealth;
    }
}
public class EngineRoom : Room
{
    public EngineRoom(List<CardAction> actions)
    {
        this.actions = actions;
        AttachRoomToAction();
        roomType = RoomType.Engine;
        maxHealth = 3;
        health = maxHealth;
    }
}
public class ReactorRoom : Room
{

    public ReactorRoom(List<CardAction> actions)
    {
        this.actions = actions;
        AttachRoomToAction();
        roomType = RoomType.Reactor;
        maxHealth = 8;
        health = maxHealth;
    }

    public override void onDestroy()
    {
        base.onDestroy();
        //UnityEngine.Debug.Log("Ship gone");
    }

}


public enum RoomType
{
    Weapons,
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
    AP,
    None
}