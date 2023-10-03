using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RoomController : MonoBehaviour
{


    public enum RoomType
    {
        Weapons,
        Sheilds,
        Engines,
        Reactor
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
			if (health==0)
			{
                onDestroy();
			}
        }

        public virtual void onDestroy()
		{
            UnityEngine.Debug.Log("Room Gone");
		}

        public void ListActions()
		{
            foreach(RoomAction ra in roomActions)
			{
                UnityEngine.Debug.Log(ra.getInfo());
            }
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







    
    private Room room;
    private SpriteRenderer spriteRenderer;

    // Start is called before the first frame update
    void Start()
    {
        room = new ReactorRoom();
        UnityEngine.Debug.Log(room.roomType);
        room.ListActions();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    void OnMouseDown()
    {
        GameManager.Instance.FireLaserAtTarget(Camera.main.ScreenToWorldPoint(Input.mousePosition));

        room.takeDamage(33f);
        UpdateHealthBar();
    }
    void UpdateHealthBar()
	{
        // Increase the red component of the color
        float redness = 1 - (room.getHealth() / room.getMaxHealth());
        Color currentColor = spriteRenderer.color;
        currentColor.r = currentColor.r + ((1 - currentColor.r) * redness);
        spriteRenderer.color = currentColor;
    }

    
}