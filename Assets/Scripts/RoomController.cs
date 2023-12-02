using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class RoomController : MonoBehaviour
{
    public static RoomController RoomPrefab;
    public GameObject OnFireIcon;
    private Room room;
    public bool isPlayer;

    
    public RoomType roomType;
    void Start()
    {
        isPlayer = transform.parent.GetComponent<MonoBehaviour>() is PlayerSpaceship;
        OnFireIcon = transform.Find("OnFireIcon").gameObject;
        if (isPlayer) Setup(roomType);
    }
    public void Setup(RoomType roomType)
    {
        if (room == null){room = createRoom(roomType, !isPlayer);}
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

    Room createRoom(RoomType roomType, bool getEveryRoom=false)
    {
        List<CardAction> laserActions            = new List<CardAction>();    
        List<CardAction> shieldActions           = new List<CardAction>();     
        List<CardAction> engineActions           = new List<CardAction>();     
        List<CardAction> reactorActions          = new List<CardAction>();
        List<CardAction> missileActions          = new List<CardAction>();      
        List<CardAction> firebombActions         = new List<CardAction>();       
        List<CardAction> shieldPiercerActions    = new List<CardAction>();            
        List<CardAction> evasiveManeouvreActions = new List<CardAction>();               
        List<CardAction> batteryActions          = new List<CardAction>();      
        List<CardAction> EMPActions              = new List<CardAction>();  

        laserActions           .AddRange(new List<CardAction> { new LaserAction()});
        shieldActions          .AddRange(new List<CardAction> { new FocusedShieldAction()});
        engineActions          .AddRange(new List<CardAction> { new SpeedUpAction()});
        reactorActions         .AddRange(new List<CardAction> { new OverdriveAction()});

        missileActions         .AddRange(new List<CardAction> {new MissileAction()});
        firebombActions        .AddRange(new List<CardAction> {new FirebombAction()});
        shieldPiercerActions   .AddRange(new List<CardAction> {new ShieldPiercerAction()});
        evasiveManeouvreActions.AddRange(new List<CardAction> {new EvasiveManeouvreAction()});
        batteryActions         .AddRange(new List<CardAction> {new ChargeBatteriesAction()});
        EMPActions             .AddRange(new List<CardAction> {new EMPAction()});
        
        if (getEveryRoom)
        {
            laserActions   .AddRange(new List<CardAction> {new BuffEnergyWeaponAction()});
            shieldActions  .AddRange(new List<CardAction> {new GeneralShieldAction(), new BigBoyShieldAction(), new SemiPermanentShieldAction()});
            engineActions  .AddRange(new List<CardAction> {new BigBoySpeedUpAction(), new OverHeatAction()});
        }
        
        if      (roomType == RoomType.Laser)            {return new Room(laserActions,            roomType, 5);}
        else if (roomType == RoomType.Shield)           {return new Room(shieldActions,           roomType, 3);}
        else if (roomType == RoomType.Engine)           {return new Room(engineActions,           roomType, 5);}
        else if (roomType == RoomType.Reactor)          {return new Room(reactorActions,          roomType, 8);}
        else if (roomType == RoomType.Missile)          {return new Room(missileActions,          roomType, 3);}
        else if (roomType == RoomType.Firebomb)         {return new Room(firebombActions,         roomType, 3);}
        else if (roomType == RoomType.ShieldPiercer)    {return new Room(shieldPiercerActions,    roomType, 3);}
        else if (roomType == RoomType.EvasiveManeouvre) {return new Room(evasiveManeouvreActions, roomType, 3);}
        else if (roomType == RoomType.Battery)          {return new Room(batteryActions,          roomType, 3);}
        else if (roomType == RoomType.EMP)              {return new Room(EMPActions,              roomType, 3);}
        else return new Room(new List<CardAction>(), RoomType.Reactor,  0);
    }
}

public class Room
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



    public Room Clone(){
        List<CardAction> actions = new List<CardAction>();
        foreach(CardAction action in this.actions){
            actions.Add(action.Clone());
        }
        Room c_room = new Room(actions,roomType,maxHealth);
        c_room.defence = this.defence;
        c_room.incomingDamage = this.incomingDamage;
        c_room.disabled = this.disabled;
        c_room.destroyed = this.destroyed;
        c_room.health = this.health;
        List<CombatEffect> c_effectsApplied = new List<CombatEffect>();
        foreach(CombatEffect effect in effectsApplied){
            c_effectsApplied.Add(effect.Clone());
        }
        c_room.effectsApplied = c_effectsApplied;
        return c_room;
    }

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
        if(Health!=null){
            Health.text = newHealth.ToString();
        }
    }
    public void takeDamage(float damage)
    {
        if (defence > 0)
        {
            for(int i = effectsApplied.Count-1; i >= 0; i--)
            {
                if (effectsApplied[i] is ShieldEffect)
                {
                    ShieldEffect effect = (ShieldEffect) effectsApplied[i];
                    defence -= effect.increase;
                    damage -= effect.increase;
                    effectsApplied.RemoveAt(i);
                }
                if (defence == 0 || damage == 0){break;}
            }
        }

        health = health < damage ? 0 : health - damage;
    }
    public void updateHealthGraphics()
    {
        if(Health==null){
            return;
        }
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
        
        
    }
    public void DecreaseDefence(float adjustment)
    {
        defence += adjustment;
        if(Shield!=null){
            Shield.text = defence.ToString();
        }
        
    }
    public void IncreaseAttackIntent(float damage)
    {
        incomingDamage += damage;
        if(Attack!=null){
            Attack.text = incomingDamage.ToString();    
        }
        
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

        if (isPlayer){
            foreach(CardAction action in actions)
            {
                action.card.cardController.gameObject.SetActive(false);
            }
        }        //UnityEngine.Debug.Log("Room Destroyed");

    }
    public void SetOnFireIcon(bool active)
    {
        parent.OnFireIcon.SetActive(active);
    }
    public void UpdateHealthBar()
    {
        if(Health==null){
            return;
        }

        if (getHealth() == 0)
        {
            spriteRenderer.color = new Color(0.0f, 0.0f, 0.0f); // Black
            return;
        }
        

        // check if room is on fire, if so make room orange
        foreach (CombatEffect ce in effectsApplied){
            if(ce is OnFireEffect){
                spriteRenderer.color = new Color(1f, 0.9f, 0.0f); // Orange
                return;
            }
        }
        // Increase the red component of the room
        Color targetColour = new Color(1.0f, 0.0f, 0.0f); // Red
        Color defaultColour = new Color(1.0f,1.0f,1.0f);//White
        Color currentColour = spriteRenderer.color;
        Color colourDifference = targetColour - defaultColour;

        float redComponentIncrease = 1f - (getHealth() / getMaxHealth());

        spriteRenderer.color = defaultColour + (colourDifference * redComponentIncrease);
        
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
        if(Title==null){
            return;
        }
        Title.text = roomType.ToString();
        Attack.text = incomingDamage.ToString();
        Shield.text = defence.ToString();
        Health.text = health.ToString();
    }
    public Room(List<CardAction> actions, RoomType type, float maxHealth)
    {
        this.actions = actions;
        AttachRoomToAction();
        this.roomType = type;
        this.maxHealth = maxHealth;
        this.health = maxHealth;
    }

}

public enum RoomType
{
    Laser,
    Missile,
    Firebomb,
    ShieldPiercer,
    Shield,
    Engine,
    EvasiveManeouvre,
    Battery,
    EMP,
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