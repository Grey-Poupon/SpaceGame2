using System.Collections.Generic;
using System;

public abstract class CardAction{ 

    private Dictionary<Type, List<CombatEffect>> effectsByType;
    public List<CombatEffect> effects;
    public Room sourceRoom;
    public Room affectedRoom;
    public string name;
    public float cooldown;
    public float cost;
    public float turnsUntilReady = 0;
    public string description;

    public void Activate()
    {
        turnsUntilReady = cooldown;

        foreach (CombatEffect effect in effects)
        {
            effect.Activate(affectedRoom);
        }
    }

    public List<CombatEffect> getEffectsByType(Type type)
    {
        if (this.effectsByType == null){CreateEffectLookups();}
        if (!this.effectsByType.ContainsKey(type))
        {
            this.effectsByType[type] = new List<CombatEffect>();
        }
        return this.effectsByType[type];
    }
    public void CreateEffectLookups()
    {
        this.effectsByType = new Dictionary<Type, List<CombatEffect>>();
        foreach (CombatEffect effect in effects)
        {
            if (!this.effectsByType.ContainsKey(effect.GetType())) { this.effectsByType[effect.GetType()] = new List<CombatEffect>();}

            this.effectsByType[effect.GetType()].Add(effect);
        }
    }
    public bool isReady()
    {
        return turnsUntilReady == 0 && sourceRoom.health > 0;
    }
    public bool CanBeUsed(float AP)
    {
        return isReady() && cost <= AP;
    }
}

// +1 dmg
public class LaserAction : CardAction
{
    public LaserAction()
    {
        this.effects = new List<CombatEffect>{new DamageEffect(1, false, 1)};
        this.name = "Laser";
        this.cooldown = 0;
        this.cost = 1;
        this.description = "";
    }
}

// +2 dmg (cooldown)
public class MissileAction : CardAction
{
    public MissileAction()
    {
        this.effects = new List<CombatEffect>{new DamageEffect(1, false, 2)};
        this.name = "Missile";
        this.cooldown = 3;
        this.cost = 1;
        this.description = "";
    }
}

// (add extinguish card to enemy hand +1 dmg if not spent and potential for spread)
public class FirebombAction : CardAction
{
    public FirebombAction()
    {
        this.effects = new List<CombatEffect>{new OnFireEffect(1, false, 1)};
        this.name = "Firebomb";
        this.cooldown = 1;
        this.cost = 1;
        this.description = "";
    }
}

// piercer (extra dmg to shields)
public class ShieldPiercerAction : CardAction
{
    public ShieldPiercerAction()
    {
        this.effects = new List<CombatEffect>{new ShieldOnlyDamageEffect(1, false, 3)};
        this.name = "ShieldPiercer";
        this.cooldown = 0;
        this.cost = 2;
        this.description = "3 Damage Shields Only";
    }
}


public class FocusedShieldAction : CardAction
{
    public FocusedShieldAction()
    {
        this.effects = new List<CombatEffect>{new ShieldEffect(2, true, 1)};
        this.name = "FocusedShield";
        this.cooldown = 0;
        this.cost = 1;
        this.description = "";
    }
}
public class GeneralShieldAction : CardAction
{
    public GeneralShieldAction()
    {
        this.effects = new List<CombatEffect>{new GeneralShieldEffect(2, true, 1)};
        this.name = "GeneralShield";
        this.cooldown = 2;
        this.cost = 1;
        this.description = "";
    }
}
public class BigBoyShieldAction : CardAction
{
    public BigBoyShieldAction()
    {
        this.effects = new List<CombatEffect>{new ShieldEffect(2, true, 2)};
        this.name = "BigBoyShield";
        this.cooldown = 2;
        this.cost = 1;
        this.description = "";
    }
}
public class SemiPermanentShieldAction : CardAction
{
    public SemiPermanentShieldAction()
    {
        this.effects = new List<CombatEffect>{new ShieldEffect(99, true, 1)};
        this.name = "SemiPermanentShield";
        this.cooldown = 1;
        this.cost = 1;
        this.description = "";
    }
}
	
    
public class SpeedUpAction : CardAction
{
    public SpeedUpAction()
    {
        this.effects = new List<CombatEffect>{new SpeedEffect(1, true, 1)};
        this.name = "SpeedUp";
        this.cooldown = 0;
        this.cost = 1;
        this.description = "";
    }
}
// (cooldown)
public class BigBoySpeedUpAction : CardAction
{
    public BigBoySpeedUpAction()
    {
        this.effects = new List<CombatEffect>{new SpeedEffect(1, true, 2)};
        this.name = "BigBoySpeedUp";
        this.cooldown = 2;
        this.cost = 1;
        this.description = "";
    }
}
// (Immunity Frames)
public class EvasiveManeouvreAction : CardAction
{
    public EvasiveManeouvreAction()
    {
        this.effects = new List<CombatEffect>{new GeneralShieldEffect(1, true, 99)};
        this.name = "EvasiveManeouvre";
        this.cooldown = 3;
        this.cost = 3;
        this.description = "";
    }
}
// (Dmg room for extra speed)
public class OverHeatAction : CardAction
{
    public OverHeatAction()
    {
        this.effects = new List<CombatEffect>{new DamageEffect(1, true, 1), new SpeedEffect(1, true, 1)};
        this.name = "OverHeat";
        this.cooldown = 0;
        this.cost = 0;
        this.description = "";
    }
}

// +1 AP (cooldown)
public class OverdriveAction : CardAction
{
    public OverdriveAction()
    {
        this.effects = new List<CombatEffect>{new APEffect(1, true, 1)};
        this.name = "Overdrive";
        this.cooldown = 3;
        this.cost = 0;
        this.description = "";
    }
}
// energy weapons (cooldown)
public class BuffEnergyWeaponAction : CardAction
{
    public BuffEnergyWeaponAction()
    {
        this.effects = new List<CombatEffect>{new FreeLaserEffect(99, true)};
        this.name = "BuffEnergyWeapon";
        this.cooldown = 2;
        this.cost = 3;
        this.description = "";
    }
}
public class FreeLaserAction : CardAction
{
    public FreeLaserAction()
    {
        this.effects = new List<CombatEffect>{new DamageEffect(1, false, 1)};
        this.name = "Free Laser";
        this.cooldown = 2;
        this.cost = 3;
        this.description = "";
    }
}
// (-AP +AP card)
public class ChargeBatteriesAction : CardAction
{
    public ChargeBatteriesAction()
    {
        this.effects = new List<CombatEffect>{new ChargeBatteriesEffect(1, true, 1)};
        this.name = "Charge Batteries";
        this.cooldown = 1;
        this.cost = 1;
        this.description = "";
    }
}
public class DischargeChargeBatteriesAction : CardAction
{
    public DischargeChargeBatteriesAction()
    {
        this.effects = new List<CombatEffect>{new APEffect(1, true, 1)};
        this.name = "Discharge Batteries";
        this.cooldown = 99;
        this.cost = 0;
        this.description = "";
    }
}
// (both players skip a turn)
public class EMPAction : CardAction
{
    public EMPAction()
    {
        this.effects = new List<CombatEffect>{new DisableRoomEffect(2, false)};
        this.name = "Disable room NEXT turn";
        this.cooldown = 3;
        this.cost = 2;
        this.description = "";
    }
}

public class StopFireAction : CardAction
{
    public StopFireAction()
    {
        this.effects = new List<CombatEffect>{new StopFireEffect()};
        this.name = "StopFire";
        this.cooldown = 1;
        this.cost = 1;
        this.description = "";
    }
}