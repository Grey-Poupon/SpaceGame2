using System.Collections.Specialized;
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

    public Hand playerHand;
    public Hand enemyHand;

    public PlayerSpaceship playerShip;
    public EnemySpaceship enemyShip;
    public Dictionary<RoomType, List<Room>> playerRooms = new Dictionary<RoomType, List<Room>>();
    public Dictionary<RoomType, List<Room>> enemyRooms = new Dictionary<RoomType, List<Room>>();

    public List<CardAction> playerTurnActions = new List<CardAction>();
    public List<CardAction> enemyTurnActions = new List<CardAction>();
    private TextMeshProUGUI playerTurnActionsText;
    private TextMeshProUGUI enemyTurnActionsText;

    private TextMeshProUGUI playerSpeedText;
    private TextMeshProUGUI enemySpeedText;
    private TextMeshProUGUI playerAPText;
    private TextMeshProUGUI enemyAPText;
    
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
        this.playerSpeedText = GameObject.Find("PlayerSpeedText").GetComponent<TextMeshProUGUI>();
        this.enemySpeedText = GameObject.Find("EnemySpeedText").GetComponent<TextMeshProUGUI>();
        this.playerAPText = GameObject.Find("PlayerAPText").GetComponent<TextMeshProUGUI>();
        this.enemyAPText = GameObject.Find("EnemyAPText").GetComponent<TextMeshProUGUI>();
        StartCoroutine(StartGame());

    }

    private IEnumerator StartGame()
    {
        // Wait for 0.5 seconds
        while (playerShip == null || enemyShip == null || playerHand == null)
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

    public void RegisterPlayerHand(Hand playerHand)
    {
        this.playerHand = playerHand;
    }
   
    public void RegisterEnemyHand(Hand enemyHand)
    {
        this.enemyHand = enemyHand;
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
            
            List<Card> cards = MakeCards(room.actions);
            AddCardsToHand(cards, isPlayer);
        }
        else
        {
            if (!enemyRooms.ContainsKey(room.roomType))
            {
                enemyRooms[room.roomType] = new List<Room>();
            }

            enemyRooms[room.roomType].Add(room);
            List<Card> cards = MakeCards(room.actions, false);
            AddCardsToHand(cards, isPlayer);
        }
    }
    
    public List<Card> MakeCards(List<CardAction> actions, bool makePrefabs = true)
    {
        List<Card> cards = new List<Card>();
        foreach (CardAction action in actions)
        {
            Card card = new Card(action);
            if (makePrefabs)
            {
                // Instantiate a new card prefab & set its parent as the playerHand
                CardController cardController = Instantiate(playerHand.cardPrefab, playerHand.parent.transform);
                cardController.Setup(card);
                card.Setup(cardController);
            }
            cards.Add(card);

        }
        if (makePrefabs)
        {
            // Ensure the cards are properly positioned within the layout group
            playerHand.Organise();
        }        
        return cards;
    }
    
    public void AddCardsToHand(List<Card> cards, bool isPlayer)
    {
        foreach (Card card in cards)
        {
            if (isPlayer){ playerHand.AddCard(card); }
            else         { enemyHand.AddCard(card);     }
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
        if (turn == TurnTypes.Resolve)
        {
            ResolveActions();
            ResetTempStats();
            ResetTurnActions();
            turn = TurnTypes.Enemy;
        }
        if (turn == TurnTypes.Enemy)
        {
            EnemyChooseActions();
            
            ShowPotentialPassiveEffects();
            UpdateHandStats();
            UpdateUIText();
            UpdateRoomText();
            turn = TurnTypes.Player;
        }
    }

    public void ResolveActions()
    {


        // Decide Who goes first
        System.Random random = new System.Random();
        // foreach (CardAction action in enemyTurnActions)
        // {
        //     foreach (SpeedEffect effect in action.GetEffectsByType(typeof(SpeedEffect)))
        //     {
        //         effect.TriggerEffect();
        //     }
        // }

        // foreach (CardAction action in playerTurnActions)
        // {
        //     foreach (SpeedEffect effect in action.GetEffectsByType(typeof(SpeedEffect)))
        //     {
        //         effect.TriggerEffect();
        //     }
        // }
        bool playerFirst = (playerShip.speed > enemyShip.speed) ? true :
                                (enemyShip.speed > playerShip.speed) ? false :
                                   (random.Next(2) == 0) ? true : false;

        // Activate Each Action
        if (playerFirst)
        {
            UnityEngine.Debug.Log(" -- Player -- ");
            PlayOutActions(playerTurnActions, playerRooms);
            UnityEngine.Debug.Log(" -- Enemy  -- ");
            PlayOutActions(enemyTurnActions, enemyRooms);
        }
        else
        {
            UnityEngine.Debug.Log(" -- Enemy  -- ");
            PlayOutActions(enemyTurnActions, enemyRooms);
            UnityEngine.Debug.Log(" -- Player -- ");
            PlayOutActions(playerTurnActions, playerRooms);
        }
    }

    public void PlayOutActions(List<CardAction> actions, Dictionary<RoomType, List<Room>> rooms)
    {
        List<System.Action> weaponCalls = new List<System.Action>();
        UnityEngine.Debug.Log("Activate old effects");
        // Trigger any effects that are still affecting the affected
        List<Room> allRooms = rooms.Values.SelectMany(x => x).ToList();
        foreach (Room room in allRooms)
        {
            // Have to be careful here as effects will remove themselves from the rooms 
            int i = 0;
            while (i < room.effectsApplied.Count)
            {
                int currentCount = room.effectsApplied.Count;
                room.effectsApplied[i].Activate();

                if(room.effectsApplied.Count == currentCount)
                {
                    i++;
                }
            }
            foreach(CombatEffect effect in room.effectsApplied)
            {
                effect.Activate();
            }
        }
        UnityEngine.Debug.Log("Activate new actions");
        // Activate actions, which will apply and trigger some more effects
        foreach (CardAction action in actions)
        {
            UnityEngine.Debug.Log("Activate: " + action.name);
            if (action.sourceRoom.disabled || action.sourceRoom.health <= 0 || action.card.turnsUntilReady != 0) {continue;}
            if (action is LaserAction || action is FreeLaserAction){ weaponCalls.Add(() => FireLaserAtTarget(action.affectedRoom.parent.transform.position, action.affectedRoom)); }
            
            action.Activate();
        }

        StartCoroutine(InvokeWeaponActionsWithDelay(weaponCalls));
    }

    public void EnemyChooseActions()
    {
        System.Random random = new System.Random();

        // If i have a reactor Action take it
        List<Card> APUps = enemyHand.GetCardsByAction(typeof(SpeedUpAction));
        if (APUps.Count > 0)
        {
            foreach (Card APCard in APUps)
            {
                if (APCard.CanBeUsed(enemyShip.AP))
                {
                    APCard.cardAction.affectedRoom = APCard.cardAction.sourceRoom;
                    SubmitCard(APCard, false);
                }
            }
        }
        List<Card> laserCards = enemyHand.GetCardsByAction(typeof(LaserAction));
        while (enemyShip.AP > 2)
        {
            foreach (Card laserCard in laserCards)
            {        
                if (laserCard.CanBeUsed(enemyShip.AP))
                {
                    Room target = playerRooms[RoomType.Reactor][0];
                    laserCard.cardAction.affectedRoom = target;
                    foreach (CombatEffect effect in laserCard.cardAction.effects) effect.affectedRoom = target;
                    SubmitCard(laserCard, false);
                    break;
                }
            }
        }
        int c = 0;
        List<Card> shieldCards = enemyHand.GetCardsByAction(typeof(FocusedShieldAction));
        List<Card> speedUpCards = enemyHand.GetCardsByAction(typeof(SpeedUpAction));
        while (enemyShip.AP > 0)
        {
            List<Room> weaponsRooms = enemyRooms[RoomType.Weapons].Where(obj => obj.health > 0).ToList();
            List<Room> reactorRooms = enemyRooms[RoomType.Shield].Where(obj => obj.health > 0).ToList();
            List<Room> shieldRooms = enemyRooms[RoomType.Reactor].Where(obj => obj.health > 0).ToList();
            
            if (random.Next(2) == 0 && (weaponsRooms.Count > 0 || reactorRooms.Count > 0 || shieldRooms.Count > 0))
            {
                
                foreach (Card shieldCard in shieldCards)
                {
                    if (shieldCard.CanBeUsed(enemyShip.AP))
                    {
                    Room target;
                    if (weaponsRooms.Count > 0)      {target = weaponsRooms[0];}
                    else if (reactorRooms.Count > 0) {target = reactorRooms[0];}
                    else                             {target = enemyShip.GetRoomList()[0];}                   
                    shieldCard.cardAction.affectedRoom = target;
                    foreach (CombatEffect effect in shieldCard.cardAction.effects) effect.affectedRoom = target;

                    SubmitCard(shieldCard, false);
                    break;
                    }
                }
            }
            else if (enemyRooms.ContainsKey(RoomType.Engine) && enemyRooms[RoomType.Engine].Any(item => item.health > 0))
            {
                foreach (Card speedUpCard in speedUpCards)
                {
                    speedUpCard.cardAction.affectedRoom = speedUpCard.cardAction.sourceRoom;;
                    SubmitCard(speedUpCard, false);
                }
            }
            else
            {
                c += 1;
                if (c > 10){break; }
            }
        }
    }

    public void ResetTempStats()
    {
        // Reset all temp stats for next turn
        playerShip.ResetAP();
        playerShip.ResetSpeed();
        playerShip.ResetTempRoomStats();

        enemyShip.ResetAP();
        enemyShip.ResetSpeed();
        enemyShip.ResetTempRoomStats();
    }
    
    public void ResetTurnActions()
    {
        enemyTurnActionsText.text = "";
        playerTurnActionsText.text = "";

        playerTurnActions.Clear();
        enemyTurnActions.Clear();
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

    public void PickCard(Card card)
    {
        Cursor.SetCursor(customCursor, new Vector2(customCursor.width / 2, customCursor.height / 2), CursorMode.ForceSoftware);
        selectedCard = card;
        card.cardController.gameObject.SetActive(false);
    }

    public void RoomClicked(Vector3 targetPosition, Room target)
    {
        selectedCard.cardAction.affectedRoom = target;
        SubmitCard(selectedCard, true);
        if(selectedCard.cardAction.cooldown==0){
            selectedCard.cardController.gameObject.SetActive(true);
        }
        
        selectedCard = null;
        Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
    }

    public void DeselectCard()
    {   
        if (selectedCard != null)
        {
            selectedCard.cardController.gameObject.SetActive(true);
            selectedCard = null;
        }     
        Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
    }

    public void ShowPotentialPassiveEffects()
    {
        foreach (Room room in playerShip.GetRoomList())
        {
            foreach(CombatEffect effect in room.effectsApplied)
            {
                effect.ShowPotentialEffect();
            }
        }

        foreach (Room room in enemyShip.GetRoomList())
        {
            foreach(CombatEffect effect in room.effectsApplied)
            {
                effect.ShowPotentialEffect();
            }
        }
    }

    public void ShowPotentialEffect(CardAction action)
    {
        foreach (CombatEffect effect in action.effects)
        {
            effect.ShowPotentialEffect();
        }
    }

    public void SubmitCard(Card card, bool isPlayer)
    {
        CardAction action = card.cardAction.Clone();
        ShowPotentialEffect(card.cardAction);
        if (isPlayer)
        { 
            playerShip.AdjustAP(-action.cost);

            playerTurnActions.Add(action);
            if (playerTurnActionsText.text != "")
            {
                playerTurnActionsText.text += '\n';
            }
            playerTurnActionsText.text += action.name;
            playerTurnActionsText.text += " -> ";
            playerTurnActionsText.text += action.affectedRoom.roomType.ToString();
        }
        else
        {

            enemyShip.AdjustAP(-action.cost);
            
            enemyTurnActions.Add(action);
            if (enemyTurnActionsText.text != "")
            {
                enemyTurnActionsText.text += '\n';
            }
            enemyTurnActionsText.text += action.name;
            enemyTurnActionsText.text += " -> ";
            enemyTurnActionsText.text += action.affectedRoom.roomType.ToString();
        }
    }

    public void UpdateUIText()
    {
            UpdateHandStats();
            UpdateAPGraphics(true);
            UpdateSpeedGraphics(true);
            UpdateAPGraphics(false);
            UpdateSpeedGraphics(false);
    }

    public void UpdateSpeedGraphics(bool isPlayer)
    {
        if (isPlayer) { this.playerSpeedText.text = playerShip.speed.ToString(); }
        else          { this.enemySpeedText.text  = enemyShip.speed.ToString(); }
    }

    public void UpdateAPGraphics(bool isPlayer)
    {
        if (isPlayer) { this.playerAPText.text = playerShip.AP.ToString(); }
        else          { this.enemyAPText.text  = enemyShip.AP.ToString(); }
    }
    
    public void UpdateHandStats()
    {
        foreach(Card card in playerHand.GetCards()){if (card.turnsUntilReady > 0 ) {card.NextTurn();}}
        foreach(Card card in enemyHand.GetCards() ){if (card.turnsUntilReady > 0 ) {card.NextTurn();}}
    }

    public void UpdateRoomText()
    {
        foreach(Room room in playerShip.GetRoomList()){room.UpdateTextGraphics();}
        foreach(Room room in enemyShip.GetRoomList()){room.UpdateTextGraphics();}

    }
}


