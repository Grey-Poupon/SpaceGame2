using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using UnityEngine;

public class EnemyAI
{
    private Spaceship targetShip;
    private Spaceship myShip;
    private Hand myHand;
    private Action<Card, bool> _SubmitCard;

    public EnemyAI(Spaceship targetShip, Spaceship myShip, Hand myHand)
    {
        this.myShip = myShip;
        this.targetShip = targetShip;
        this.myHand = myHand;
    }

    public void ChooseActions(Action<Card, bool> SubmitCard)
    {
        // Using SubmitCard here because it calculates AP changes and other interactions for us
        this._SubmitCard = SubmitCard;
        AIAttackOrRandom();
    }

    public void AIOldStyle()
    {
        System.Random random = new System.Random();

        // If i have a reactor Action take it
        List<Card> APUps = myHand.GetCardsByAction(typeof(OverdriveAction));

        if (APUps.Count > 0)
        {
            foreach (Card APCard in APUps)
            {
                if (APCard.CanBeUsed(myShip.AP))
                {
                    APCard.cardAction.affectedRoom = APCard.cardAction.sourceRoom;
                    _SubmitCard?.Invoke(APCard, false);
                }
            }
        }
        List<Card> laserCards = myHand.GetCardsByAction(typeof(LaserAction));
        while (myShip.AP > 2 && laserCards.Count > 0)
        {
            foreach (Card laserCard in laserCards)
            {
                if (laserCard.CanBeUsed(myShip.AP))
                {
                    Room target = targetShip.rooms[RoomType.Reactor][0];
                    laserCard.cardAction.affectedRoom = target;
                    foreach (CombatEffect effect in laserCard.cardAction.effects)
                        effect.affectedRoom = target;
                    _SubmitCard?.Invoke(laserCard, false);
                    break;
                }
            }
        }
        int c = 0;
        List<Card> shieldCards = myHand.GetCardsByAction(typeof(FocusedShieldAction));
        List<Card> speedUpCards = myHand.GetCardsByAction(typeof(SpeedUpAction));
        while (myShip.AP > 0 && (shieldCards.Count + speedUpCards.Count) > 0)
        {
            List<Room> weaponsRooms = myShip
                .rooms[RoomType.Laser]
                .Where(obj => obj.health > 0)
                .ToList();
            List<Room> reactorRooms = myShip
                .rooms[RoomType.Shield]
                .Where(obj => obj.health > 0)
                .ToList();
            List<Room> shieldRooms = myShip
                .rooms[RoomType.Reactor]
                .Where(obj => obj.health > 0)
                .ToList();

            if (
                UnityEngine.Random.Range(0, 2) == 0
                && (weaponsRooms.Count > 0 || reactorRooms.Count > 0 || shieldRooms.Count > 0)
            )
            {
                foreach (Card shieldCard in shieldCards)
                {
                    if (shieldCard.CanBeUsed(myShip.AP))
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
                            target = myShip.GetRoomList()[0];
                        }
                        shieldCard.cardAction.affectedRoom = target;
                        foreach (CombatEffect effect in shieldCard.cardAction.effects)
                            effect.affectedRoom = target;

                        _SubmitCard?.Invoke(shieldCard, false);
                        break;
                    }
                }
            }
            else if (
                myShip.rooms.ContainsKey(RoomType.Engine)
                && myShip.rooms[RoomType.Engine].Any(item => item.health > 0)
            )
            {
                foreach (Card speedUpCard in speedUpCards)
                {
                    speedUpCard.cardAction.affectedRoom = speedUpCard.cardAction.sourceRoom;
                    _SubmitCard?.Invoke(speedUpCard, false);
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

    public void ChooseRandomActions()
    {
        System.Random random = new System.Random();
        List<Card> cards = new List<Card>(myHand.GetCards());

        while (myShip.AP > 0)
        {
            int index = UnityEngine.Random.Range(0, cards.Count);
            Card card = cards[index];
            if (card.CanBeUsed(myShip.AP))
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
                    int roomindex = UnityEngine.Random.Range(0, targetShip.GetRoomList().Count);
                    card.cardAction.SetAffectedRoom(targetShip.GetRoomList()[roomindex]);
                }
                else
                {
                    card.cardAction.SetAffectedRoom(card.cardAction.sourceRoom);
                }

                _SubmitCard?.Invoke(card, false);
                if (card.cardAction.cooldown > 0)
                {
                    cards.Remove(card);
                }
            }
        }
    }

    public void AIAllOutAttack()
    {
        List<Card> laserCards = myHand
            .GetCardsByAction(typeof(LaserAction))
            .Where(obj => obj.IsReady())
            .ToList();
        if (laserCards.Count > 0)
        {
            Card card = laserCards[0];
            while (card.CanBeUsed(myShip.AP))
            {
                List<Room> targets = targetShip
                    .rooms[RoomType.Laser]
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

                _SubmitCard?.Invoke(card, false);
            }
        }
        else
        {
            ChooseRandomActions();
        }
    }

    public void AISemiPermanentShieldShip()
    {
        List<Card> SPShield = myHand
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

            foreach (Room target in myShip.GetRoomList())
            {
                shield.cardAction.affectedRoom = target;
                foreach (CombatEffect effect in shield.cardAction.effects)
                    effect.affectedRoom = target;

                _SubmitCard?.Invoke(shield, false);
                _SubmitCard?.Invoke(shield, false);
                _SubmitCard?.Invoke(shield, false);
            }
        }
    }

    public void AIEMP()
    {
        List<Card> emps = myHand
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
                Room target in targetShip
                    .rooms[RoomType.Laser]
                    .Where(obj => !obj.destroyed && !obj.disabled)
                    .ToList()
            )
            {
                emp.cardAction.affectedRoom = target;
                foreach (CombatEffect effect in emp.cardAction.effects)
                    effect.affectedRoom = target;
                _SubmitCard?.Invoke(emp, false);
                targetFound = true;
                break;
            }
            if (!targetFound)
            {
                foreach (
                    Room target in targetShip
                        .GetRoomList()
                        .Where(obj => !obj.destroyed && !obj.disabled)
                        .ToList()
                )
                {
                    emp.cardAction.affectedRoom = target;
                    foreach (CombatEffect effect in emp.cardAction.effects)
                        effect.affectedRoom = target;
                    _SubmitCard?.Invoke(emp, false);
                    targetFound = true;
                    break;
                }
            }
        }
    }

    public void AIAttackDefend()
    {
        if (GameManagerController.Instance.turnCounter % 2 == 0)
        {
            AIAllOutAttack();
        }
        else
        {
            AISemiPermanentShieldShip();
        }
    }

    public void AIAttackOrRandom()
    {
        if (GameManagerController.Instance.turnCounter % 2 == 0)
        {
            AIAllOutAttack();
        }
        else
        {
            ChooseRandomActions();
        }
    }

    public void AIEMPOrFiftyFifty()
    {
        if (GameManagerController.Instance.turnCounter % 3 == 0)
        {
            AIEMP();
            ChooseRandomActions();
        }
        else if (GameManagerController.Instance.turnCounter % 3 == 1)
        {
            AIAllOutAttack();
        }
        else
        {
            AISemiPermanentShieldShip();
        }
    }
}
