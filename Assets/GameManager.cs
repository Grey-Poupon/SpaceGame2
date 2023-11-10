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
    public bool IsSimulation;
    public int turnCounter = 0;
    public PrefabHolder prefabHolder;
    public TreeNode gameTree;
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
        IsSimulation=false;
        StartCoroutine(StartGame());

    }

    private IEnumerator StartGame()
    {
        // Wait for 0.5 seconds
        while (playerShip == null || playerHand == null)
        {
            yield return new WaitForSeconds(0.1f);
        }
        yield return new WaitForSeconds(0.1f);

        // Call your non-async function here
        turn = TurnTypes.Enemy;
        foreach(Card card in playerHand.GetCards())
        {
            if(card.cardAction.cooldown > 0)
            {
                card.cardController.gameObject.SetActive(false);
                card.turnsUntilReady = card.cardAction.cooldown + 1;
            }
        }
        if (IsSimulation)
        {
            int limit = 10;
            int counter = 0;
            while(counter < limit && playerRooms[RoomType.Reactor][0].health > 0 && enemyRooms[RoomType.Reactor][0].health > 0)
            {
                counter ++;
                
                ResolveActions();

                ResetTempStats();
                ResetTurnActions();

                // ----  End of Round  ---

                float value = playerRooms[RoomType.Reactor][0].health / playerRooms[RoomType.Reactor][0].getMaxHealth();

                // ---- Start of Round ---

                SimulateEnemyTurn();

                UpdateHandStats();

                SimulatePlayerTurn();
                break;
            }
            UnityEngine.Debug.Log("It's the END of the SIMULATION ! THe MaTrIx ItS CoLaPsInG :%SZDSX");
        }
        else
        {
            BuildAShip();
            FinishTurn();
        }
        
    }
    public void BuildAShip()
    {
        float xBound = 3;
        float yBound = 1;
        float xPos = 0;
        float yPos = 0;
        int numRooms = 5;

        List<(float, float)> directions = new List<(float, float)>(){(1, 0), (0, 1), (-1, 0), (0, -1)};
        HashSet<(float, float)> roomLocations = new HashSet<(float, float)>();
        List<RoomType> roomTypes = RoomType.GetValues(typeof(RoomType))
            .Cast<RoomType>()
            .OrderBy(e => UnityEngine.Random.value)
            .Take(numRooms)
            .ToList();
        // Create a new Ship
        EnemySpaceship newShip = Instantiate(prefabHolder.enemySpaceshipPrefab, new Vector3(2, 2, 0), Quaternion.identity);        
        
        // Setup first Room
        RoomController rootRoom = Instantiate(prefabHolder.roomPrefab, newShip.transform);
        rootRoom.transform.localPosition = new Vector3(xPos, yPos, 0);
        rootRoom.name = "RootRoom";
        rootRoom.Setup(RoomType.Reactor);

        roomLocations.Add((xPos, yPos));
        
        while (roomTypes.Count > 0)
        {
            // Define all valid locations I.E. not overlapping
            List<(float, float)> validLocations = directions
                                                .Select(direction => (xPos + direction.Item1, yPos + direction.Item2))
                                                .Where(position => !roomLocations.Contains(position) && Math.Abs(position.Item1) <= xBound && Math.Abs(position.Item2) <= yBound)
                                                .ToList();

            // If there are not valid locations we fucked up
            if (validLocations.Count == 0)
            {
                UnityEngine.Debug.Log("Yeah, the ship factory got caught in a dead end, so write better code, NOW!");
                break;
            }

            // Choose a random valid location and move the pointers to this so
            // we can generate from here next loop
            int idx = UnityEngine.Random.Range(0, validLocations.Count);            
            xPos = validLocations[idx].Item1;
            yPos = validLocations[idx].Item2;

            // Make the room
            RoomController roomController = Instantiate(prefabHolder.roomPrefab, newShip.transform);
            roomController.transform.localPosition = new Vector3(xPos, yPos, 0);
            roomLocations.Add((xPos, yPos));
            
            // Choose a random room type from the remaining options
            idx = UnityEngine.Random.Range(0, roomTypes.Count);
            roomController.Setup(roomTypes[idx]);
            roomController.name = roomTypes[idx].ToString() + " Room";
            roomTypes.RemoveAt(idx);
        }
    }

    public void SimulatePlayerTurn()
    {
        Dictionary<int, List<CardAction>> allCardCombinations = new Dictionary<int, List<CardAction>>();
        GenerateCardCombinations(new List<CardAction>(), 0, allCardCombinations, playerShip.AP, playerHand.GetCards());

        // string result = "";
        // foreach(List<CardAction> cardCombo in allCardCombinations)
        // {
        //     result += string.Join(" | ", cardCombo.Select(obj => !obj.needsTarget ? obj.name : obj.name + " -> " + obj.affectedRoom.roomType.ToString()));
        //     result += "\n";
        // }
        // UnityEngine.Debug.Log(result);

        UnityEngine.Debug.Log("Player Turns");
        UnityEngine.Debug.Log(allCardCombinations.Values.SelectMany(x => x).ToList().Count);
    }
    public void SimulateEnemyTurn()
    {
        Dictionary<int, List<CardAction>> allCardCombinations = new Dictionary<int, List<CardAction>>();
        GenerateCardCombinations(new List<CardAction>(), 0, allCardCombinations, enemyShip.AP, enemyHand.GetCards());

        // string result = "";
        // foreach(List<CardAction> cardCombo in allCardCombinations)
        // {
        //     result += string.Join(" | ", cardCombo.Select(obj => !obj.needsTarget ? obj.name : obj.name + " -> " + obj.affectedRoom.roomType.ToString()));
        //     result += "\n";
        // }
        // UnityEngine.Debug.Log(result);
        UnityEngine.Debug.Log("Enemy Turns");
        UnityEngine.Debug.Log(allCardCombinations.Values.SelectMany(x => x).ToList().Count);
    }
    
    public void GenerateCardCombinations(List<CardAction> currentCombination, int index, Dictionary<int, List<CardAction>> allCardCombinations, float APLeft, List<Card> cardPool)
    {

        if (index == cardPool.Count)
        {
            int currentComboHash = GenerateCardCombinationHash(currentCombination);
            if (allCardCombinations.ContainsKey(currentComboHash))
            {
                return;
            }
            allCardCombinations[currentComboHash] = currentCombination;
            return;
        }
        CardAction currentAction = cardPool[index].cardAction;

        // Use the current action
        if (currentAction.CanBeUsed(APLeft))
        {
            float newAP = APLeft - currentAction.cost;
            int nextCard = currentAction.cooldown > 0 ? 1 : 0; // If the card isn't infinite move onto the next action
            if (currentAction.needsTarget)
            {
                foreach(Room room in enemyShip.GetRoomList())
                {
                    CardAction cardActionWithTarget = currentAction.Clone();
                    cardActionWithTarget.affectedRoom = room;
                    List<CardAction> comboWithActionTarget = new List<CardAction>( currentCombination.Concat(new List<CardAction>{cardActionWithTarget}) );
                    
                    GenerateCardCombinations(comboWithActionTarget, index + nextCard, allCardCombinations, newAP, cardPool);
                }
                foreach(Room room in playerShip.GetRoomList())
                {
                    CardAction cardActionWithTarget = currentAction.Clone();
                    cardActionWithTarget.affectedRoom = room;
                    List<CardAction> comboWithActionTarget = new List<CardAction>( currentCombination.Concat(new List<CardAction>{cardActionWithTarget}) );
                    
                    GenerateCardCombinations(comboWithActionTarget, index + nextCard, allCardCombinations, newAP, cardPool);
                }
            }
            else
            {
                List<CardAction> comboWithAction = new List<CardAction>( currentCombination.Concat(new List<CardAction>{currentAction}) );
                GenerateCardCombinations(comboWithAction, index + nextCard, allCardCombinations, newAP, cardPool);
            }
        }
        // Don't use the current action
        GenerateCardCombinations(currentCombination, index + 1, allCardCombinations, APLeft, cardPool);

    }

    public int GenerateCardCombinationHash(List<CardAction> cardActions)
    {
        // Convert the list of CardAction objects into a set of strings
        HashSet<string> cardActionSet = new HashSet<string>(
            cardActions.Select(ca => $"{ca.GetType().ToString()}:{ca.sourceRoom.roomType.ToString()}:{(ca.affectedRoom == null ? 1 : ca.affectedRoom.roomType.ToString())}")
        );

        // Sort the elements in the HashSet
        var sortedCardActionSet = new SortedSet<string>(cardActionSet);

        int hash = 17;
        foreach (var item in sortedCardActionSet)
        {
            hash = hash * 31 + item.GetHashCode();
        }
        return hash;
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
                CardController cardController = Instantiate(prefabHolder.cardPrefab, playerHand.parent.transform);
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
            else         { enemyHand.AddCard(card);  }
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

    public void FireLaserAtTarget(CardAction action)
    {
        Vector3 origin;
        Vector3 targetPosition = action.affectedRoom.parent.transform.position;
        Room target = action.affectedRoom;
        if(action.sourceRoom.isPlayer){
            origin = playerShip.transform.position;
        }
        else{
            origin = enemyShip.transform.position;
        }

        if (playerShip != null)
        {
            
            // Calculate the direction to the target position.
            Vector3 direction = (targetPosition - origin).normalized;
            // Instantiate the laser at the spaceship's position.
            LaserShot laser = Instantiate(prefabHolder.laserPrefab, origin, Quaternion.identity);
            
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
            turnCounter ++;
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
        if (playerFirst){UnityEngine.Debug.Log("Player First");}
        else{UnityEngine.Debug.Log("Enemy First");}

        // Activate Each Action
        if (playerFirst)
        {
            PlayOutActions(playerTurnActions, playerRooms);
            PlayOutActions(enemyTurnActions, enemyRooms);
        }
        else
        {
            PlayOutActions(enemyTurnActions, enemyRooms);
            PlayOutActions(playerTurnActions, playerRooms);
        }
    }

    public void PlayOutActions(List<CardAction> actions, Dictionary<RoomType, List<Room>> rooms)
    {
        bool verbose = true;
        List<System.Action> weaponCalls = new List<System.Action>();
        string explanation = "";
        // Trigger any effects that are still affecting the affected
        List<Room> allRooms = rooms.Values.SelectMany(x => x).ToList();
        foreach (Room room in allRooms)
        {
            // Have to be careful here as effects will remove themselves from the rooms 
            // To Do there is a big where if a effect remove another effect shit will get wild
                
            if (room.effectsApplied.Count > 0)
            {
                List<CombatEffect> effectsCopy = room.effectsApplied.Select(obj => obj).ToList();
                explanation += "\nThe " +( room.isPlayer ? "Players " : "Enemys ") + room.roomType.ToString() + "Room is affected by: " + string.Join(" | ", room.effectsApplied.Select(obj => obj.GetType().ToString()).ToList());
                foreach(CombatEffect effect in effectsCopy)
                {
                    if (room.effectsApplied.Contains(effect))
                    {
                        effect.Activate();
                    }
                }
            }
        }
        explanation += "\n";

        // Activate actions, which will apply and trigger some more effects
        foreach (CardAction action in actions)
        {

            if (!action.IsReady())
            {
                explanation += "\n" + action.name + " Was not played because: Disabled " + action.sourceRoom.disabled + " Destroyed " + action.sourceRoom.destroyed + (action.card.turnsUntilReady!=0 ? "Action Not Ready" : "Action Ready") ;
                continue;
            }
            explanation += "\n" + action.name + " Was played";
            if (action is LaserAction || action is FreeLaserAction || action is MissileAction){ weaponCalls.Add(() => FireLaserAtTarget(action)); }
            if (action.effects.Where(obj => obj is DamageEffect).ToList().Count > 0)
            {
                explanation += " on a room with " + action.affectedRoom.defence.ToString() + " defence";
            }
            action.Activate();
        }
        if (verbose) UnityEngine.Debug.Log(explanation);
        StartCoroutine(InvokeWeaponActionsWithDelay(weaponCalls));
    }

    public void EnemyChooseActions()
    {
        EnemyChooseRandomActions();
    }
    public void EnemyAIOldStyle()
    {
        System.Random random = new System.Random();

        // If i have a reactor Action take it
        List<Card> APUps = enemyHand.GetCardsByAction(typeof(OverdriveAction));

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
        while (enemyShip.AP > 2 && laserCards.Count > 0)
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
        while (enemyShip.AP > 0 && (shieldCards.Count + speedUpCards.Count) > 0)
        {
            List<Room> weaponsRooms = enemyRooms[RoomType.Laser].Where(obj => obj.health > 0).ToList();
            List<Room> reactorRooms = enemyRooms[RoomType.Shield].Where(obj => obj.health > 0).ToList();
            List<Room> shieldRooms  = enemyRooms[RoomType.Reactor].Where(obj => obj.health > 0).ToList();
            
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
    public void EnemyChooseRandomActions()
    {
        System.Random random = new System.Random();
        List<Card> cards = new List<Card>(enemyHand.GetCards());

        while(enemyShip.AP>0){
            int index = random.Next(cards.Count);
            Card card = cards[index];
            if(card.CanBeUsed(enemyShip.AP)){
                if(card.cardAction.needsTarget && card.cardAction is LaserAction || card.cardAction is MissileAction || card.cardAction is FreeLaserAction || card.cardAction is ShieldPiercerAction || card.cardAction is FirebombAction || card.cardAction is EMPAction)
                {
                    List<Room> playerRooms = playerShip.GetRoomList();
                    int roomindex = random.Next(playerRooms.Count);
                    card.cardAction.SetAffectedRoom(playerRooms[roomindex]);
                }
                else
                {
                    card.cardAction.SetAffectedRoom(card.cardAction.sourceRoom);
                }
                
                SubmitCard(card,false);
                if(card.cardAction.cooldown>0){
                    cards.Remove(card);
                }
            }
        }

    
    }

    public void EnemyAIAllOutAttack()
    {
        List<Card> laserCards = enemyHand.GetCardsByAction(typeof(LaserAction)).Where(obj => obj.IsReady()).ToList();
        if (laserCards.Count > 0)
        {
            Card laser = laserCards[0];
            while (laser.CanBeUsed(enemyShip.AP - 1))
            {
                List<Room> targets = playerRooms[RoomType.Laser].Where(obj => obj.health > 0).ToList();
                if (targets.Count == 0){break;}

                Room target = targets[0];
                laser.cardAction.affectedRoom = target;
                foreach (CombatEffect effect in laser.cardAction.effects) effect.affectedRoom = target;

                SubmitCard(laser, false);
            }
        }
    }
    public void EnemyAISemiPermanentShieldShip()
    {
        List<Card> SPShield = enemyHand.GetCardsByAction(typeof(SemiPermanentShieldAction)).Where(obj => !obj.cardAction.sourceRoom.destroyed && !obj.cardAction.sourceRoom.disabled).ToList();
        if (SPShield.Count > 0)
        {
            Card shield = SPShield[0];
            shield.cardAction.cooldown = 0;
            shield.cardAction.cost = 0;

            foreach (Room target in enemyShip.GetRoomList())
            {
                shield.cardAction.affectedRoom = target;
                foreach (CombatEffect effect in shield.cardAction.effects) effect.affectedRoom = target;

                SubmitCard(shield, false);
                SubmitCard(shield, false);
                SubmitCard(shield, false);
            }
        }
    }
    public void EnemyAIEMP()
    {
        List<Card> emps = enemyHand.GetCardsByAction(typeof(EMPAction)).Where(obj => !obj.cardAction.sourceRoom.destroyed && !obj.cardAction.sourceRoom.disabled).ToList();
        if (emps.Count > 0)
        {
            Card emp = emps[0];
            emp.turnsUntilReady = 0;
            bool targetFound = false;

            foreach (Room target in playerRooms[RoomType.Laser].Where(obj => !obj.destroyed && !obj.disabled).ToList())
            {
                emp.cardAction.affectedRoom = target;
                foreach (CombatEffect effect in emp.cardAction.effects) effect.affectedRoom = target;
                SubmitCard(emp, false);
                targetFound=true;
                break;
            }
            if (!targetFound)
            {
            foreach (Room target in playerShip.GetRoomList().Where(obj => !obj.destroyed && !obj.disabled).ToList())
            {
                emp.cardAction.affectedRoom = target;
                foreach (CombatEffect effect in emp.cardAction.effects) effect.affectedRoom = target;
                SubmitCard(emp, false);
                targetFound=true;
                break;
            }
            }

        }
    }
    public void EnemyAIFiftyFifty()
    {
        if(turnCounter % 2 == 0)
        {
            EnemyAIAllOutAttack();
        }
        else
        {
            EnemyAISemiPermanentShieldShip();
        }
    }
    public void EnemyAIEMPOrFiftyFifty()
    {
        if(turnCounter % 3 == 0)
        {
            EnemyAIEMP();
            EnemyChooseRandomActions();
        }
        else if(turnCounter % 3 == 1)
        {
            EnemyAIAllOutAttack();
        }
        else
        {
            EnemyAISemiPermanentShieldShip();
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
        if(!card.cardAction.needsTarget){
            RoomClicked(Vector3.zero,card.cardAction.sourceRoom);
        }
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


    public void RestartTurn(){
        // need a way to reset GUI on rooms as current method will not remove shield no. , perhaps the showPotentialEffect looks through submitted actions?
        playerShip.AdjustAP(playerShip.defaultAP-playerShip.AP);
        foreach(CardAction action in playerTurnActions){
            if(action.cooldown>0){
                action.card.cardController.gameObject.SetActive(true);
            }
            
            foreach(CombatEffect effect in action.effects){
                if(effect is DamageEffect){ // need to enumerate all effects that implement attack intent
                    DamageEffect dmgEffect = effect as DamageEffect;
                    action.affectedRoom.IncreaseAttackIntent(-dmgEffect.damage);
                }
            }
        }
        playerTurnActions.Clear();
        playerTurnActionsText.text="";
    }
    public void UndoAction(){
        // need a way to reset GUI on rooms as current method will not remove  shield no. , perhaps the showPotentialEffect looks through submitted actions?
        
        if(playerTurnActions.Count>0){
            CardAction lastAction = playerTurnActions[playerTurnActions.Count -1];
            playerShip.AdjustAP(lastAction.cost);
            if(playerTurnActions.Count==1){
                playerTurnActionsText.text="";
            }
            else{
                playerTurnActionsText.text=playerTurnActionsText.text.Substring(0,playerTurnActionsText.text.Length-1-lastAction.name.Length -" -> ".Length -lastAction.affectedRoom.roomType.ToString().Length);
            }
            if(lastAction.cooldown>0){
                lastAction.card.cardController.gameObject.SetActive(true);
            }
            foreach(CombatEffect effect in lastAction.effects){
                if(effect is DamageEffect){ // need to enumerate all effects that implement attack intent
                    DamageEffect dmgEffect = effect as DamageEffect;
                    lastAction.affectedRoom.IncreaseAttackIntent(-dmgEffect.damage);
                }
            }
            playerTurnActions.Remove(lastAction);
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
        //UnityEngine.Debug.Log(string.Join(" & ", playerHand.GetCards().Select(obj=>obj.cardAction.name + ":"+obj.turnsUntilReady.ToString()).ToList()));
        foreach(Card card in playerHand.GetCards())
        {if (card.turnsUntilReady > 0 ) {card.NextTurn();}}
        foreach(Card card in enemyHand.GetCards() )
        {if (card.turnsUntilReady > 0 ) {card.NextTurn();}}
    }

    public void UpdateRoomText()
    {
        foreach(Room room in playerShip.GetRoomList()){room.UpdateTextGraphics();}
        foreach(Room room in enemyShip.GetRoomList()){room.UpdateTextGraphics();}

    }
}


