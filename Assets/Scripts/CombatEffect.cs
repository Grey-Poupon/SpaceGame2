using System.ComponentModel;
using System.Collections.Generic;
using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
public abstract class CombatEffect
{
    public float duration;
    public bool affectsSelf;
    public Room affectedRoom;
    public CardAction action;

    protected CombatEffect()
    {

    }

    public void ApplyEffect(Room affectedRoom)
    {
        CombatEffect clone = this.Clone();
        clone.affectedRoom = affectedRoom;
        affectedRoom.effectsApplied.Add(clone);
        clone.FirstEffect();

    }
    public void Activate()
    {
        TriggerEffect();
        duration -= 1;
        
        if (duration < 1)
        {
            LastEffect();
            affectedRoom.effectsApplied.Remove(this);
        }

    }
    public CombatEffect Clone()
    {
        CombatEffect clone = (CombatEffect)Activator.CreateInstance(this.GetType());
        Type sourceType = this.GetType();
        Type targetType = clone.GetType();

        foreach (FieldInfo sourceField in sourceType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
        {
            FieldInfo targetField = targetType.GetField(sourceField.Name);
            if (targetField != null && targetField.FieldType == sourceField.FieldType)
            {
                object value = sourceField.GetValue(this);
                targetField.SetValue(clone, value);
            }
        }
        return clone;
    }
    public abstract void TriggerEffect();

    public virtual void LastEffect()
    {
        // Pass
    }
    public virtual void FirstEffect()
    {
        Activate();
    }
    public virtual void ShowPotentialEffect()
    {
        // Pass
    }
}


public class ShieldEffect : CombatEffect
{
    public float increase;
    public ShieldEffect(float duration, bool affectsSelf, float increase)
    {
        this.affectsSelf = affectsSelf;
        this.duration = duration;
        this.increase = increase;
    }
    public override void TriggerEffect()
    {
        affectedRoom.IncreaseDefence(increase);
    }
    
    public override void ShowPotentialEffect()
    {
        action.affectedRoom.IncreaseDefence(increase);
    }
    public ShieldEffect(){}
}

public class GeneralShieldEffect : CombatEffect
{
    public float increase;
    public GeneralShieldEffect(float duration, bool affectsSelf, float increase)
    {
        this.affectsSelf = affectsSelf;
        this.duration = duration;
        this.increase = increase;
    }
    public override void TriggerEffect()
    {
        foreach(Room room in action.sourceRoom.getParentShip().GetRoomList())
        {
            room.IncreaseDefence(increase);
        }
    }
    
    public override void ShowPotentialEffect()
    {
        foreach(Room room in action.sourceRoom.getParentShip().GetRoomList())
        {
            room.IncreaseDefence(increase);
        }
    }
    public GeneralShieldEffect(){}
}

public class DamageEffect : CombatEffect
{
    public float damage;
    public DamageEffect(float duration, bool affectsSelf, float damage)
    {
        this.affectsSelf = affectsSelf;
        this.duration = duration;
        this.damage = damage;
    }
    public override void TriggerEffect()
    {
        affectedRoom.takeDamage(damage);
        affectedRoom.updateHealthGraphics();
    }
    public override void ShowPotentialEffect()
    {
        action.affectedRoom.IncreaseAttackIntent(damage);
        // if (!affectsSelf)
        // {
        //     GameManager.Instance.DrawIntentLine(
        //         action.sourceRoom.parent.transform.position,
        //         action.affectedRoom.parent.transform.position,
        //         0.4f);   
        // }
    }
    public DamageEffect(){}
}

public class ShieldOnlyDamageEffect : CombatEffect
{
    public float damage;
    public ShieldOnlyDamageEffect(float duration, bool affectsSelf, float damage)
    {
        this.affectsSelf = affectsSelf;
        this.duration = duration;
        this.damage = damage;
    }
    public override void TriggerEffect()
    {
        damage = damage > affectedRoom.defence ? affectedRoom.defence : damage;
        affectedRoom.takeDamage(damage);
        affectedRoom.updateHealthGraphics();
    }
    
    public override void ShowPotentialEffect()
    {
        damage = damage > affectedRoom.defence ? affectedRoom.defence : damage;
        affectedRoom.IncreaseAttackIntent(damage);
    }
    public ShieldOnlyDamageEffect(){}
}

public class FreeLaserEffect : CombatEffect
{
    public float damage;
    public FreeLaserEffect(float duration, bool affectsSelf)
    {
        this.affectsSelf = affectsSelf;
        this.duration = duration;
    }

    public override void TriggerEffect()
    {
        // AffectsSelf isPlayer AffectsPlayer
        //     0           0          1
        //     0           1          0
        //     1           0          0 
        //     1           1          1 
        bool affectsPlayer = this.action.sourceRoom.isPlayer;
        CardAction action = new FreeLaserAction();
        action.sourceRoom = affectedRoom.getParentShip().GetRooms()[RoomType.Laser][0];
        List<Card> cards = GameManager.Instance.MakeCards(new List<CardAction>{action},affectsPlayer);
        GameManager.Instance.AddCardsToHand(cards, affectsPlayer);
    }
    
    public override void ShowPotentialEffect()
    {
        // Pass
    }
    public FreeLaserEffect(){}
}

public class OnFireEffect : CombatEffect
{
    public float damage;
    public float startDuration;
    public OnFireEffect(float duration, bool affectsSelf, float damage)
    {
        this.affectsSelf = affectsSelf;
        this.duration = duration;
        this.startDuration = duration;
        this.damage = damage;
    }

    public override void TriggerEffect(){}
    public override void FirstEffect()
    {
        // AffectsSelf isPlayer AffectsPlayer
        //     0           0          1
        //     0           1          0
        //     1           0          0 
        //     1           1          1 
        bool affectsPlayer = this.affectsSelf == this.action.sourceRoom.isPlayer;
        CardAction action = new StopFireAction();
        action.sourceRoom = affectedRoom;
        List<Card> cards = GameManager.Instance.MakeCards(new List<CardAction>{action});
        GameManager.Instance.AddCardsToHand(cards, affectsPlayer);

        affectedRoom.takeDamage(damage);
        affectedRoom.updateHealthGraphics();
        affectedRoom.UpdateTextGraphics();
        affectedRoom.SetOnFireIcon(true);
    }
    
    public override void LastEffect()
    {
        affectedRoom.SetOnFireIcon(false);
    }

    public override void ShowPotentialEffect()
    {
        // Pass
    }
    public OnFireEffect(){}
}

public class StopFireEffect : CombatEffect
{
    public StopFireEffect()
    {
        this.duration = 1;
    }

    public override void TriggerEffect()
    {
        OnFireEffect onFireEffect = affectedRoom.effectsApplied.FirstOrDefault(item => item is OnFireEffect) as OnFireEffect;
        if (onFireEffect != null)
        {
            affectedRoom.effectsApplied.Remove(onFireEffect);
            affectedRoom.SetOnFireIcon(false);
        }
    }
}

public class APEffect : CombatEffect
{
    public float change;
    public APEffect(float duration, bool affectsSelf, float change)
    {
        this.affectsSelf = affectsSelf;
        this.duration = duration;
        this.change = change;
    }

    public override void TriggerEffect()
    {
        if (affectsSelf == action.sourceRoom.isPlayer) {GameManager.Instance.playerShip.AdjustAP(change);}
        else {GameManager.Instance.enemyShip.AdjustAP(change);}
    }
    
    public override void ShowPotentialEffect()
    {
        if (affectsSelf == action.sourceRoom.isPlayer) {GameManager.Instance.playerShip.AdjustAP(change);}
        else {GameManager.Instance.enemyShip.AdjustAP(change);}
    }
    public APEffect(){}
}

public class SpeedEffect : CombatEffect
{
    public float change;
    public SpeedEffect(float duration, bool affectsSelf, float change)
    {
        this.affectsSelf = affectsSelf;
        this.duration = duration;
        this.change = change;
    }

    public override void TriggerEffect()
    {
        if (affectsSelf == action.sourceRoom.isPlayer) {GameManager.Instance.playerShip.AdjustSpeed(change);}
        else {GameManager.Instance.enemyShip.AdjustSpeed(change);}
    }
    
    public override void ShowPotentialEffect()
    {
        if (affectsSelf == action.sourceRoom.isPlayer) {GameManager.Instance.playerShip.AdjustSpeed(change);}
        else {GameManager.Instance.enemyShip.AdjustSpeed(change);}
    }
    public SpeedEffect(){}
}

public class ChargeBatteriesEffect : CombatEffect
{
    public float change;
    public ChargeBatteriesEffect(float duration, bool affectsSelf, float change)
    {
        this.affectsSelf = affectsSelf;
        this.duration = duration;
        this.change = change;
    }

    public override void TriggerEffect()
    {
        bool affectsPlayer = this.affectsSelf == this.action.sourceRoom.isPlayer;
        CardAction action = new DischargeChargeBatteriesAction();
        action.sourceRoom = this.action.sourceRoom;
        List<Card> cards = GameManager.Instance.MakeCards(new List<CardAction>{action});
        GameManager.Instance.AddCardsToHand(cards, affectsPlayer);
    }
    
    public override void ShowPotentialEffect()
    {
        // Pass
    }
    public ChargeBatteriesEffect(){}
}

public class DisableRoomEffect : CombatEffect
{
    public DisableRoomEffect(float duration, bool affectsSelf)
    {
        this.affectsSelf = affectsSelf;
        this.duration = duration;
    }
    public override void TriggerEffect()
    {
        // pass
    }

    public override void FirstEffect()
    {  
        affectedRoom.disabled = true;
        foreach (CardAction action in affectedRoom.actions)
        {
            if (action.card.cardController) action.card.cardController.gameObject.SetActive(false);
        }
    }

    public override void LastEffect()
    {
        affectedRoom.disabled = false;
        foreach (CardAction action in affectedRoom.actions)
        {
            if (action.card.cardController) action.card.cardController.gameObject.SetActive(true);
        }
    }
    
    public override void ShowPotentialEffect()
    {
        // Pass
    }
    public DisableRoomEffect(){}
}