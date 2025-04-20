using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

public class SimulationController
{
    public GameManager state;
    public List<Move> moves;

    public SimulationController(GameManager state)
    {
        this.state = state;
    }

    public GameManager Clone()
    {
        GameManager clone = new GameManager();

        clone.turn = TurnTypes.Enemy;
        clone.playerHand = state.playerHand.Clone();
        clone.enemyHand = state.enemyHand.Clone();
        clone.playerShip = state.playerShip.Clone();
        clone.enemyShip = state.enemyShip.Clone();
        clone.IsSimulation = state.IsSimulation;
        clone.turnCounter = state.turnCounter;

        clone.playerTurnActions = new List<CardAction>();
        clone.enemyTurnActions = new List<CardAction>();

        foreach (CardAction cardAction in state.playerTurnActions)
        {
            CardAction clonedAction = cardAction.Clone();
            clone.playerTurnActions.Add(clonedAction);
        }

        foreach (CardAction cardAction in state.enemyTurnActions)
        {
            CardAction clonedAction = cardAction.Clone();
            clone.enemyTurnActions.Add(clonedAction);
        }
        return clone;
    }

    public List<Move> GetMoves()
    {
        if (moves is null)
        {
            moves = _GenerateMoves().ToList();
        }
        return moves;
    }

    public Move GetRandomMove()
    {
        if (state.turn == TurnTypes.Player)
        {
            return GenerateRandomMove(
                state.playerShip.AP,
                state.playerHand.GetCards(),
                state.playerShip.rooms.Values.SelectMany(x => x).ToList(),
                state.enemyShip.rooms.Values.SelectMany(x => x).ToList(),
                true
            );
        }
        else
        {
            return GenerateRandomMove(
                state.enemyShip.AP,
                state.enemyHand.GetCards(),
                state.enemyShip.rooms.Values.SelectMany(x => x).ToList(),
                state.playerShip.rooms.Values.SelectMany(x => x).ToList(),
                false
            );
        }
    }

    public void DoMove(Move move)
    {
        foreach (MinCardAction m_card in move.cards)
        {
            state.SubmitCard(new Card(m_card.ca), state.turn == TurnTypes.Player);
        }
        if (state.IsSimulation)
        {
            SimulateEndTurn();
            moves = _GenerateMoves().ToList();
        }
    }

    public Move[] _GenerateMoves()
    {
        if (state.turn == TurnTypes.Player)
        {
            return GenerateMoves(state.playerShip.AP, state.playerHand.GetCards(), true);
        }
        else
        {
            return GenerateMoves(state.enemyShip.AP, state.enemyHand.GetCards(), false);
        }
    }

    public float GetResult(int player)
    {
        if (state.playerShip.rooms[RoomType.Reactor].Sum(room => room.health) <= 0)
        {
            return player == 1 ? 0 : 1;
        }
        if (state.enemyShip.rooms[RoomType.Reactor].Sum(room => room.health) <= 0)
        {
            return player == 1 ? 1 : 0;
        }
        if (player == 0)
        {
            float currentHealth = state
                .playerShip.rooms.Values.SelectMany(roomList => roomList)
                .Sum(room => room.health);
            float maxHealth = state
                .playerShip.rooms.Values.SelectMany(roomList => roomList)
                .Sum(room => room.maxHealth);
            return 1 - currentHealth / maxHealth;
        }
        else
        {
            float currentHealth = state
                .enemyShip.rooms.Values.SelectMany(roomList => roomList)
                .Sum(room => room.health);
            float maxHealth = state
                .enemyShip.rooms.Values.SelectMany(roomList => roomList)
                .Sum(room => room.maxHealth);
            return 1 - currentHealth / maxHealth;
        }
    }

    public void MCTSEnemyTurn()
    {
        UnityEngine.Debug.Log(" - - - ENEMY - - - - ");
        GameManager gameState = Clone();
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
                turn.Add(card.cardAction.QuickClone(state));
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
            if (totalDamage > highestDamage)
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
                        turn.Add(t_ca.QuickClone(state));
                    }
                }
            }

            // Roll on table
            string group = rollTable[UnityEngine.Random.Range(0, rollTable.Count - 1)];
            MinCardAction action = cardActionGroups[group]
                [UnityEngine.Random.Range(0, cardActionGroups[group].Count)]
                .QuickClone(state);

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

        List<Room> enemyRoomList = state.enemyShip.rooms.Values.SelectMany(x => x).ToList(); // make a local reference so we don't have to keep calling functions
        List<Room> playerRoomList = state.playerShip.rooms.Values.SelectMany(x => x).ToList();
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

                    MinCardAction m_cardAction = t_cardAction.QuickClone(state);
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
                foreach (Room room in state.enemyShip.GetRoomList())
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
                foreach (Room room in state.playerShip.GetRoomList())
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

    public void SimulateEndTurn()
    {
        if (state.turn == TurnTypes.Player)
        {
            state.turn = TurnTypes.Resolve;
        }
        if (state.turn == TurnTypes.Resolve)
        {
            state.ResolveActions();
            if (state.enemyShip.rooms[RoomType.Reactor][0].health <= 0)
            {
                state.enemyShip.onDestroy();
            }
            state.ResetTempStats();
            state.ResetTurnActions();
            state.turn = TurnTypes.Enemy;
        }
        if (state.turn == TurnTypes.Enemy)
        {
            state.TickAllCards(); // Ticks all time dependent card
            state.turn = TurnTypes.Player;
            state.turnCounter++;
        }
    }
}
