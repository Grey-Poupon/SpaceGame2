using System.Threading;
using System.Collections.Generic;
using System;
using System.Linq;


    public class MinCardAction{

        public CardAction ca;
        public Room targetRoom;

        public MinCardAction(CardAction ca, GameManager state){
            this.ca = ca;
            targetRoom = ca.affectedRoom;
            ca.state = state;
        } 

    }
public abstract class CardAction{ 

    private Dictionary<Type, List<CombatEffect>> effectsByType;
    public List<CombatEffect> effects;
    public Room sourceRoom;
    public Room affectedRoom;
    public string name;
    public int cooldown;
    public float cost;
    public string description;
    public bool needsTarget;
    public GameManager state;
    public bool offensive =false;
    public Card card;
    public string group;




    public void Activate(){
        card.turnsUntilReady = cooldown;       
        foreach (CombatEffect effect in effects){
            effect.ApplyEffect(affectedRoom);
        }
    }

    public void SetAffectedRoom(Room target){
            affectedRoom = target;
            foreach (CombatEffect effect in effects) effect.affectedRoom = target;
    }

    public List<CombatEffect> GetEffectsByType(Type type){
        if (this.effectsByType == null){CreateEffectLookups();}
        if (!this.effectsByType.ContainsKey(type)){
            this.effectsByType[type] = new List<CombatEffect>();
        }
        return this.effectsByType[type];
    }

    public void CreateEffectLookups(){
        this.effectsByType = new Dictionary<Type, List<CombatEffect>>();
        foreach (CombatEffect effect in effects){
            if (!this.effectsByType.ContainsKey(effect.GetType())) { this.effectsByType[effect.GetType()] = new List<CombatEffect>();}

            this.effectsByType[effect.GetType()].Add(effect);
        }
    }
    public bool IsReady(){
        return !sourceRoom.destroyed && !sourceRoom.disabled && card.turnsUntilReady == 0;
    }
    public bool CanBeUsed(float AP){
        return IsReady() && cost <= AP;
    }

    public CardAction Clone(){
        
        CardAction clone = (CardAction)Activator.CreateInstance(this.GetType());
        clone.effects = this.effects.Select(eff => eff.Clone()).ToList();
        clone.name = this.name;
        clone.cooldown = this.cooldown;
        clone.card = this.card;
        clone.cost = this.cost;
        clone.offensive = this.offensive;
        clone.description = this.description;
        clone.sourceRoom = this.sourceRoom;
        clone.affectedRoom = this.affectedRoom;
        clone.needsTarget = this.needsTarget;
        clone.card.turnsUntilReady = this.card.turnsUntilReady;
        clone.CreateEffectLookups();
        clone.AttachToEffect();
        return clone;
    } 
        public MinCardAction QuickClone(GameManager state){
        MinCardAction minCardAction = new MinCardAction(this, state);
        return minCardAction;
    }







    public static void CloneFrom(CardAction clone, CardAction toClone){}

    public void AttachToEffect(){
        foreach(CombatEffect effect in effects){
            effect.action = this;
        }
    }
}

// +1 dmg
public class LaserAction : CardAction
{
    public LaserAction(){
        this.effects = new List<CombatEffect>{new DamageEffect(1, false, 1)};
        AttachToEffect();
        this.name = "Laser";
        this.offensive = true;
        this.cooldown = 0;
        this.cost = 1;
        this.description = "";
        this.needsTarget = true;
        this.group = "Offensive";
    }
}

// +2 dmg (cooldown)
public class MissileAction : CardAction
{
    public MissileAction(){
        this.effects = new List<CombatEffect>{new DamageEffect(1, false, 2)};
        AttachToEffect();
        this.name = "Missile";
        this.offensive = true;
        this.cooldown = 3;
        this.cost = 1;
        this.description = "";
        this.needsTarget = true;
        this.group = "Offensive";
    }
}

// (add extinguish card to enemy hand +1 dmg if not spent and potential for spread)
public class FirebombAction : CardAction
{
    public FirebombAction(){
        this.effects = new List<CombatEffect>{new OnFireEffect(100, false, 1)};
        AttachToEffect();
        this.name = "Fire Bomb";
        this.offensive = true;
        this.cooldown = 1;
        this.cost = 1;
        this.description = "";
        this.needsTarget = true;
        this.group = "Offensive";
    }
}

// piercer (extra dmg to shields)
public class ShieldPiercerAction : CardAction
{
    public ShieldPiercerAction(){
        this.effects = new List<CombatEffect>{new ShieldOnlyDamageEffect(1, false, 3)};
        AttachToEffect();
        this.name = "Shield Piercer";
        this.offensive = true;
        this.cooldown = 0;
        this.cost = 2;
        this.description = "3 Damage Shields Only";
        this.needsTarget = true;
        this.group = "Offensive";
    }
}


public class FocusedShieldAction : CardAction
{
    public FocusedShieldAction(){
        this.effects = new List<CombatEffect>{new ShieldEffect(2, true, 1)};
        AttachToEffect();
        this.name = "Focused Shield";
        this.offensive = false;
        this.cooldown = 0;
        this.cost = 1;
        this.description = "";
        this.needsTarget = true;
        this.group = "Shield";
    }
}
public class GeneralShieldAction : CardAction
{
    public GeneralShieldAction(){
        this.effects = new List<CombatEffect>{new GeneralShieldEffect(2, true, 1)};
        AttachToEffect();
        this.offensive = false;
        this.name = "General Shield";
        this.cooldown = 2;
        this.cost = 1;
        this.description = "";
        this.needsTarget = false;
        this.group = "Shield";
    }
}
public class BigBoyShieldAction : CardAction
{
    public BigBoyShieldAction(){
        this.effects = new List<CombatEffect>{new ShieldEffect(2, true, 2)};
        AttachToEffect();
        this.offensive = false;
        this.name = "Big Boy Shield";
        this.cooldown = 2;
        this.cost = 1;
        this.description = "";
        this.needsTarget = true;
        this.group = "Shield";
    }
}
public class SemiPermanentShieldAction : CardAction
{
    public SemiPermanentShieldAction(){
        this.effects = new List<CombatEffect>{new ShieldEffect(99, true, 1)};
        AttachToEffect();
        this.offensive = false;
        this.name = "Semi Permanent Shield";
        this.cooldown = 1;
        this.cost = 1;
        this.description = "";
        this.needsTarget = true;
        this.group = "Shield";
    }
}
	
    
public class SpeedUpAction : CardAction
{
    public SpeedUpAction(){
        this.effects = new List<CombatEffect>{new SpeedEffect(1, true, 1)};
        AttachToEffect();
        this.offensive = false;
        this.name = "Speed Up";
        this.cooldown = 0;
        this.cost = 1;
        this.description = "";
        this.needsTarget = false;
        this.group = "Speed";
    }
}
// (cooldown)
public class BigBoySpeedUpAction : CardAction
{
    public BigBoySpeedUpAction(){
        this.effects = new List<CombatEffect>{new SpeedEffect(1, true, 2)};
        AttachToEffect();
        this.offensive = false;
        this.name = "Big Boy Speed Up";
        this.cooldown = 2;
        this.cost = 1;
        this.description = "";
        this.needsTarget = false;
        this.group = "Speed";
    }
}
// (Immunity Frames)
public class EvasiveManeouvreAction : CardAction
{
    public EvasiveManeouvreAction(){
        this.effects = new List<CombatEffect>{new GeneralShieldEffect(1, true, 99)};
        AttachToEffect();
        this.offensive = false;
        this.name = "Evasive Manoeuvre";
        this.cooldown = 3;
        this.cost = 3;
        this.description = "";
        this.needsTarget = false;
        this.group = "Speed";
    }
}
// (Dmg room for extra speed)
public class OverHeatAction : CardAction
{
    public OverHeatAction(){
        this.effects = new List<CombatEffect>{new DamageEffect(1, true, 1), new SpeedEffect(1, true, 1)};
        AttachToEffect();
        this.offensive = false;
        this.name = "Over Heat";
        this.cooldown = 0;
        this.cost = 0;
        this.description = "";
        this.needsTarget = true;
        this.group = "Special";
    }
}

// +1 AP (cooldown)
public class OverdriveAction : CardAction
{
    public OverdriveAction(){
        this.effects = new List<CombatEffect>{new APEffect(1, true, 1)};
        AttachToEffect();
        this.name = "Over Drive";
        this.cooldown = 3;
        this.cost = 0;
        this.description = "";
        this.needsTarget = false;
        this.group = "AP";
    }
}
// energy weapons (cooldown)
public class BuffEnergyWeaponAction : CardAction
{
    public BuffEnergyWeaponAction(){
        this.effects = new List<CombatEffect>{new FreeLaserEffect(1, true)};
        AttachToEffect();
        this.name = "Buff Energy Weapon";
        this.cooldown = 2;
        this.cost = 3;
        this.description = "";
        this.needsTarget = false;
        this.group = "Special";
    }
}
public class FreeLaserAction : CardAction
{
    public FreeLaserAction(){
        this.effects = new List<CombatEffect>{new DamageEffect(1, false, 1)};
        AttachToEffect();
        this.name = "Free Laser";
        this.cooldown = 1;
        this.offensive=true;

        this.cost = 0;
        this.description = "";
        this.needsTarget = true;
        this.group = "Offensive";
    }
}
// (-AP +AP card)
public class ChargeBatteriesAction : CardAction
{
    public ChargeBatteriesAction(){
        this.effects = new List<CombatEffect>{new ChargeBatteriesEffect(1, true, 1)};
        AttachToEffect();
        this.name = "Charge Batteries";
        this.cooldown = 1;
        this.cost = 1;
        this.description = "";
        this.needsTarget = false;
        this.group = "Special";
    }
}
public class DischargeChargeBatteriesAction : CardAction
{
    public DischargeChargeBatteriesAction(){
        this.effects = new List<CombatEffect>{new APEffect(1, true, 1)};
        AttachToEffect();
        this.name = "Discharge Batteries";
        this.cooldown = 99;
        this.cost = 0;
        this.description = "";
        this.needsTarget = false;
        this.group = "AP";
    }
}
// (both players skip a turn)
public class EMPAction : CardAction
{
    public EMPAction(){
        this.effects = new List<CombatEffect>{new DisableRoomEffect(2, false)};
        AttachToEffect();
        this.name = "EMP";
        this.cooldown = 3;
        this.offensive=true;
        this.cost = 2;
        this.description = "Disable room NEXT turn";
        this.needsTarget = true;
        this.group = "Special";
    }
}

public class StopFireAction : CardAction
{
    public StopFireAction(){
        this.effects = new List<CombatEffect>{new StopFireEffect()};
        AttachToEffect();
        this.name = "Stop Fire";
        this.offensive=false;
        this.cooldown = 100;
        this.cost = 1;
        this.description = "";
        this.needsTarget = true;
        this.group = "Debuff";
    }
}