using System.ComponentModel;
using System.Collections.Generic;
using System;
using System.Linq;
public abstract class CombatEffect
{
    public float duration;
    public bool affectsSelf;
    public Room affectedRoom;
    public void Activate(Room affectedRoom)
    {
        this.affectedRoom = affectedRoom;
        TriggerEffect();
        duration -= 1;
        if (duration < 1)
        {
            FinalEffect();
            affectedRoom.effectsApplied.Remove(this);
        }

    }

    public abstract void TriggerEffect();

    public virtual void FinalEffect()
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
        affectedRoom.increaseDefence(increase);
    }

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
        foreach(Room room in affectedRoom.getParentShip().getRooms().Values.SelectMany(x => x).ToList())
        {
            affectedRoom.increaseDefence(increase);
        }
    }

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
    }

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
    }

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
        bool affectsPlayer = this.affectsSelf == affectedRoom.isPlayer;
        CardAction action = new FreeLaserAction();
        action.sourceRoom = affectedRoom;
        List<Card> cards = GameManager.Instance.MakeCards(new List<CardAction>{action});
        GameManager.Instance.AddCardsToHand(cards, affectsPlayer);

        affectedRoom.takeDamage(damage);
    }
}
public class OnFireEffect : CombatEffect
{
    public float damage;
    public OnFireEffect(float duration, bool affectsSelf, float damage)
    {
        this.affectsSelf = affectsSelf;
        this.duration = duration;
        this.damage = damage;
    }

    public override void TriggerEffect()
    {
        // AffectsSelf isPlayer AffectsPlayer
        //     0           0          1
        //     0           1          0
        //     1           0          0 
        //     1           1          1 
        bool affectsPlayer = this.affectsSelf == affectedRoom.isPlayer;
        CardAction action = new StopFireAction();
        action.sourceRoom = affectedRoom;
        List<Card> cards = GameManager.Instance.MakeCards(new List<CardAction>{action});
        GameManager.Instance.AddCardsToHand(cards, affectsPlayer);

        affectedRoom.takeDamage(damage);
    }
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
        if (affectsSelf) {affectedRoom.getParentShip().AdjustAP(change);}
        else {affectedRoom.getParentShip(true).AdjustAP(change);}
    }
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
        if (affectsSelf) {affectedRoom.getParentShip().AdjustSpeed(change);}
        else {affectedRoom.getParentShip(true).AdjustSpeed(change);}
    }
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
        bool affectsPlayer = this.affectsSelf == affectedRoom.isPlayer;
        CardAction action = new DischargeChargeBatteriesAction();
        action.sourceRoom = affectedRoom;
        List<Card> cards = GameManager.Instance.MakeCards(new List<CardAction>{action});
        GameManager.Instance.AddCardsToHand(cards, affectsPlayer);
    }
}

public class DisableRoomEffect : CombatEffect
{
    public float startDuration;
    public DisableRoomEffect(float duration, bool affectsSelf)
    {
        this.affectsSelf = affectsSelf;
        this.duration = duration;
        this.startDuration = duration;
    }

    public override void TriggerEffect()
    {  
        if (duration < startDuration)
        {
            affectedRoom.disabled = true;
        }
    }

    public override void FinalEffect()
    {
        affectedRoom.disabled = false;
    }
}