using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System;
using UnityEngine.UI;
public class RoomController : MonoBehaviour
{
    public static RoomController RoomPrefab;
    public GameObject OnFireIcon;
    private Room room;
    public bool isPlayer;
    public GameObject healthBar;
    public GameObject shieldBar;
    public Renderer healthBarRenderer;
    public Renderer shieldBarRenderer;
    public RoomType roomType;
    void Start()
    {
        isPlayer = ((SpaceshipController) transform.parent.GetComponent<MonoBehaviour>()).spaceship.isPlayer;
        Setup(roomType);
    }
    public void Setup(RoomType roomType)
    {
        if (room == null){room = createRoom(roomType, !isPlayer);}
        GameManagerController.Instance.RegisterRoom(room, isPlayer);

        room.spriteRenderer = GetComponent<SpriteRenderer>();
        healthBarRenderer = healthBar.GetComponent<Renderer>();
        shieldBarRenderer = shieldBar.GetComponent<Renderer>();

        // Create a new material instance for this GameObject
        healthBarRenderer.material = new Material(healthBarRenderer.material);
        shieldBarRenderer.material = new Material(shieldBarRenderer.material);

        Transform canvas = transform.Find("Canvas");

        room.parent = this;
        room.isPlayer = isPlayer;

        if (room.Title == null && canvas != null){
            room.Title  = canvas.Find("Title").GetComponent<TextMeshProUGUI>();
            room.Attack = canvas.Find("Attack").GetComponent<TextMeshProUGUI>();
            room.Shield = canvas.Find("Shield").GetComponent<TextMeshProUGUI>();
            room.Health = canvas.Find("Health").GetComponent<TextMeshProUGUI>();
            room.UpdateTextGraphics();
        }
    }
    void Update()
    {
        // Update the alpha based on the direction of fading
    }

    void SetAlpha(float alpha)
    {
    }
    void OnMouseDown()
    {
        if (GameManagerController.Instance.selectedCard != null)
        {
            GameManagerController.Instance.RoomClicked(Camera.main.ScreenToWorldPoint(Input.mousePosition), room);
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
        
        // if (getEveryRoom)
        // {
        //     laserActions   .AddRange(new List<CardAction> {new BuffEnergyWeaponAction()});
        //     shieldActions  .AddRange(new List<CardAction> {new GeneralShieldAction(), new BigBoyShieldAction(), new SemiPermanentShieldAction()});
        //     engineActions  .AddRange(new List<CardAction> {new BigBoySpeedUpAction(), new OverHeatAction()});
        // }
        
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
    public float maxHealth;
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

    public string ID;

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
            CombatEffect clonedEffect = effect.Clone();
            CardAction clonedAction = clonedEffect.action.Clone();
            clonedEffect.action = clonedAction;
            clonedAction.affectedRoom = c_room;
            c_effectsApplied.Add(clonedEffect);
        }
        c_room.effectsApplied = c_effectsApplied;
        return c_room;
    }

    public Spaceship getParentShip(bool invert = false)
    {
        if (actions[0].state == null)
        {
            if (parent.isPlayer != invert){return GameManagerController.Instance.playerShip;}
            return GameManagerController.Instance.enemyShip;
        } else {
            if (parent.isPlayer != invert){return actions[0].state.playerShip;}
            return actions[0].state.enemyShip;
        }
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
        UpdateTextGraphics();
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
        UpdateTextGraphics();
        if (health==0)
        {
            onDestroy();
        }
        UpdateHealthBar();
    }
    public void IncreaseDefence(float adjustment)
    {
        defence += adjustment;
        UpdateTextGraphics();
        
    }
    public void DecreaseDefence(float adjustment)
    {
        defence += adjustment;
        UpdateTextGraphics();
        
    }
    public void IncreaseAttackIntent(float damage)
    {
        incomingDamage += damage;
        UpdateTextGraphics();
        parent.shieldBarRenderer.material.SetFloat("_FlashingSegments", Math.Min(this.incomingDamage, this.defence) );
        parent.healthBarRenderer.material.SetFloat("_FlashingSegments", Math.Max(this.incomingDamage - this.defence, 0));

        
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
                if (action.IsReady()){action.card.cardController.ToggleTransparency(false);}
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
                action.card.cardController.ToggleTransparency(true);
            }
        }        //UnityEngine.Debug.Log("Room Destroyed");

    }
    public void SetOnFireIcon(bool active)
    {
        if (parent != null){
            if (parent.OnFireIcon == null){
                parent.OnFireIcon = parent.transform.Find("OnFireIcon").gameObject;
            }
            parent.OnFireIcon.SetActive(active);
        }
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
        if(Title!=null){
            Title.text = roomType.ToString();
        }
        parent.healthBarRenderer.material.SetFloat("_NumberOfSegments", this.getMaxHealth());
        parent.healthBarRenderer.material.SetFloat("_FlashingSegments", Math.Max(this.incomingDamage - this.defence, 0));
        parent.healthBarRenderer.material.SetFloat("_FlashingOffset", this.defence);
        parent.healthBarRenderer.material.SetFloat("_RemovedSegments", this.getMaxHealth() - this.getHealth());

        parent.shieldBarRenderer.material.SetFloat("_NumberOfSegments", this.getMaxHealth());
        parent.shieldBarRenderer.material.SetFloat("_FlashingSegments", Math.Min(this.incomingDamage, this.defence) );
        parent.shieldBarRenderer.material.SetFloat("_RemovedSegments", this.getMaxHealth() - this.defence);

    }
    public Room(List<CardAction> actions, RoomType type, float maxHealth)
    {
        this.actions = actions;
        AttachRoomToAction();
        this.roomType = type;
        this.maxHealth = maxHealth;
        this.health = maxHealth;
        Guid newGuid = Guid.NewGuid();
        this.ID = type.ToString() + "_" + Convert.ToBase64String(newGuid.ToByteArray());
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