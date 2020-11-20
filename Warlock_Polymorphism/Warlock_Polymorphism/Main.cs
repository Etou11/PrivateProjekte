using robotManager.Helpful;
using robotManager.Products;
using System;
using System.Collections.Generic;
using System.Threading;
using wManager.Wow.Class;
using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;


public class Main : ICustomClass
{
    public static bool _isLaunched = false;
    static bool fearedTargetExists = false;
    public static readonly uint soulshardId = 6265;

    private List<RotationStep> RotationList = new List<RotationStep>();

    //Beast * Dragonkin * Demon * Elemental * Giant * Undead * Humanoid * Mechanical * Not specified * Totem
    static readonly List<String> noImmunity = new List<String>() { "" };
    static readonly List<String> immunityFear = new List<String>() { "Elemental", "Mechanical", "Undead" };
    static readonly List<String> immunityDrainLife = new List<String>() { "Undead" };
    static readonly List<String> immunityFire = new List<String>() { "Elemental", "Dragonkin" };

    public float Range
    {
        get
        {
            return 25f;
        }
    }

    private void StartFightClass()
    {
        Logging.Write("Start FC called");
        CreateSpells();
        Rotation MainRotation = new Rotation();
        MainRotation.ExecuteRotation();
    }

    private void CreateSpells()
    {

        RotationList.AddRange(new List<RotationStep>
        {
                new RotationStep(new Cspell("Immolate", true, true, immunityFire, new CustomConditions(0, 20, 0, (Func<WoWLocalPlayer, WoWUnit, bool>) ((me, tar) => tar.HealthPercent > 50)))),
                new RotationStep(new Cspell("Shadow Bolt", true, true, noImmunity, new CustomConditions(10, 5, 0, (Func<WoWLocalPlayer, WoWUnit, bool>) ((me, tar) => true )))),
                //new RotationStep(new Cspell("Shoot", true, false, noImmunity, new CustomConditions(0,0,0, (Func<WoWLocalPlayer, WoWUnit, bool>) ((me, tar) => true)))),
                //new RotationStep(new Cspell("Attack", false, false, noImmunity, new CustomConditions(0,0,0, (Func<WoWLocalPlayer, WoWUnit, bool>) ((me, tar) => me.HealthPercent > 0)))),
                //new RotationStep(new Cspell("Attack", false, false, noImmunity, new CustomConditions(0,0,0, (Func<Cspell, WoWLocalPlayer, bool>) ((test, me) => true))))
                 new RotationStep(new Cspell("Attack", false, false, noImmunity, new CustomConditions(0, 0, 0, (Func<WoWLocalPlayer, WoWUnit, bool>) ((me, tar) => true ))))
         });

    }

    public static List<WoWUnit> GetAttackers()
    { 
        List<WoWUnit> attackersList = ObjectManager.GetWoWUnitAttackables(40);

        if(attackersList.Count <= 0)
            return (List<WoWUnit>)null;

        attackersList.RemoveAll(o => !o.InCombat || !o.IsAlive || (o.IsTaggedByOther && !o.IsTargetingMeOrMyPet));
        attackersList.Sort((x, y) => x.Health != y.Health ? x.Health.CompareTo(y.Health) : x.Guid.CompareTo(y.Guid));

        try
        {
            //if(attackersList.Find(o => o.HaveBuff(Fear.Name)).IsValid)
            //fearedTargetExists = true;

        }
        catch(Exception e)
        {
            fearedTargetExists = false;
        }

        return attackersList;
    }

    public void Initialize()
    {
        _isLaunched = true;
        Logging.Write("Initialized FC");
        StartFightClass();
    }

    public void Dispose()
    {
        _isLaunched = false;
        Logging.Write("Disposed FC");
    }

    public void ShowConfiguration()
    {
        throw new NotImplementedException();
    }
}


public class Rotation
{



    public void ExecuteRotation()
    {
        while(Main._isLaunched)
        {
            while(Products.InPause)
            {
                Thread.Sleep(1000);
            }

            try
            {
                foreach(RotationStep ele in rota)
                {
                    if(ele.GetCspell.CheckMandatoryConditions && ele.GetCspell.GetCustomConditions.CustomConditionsCheck)
                    {
                        Logging.Write("Launch Spell: " + ele.GetCspell.Name);
                        ele.GetCspell.Launch();
                        break;
                    }
                   
                }
                Thread.Sleep(100);
            }
            catch(Exception e)
            {
                Logging.Write("E: " + e);
            }
        }
    }
}

    public class RotationStep
    {
        Cspell cSpell;
        int priority = 0;

        public RotationStep(Cspell cSpell)
        {
            this.cSpell = cSpell;
            this.priority = 0;
        }

        public RotationStep(Cspell cSpell, int priority)
        {
            this.cSpell = cSpell;
            this.priority = priority;
        }

        public Cspell GetCspell
        {
            get
            {
                return this.GetCspell;
            } 
        }

        public int GetPriority
        {
            get
            {
                return this.priority;
            }
        }
    }

public class Cspell : Spell
{
    List<String> Immunity;
    CustomConditions CustomConditions;
    bool stopMove, waitIsCast = false;

    public List<String> GetImmunity { get; }

    public Cspell(String name, bool stopMove, bool waitIsCast, List<string> Immunity, CustomConditions CustomConditions) : base(name)
    {
        this.Name = name;
        this.stopMove = stopMove;
        this.waitIsCast = waitIsCast;
        this.Immunity = Immunity;
        this.CustomConditions = CustomConditions;
    }

    public bool CheckMandatoryConditions
    {
        get
        {
            return this.KnownSpell && this.IsDistanceGood && this.HaveBuff && CurrentlyNotCasting && this.VanillaIsSpellUsable && this.TargetIsNotImmune;
        }
    }

    private new bool IsDistanceGood
    {
        get
        {
            return ObjectManager.Target.Guid > 0 && ((ObjectManager.Me.Position.DistanceTo(ObjectManager.Target.Position)) < (this.MaxRange + (ObjectManager.Me.Position.DistanceTo(ObjectManager.Target.Position) - ObjectManager.Target.GeHitBoxtDistance) - 3));
        }
    }

    private bool TargetIsNotImmune
    {
        get
        {
            foreach(String ele in this.Immunity)
            {
                if(Lua.LuaDoString<String>("return UnitCreatureType(\"target\")") == ele)
                    return false;
            }
            return true;
        }
    }

    private bool VanillaIsSpellUsable
    {
        get
        {
            if(!this.IsInActionBar)
            {
                SupportFunctions.SpellToActionbar(this);
            }
            return SpellManager.SpellUsable(this.Name);
        }
    }

    private bool CurrentlyNotCasting
    {
        get
        {
            return ObjectManager.Me.CastingSpell.Id == 0;
        }
    }

    private new bool HaveBuff
    {
        get
        {
            return !(ObjectManager.Target.HaveBuff(this.Name) || ObjectManager.Me.HaveBuff(this.Name) || ObjectManager.Pet.HaveBuff(this.Name));
        }
    }

    public new void Launch()
    {
        if((this.Name == "Shoot" && SupportFunctions.HaveWand()) || (this.Name == "Attack" && !SupportFunctions.HaveWand()))
        {
            SupportFunctions.ActivateAutoAttack(this);
            return;
        }
        base.Launch(this.stopMove, this.waitIsCast);
    }

    public CustomConditions GetCustomConditions
    {
        get
        {
            return this.CustomConditions;
        }
    }
}


public class CustomConditions
{
    private int manaLower, tarHpLower, attackerCount = 0;
    private Func<WoWLocalPlayer, WoWUnit, bool> additionalConditions;

    public CustomConditions(int manaLower, int tarHpLower, int attackerCount, Func<WoWLocalPlayer, WoWUnit, bool> additionalConditions)
    {
        this.manaLower = manaLower;
        this.tarHpLower = tarHpLower;
        this.attackerCount = attackerCount;
        this.additionalConditions = additionalConditions;
    }
    
    public bool CustomConditionsCheck
    {
        get
        {
            return ObjectManager.Me.ManaPercentage >= this.manaLower && ObjectManager.Target.HealthPercent >= this.tarHpLower && this.additionalConditions(ObjectManager.Me, ObjectManager.Target);
        }
    }


    public CustomConditions GetCustomDefaultConditions
    {
        get
        {
            return this;
        }
    }

    override
    public String ToString()
    {
        return "Mana: " + (ObjectManager.Me.ManaPercentage >= this.manaLower) + " Health: " +  (ObjectManager.Target.HealthPercent >= this.tarHpLower) + " Attackers Count: " +  (Main.GetAttackers().Count < this.attackerCount);
    }
}


    public class CustomBuffCondtions
    {
    /*
        //int soulShardCount ItemsManager.GetItemCountById(Main.soulshardId) >= this.soulShardCount
        private int manaLower, tarHpLower, attackerCount, soulShardCount, petHp = 0;



        public bool CustomConditionsCheck
        {
            get
            {
                return ObjectManager.Me.ManaPercentage > manaLower && ObjectManager.Target.HealthPercent > tarHpLower && Main.GetAttackers().Count > 0;
            }
        }
    */
    }

public class SupportFunctions
{
    public static bool InCombat()
    {
        return Lua.LuaDoString<bool>("return UnitAffectingCombat(\"player\");") || Lua.LuaDoString<bool>("return UnitAffectingCombat(\"pet\");");
    }

    public static bool HaveWand()
    {
        return Lua.LuaDoString<bool>("return HasWandEquipped();");
    }

    public static void SpellToActionbar(Cspell cSpell)
    {
        Lua.LuaDoString("local i = 1 while true do local spellName, spellRank = GetSpellName(i, BOOKTYPE_SPELL) if not spellName then do break end end if string.find(spellName, \"" + cSpell.Name + "\") then do PickupSpell(i, BOOKTYPE_SPELL) PlaceAction(" + 1 + ") ClearCursor() break end end i = i + 1 end;");
    }

    public static bool IsAutoAttackActive(Cspell cSpell)
    {
        SpellToActionbar(cSpell);
        return Lua.LuaDoString<bool>("if not IsCurrentAction(1) then return true end");
    }

    public static bool ActivateAutoAttack(Cspell cSpell)
    {
        SpellToActionbar(cSpell);
        return Lua.LuaDoString<bool>("if not IsCurrentAction(1) then UseAction(1) end");
    }
}

