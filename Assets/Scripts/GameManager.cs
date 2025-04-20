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
    public EnemyAI enemyAI;
    public SimulationController simulationController;
    public List<CardAction> playerTurnActions = new List<CardAction>();
    public List<CardAction> enemyTurnActions = new List<CardAction>();
    public bool IsSimulation;
    public int turnCounter = 0;
    public List<IntentLine> activeIntentLines = new List<IntentLine>();
    private List<Move> moves;

// #########################################################################################################################################################
// #                                                Game Setup                                                                                             #
// #########################################################################################################################################################


    public IEnumerator StartGame()
    {
        this.playerHand = new Hand();
        this.enemyHand = new Hand();
        this.playerShip = BuildAPlayerShip().spaceship;
        this.enemyShip = BuildAShip().spaceship;
        this.enemyAI = new EnemyAI(playerShip, enemyShip, enemyHand);
        this.simulationController = new SimulationController(this);
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

        FinishTurn();
    }

    public SpaceshipController BuildAShip()
    {
        enemyHand.Clear();

        // Create a new Ship
        SpaceshipController newShip = (SpaceshipController)
            gameManagerController._Instantiate(
                gameManagerController.prefabHolder.enemySpaceshipPrefab,
                new Vector3(4, 11, 5),
                Quaternion.identity
            );
        newShip.gameObject.tag = "enemy";
        newShip.spaceship.ResetAP(IsSimulation);
        newShip.init();

        int numRooms = UnityEngine.Random.Range(4, Mathf.Min(newShip.validRoomPositions.Count, 6));

        float xPos = -3;
        float yPos = 0;

        // Randomly select room types
        List<RoomType> roomTypes = RoomType
            .GetValues(typeof(RoomType))
            .Cast<RoomType>()
            .OrderBy(e => UnityEngine.Random.value)
            .Take(numRooms)
            .ToList();

        if (!roomTypes.Contains(RoomType.Reactor))
        {
            roomTypes.Add(RoomType.Reactor);
            roomTypes.RemoveAt(0);
        }

        for (int i = 0; i < roomTypes.Count; i++)
        {
            if (i < newShip.validRoomPositions.Count)
            {
                xPos = newShip.validRoomPositions[i].x;
                yPos = newShip.validRoomPositions[i].y;
            }

            // Make the room
            newShip.addRoom(roomTypes[i], xPos, yPos);
        }

        return newShip;
    }

    public SpaceshipController BuildAPlayerShip()
    {
        playerHand.Clear();
        // Create a new Ship
        SpaceshipController newShip = (SpaceshipController)
            gameManagerController._Instantiate(
                gameManagerController.prefabHolder.playerSpaceshipPrefab,
                new Vector3(-6, 10, 5),
                Quaternion.identity
            );
        newShip.gameObject.tag = "player";
        newShip.init();
        newShip.spaceship.ResetAP(IsSimulation);

        // Setup Basic Rooms
        newShip.addRoom(RoomType.Reactor, -1.75f, 0);
        newShip.addRoom(RoomType.Laser, 2, 0);
        newShip.addRoom(RoomType.Firebomb, 0, 0);
        newShip.addRoom(RoomType.Engine, -1.75f, 2);
        newShip.addRoom(RoomType.Shield, -1.75f, -2);

        return newShip;
    }

    public void RegisterRoom(Room room, bool isPlayer)
    {
        if (isPlayer)
        {
            List<Card> cards = MakeCards(room.actions);
            AddCardsToHand(cards, isPlayer);
        }
        else
        {
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
                string prefabName = "";
                UnityEngine.Object prefab = null;
                if (action is LaserAction)
                    prefabName = "Laser";
                if (action is FirebombAction)
                    prefabName = "FireBomb";
                if (action is FocusedShieldAction)
                    prefabName = "Shield";
                if (action is SpeedUpAction)
                    prefabName = "SpeedUp";
                if (action is OverdriveAction)
                    prefabName = "Overdrive";

                foreach (PrefabEntry entry in gameManagerController.prefabHolder.prefabList)
                {
                    if (entry.key == prefabName)
                        prefab = entry.prefab;
                }

                if (prefab is null)
                {
                    prefab = gameManagerController.prefabHolder.cardPrefab;
                }

                // _Instantiate a new card prefab & set its parent as the playerHand
                CardController cardController = (CardController)
                    (
                        (GameObject)
                            gameManagerController._Instantiate(
                                prefab,
                                gameManagerController.PlayerCardContainer.transform
                            )
                    ).GetComponent<CardController>();
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

// #########################################################################################################################################################
// #                                                Manage Turn                                                                                            #
// #########################################################################################################################################################

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
            if (enemyShip.rooms[RoomType.Reactor][0].health <= 0)
            {
                enemyShip.onDestroy();
                turn = TurnTypes.PlayerWin;
            }
            else if (playerShip.rooms[RoomType.Reactor][0].health <= 0)
            {
                playerShip.onDestroy();
                turn = TurnTypes.EnemyWin;
            }
        }
        if (turn == TurnTypes.Enemy)
        {
            enemyAI.ChooseActions(SubmitCard);
            ShowPotentialPassiveEffects();
            TickAllCards();
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
            PlayOutActions(playerTurnActions, playerShip.rooms);
            PlayOutActions(enemyTurnActions, enemyShip.rooms);
        }
        else
        {
            PlayOutActions(enemyTurnActions, enemyShip.rooms);
            PlayOutActions(playerTurnActions, playerShip.rooms);
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
                        + (room.isPlayer ? "Players " : "Enemies ")
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
            if (explanation == "\n") {explanation = "Played no cards played this turn";}
            UnityEngine.Debug.Log(explanation);

        if (!IsSimulation)
            gameManagerController._StartCoroutine(InvokeWeaponActionsWithDelay(weaponCalls));
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

    public void TickAllCards()
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
// #########################################################################################################################################################
// #                                                UI Update                                                                                              #
// #########################################################################################################################################################


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

    public void DrawIntentLine(Vector3 startPoint, Vector3 endPoint, float fuzziness = 0)
    {
        return;
        // if (IsSimulation == true)
        // {
        //     return;
        // }
        // float getFuzzy(float fuzziness, float steps = 2)
        // {
        //     float point = UnityEngine.Random.Range(-fuzziness / 2f, fuzziness / 2f);
        //     //float value = Mathf.Round(point * steps) / steps;
        //     return point;
        // }

        // var fuzzyOffset = new Vector3(getFuzzy(fuzziness), getFuzzy(fuzziness), -1);

        // IntentLine intentLine = (IntentLine)
        //     gameManagerController._Instantiate(gameManagerController.prefabHolder.intentLine);
        // intentLine.DrawCurvedLine(startPoint + fuzzyOffset, endPoint + fuzzyOffset);

        // activeIntentLines.Add(intentLine);
    }

    public void FireLaserAtTarget(CardAction action)
    {
        Vector3 targetPosition = action.affectedRoom.parent.transform.position;
        Vector3 origin = action.sourceRoom.parent.transform.position;
        origin.z = origin.z - 1;
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

// #########################################################################################################################################################
// #                                                player controls                                                                                       #
// #########################################################################################################################################################
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

    public void DeselectCard()
    {
        if (selectedCard != null)
        {
            selectedCard.cardController.ToggleTransparency(false);
            selectedCard = null;
        }
        gameManagerController.SetCursor(true);
    }

    public void RoomClicked(Vector3 targetPosition, Room target)
    {
        selectedCard.cardAction.affectedRoom = target;
        SubmitCard(selectedCard, true);

        selectedCard = null;
        gameManagerController.SetCursor(true);
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

}
