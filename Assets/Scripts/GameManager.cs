using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Net.Mime;
using System.Threading;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;

public enum TurnTypes
{
    Player,
    Enemy,
    Resolve,
    PlayerWin,
    EnemyWin
}

public class GameManager
{
    public GameManagerController gameManagerController;
    public TurnTypes turn = TurnTypes.Enemy;
    public Card selectedCard;
    public Hand playerHand;
    public Hand enemyHand;
    public Spaceship playerShip;
    public Spaceship enemyShip;
    public Dictionary<RoomType, List<Room>> playerRooms = new Dictionary<RoomType, List<Room>>();
    public Dictionary<RoomType, List<Room>> enemyRooms = new Dictionary<RoomType, List<Room>>();
    public List<CardAction> playerTurnActions = new List<CardAction>();
    public List<CardAction> enemyTurnActions = new List<CardAction>();
    public bool IsSimulation;
    public int turnCounter = 0;
    public List<IntentLine> activeIntentLines = new List<IntentLine>();
    private List<Move> moves;

    public GameManager Clone()
    {
        GameManager clone = new GameManager();

        clone.turn = TurnTypes.Enemy;
        clone.playerHand = this.playerHand.Clone();
        clone.enemyHand = this.enemyHand.Clone();
        clone.playerShip = this.playerShip.Clone();
        clone.enemyShip = this.enemyShip.Clone();
        clone.IsSimulation = this.IsSimulation;
        clone.turnCounter = this.turnCounter;

        clone.playerRooms = new Dictionary<RoomType, List<Room>>();
        clone.enemyRooms = new Dictionary<RoomType, List<Room>>();
        clone.playerTurnActions = new List<CardAction>();
        clone.enemyTurnActions = new List<CardAction>();

        foreach (var kvp in this.playerRooms)
        {
            List<Room> clonedRooms = new List<Room>();
            foreach (var room in kvp.Value)
            {
                Room clonedRoom = (Room)room.Clone();
                clonedRooms.Add(clonedRoom);
            }
            clone.playerRooms.Add(kvp.Key, clonedRooms);
        }

        foreach (var kvp in this.enemyRooms)
        {
            List<Room> clonedRooms = new List<Room>();
            foreach (var room in kvp.Value)
            {
                Room clonedRoom = (Room)room.Clone();
                clonedRooms.Add(clonedRoom);
            }
            clone.enemyRooms.Add(kvp.Key, clonedRooms);
        }

        foreach (CardAction cardAction in this.playerTurnActions)
        {
            CardAction clonedAction = cardAction.Clone();
            clone.playerTurnActions.Add(clonedAction);
        }

        foreach (CardAction cardAction in this.enemyTurnActions)
        {
            CardAction clonedAction = cardAction.Clone();
            clone.enemyTurnActions.Add(clonedAction);
        }
        return clone;
    }

    public List<Move> GetMoves()
    {
        if (this.moves is null)
        {
            this.moves = _GenerateMoves().ToList();
        }
        return this.moves;
    }

    public Move GetRandomMove()
    {
        if (this.turn == TurnTypes.Player)
        {
            return GenerateRandomMove(
                playerShip.AP,
                playerHand.GetCards(),
                playerRooms.Values.SelectMany(x => x).ToList(),
                enemyRooms.Values.SelectMany(x => x).ToList(),
                true
            );
        }
        else
        {
            return GenerateRandomMove(
                enemyShip.AP,
                enemyHand.GetCards(),
                enemyRooms.Values.SelectMany(x => x).ToList(),
                playerRooms.Values.SelectMany(x => x).ToList(),
                false
            );
        }
    }

    public void DoMove(Move move)
    {
        foreach (MinCardAction m_card in move.cards)
        {
            SubmitCard(new Card(m_card.ca), turn == TurnTypes.Player);
        }
        if (IsSimulation)
        {
            SimulateEndTurn();
            this.moves = _GenerateMoves().ToList();
        }
    }

    public Move[] _GenerateMoves()
    {
        if (this.turn == TurnTypes.Player)
        {
            return GenerateMoves(playerShip.AP, playerHand.GetCards(), true);
        }
        else
        {
            return GenerateMoves(enemyShip.AP, enemyHand.GetCards(), false);
        }
    }

    public float GetResult(int player)
    {
        if (playerRooms[RoomType.Reactor].Sum(room => room.health) <= 0)
        {
            return player == 1 ? 0 : 1;
        }
        if (enemyRooms[RoomType.Reactor].Sum(room => room.health) <= 0)
        {
            return player == 1 ? 1 : 0;
        }
        if (player == 0)
        {
            float currentHealth = playerRooms
                .Values.SelectMany(roomList => roomList)
                .Sum(room => room.health);
            float maxHealth = playerRooms
                .Values.SelectMany(roomList => roomList)
                .Sum(room => room.maxHealth);
            return 1 - currentHealth / maxHealth;
        }
        else
        {
            float currentHealth = enemyRooms
                .Values.SelectMany(roomList => roomList)
                .Sum(room => room.health);
            float maxHealth = enemyRooms
                .Values.SelectMany(roomList => roomList)
                .Sum(room => room.maxHealth);
            return 1 - currentHealth / maxHealth;
        }
    }

    public IEnumerator StartGame()
    {
        RegisterPlayerHand(new Hand());
        RegisterEnemyHand(new Hand());
        BuildAPlayerShip();

        // Wait for 0.5 seconds
        while (playerShip == null || playerHand == null)
        {
            yield return new WaitForSeconds(0.1f);
        }
        yield return new WaitForSeconds(0.1f);

        // Call your non-async function here
        turn = TurnTypes.Resolve;
        foreach (Card card in playerHand.GetCards())
        {
            if (card.cardAction.cooldown > 0)
            {
                card.cardController.ToggleTransparency(true);
                card.turnsUntilReady = card.cardAction.cooldown + 1;
            }
        }
        BuildAShip();
        FinishTurn();
    }

    public void DrawIntentLine(Vector3 startPoint, Vector3 endPoint, float fuzziness = 0)
    {
        return;
        if (IsSimulation == true)
        {
            return;
        }
        float getFuzzy(float fuzziness, float steps = 2)
        {
            float point = UnityEngine.Random.Range(-fuzziness / 2f, fuzziness / 2f);
            //float value = Mathf.Round(point * steps) / steps;
            return point;
        }

        var fuzzyOffset = new Vector3(getFuzzy(fuzziness), getFuzzy(fuzziness), -1);

        IntentLine intentLine = (IntentLine)
            gameManagerController._Instantiate(gameManagerController.prefabHolder.intentLine);
        intentLine.DrawCurvedLine(startPoint + fuzzyOffset, endPoint + fuzzyOffset);

        activeIntentLines.Add(intentLine);
    }

    public void BuildAShip()
    {
        enemyRooms.Clear();
        enemyHand.Clear();
        void addRoom(RoomType roomType, float xPos, float yPos, Transform parent)
        {
            // Setup Basic Rooms
            RoomController rootRoom = (RoomController)
                gameManagerController._Instantiate(
                    gameManagerController.prefabHolder.roomPrefab,
                    parent
                );
            rootRoom.transform.localPosition = new Vector3(xPos, yPos, 0);
            rootRoom.name = roomType.ToString() + " Room";
            rootRoom.Setup(roomType);
        }
        float xBound = 3;
        float yBound = 1;
        float xPos = 0;
        float yPos = 0;
        int numRooms = 5;

        // The number of rooms is kinda random because the basic rooms may not be in the 5 rooms defined here
        // so it could be anywhere from 5 to 9 rooms
        List<(float, float)> directions = new List<(float, float)>()
        {
            (1, 0),
            (0, 1),
            (-1, 0),
            (0, -1)
        };
        HashSet<(float, float)> roomLocations = new HashSet<(float, float)>();
        List<RoomType> roomTypes = RoomType
            .GetValues(typeof(RoomType))
            .Cast<RoomType>()
            .OrderBy(e => UnityEngine.Random.value)
            .Take(numRooms)
            .ToList();
        // Create a new Ship
        SpaceshipController newShip = (SpaceshipController)
            gameManagerController._Instantiate(
                gameManagerController.prefabHolder.enemySpaceshipPrefab,
                new Vector3(4, 11, 4),
                Quaternion.identity
            );
        newShip.gameObject.tag = "enemy";
        newShip.init();
        newShip.spaceship.ResetAP(IsSimulation);
        // Setup Basic Rooms
        addRoom(RoomType.Reactor, xPos, yPos, newShip.transform);
        roomTypes.Remove(RoomType.Reactor);
        roomLocations.Add((xPos, yPos));

        addRoom(RoomType.Laser, xPos + 1, yPos, newShip.transform);
        roomTypes.Remove(RoomType.Laser);
        roomLocations.Add((xPos + 1, yPos));

        addRoom(RoomType.Engine, xPos, yPos + 1, newShip.transform);
        roomTypes.Remove(RoomType.Engine);
        roomLocations.Add((xPos, yPos + 1));

        addRoom(RoomType.Shield, xPos + 2, yPos, newShip.transform);
        roomTypes.Remove(RoomType.Shield);
        roomLocations.Add((xPos + 2, yPos));

        while (roomTypes.Count > 0)
        {
            // Define all valid locations I.E. not overlapping
            List<(float, float)> validLocations = directions
                .Select(direction => (xPos + direction.Item1, yPos + direction.Item2))
                .Where(position =>
                    !roomLocations.Contains(position)
                    && Math.Abs(position.Item1) <= xBound
                    && Math.Abs(position.Item2) <= yBound
                )
                .ToList();

            // If there are not valid locations we fucked up
            if (validLocations.Count == 0)
            {
                UnityEngine.Debug.Log(
                    "Yeah, the ship factory got caught in a dead end, so write better code, NOW!"
                );
                break;
            }

            // Choose a random valid location and move the pointers to this so
            // we can generate from here next loop
            int idx = UnityEngine.Random.Range(0, validLocations.Count);
            xPos = validLocations[idx].Item1;
            yPos = validLocations[idx].Item2;

            // Choose a random room type from the remaining options
            idx = UnityEngine.Random.Range(0, roomTypes.Count);

            // Make the room
            addRoom(roomTypes[idx], xPos, yPos, newShip.transform);
            roomLocations.Add((xPos, yPos));
            roomTypes.RemoveAt(idx);
        }
    }
    public void BuildAPlayerShip()
    {
        playerRooms.Clear();
        playerHand.Clear();
        void addRoom(RoomType roomType, float xPos, float yPos, Transform parent)
        {
            // Setup Basic Rooms
            RoomController rootRoom = (RoomController)
                gameManagerController._Instantiate(
                    gameManagerController.prefabHolder.roomPrefab,
                    parent
                );
            rootRoom.transform.localPosition = new Vector3(xPos, yPos, 0);
            rootRoom.name = roomType.ToString() + " Room";
            rootRoom.Setup(roomType);
        }

        // Create a new Ship
        SpaceshipController newShip = (SpaceshipController)
            gameManagerController._Instantiate(
                gameManagerController.prefabHolder.playerSpaceshipPrefab,
                new Vector3(-7, 12.5f, 4),
                Quaternion.identity
            );
        newShip.gameObject.tag = "player";
        newShip.init();
        newShip.spaceship.ResetAP(IsSimulation);
        
        // Setup Basic Rooms
        addRoom(RoomType.Reactor, -1, -1, newShip.transform);
        addRoom(RoomType.Laser,   -1, -2, newShip.transform);
        addRoom(RoomType.Engine,  -1, -3, newShip.transform);
        addRoom(RoomType.Shield,  -1, -4, newShip.transform);
        addRoom(RoomType.Firebomb,-1, -5, newShip.transform);

    }

    public void SimulatePlayerTurn()
    {
        UnityEngine.Debug.Log(" - - - PLAYER - - - - ");
        GameManager gameState = this.Clone();
        gameState.IsSimulation = true;
        Move bestMove = ISMCTS.Search(gameState, 10);
        foreach (MinCardAction m_action in bestMove.cards)
        {
            UnityEngine.Debug.Log(
                m_action.ca.name + " -> " + nameof(m_action.ca.affectedRoom.roomType)
            );
        }
        UnityEngine.Debug.Log(" - - - - - - - ");
        DoMove(bestMove);
    }

    public void MCTSEnemyTurn()
    {
        UnityEngine.Debug.Log(" - - - ENEMY - - - - ");
        GameManager gameState = this.Clone();
        gameState.IsSimulation = true;
        Move bestMove = ISMCTS.Search(gameState, 100, 20);
        foreach (MinCardAction m_action in bestMove.cards)
        {
            UnityEngine.Debug.Log(
                m_action.ca.name + " -> " + nameof(m_action.ca.affectedRoom.roomType)
            );
        }
        UnityEngine.Debug.Log(" - - - - - - - ");
        DoMove(bestMove);
    }

    public Move GenerateRandomMove(
        float ap,
        List<Card> cardPool,
        List<Room> yourRooms,
        List<Room> opponentRooms,
        bool isPlayer
    )
    {
        List<MinCardAction> turn = new List<MinCardAction>();
        Dictionary<String, List<CardAction>> cardActions =
            new Dictionary<String, List<CardAction>>();
        Dictionary<String, List<CardAction>> cardActionGroups =
            new Dictionary<String, List<CardAction>>();
        // Roll Table
        List<string> rollTable = new List<string>();
        int idx = 0;

        // Boost AP
        foreach (Card card in cardPool)
        {
            if (card.cardAction.group == "AP" && card.cardAction.CanBeUsed(ap))
            {
                card.cardAction.affectedRoom = yourRooms[0];
                turn.Add(card.cardAction.QuickClone(this));
                ap += card
                    .cardAction.GetEffectsByType(typeof(APEffect))
                    .Sum(ap => ((APEffect)ap).change);
            }
        }
        if (ap < 1)
        {
            return new Move(turn);
        }
        // Group usable cards
        foreach (Card card in cardPool)
        {
            if (card.CanBeUsed(ap))
            {
                if (!cardActions.ContainsKey(card.cardAction.name))
                {
                    cardActions[card.cardAction.name] = new List<CardAction>();
                }
                if (!cardActionGroups.ContainsKey(card.cardAction.group))
                {
                    cardActionGroups[card.cardAction.group] = new List<CardAction>();
                }
                cardActions[card.cardAction.name].Add(card.cardAction);
                cardActionGroups[card.cardAction.group].Add(card.cardAction);

                // Build roll table
                if (!rollTable.Any(value => value.Contains(card.cardAction.group)))
                {
                    if (card.cardAction.group == "Offensive")
                    {
                        for (int i = 0; i < 4; i++)
                        {
                            rollTable.Add("Offensive");
                        }
                    }
                    else if (card.cardAction.group == "Shield")
                    {
                        for (int i = 0; i < 4; i++)
                        {
                            rollTable.Add("Shield");
                        }
                    }
                    else if (card.cardAction.group == "Special")
                    {
                        for (int i = 0; i < 2; i++)
                        {
                            rollTable.Add("Special");
                        }
                        //} else if (card.cardAction.group == "Speed")    {for (int i = 0; i < 4; i++){rollTable.Add("Speed");}
                    }
                    idx = rollTable.Count;
                }
            }
        }

        // Find Targets
        // Targeting
        // Randomly pick from a set of "good" options
        // 1: Lowest (Health + Shield)
        // 2: Lowest Health
        // 3: Highest Damage
        // 4: Laser
        // 5: Reactor
        // 6: Random
        float lowestHealth = 99;
        float lowestHealthShield = 99;
        float highestDamage = -99;
        Room lowestHealthRoom = opponentRooms[0];
        Room lowestHealthShieldRoom = opponentRooms[0];
        Room highestDamageRoom = opponentRooms[0];
        List<Room> opponentLaserRooms = new List<Room>();
        List<Room> opponentReactorRooms = new List<Room>();

        foreach (Room room in opponentRooms)
        {
            if (room.health < lowestHealth)
            {
                lowestHealth = room.health;
                lowestHealthRoom = room;
            }
            if (room.health + room.defence < lowestHealthShield)
            {
                lowestHealthShield = room.health + room.defence;
                lowestHealthShieldRoom = room;
            }
            float totalDamage = room
                .actions.Where(action => action.group == "Offensive")
                .Sum(action =>
                    action
                        .GetEffectsByType(typeof(DamageEffect))
                        .Sum(effect => ((DamageEffect)effect).damage)
                );
            if (totalDamage != null && totalDamage > highestDamage)
            {
                highestDamage = totalDamage;
                highestDamageRoom = room;
            }
            if (room.roomType == RoomType.Laser)
            {
                opponentLaserRooms.Add(room);
            }
            if (room.roomType == RoomType.Reactor)
            {
                opponentReactorRooms.Add(room);
            }
        }

        List<Room> inDangerRooms = new List<Room>();
        foreach (Room room in yourRooms)
        {
            if (room.health + room.defence <= highestDamage)
            {
                inDangerRooms.Add(room);
            }
        }

        int speedupRoll = -99;
        while (ap > 0)
        {
            // Speed up first
            if (speedupRoll == -99 && cardActionGroups.ContainsKey("Speed"))
            {
                // Spend a MAX of a Third of AP on Speed Ups
                int maxSpeedActions = (int)(
                    cardActionGroups["Speed"].Count > ap / 3
                        ? cardActionGroups["Speed"].Count
                        : ap / 3
                );
                speedupRoll = (int)UnityEngine.Random.Range(-maxSpeedActions, maxSpeedActions);
                for (int i = 0; i < speedupRoll; i++)
                {
                    CardAction t_ca = cardActionGroups["Speed"][
                        UnityEngine.Random.Range(0, cardActionGroups["Speed"].Count)
                    ];
                    t_ca.affectedRoom = yourRooms[0];
                    if (t_ca.CanBeUsed(ap))
                    {
                        ap -= t_ca.cost;
                        turn.Add(t_ca.QuickClone(this));
                    }
                }
            }

            // Roll on table
            string group = rollTable[UnityEngine.Random.Range(0, rollTable.Count - 1)];
            MinCardAction action = cardActionGroups[group]
                [UnityEngine.Random.Range(0, cardActionGroups[group].Count)]
                .QuickClone(this);

            // Targeting, Hopefully smart
            if (action.ca.group == "Offensive")
            {
                int target = UnityEngine.Random.Range(1, 6);
                if (target == 1)
                {
                    action.ca.affectedRoom = lowestHealthShieldRoom;
                }
                else if (target == 2)
                {
                    action.ca.affectedRoom = lowestHealthRoom;
                }
                else if (target == 3)
                {
                    action.ca.affectedRoom = highestDamageRoom;
                }
                else if (target == 4)
                {
                    action.ca.affectedRoom = opponentLaserRooms[
                        UnityEngine.Random.Range(0, opponentLaserRooms.Count)
                    ];
                }
                else if (target == 5)
                {
                    action.ca.affectedRoom = opponentReactorRooms[
                        UnityEngine.Random.Range(0, opponentReactorRooms.Count)
                    ];
                }
                else if (target == 6)
                {
                    action.ca.affectedRoom = opponentRooms[
                        UnityEngine.Random.Range(0, opponentRooms.Count)
                    ];
                }
            }
            else if (action.ca.group == "Shield")
            {
                if (inDangerRooms.Count > 0)
                {
                    action.ca.affectedRoom = inDangerRooms[
                        UnityEngine.Random.Range(0, inDangerRooms.Count)
                    ];
                }
                else
                {
                    action.ca.affectedRoom = yourRooms[
                        UnityEngine.Random.Range(0, yourRooms.Count)
                    ];
                }
            }
            else
            {
                if (action.ca.offensive)
                {
                    action.ca.affectedRoom = opponentRooms[
                        UnityEngine.Random.Range(0, opponentRooms.Count)
                    ];
                }
                else
                {
                    action.ca.affectedRoom = yourRooms[
                        UnityEngine.Random.Range(0, yourRooms.Count)
                    ];
                }
            }
            ap -= action.ca.cost;
            turn.Add(action);
        }
        return new Move(turn);
    }

    public Move[] GenerateMoves(float ap, List<Card> cardPool, bool isPlayer)
    {
        // This method will be called a lot of times so it is important to be efficient. however, for some ships and cards the number of actions >10,000 so I decided to randomly sample some actions instead
        // as it is relatively quick
        int numCombos = 500; // number of random samples
        Move[] combinations = new Move[numCombos]; // array that holds possible turns, an array as we need to be threadsafe which a list is not
        //var hashes = new List<int>(); // useful for debugging and seeing if we are getting unique turns

        System.Random rnd = new System.Random(); // not technically thread safe but seems to work _/'(o_o)'\_
        List<CardAction> cardActions = new List<CardAction>();
        foreach (var card in cardPool)
        {
            cardActions.Add(card.cardAction);
        }

        List<Room> enemyRoomList = enemyRooms.Values.SelectMany(x => x).ToList(); // make a local reference so we don't have to keep calling functions
        List<Room> playerRoomList = playerRooms.Values.SelectMany(x => x).ToList();
        int enemyRoomsLen = enemyRoomList.Count;
        int playerRoomsLen = playerRoomList.Count;
        Room[] enemyRoomsArr = new Room[enemyRoomsLen]; // made into arrays to increase access speed
        Room[] playerRoomsArr = new Room[playerRoomsLen];
        for (int i = 0; i < enemyRoomsLen; i++)
        {
            enemyRoomsArr[i] = enemyRoomList[i];
        }

        for (int i = 0; i < playerRoomsLen; i++)
        {
            playerRoomsArr[i] = playerRoomList[i];
        }
        // parralel for loop
        Parallel.For(
            0,
            numCombos,
            i =>
            {
                //var rnd = new System.Random(Thread.CurrentThread.ManagedThreadId);
                var turn = new List<MinCardAction>();
                List<CardAction> c_cardActions = new List<CardAction>(cardActions);
                float remainingAp = ap;
                while (remainingAp > 0 && c_cardActions.Count > 0 && turn.Count < 6)
                {
                    // randomly pick card
                    int randIndex = rnd.Next(0, c_cardActions.Count);
                    CardAction t_cardAction = c_cardActions[randIndex];

                    if (!t_cardAction.CanBeUsed(remainingAp))
                    {
                        c_cardActions.Remove(t_cardAction);
                        continue;
                    }
                    else if (t_cardAction.cooldown > 0)
                    {
                        c_cardActions.Remove(t_cardAction);
                    }
                    remainingAp -= t_cardAction.cost;

                    MinCardAction m_cardAction = t_cardAction.QuickClone(this);
                    if (t_cardAction.needsTarget)
                    {
                        // randomly pick room
                        if (
                            (t_cardAction.offensive && isPlayer)
                            || (!(t_cardAction.offensive) && !isPlayer)
                        )
                        {
                            randIndex = rnd.Next(0, enemyRoomsLen);
                            m_cardAction.targetRoom = enemyRoomsArr[randIndex];
                            m_cardAction.ca.SetAffectedRoom(m_cardAction.targetRoom);
                        }
                        else
                        {
                            randIndex = rnd.Next(0, playerRoomsLen);
                            m_cardAction.targetRoom = playerRoomsArr[randIndex];
                            m_cardAction.ca.SetAffectedRoom(m_cardAction.targetRoom);
                        }
                    }
                    else
                    {
                        m_cardAction.ca.SetAffectedRoom(m_cardAction.ca.sourceRoom);
                    }
                    turn.Add(m_cardAction);
                }
                combinations[i] = new Move(turn);
            }
        );

        // for(int i =0;i<numCombos;i++){
        //     int hash = GenerateCardCombinationHash(combinations[i]);
        //     if(!hashes.Contains(hash)){
        //         hashes.Add(hash);
        //     }
        // }
        // UnityEngine.Debug.Log(hashes.Count.ToString()+" Unique turns");

        return combinations;
    }

    public void GenerateCardCombinations(
        List<CardAction> currentCombination,
        int index,
        Dictionary<int, List<CardAction>> allCardCombinations,
        float APLeft,
        List<Card> cardPool
    )
    {
        if (currentCombination.Count > 6)
        {
            return;
        }

        if (allCardCombinations.Count > 1000)
        {
            return;
        }

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
                foreach (Room room in enemyShip.GetRoomList())
                {
                    CardAction cardActionWithTarget = currentAction.Clone();
                    cardActionWithTarget.affectedRoom = room;
                    List<CardAction> comboWithActionTarget = new List<CardAction>(
                        currentCombination.Concat(new List<CardAction> { cardActionWithTarget })
                    );

                    GenerateCardCombinations(
                        comboWithActionTarget,
                        index + nextCard,
                        allCardCombinations,
                        newAP,
                        cardPool
                    );
                }
                foreach (Room room in playerShip.GetRoomList())
                {
                    CardAction cardActionWithTarget = currentAction.Clone();
                    cardActionWithTarget.affectedRoom = room;
                    List<CardAction> comboWithActionTarget = new List<CardAction>(
                        currentCombination.Concat(new List<CardAction> { cardActionWithTarget })
                    );

                    GenerateCardCombinations(
                        comboWithActionTarget,
                        index + nextCard,
                        allCardCombinations,
                        newAP,
                        cardPool
                    );
                }
            }
            else
            {
                List<CardAction> comboWithAction = new List<CardAction>(
                    currentCombination.Concat(new List<CardAction> { currentAction })
                );
                GenerateCardCombinations(
                    comboWithAction,
                    index + nextCard,
                    allCardCombinations,
                    newAP,
                    cardPool
                );
            }
        }
        // Don't use the current action
        GenerateCardCombinations(
            currentCombination,
            index + 1,
            allCardCombinations,
            APLeft,
            cardPool
        );
    }

    public int GenerateCardCombinationHash(List<CardAction> cardActions)
    {
        // Convert the list of CardAction objects into a set of strings
        HashSet<string> cardActionSet = new HashSet<string>(
            cardActions.Select(ca =>
                $"{ca.GetType().ToString()}:{ca.sourceRoom.roomType.ToString()}:{(ca.affectedRoom == null ? 1 : ca.affectedRoom.roomType.ToString())}"
            )
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

    public void update() { }

    public void RegisterPlayerShip(Spaceship playerShip)
    {
        this.playerShip = playerShip;
    }

    public void RegisterEnemyShip(Spaceship enemyShip)
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
                // _Instantiate a new card prefab & set its parent as the playerHand
                CardController cardController = (CardController)
                    gameManagerController._Instantiate(
                        gameManagerController.prefabHolder.cardPrefab,
                        gameManagerController.PlayerCardContainer.transform
                    );
                cardController.Setup(card);
                card.Setup(cardController);
            }
            cards.Add(card);
        }
        return cards;
    }

    public void AddCardsToHand(List<Card> cards, bool isPlayer)
    {
        foreach (Card card in cards)
        {
            if (isPlayer)
            {
                playerHand.AddCard(card);
            }
            else
            {
                enemyHand.AddCard(card);
            }
        }
    }

    public void RemovePlayerShip()
    {
        this.playerShip = null;
    }

    public void RemoveEnemyShip()
    {
        this.enemyShip = null;
    }

    public void FireLaserAtTarget(CardAction action)
    {
        Vector3 targetPosition = action.affectedRoom.parent.transform.position;
        Vector3 origin = action.sourceRoom.parent.transform.position;
        Room target = action.affectedRoom;

        if (playerShip != null)
        {
            // Calculate the direction to the target position.
            Vector3 direction = (targetPosition - origin).normalized;
            // Instantiate the laser at the spaceship's position.
            LaserShot laser = (LaserShot)
                gameManagerController._Instantiate(
                    gameManagerController.prefabHolder.laserPrefab,
                    origin,
                    Quaternion.identity
                );

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
            if (enemyRooms[RoomType.Reactor][0].health <= 0)
            {
                enemyShip.onDestroy();
                turn = TurnTypes.PlayerWin;
            }
            else if (playerRooms[RoomType.Reactor][0].health <= 0)
            {
                playerShip.onDestroy();
                turn = TurnTypes.EnemyWin;
            }
        }
        if (turn == TurnTypes.Enemy)
        {
            EnemyChooseActions();
            ShowPotentialPassiveEffects();
            UpdateHandStats();
            gameManagerController.UpdateUIText();
            UpdateRoomText();
            turn = TurnTypes.Player;
            turnCounter++;
        }
        if (turn == TurnTypes.PlayerWin)
        {
            gameManagerController.PlayerWin();
        }
        if (turn == TurnTypes.EnemyWin)
        {
            gameManagerController.EnemyWin();
        }
    }

    public void SimulateEndTurn()
    {
        if (turn == TurnTypes.Player)
        {
            turn = TurnTypes.Resolve;
        }
        if (turn == TurnTypes.Resolve)
        {
            ResolveActions();
            if (enemyRooms[RoomType.Reactor][0].health <= 0)
            {
                enemyShip.onDestroy();
            }
            ResetTempStats();
            ResetTurnActions();
            turn = TurnTypes.Enemy;
        }
        if (turn == TurnTypes.Enemy)
        {
            UpdateHandStats(); // Ticks all time dependent card
            turn = TurnTypes.Player;
            turnCounter++;
        }
    }

    public void ResolveActions()
    {
        bool playerFirst = true;

        // Decide Who goes first
        System.Random random = new System.Random();
        if (IsSimulation)
        {
            foreach (CardAction action in enemyTurnActions)
            {
                foreach (SpeedEffect effect in action.GetEffectsByType(typeof(SpeedEffect)))
                {
                    effect.TriggerEffect();
                }
            }

            foreach (CardAction action in playerTurnActions)
            {
                foreach (SpeedEffect effect in action.GetEffectsByType(typeof(SpeedEffect)))
                {
                    effect.TriggerEffect();
                }
            }
            playerFirst =
                (playerShip.speed > enemyShip.speed)
                    ? true
                    : (enemyShip.speed > playerShip.speed)
                        ? false
                        : (random.Next(2) == 0)
                            ? true
                            : false;
            playerShip.ResetSpeed();
            enemyShip.ResetSpeed();
        }
        else
        {
            playerFirst = playerShip.speed >= enemyShip.speed;
        }

        //if (playerFirst){UnityEngine.Debug.Log("Player First");}
        //else{UnityEngine.Debug.Log("Enemy First");}

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
                if (verbose)
                    explanation +=
                        "\nThe "
                        + (room.isPlayer ? "Players " : "Enemys ")
                        + room.roomType.ToString()
                        + "Room is affected by: "
                        + string.Join(
                            " | ",
                            room.effectsApplied.Select(obj => obj.GetType().ToString()).ToList()
                        );
                foreach (CombatEffect effect in effectsCopy)
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
                if (verbose)
                    explanation +=
                        "\n"
                        + action.name
                        + " Was not played because: Disabled "
                        + action.sourceRoom.disabled
                        + " Destroyed "
                        + action.sourceRoom.destroyed
                        + (action.card.turnsUntilReady != 0 ? "Action Not Ready" : "Action Ready");
                continue;
            }
            explanation += "\n" + action.name + " Was played";
            if (action is LaserAction || action is FreeLaserAction || action is MissileAction)
            {
                weaponCalls.Add(() => FireLaserAtTarget(action));
            }
            if (action.effects.Where(obj => obj is DamageEffect).ToList().Count > 0)
            {
                if (verbose)
                    explanation +=
                        " on a room with " + action.affectedRoom.defence.ToString() + " defence";
            }
            action.Activate();
        }
        if (!IsSimulation && verbose)
            UnityEngine.Debug.Log(explanation);

        if (!IsSimulation)
            gameManagerController._StartCoroutine(InvokeWeaponActionsWithDelay(weaponCalls));
    }

    public void EnemyChooseActions()
    {
        //EnemyChooseRandomActions();
        EnemyAIAttackOrRandom();
#if UNITY_EDITOR
        //UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
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
                    foreach (CombatEffect effect in laserCard.cardAction.effects)
                        effect.affectedRoom = target;
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
            List<Room> weaponsRooms = enemyRooms[RoomType.Laser]
                .Where(obj => obj.health > 0)
                .ToList();
            List<Room> reactorRooms = enemyRooms[RoomType.Shield]
                .Where(obj => obj.health > 0)
                .ToList();
            List<Room> shieldRooms = enemyRooms[RoomType.Reactor]
                .Where(obj => obj.health > 0)
                .ToList();

            if (
                random.Next(2) == 0
                && (weaponsRooms.Count > 0 || reactorRooms.Count > 0 || shieldRooms.Count > 0)
            )
            {
                foreach (Card shieldCard in shieldCards)
                {
                    if (shieldCard.CanBeUsed(enemyShip.AP))
                    {
                        Room target;
                        if (weaponsRooms.Count > 0)
                        {
                            target = weaponsRooms[0];
                        }
                        else if (reactorRooms.Count > 0)
                        {
                            target = reactorRooms[0];
                        }
                        else
                        {
                            target = enemyShip.GetRoomList()[0];
                        }
                        shieldCard.cardAction.affectedRoom = target;
                        foreach (CombatEffect effect in shieldCard.cardAction.effects)
                            effect.affectedRoom = target;

                        SubmitCard(shieldCard, false);
                        break;
                    }
                }
            }
            else if (
                enemyRooms.ContainsKey(RoomType.Engine)
                && enemyRooms[RoomType.Engine].Any(item => item.health > 0)
            )
            {
                foreach (Card speedUpCard in speedUpCards)
                {
                    speedUpCard.cardAction.affectedRoom = speedUpCard.cardAction.sourceRoom;
                    ;
                    SubmitCard(speedUpCard, false);
                }
            }
            else
            {
                c += 1;
                if (c > 10)
                {
                    break;
                }
            }
        }
    }

    public void EnemyChooseRandomActions()
    {
        System.Random random = new System.Random();
        List<Card> cards = new List<Card>(enemyHand.GetCards());

        while (enemyShip.AP > 0)
        {
            int index = random.Next(cards.Count);
            Card card = cards[index];
            if (card.CanBeUsed(enemyShip.AP))
            {
                if (
                    card.cardAction.needsTarget && card.cardAction is LaserAction
                    || card.cardAction is MissileAction
                    || card.cardAction is FreeLaserAction
                    || card.cardAction is ShieldPiercerAction
                    || card.cardAction is FirebombAction
                    || card.cardAction is EMPAction
                )
                {
                    List<Room> playerRooms = playerShip.GetRoomList();
                    int roomindex = random.Next(playerRooms.Count);
                    card.cardAction.SetAffectedRoom(playerRooms[roomindex]);
                }
                else
                {
                    card.cardAction.SetAffectedRoom(card.cardAction.sourceRoom);
                }

                SubmitCard(card, false);
                if (card.cardAction.cooldown > 0)
                {
                    cards.Remove(card);
                }
            }
        }
    }

    public void EnemyAIAllOutAttack()
    {
        List<Card> laserCards = enemyHand
            .GetCardsByAction(typeof(LaserAction))
            .Where(obj => obj.IsReady())
            .ToList();
        if (laserCards.Count > 0)
        {
            Card card = laserCards[0];
            while (card.CanBeUsed(enemyShip.AP))
            {
                List<Room> targets = playerRooms[RoomType.Laser]
                    .Where(obj => obj.health > 0)
                    .ToList();
                if (targets.Count == 0)
                {
                    break;
                }

                Room target = targets[0];
                card.cardAction.affectedRoom = target;
                foreach (CombatEffect effect in card.cardAction.effects)
                    effect.affectedRoom = target;

                SubmitCard(card, false);
            }
        }
        else
        {
            EnemyChooseRandomActions();
        }
    }

    public void EnemyAISemiPermanentShieldShip()
    {
        List<Card> SPShield = enemyHand
            .GetCardsByAction(typeof(SemiPermanentShieldAction))
            .Where(obj =>
                !obj.cardAction.sourceRoom.destroyed && !obj.cardAction.sourceRoom.disabled
            )
            .ToList();
        if (SPShield.Count > 0)
        {
            Card shield = SPShield[0];
            shield.cardAction.cooldown = 0;
            shield.cardAction.cost = 0;

            foreach (Room target in enemyShip.GetRoomList())
            {
                shield.cardAction.affectedRoom = target;
                foreach (CombatEffect effect in shield.cardAction.effects)
                    effect.affectedRoom = target;

                SubmitCard(shield, false);
                SubmitCard(shield, false);
                SubmitCard(shield, false);
            }
        }
    }

    public void EnemyAIEMP()
    {
        List<Card> emps = enemyHand
            .GetCardsByAction(typeof(EMPAction))
            .Where(obj =>
                !obj.cardAction.sourceRoom.destroyed && !obj.cardAction.sourceRoom.disabled
            )
            .ToList();
        if (emps.Count > 0)
        {
            Card emp = emps[0];
            emp.turnsUntilReady = 0;
            bool targetFound = false;

            foreach (
                Room target in playerRooms[RoomType.Laser]
                    .Where(obj => !obj.destroyed && !obj.disabled)
                    .ToList()
            )
            {
                emp.cardAction.affectedRoom = target;
                foreach (CombatEffect effect in emp.cardAction.effects)
                    effect.affectedRoom = target;
                SubmitCard(emp, false);
                targetFound = true;
                break;
            }
            if (!targetFound)
            {
                foreach (
                    Room target in playerShip
                        .GetRoomList()
                        .Where(obj => !obj.destroyed && !obj.disabled)
                        .ToList()
                )
                {
                    emp.cardAction.affectedRoom = target;
                    foreach (CombatEffect effect in emp.cardAction.effects)
                        effect.affectedRoom = target;
                    SubmitCard(emp, false);
                    targetFound = true;
                    break;
                }
            }
        }
    }

    public void EnemyAIAttackDefend()
    {
        if (turnCounter % 2 == 0)
        {
            EnemyAIAllOutAttack();
        }
        else
        {
            EnemyAISemiPermanentShieldShip();
        }
    }

    public void EnemyAIAttackOrRandom()
    {
        if (turnCounter % 2 == 0)
        {
            EnemyAIAllOutAttack();
        }
        else
        {
            EnemyChooseRandomActions();
        }
    }

    public void EnemyAIEMPOrFiftyFifty()
    {
        if (turnCounter % 3 == 0)
        {
            EnemyAIEMP();
            EnemyChooseRandomActions();
        }
        else if (turnCounter % 3 == 1)
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
        playerShip.ResetAP(IsSimulation);
        playerShip.ResetSpeed(IsSimulation);
        playerShip.ResetTempRoomStats();

        enemyShip.ResetAP(IsSimulation);
        enemyShip.ResetSpeed(IsSimulation);
        enemyShip.ResetTempRoomStats();
        if (!IsSimulation)
            gameManagerController.ClearIntentLines();
    }

    public void ResetTurnActions()
    {
        if (!IsSimulation)
            gameManagerController.ClearTurnText();

        playerTurnActions.Clear();
        enemyTurnActions.Clear();
    }

    public void RegisterAttackComplete(Room RoomHit, string AttackType)
    {
        if (AttackType == "Laser")
        {
            RoomHit.updateHealthGraphics();
        }
    }

    public void PickCard(Card card)
    {
        gameManagerController.SetCursor();
        selectedCard = card;
        card.cardController.ToggleTransparency(true);

        if (selectedCard.cardAction.cooldown == 0)
        {
            selectedCard.cardController.ToggleTransparency(false);
        }
        if (!card.cardAction.needsTarget)
        {
            RoomClicked(Vector3.zero, card.cardAction.sourceRoom);
        }
    }

    public void RoomClicked(Vector3 targetPosition, Room target)
    {
        selectedCard.cardAction.affectedRoom = target;
        SubmitCard(selectedCard, true);

        selectedCard = null;
        gameManagerController.SetCursor(true);
    }

    public void DeselectCard()
    {
        if (selectedCard != null)
        {
            selectedCard.cardController.ToggleTransparency(false);
            selectedCard = null;
        }
        gameManagerController.SetCursor(true);
    }

    public void ShowPotentialPassiveEffects()
    {
        foreach (Room room in playerShip.GetRoomList())
        {
            foreach (CombatEffect effect in room.effectsApplied)
            {
                effect.ShowPotentialEffect();
            }
        }

        foreach (Room room in enemyShip.GetRoomList())
        {
            foreach (CombatEffect effect in room.effectsApplied)
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

    public void RestartTurn()
    {
        // need a way to reset GUI on rooms as current method will not remove shield no. , perhaps the showPotentialEffect looks through submitted actions?
        playerShip.AdjustAP(playerShip.defaultAP - playerShip.AP, IsSimulation);
        foreach (CardAction action in playerTurnActions)
        {
            if (action.cooldown > 0)
            {
                action.card.cardController.ToggleTransparency(false);
            }

            foreach (CombatEffect effect in action.effects)
            {
                if (effect is DamageEffect)
                { // need to enumerate all effects that implement attack intent
                    DamageEffect dmgEffect = effect as DamageEffect;
                    action.affectedRoom.IncreaseAttackIntent(-dmgEffect.damage);
                }
            }
        }
        playerTurnActions.Clear();
        gameManagerController.playerTurnActionsText.text = "";
    }

    public void UndoAction()
    {
        // need a way to reset GUI on rooms as current method will not remove  shield no. , perhaps the showPotentialEffect looks through submitted actions?

        if (playerTurnActions.Count > 0)
        {
            CardAction lastAction = playerTurnActions[playerTurnActions.Count - 1];
            playerShip.AdjustAP(lastAction.cost, IsSimulation);
            if (playerTurnActions.Count == 1)
            {
                gameManagerController.playerTurnActionsText.text = "";
            }
            else
            {
                gameManagerController.UndoTurnText(lastAction);
            }
            if (lastAction.cooldown > 0)
            {
                lastAction.card.cardController.ToggleTransparency(false);
            }
            foreach (CombatEffect effect in lastAction.effects)
            {
                if (effect is DamageEffect)
                { // need to enumerate all effects that implement attack intent
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
        if (!IsSimulation)
            ShowPotentialEffect(card.cardAction);
        if (isPlayer)
        {
            playerShip.AdjustAP(-action.cost, IsSimulation);
            playerTurnActions.Add(action);
            if (!IsSimulation)
                gameManagerController.AddActionToTurnText(action, true);
        }
        else
        {
            enemyShip.AdjustAP(-action.cost, IsSimulation);
            enemyTurnActions.Add(action);
            if (!IsSimulation)
                gameManagerController.AddActionToTurnText(action, false);
        }
    }

    public void UpdateHandStats()
    {
        //UnityEngine.Debug.Log(string.Join(" & ", playerHand.GetCards().Select(obj=>obj.cardAction.name + ":"+obj.turnsUntilReady.ToString()).ToList()));
        foreach (Card card in playerHand.GetCards())
        {
            if (card.turnsUntilReady > 0)
            {
                card.NextTurn();
            }
        }
        foreach (Card card in enemyHand.GetCards())
        {
            if (card.turnsUntilReady > 0)
            {
                card.NextTurn();
            }
        }
    }

    public void UpdateRoomText()
    {
        foreach (Room room in playerShip.GetRoomList())
        {
            room.UpdateTextGraphics();
        }
        foreach (Room room in enemyShip.GetRoomList())
        {
            room.UpdateTextGraphics();
        }
    }

    public bool IsGameOver()
    {
        return turn == TurnTypes.PlayerWin || turn == TurnTypes.EnemyWin;
    }
}
