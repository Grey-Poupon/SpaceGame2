using System.Collections.Specialized;
using System.Security.Cryptography;
using System;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.Linq;
 public enum TurnTypes {Player, Enemy, Resolve}

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;
    public LaserShot laserPrefab;
    public TurnTypes turn = TurnTypes.Enemy;
    public Texture2D customCursor;

    public Card selectedCard;

    public Hand hand;
    public PlayerSpaceship playerShip;
    public EnemySpaceship enemyShip;
    public Dictionary<RoomType, List<Room>> playerRooms = new Dictionary<RoomType, List<Room>>();
    public Dictionary<RoomType, List<Room>> enemyRooms = new Dictionary<RoomType, List<Room>>();

    public List<RoomAction> playerTurnActions = new List<RoomAction>();
    public List<RoomAction> enemyTurnActions = new List<RoomAction>();
    private TextMeshProUGUI playerTurnActionsText;
    private TextMeshProUGUI enemyTurnActionsText;
    
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

    private void Start()
    {
        playerTurnActionsText = GameObject.Find("PlayerActionList").GetComponent<TextMeshProUGUI>();
        enemyTurnActionsText = GameObject.Find("EnemyActionList").GetComponent<TextMeshProUGUI>();
        StartCoroutine(StartGame());

    }


    private IEnumerator StartGame()
    {
        // Wait for 0.5 seconds
        while (playerShip == null && enemyShip == null && hand == null)
        {
            yield return new WaitForSeconds(0.5f);
        }
        yield return new WaitForSeconds(0.5f);

        // Call your non-async function here
        turn = TurnTypes.Enemy;
        FinishTurn();
    }

    public void update()
    {

    }

    public void RegisterPlayerShip(PlayerSpaceship playerShip)
    {
        this.playerShip = playerShip;
    }

    public void RegisterEnemyShip(EnemySpaceship enemyShip)
    {
        this.enemyShip = enemyShip;
    }

    public void RegisterPlayerHand(Hand hand)
    {
        this.hand = hand;
    }

    public void RegisterRoom(Room room, bool isPlayer)
    {
        if (isPlayer)
        {
            if (!playerRooms.ContainsKey(room.roomType))
            {
                playerRooms[room.roomType] = new List<Room>();
            }

            playerRooms[room.roomType].Add(room);
            AddRoomAction(room);
        }
        else
        {
            if (!enemyRooms.ContainsKey(room.roomType))
            {
                enemyRooms[room.roomType] = new List<Room>();
            }

            enemyRooms[room.roomType].Add(room);
        }
    }
    
    public void RemovePlayerShip()
    {
        this.playerShip=null;
    }

    public void RemoveEnemyShip()
    {
        this.enemyShip = null;
    }

    public void FireLaserAtTarget(Vector3 targetPosition, Room target)
    {
        if (playerShip != null)
        {
            // Calculate the direction to the target position.
            Vector3 direction = (targetPosition - playerShip.transform.position).normalized;
            // Instantiate the laser at the spaceship's position.
            LaserShot laser = Instantiate(laserPrefab, playerShip.transform.position, Quaternion.identity);
            
            // Rotate the laser to face the target direction.
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            laser.transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
            laser.StartMoving(targetPosition);

            // Pass the room/target so it can call GM to do damage when it hits
            laser.target = target;
        }
    }

    private IEnumerator InvokeWeaponActionsWithDelay(List<System.Action> actions)
    {
        foreach (var actionCall in actions)
        {
            // Invoke the function
            actionCall.Invoke();

            // Wait for 0.5 seconds
            yield return new WaitForSeconds(0.5f);
        }
    }
    public void FinishTurn()
    {
        if (turn == TurnTypes.Player)
        {
            turn = TurnTypes.Resolve;
        }
        else if (turn == TurnTypes.Enemy)
        {
            enemyChooseActions();
            turn = TurnTypes.Player;
        }
        if (turn == TurnTypes.Resolve)
        {
            ResolveActions();
            turn = TurnTypes.Enemy;
        }
        UnityEngine.Debug.Log("Turn is now:" + turn.ToString());
    }

    public void ResolveActions()
    {

        System.Random random = new System.Random();

        foreach (RoomAction action in enemyTurnActions)
        {
            if (action is EngineAction)
            {
                enemyShip.speed += 1;
            }
        }

        foreach (RoomAction action in playerTurnActions)
        {
            if (action is EngineAction)
            {
                playerShip.speed += 1;
            }
        }
        bool playerFirst = (playerShip.speed > enemyShip.speed) ? true :
                                (enemyShip.speed > playerShip.speed) ? false :
                                   (random.Next(2) == 0) ? true : false;

        if (playerFirst)
        {
            playOutActions(playerTurnActions);
            playOutActions(enemyTurnActions);
        }
        else
        {
            playOutActions(enemyTurnActions);
            playOutActions(playerTurnActions);
        }
        playerShip.resetAP();
        playerShip.resetSpeed();
        enemyShip.resetAP();
        enemyShip.resetSpeed();
        enemyTurnActionsText.text = "";
        playerTurnActionsText.text = "";
        playerTurnActions.Clear();
        enemyTurnActions.Clear();
    }

    public void playOutActions(List<RoomAction> actions)
    {
        List<System.Action> weaponCalls = new List<System.Action>();

        foreach (RoomAction action in actions)
            {
                UnityEngine.Debug.Log(action);
                if (action.sourceRoom.health <= 0 || action.sourceRoom.turnsUntilReady != 0) {continue;}
                
                if (action.affectedStat == Stat.Health)
                {
                    if (action.statAdjustment > 0)
                    {
                        action.affectedRoom.heal(action.statAdjustment);
                    }
                    else
                    {
                        action.affectedRoom.takeDamage(Math.Abs(action.statAdjustment));
                        weaponCalls.Add(() => FireLaserAtTarget(action.affectedRoom.parent.transform.position, action.affectedRoom));
                        // FireLaserAtTarget(action.affectedRoom.parent.transform.position, action.affectedRoom);
                    }
                }
                else if (action.affectedStat == Stat.Defence)
                {
                    if (action.statAdjustment > 0)
                    {
                        action.affectedRoom.increaseDefence(action.statAdjustment);
                    }
                    else
                    {
                        action.affectedRoom.decreaseDefence(Math.Abs(action.statAdjustment));
                    }
                }

                action.activate();
            }
            StartCoroutine(InvokeWeaponActionsWithDelay(weaponCalls));

    }

    public void enemyChooseActions()
    {
        System.Random random = new System.Random();

        // If i have a reactor Action take it
        if (enemyRooms.ContainsKey(RoomType.Reactor))
        {
            foreach (ReactorRoom reactorRoom in enemyRooms[RoomType.Reactor])
            {
                if (reactorRoom.turnsUntilReady == 0 && reactorRoom.health > 0)
                {
                    ReactorAction reactorAction = new ReactorAction();
                    reactorAction.affectedRoom = reactorRoom;
                    reactorAction.sourceRoom = reactorRoom;  
                    PlayAction(reactorAction, false);

                    enemyShip.AP += 1;
                }
            }
        }
        while (enemyShip.AP > 2 && enemyRooms.ContainsKey(RoomType.Laser) && enemyRooms[RoomType.Laser].Any(item => item.health > 0))
        {
            foreach (LaserRoom laserRoom in enemyRooms[RoomType.Laser])
            {        
                if (laserRoom.health > 0)
                {
                    LaserAction laserAction = new LaserAction();
                    Room target = playerRooms[RoomType.Reactor][0];
                    if (playerRooms.ContainsKey(RoomType.Laser))
                    {
                        target = playerRooms[RoomType.Laser].OrderBy(obj => obj.health).First();
                    }
                    laserAction.affectedRoom = target;
                    laserAction.sourceRoom = laserRoom;  
                    PlayAction(laserAction, false);
                    enemyShip.AP -= 1;
                }
            }
        }
        int c = 0;
        while (enemyShip.AP > 0)
        {
            if (random.Next(2) == 0)
            {
                if (enemyRooms.ContainsKey(RoomType.Shield) && enemyRooms.ContainsKey(RoomType.Laser) && enemyRooms[RoomType.Laser].Any(item => item.health > 0) && enemyRooms[RoomType.Shield].Any(item => item.health > 0))
                {
                    ShieldAction shieldAction = new ShieldAction();
                    shieldAction.affectedRoom = enemyRooms[RoomType.Laser].OrderBy(obj => obj.health).First();
                    shieldAction.sourceRoom = enemyRooms[RoomType.Shield][0];  
                    PlayAction(shieldAction, false);
                    enemyShip.AP -= 1;
                }
            }
            else if (enemyRooms.ContainsKey(RoomType.Engine) && enemyRooms[RoomType.Engine].Any(item => item.health > 0))
            {
                    EngineAction engineAction = new EngineAction();
                    engineAction.affectedRoom = enemyRooms[RoomType.Engine][0];
                    engineAction.sourceRoom = enemyRooms[RoomType.Engine][0];
                    PlayAction(engineAction, false);
                    enemyShip.AP -= 1;
            }
            else
            {
                c += 1;
                if (c > 10)
                {
                enemyShip.AP = 0;
                }
            }

        }
        enemyShip.resetAP();
    }

    public void RegisterAttackComplete(Room RoomHit,string AttackType)
    {
        if (AttackType == "Laser")
        {
            RoomHit.updateHealthGraphics();
        }
    }

    public void Action1()
    {
        if (turn == TurnTypes.Player)
        {
        }
    }

    public void AddRoomAction(Room room)
    {
        if (hand == null)
        {
            UnityEngine.Debug.Log("Null hand");
            return;

        }
        hand.AddCard(
            room.roomType.ToString(),
            room.cooldown.ToString(),
            room.cost.ToString(),
            room.description.ToString(),
            room.roomType,
            room.targetSelf,
            room
        );

    }

    public void PlayCard(Card card)
    {
        Cursor.SetCursor(customCursor, new Vector2(customCursor.width / 2, customCursor.height / 2), CursorMode.ForceSoftware);
        selectedCard = card;
        card.gameObject.SetActive(false);
        
    }

    public void ReleaseCard()
    {
        if (selectedCard != null)
        {
            selectedCard.gameObject.SetActive(true);
            selectedCard = null;
            Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
        }
    }

    public void RoomClicked(Vector3 targetPosition, Room target)
    {

        if (selectedCard.roomType == RoomType.Laser)
        {
            //FireLaserAtTarget(targetPosition, target);
            LaserAction laserAction = new LaserAction();
            laserAction.affectedRoom = target;
            laserAction.sourceRoom = selectedCard.sourceRoom;
            PlayAction(laserAction, true);
            
            
            selectedCard.gameObject.SetActive(true);
            selectedCard = null;
            Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
        }
        else if (selectedCard.roomType == RoomType.Missile)
        {
            MissileAction missileAction = new MissileAction();
            missileAction.affectedRoom = target;
            missileAction.sourceRoom = selectedCard.sourceRoom;
            PlayAction(missileAction, true);
            
            selectedCard.gameObject.SetActive(true);
            selectedCard = null;
            Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
        }
        else if (selectedCard.roomType == RoomType.CargoHold)
        {
            CargoHoldAction cargoHoldAction = new CargoHoldAction();
            cargoHoldAction.affectedRoom = target;
            cargoHoldAction.sourceRoom = selectedCard.sourceRoom;
            PlayAction(cargoHoldAction, true);
            
            selectedCard.gameObject.SetActive(true);
            selectedCard = null;
            Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
        }
        else if (selectedCard.roomType == RoomType.Shield)
        {
            ShieldAction shieldAction = new ShieldAction();
            shieldAction.affectedRoom = target;
            shieldAction.sourceRoom = selectedCard.sourceRoom;
            PlayAction(shieldAction, true);
            
            selectedCard.gameObject.SetActive(true);
            selectedCard = null;
            Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
        }
        else if (selectedCard.roomType == RoomType.Engine)
        {
            EngineAction engineAction = new EngineAction();
            engineAction.affectedRoom = target;
            engineAction.sourceRoom = selectedCard.sourceRoom;
            PlayAction(engineAction, true);
            
            selectedCard.gameObject.SetActive(true);
            selectedCard = null;
            Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
        }
        else if (selectedCard.roomType == RoomType.Reactor)
        {
            ReactorAction reactorAction = new ReactorAction();
            reactorAction.affectedRoom = target;
            reactorAction.sourceRoom = selectedCard.sourceRoom;
            PlayAction(reactorAction, true);
            
            selectedCard.gameObject.SetActive(true);
            selectedCard = null;
            Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
        }       
    }


    public void PlayAction(RoomAction action, bool isPlayer)
    {
        if (isPlayer)
        { 
            if (action.affectedStat == Stat.AP && action.affectsSelf) {playerShip.AP += action.statAdjustment;}
            playerShip.AP -= action.sourceRoom.cost;

            playerTurnActions.Add(action);
            if (playerTurnActionsText.text != "")
            {
                playerTurnActionsText.text += '\n';
            }
            playerTurnActionsText.text += action.roomName;
            playerTurnActionsText.text += " -> ";
            playerTurnActionsText.text += action.affectedRoom.roomType.ToString();
        }
        else
        {
            enemyTurnActions.Add(action);
            if (enemyTurnActionsText.text != "")
            {
                enemyTurnActionsText.text += '\n';
            }
            enemyTurnActionsText.text += action.roomName;
            enemyTurnActionsText.text += " -> ";
            enemyTurnActionsText.text += action.affectedRoom.roomType.ToString();
        }
    }
}


