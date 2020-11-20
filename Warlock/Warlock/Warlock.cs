using System;
using robotManager.Helpful;
using wManager.Wow.Class;
using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;
using System.Threading;
using robotManager.Products;
using wManager.Wow.Enums;
using System.Collections.Generic;
using wManager.Wow;
using System.ComponentModel;
using wManager.Events;
using System.Diagnostics;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using wManager.Wow.Bot.Tasks;


public class Main : ICustomClass
{
    //Range
    //1 = melee, 2 = mid range, 3 = default range
    private static float setRange = 3;
    private float cRange = 25f;


    float ICustomClass.Range
    {
        get
        {
            return cRange;
        }
    }


    //Default Values
    //Global

    bool _isLaunched;
    private static bool _isSubscribed = false;
    const uint soulShardId = 6265;
    private static bool _restartInProgress = false;
    private static bool _lockTarget = false;
    private static bool _stopMovement = false;
    private static bool _tradeInProgress = false;
    private static bool _hasFinished = false;
    private static bool _manageMovementActive = false;
    private bool _lockSpells = false;
    private static bool fearedTargetExists = false;
    //private static bool stopRange = false;
    Random rnd = new Random();

    Dictionary<WoWPlayer, DateTime> blacklistedPlayersUnendingBreath = new Dictionary<WoWPlayer, DateTime>(new WoWPlayerNameComparer());
    Dictionary<WoWPlayer, DateTime> blacklistedPlayersHealthstones = new Dictionary<WoWPlayer, DateTime>(new WoWPlayerNameComparer());
    List<Stones> listHStones = new List<Stones>();
    List<Stones> listSStones = new List<Stones>();
    Stones currentHealthstone;
    Stones currentSoulstone;

    List<WoWUnit> targets = new List<WoWUnit>();
    WoWUnit petTarget;

    // Beast * Dragonkin * Demon * Elemental * Giant * Undead * Humanoid * Mechanical * Not specified * Totem
    static readonly List<String> noImmunity = new List<String>() { "" };
    static readonly List<String> immunityFear = new List<String>(){ "Elemental", "Mechanical", "Undead" };
    static readonly List<String> immunityDrainLife = new List<String>(){ "Undead"};
    static readonly List<String> immunityFire = new List<String>(){ "Elemental", "Dragonkin" };

    //Spells
    private static int defaultCooldown = 1500;

    //Combat
    private static int manaUseSpell = 15;
    private static int hpEnemyUseSpell = 20;
    //Buffs
    private static int manaSummon = 50;
    private static int manaBuff = 25;

    //Values Spells
    //HP target
    private static int hpSpellDefault = 1;
    private static int hpCurseOfAgony = 10;
    private static int hpImmolate = 10;
    private static int hpCorruption = 10;

    //Mana me
    private static int manaSpellDefault = 1;
    private static int manaCurseOfAgony = 10;
    private static int manaImmolate = 10;
    private static int manaCorruption = 10;

    //Pet
    private static int healthFunnelHpMe = 15;
    private static int healthFunnelHpPet = 15;


    //Dmg
    Cspell ShadowBolt = new Cspell("Shadow Bolt", defaultCooldown, hpSpellDefault, manaSpellDefault, noImmunity);
    Cspell Shoot = new Cspell("Shoot", defaultCooldown, 0, 0, noImmunity);

    //Dot
    Cspell Immolate = new Cspell("Immolate", defaultCooldown, hpImmolate, manaImmolate, immunityFire);
    Cspell Corruption = new Cspell("Corruption", defaultCooldown, hpCorruption, manaCorruption, noImmunity);
    Cspell CurseOfAgony = new Cspell("Curse of Agony", defaultCooldown, hpCurseOfAgony, manaCurseOfAgony, noImmunity);
    Cspell SiphonLife = new Cspell("Siphon Life", defaultCooldown, hpSpellDefault, manaSpellDefault, noImmunity);

    //Buff
    Cspell DemonSkin = new Cspell("Demon Skin", defaultCooldown, hpSpellDefault, manaSpellDefault, noImmunity);
    Cspell DemonArmor = new Cspell("Demon Armor", defaultCooldown, hpSpellDefault, manaSpellDefault, noImmunity);
    Cspell UnendingBreath = new Cspell("Unending Breath", defaultCooldown, hpSpellDefault, manaSpellDefault, noImmunity);

    //Pet
    Cspell SummonImp = new Cspell("Summon Imp", defaultCooldown, hpSpellDefault, manaSpellDefault, noImmunity);
    Cspell SummonVoidwalker = new Cspell("Summon Voidwalker", defaultCooldown, hpSpellDefault, manaSpellDefault, noImmunity);

    //Utility
    Cspell Fear = new Cspell("Fear", defaultCooldown, hpSpellDefault, manaSpellDefault, immunityFear);
    Cspell DrainLife = new Cspell("Drain Life", defaultCooldown, hpSpellDefault, manaSpellDefault, immunityDrainLife);
    Cspell DrainSoul = new Cspell("Drain Soul", defaultCooldown, hpSpellDefault, manaSpellDefault, noImmunity);
    Cspell HealthFunnel = new Cspell("Health Funnel", defaultCooldown, hpSpellDefault, manaSpellDefault, noImmunity);
    Cspell LifeTap = new Cspell("Life Tap", defaultCooldown, hpSpellDefault, manaSpellDefault, noImmunity);

    //Cooldowns
    Cspell AmplifyCurse = new Cspell("Amplify Curse", 180000, hpSpellDefault, manaSpellDefault, noImmunity);

    //Pet Spell
    Cspell ConsumeShadows = new Cspell("Consume Shadows", defaultCooldown, 0, 0, noImmunity);

    //Healthstone - Spell
    static Cspell HealthstoneMinor = new Cspell("Create Healthstone (Minor)", defaultCooldown, hpSpellDefault, manaSpellDefault, noImmunity);
    static Cspell HealthstoneLesser = new Cspell("Create Healthstone (Lesser)", defaultCooldown, hpSpellDefault, manaSpellDefault, noImmunity);
    static Cspell Healthstone = new Cspell("Create Healthstone", defaultCooldown, hpSpellDefault, manaSpellDefault, noImmunity);
    static Cspell GreaterHealthstone = new Cspell("Create Healthstone (Greater)", defaultCooldown, hpSpellDefault, manaSpellDefault, noImmunity);
    static Cspell MajorHealthstone = new Cspell("Create Healthstone (Major)", defaultCooldown, hpSpellDefault, manaSpellDefault, noImmunity);

    //Soulstone - Spell
    static Cspell MinorSoulstone = new Cspell("Create Soulstone (Minor)", defaultCooldown, hpSpellDefault, manaSpellDefault, noImmunity);
    static Cspell LesserSoulstone = new Cspell("Create Soulstone (Lesser)", defaultCooldown, hpSpellDefault, manaSpellDefault, noImmunity);
    static Cspell Soulstone = new Cspell("Create Soulstone", defaultCooldown, hpSpellDefault, manaSpellDefault, noImmunity);
    static Cspell GreaterSoulstone = new Cspell("Create Soulstone (Greater)", defaultCooldown, hpSpellDefault, manaSpellDefault, noImmunity);
    static Cspell MajorSoulstone = new Cspell("Create Soulstone (Major)", defaultCooldown, hpSpellDefault, manaSpellDefault, noImmunity);

    //Healthstone - Item
    Stones healthstoneMinor = new Stones(HealthstoneMinor, "Create Healthstone (Minor)()", "Minor Healthstone", 5512, 19004, 19005, DateTime.Now.AddMinutes(-2));
    Stones healthstoneLesser = new Stones(HealthstoneLesser, "Create Healthstone (Lesser)()", "Lesser Healthstone", 5511, 19006, 19007, DateTime.Now.AddMinutes(-2));
    Stones healthstone = new Stones(Healthstone, "Create Healthstone()", "Healthstone", 5509, 19008, 19009, DateTime.Now.AddMinutes(-2));
    Stones greaterHealthstone = new Stones(GreaterHealthstone, "Create Healthstone (Greater)()", "Greater Healthstone", 5510, 19008, 19011, DateTime.Now.AddMinutes(-2));
    Stones majorHealthstone = new Stones(MajorHealthstone, "Create Healthstone (Major)()", "Major Healthstone", 9421, 19012, 19013, DateTime.Now.AddMinutes(-2));

    //SoulStone - Item
    Stones minorSoulstone = new Stones(MinorSoulstone, "Create Soulstone (Minor)()", "Minor Soulstone", 5232, DateTime.Now.AddMinutes(-30));
    Stones lesserSoulstone = new Stones(LesserSoulstone, "Create Soulstone (Lesser)()", "Lesser Soulstone", 16892, DateTime.Now.AddMinutes(-30));
    Stones soulstone = new Stones(Soulstone, "Create Soulstone()", "Soulstone", 16893, DateTime.Now.AddMinutes(-30));
    Stones greaterSoulstone = new Stones(GreaterSoulstone, "Create Soulstone (Greater)()", "Greater Soulstone", 16895, DateTime.Now.AddMinutes(-30));
    Stones majorSoulstone = new Stones(MajorSoulstone, "Create Soulstone (Major)()", "Major Soulstone", 16896, DateTime.Now.AddMinutes(-30));


    void ICustomClass.Initialize()
    {
        _isLaunched = true;
        LuaEvents();
        FightEvents.OnFightLoop += new FightEvents.FightTargetHandler(FightEventsOnFightLoop);
        FightEvents.OnFightEnd += FightEventsOnOnFightEnd;
        MovementEvents.OnMoveToPulse += MovementEventsOnOnMoveToPulse;
        //MovementEvents.OnMovementPulse += MovementEventsOnOnMovementPulse;
        //MovementEvents.OnMovementPulse += MovementEventsOnMoveToLoop;
        //MovementEvents.OnMovementLoop += MovementEventsOnOnMovementLoop;
        //MovementEvents.OnMoveToStop += MovementEventsOnOnMoveToStop;
        LootingEvents.OnLootingPulse += LootingEventsOnOnLootingPulse;
        _isSubscribed = true;
        _restartInProgress = false;
        WarlockFightClassSettings.Load();
        wManager.wManagerSetting.CurrentSetting.UseCTM = false;
        wManager.wManagerSetting.CurrentSetting.UseLuaToMove = false;
        Logging.Write("Warlock FC started");
        IngameFrame();
        InitializeStones();
        Rotation();
    }

    void ICustomClass.Dispose()
    {
        FightEvents.OnFightLoop -= new FightEvents.FightTargetHandler(FightEventsOnFightLoop);
        FightEvents.OnFightEnd -= FightEventsOnOnFightEnd;
        MovementEvents.OnMoveToPulse -= MovementEventsOnOnMoveToPulse;
        //MovementEvents.OnMovementPulse -= MovementEventsOnOnMovementPulse;
       // MovementEvents.OnMovementPulse -= MovementEventsOnMoveToLoop;
        //MovementEvents.OnMovementLoop -= MovementEventsOnOnMovementLoop;
        //MovementEvents.OnMoveToStop -= MovementEventsOnOnMoveToStop;
        LootingEvents.OnLootingPulse -= LootingEventsOnOnLootingPulse;
        WarlockFightClassSettings.CurrentSettings.Save();
        _isLaunched = false;
        _isSubscribed = false;
        Logging.Write("Warlock FC stopped");
    }

    //Reset values after fight
    private static void FightEventsOnOnFightEnd(ulong guid)
    {
        _lockTarget = false;
        //stopRange = false;
        setRange = 3;
    }

    //Prevent target switching during combat
    private static void FightEventsOnFightLoop(WoWUnit unit, CancelEventArgs cancelable)
    {
        while(_lockTarget && InCombat())
        {   
            Thread.Sleep(100);
        }
    }

    //Stop movement when trading
    private static void MovementEventsOnOnMoveToPulse(Vector3 point, CancelEventArgs cancelable)
    {
        if(_stopMovement)
        {
            Logging.Write("Stop movement for trade");
            //if(!stopRange)
            //{
            cancelable.Cancel = true;
            MovementManager.StopMoveTo();
            Thread.Sleep(50);

            while(!_hasFinished)
            {
                Thread.Sleep(50);
            }
            // }

            _hasFinished = false;
            cancelable.Cancel = false;
            _stopMovement = false;
        }
    }

    
    private static void MovementEventsOnOnMovementPulse(List<Vector3> points, CancelEventArgs cancelable)
    {
        if(_manageMovementActive)
        {
            cancelable.Cancel = true;

            while(_manageMovementActive)
            {
                Thread.Sleep(50);
            }

            cancelable.Cancel = false;
        }
    }

    /*
    private static void MovementEventsOnMoveToLoop(List<Vector3> points, CancelEventArgs cancelable)
    {
        Logging.Write("MovementEventsOnMoveToLoop fired");
    }

    private static void MovementEventsOnOnMovementLoop()
    {
        Logging.Write("MovementEventsOnOnMovementLoop fired");
        //StopAllMovement();
    }

    private static void MovementEventsOnOnMoveToStop(CancelEventArgs cancelable)
    {
        Logging.Write("MovementEventsOnOnMoveToStop fired");
    }
    */

    //Stop looting during trading
    private static void LootingEventsOnOnLootingPulse(WoWUnit unit, CancelEventArgs cancelable)
    {
    
        if(_tradeInProgress)
            cancelable.Cancel = true;
        if(cancelable.Cancel == true && !_tradeInProgress)
            cancelable.Cancel = false;
            
    }

    private void LuaEvents()
    {
        EventsLuaWithArgs.OnEventsLuaWithArgs += (LuaEventsId id, List<string> args) =>
        {
            if(_isSubscribed && id == wManager.Wow.Enums.LuaEventsId.LEARNED_SPELL_IN_TAB)
            {
                if(!_restartInProgress)
                {
                    _restartInProgress = true;
                    Logging.Write("Learned a new spell. Going to restart Fight Class... .");
                    Thread.Sleep(7000);
                    wManager.Wow.Helpers.CustomClass.ResetCustomClass();
                }
            }

            if(_isSubscribed && id == wManager.Wow.Enums.LuaEventsId.PLAYER_DEAD)
            {
                if(MinorSoulstone.KnownSpell)
                {
                    Thread.Sleep(1500);
                    Lua.LuaDoString("UseSoulstone()");
                }
            }

            if(_isSubscribed && id == wManager.Wow.Enums.LuaEventsId.TRADE_SHOW)
            {
                while(InCombat())
                {
                    Thread.Sleep(50);
                }

                _stopMovement = true;
                _tradeInProgress = true;
                GiveHealthstoneOnTrade(GetClosestPlayerHealthstone());
                _tradeInProgress = false;
                _hasFinished = true;

            }

            if(_isSubscribed && id == wManager.Wow.Enums.LuaEventsId.TRADE_ACCEPT_UPDATE)
            {
                if(args[0].Contains("1"))
                {
                    blacklistedPlayersHealthstones.Add(GetClosestPlayerHealthstone(), DateTime.Now);
                    Logging.Write("Trade successful! Adding player: " + GetClosestPlayerHealthstone() + " with time stamp: " + DateTime.Now + " to blacklist");
                }
            }

            if(_isSubscribed && id == LuaEventsId.UI_ERROR_MESSAGE)
            {
                if(args[0].Contains("Target needs to be in front of you") || args[0].Contains("You are facing the wrong way!"))
                {
                    MovementManager.Face(ObjectManager.Target.Position);
                }
            }
        };
    }

    private void SetTarget()
    {
        try
        {
            _lockTarget = true;

            if(ObjectManager.Me.Target == 0 || !ObjectManager.Target.IsAlive)
            {
                if(ObjectManager.Pet.Target != 0)
                    ObjectManager.Me.Target = ObjectManager.Pet.Target;
                Lua.LuaDoString("ClearTarget()");
                //if(!_lockTarget)
                  //  _lockTarget = true;
            }

            targets = GetAttackers();

            if(targets == null || targets.Count <= 0)
                return;

            if(targets.Count > 2)
            {
                if(!targets.LastOrDefault().HaveBuff(Fear.Name) && Fear.KnownSpell)
                {
                    ObjectManager.Me.Target = targets.LastOrDefault().Guid;
                    //if(!_lockTarget)
                       // _lockTarget = true;
                    return;
                }
            }
            
            if(!IsDotted(targets.FirstOrDefault()) || ObjectManager.Me.ManaPercentage <= 10)
            {
                ObjectManager.Me.Target = targets.FirstOrDefault().Guid;
                return;
            }
            

            if(GetCurrentPetType() == "Voidwalker")
            {
                int maxUnitsToDot = 2;
                for(int i = 0; i < targets.Count && i < maxUnitsToDot; i++)
                {
                    if(!IsDotted(targets[i]))
                    {
                        ObjectManager.Me.Target = targets[i].Guid;
                        //if(!_lockTarget)
                         //   _lockTarget = true;
                        return;
                    }
                }
            }
            
        }
        catch(Exception e)
        {
            Logging.Write("SetTarget failure: " + e);
        }
    }

    private static void StopAllMovement(double range)
    {
        while(ObjectManager.Me.GetMove && ObjectManager.Me.Position.DistanceTo(ObjectManager.Target.Position) > range)
        {
            Thread.Sleep(50);
        }

        MovementManager.MoveTo(ObjectManager.Me.Position);
    }

    private void ManageMovement(float range)
    {
        if(_manageMovementActive)
            return;
        else
            _manageMovementActive = true;
        
        while(true)
        {
            if(!InCombat() || ObjectManager.Me.Position.DistanceTo(ObjectManager.Target.Position) <= range || ObjectManager.Me.Position.DistanceTo2D(ObjectManager.Target.Position) <= range) 
                break;

            if(MovementManager.InMovement || MovementManager.InMoveTo)
            {
                Thread.Sleep(50);
                continue;
            }

            Vector3 CoordinateToTarget = new Vector3((ObjectManager.Me.Position.X + ObjectManager.Target.Position.X) / 2, (ObjectManager.Me.Position.Y + ObjectManager.Target.Position.Y) / 2, (ObjectManager.Me.Position.Z + ObjectManager.Target.Position.Z) / 2);

            List<Vector3> ListToReachPoint = new List<Vector3>();
            ListToReachPoint = PathFinder.FindPath(CoordinateToTarget);
            try
            {
                MovementManager.Go(ListToReachPoint);

            }
            catch(Exception e)
            {
                Logging.Write("Manage Movement failed: " + e);
            }
        }
        _manageMovementActive = false;
        StopAllMovement(range);
    }

    //Main rotation
    private void Rotation()
    {
        while(_isLaunched)
        {
            try
            {
                if(ObjectManager.Me.IsAlive && Products.IsStarted && !Products.InPause)
                {

                    while(ObjectManager.Pet.HaveBuff("Consume Shadows") && !InCombat() || ObjectManager.Me.InTransport || ObjectManager.Me.IsOnTaxi || Products.InPause)
                    {
                        Thread.Sleep(250);
                    }

                    if(InCombat() && ObjectManager.Target.Guid <= 0)
                        CombatRotation();

                    if(ObjectManager.Target.Guid != 0 && !Conditions.ForceIgnoreIsAttacked)
                    {
                        if(!InCombat() && ObjectManager.Me.Position.DistanceTo(ObjectManager.Target.Position) < cRange && ShadowBolt.IsDistanceGood && ObjectManager.Target.IsAttackable && TargetIsEnemy())
                            Pull();

                        if(InCombat() && ObjectManager.Target.Guid != 0 && ObjectManager.Target.IsAttackable && TargetIsEnemy())
                            CombatRotation();
                    }

                    if(!InCombat())
                    { 

                        if(Lua.LuaDoString<bool>("if CombatStatus then return true else return false end"))
                            ResetCombatFrame();

                        Buffs();
                        UpdateStatusFrame("Idle");
                    }
                }
            }
            catch(Exception e)
            {
                Logging.Write("Something went wrong: " + e);
            }

        }
    }
    
    //Pull
    private void Pull()
    {
        UpdateStatusFrame("Pull");
        
        if(ObjectManager.Pet.IsAlive && ObjectManager.Pet.HealthPercent > 0)
        {
            ObjectManager.Pet.Target = ObjectManager.Target.Guid;
            Lua.LuaDoString("PetAttack()");
        }

        if(SiphonLife.KnownSpell)
        {
            SiphonLife.Launch(true, true);
            return;
        }
        else if(Immolate.KnownSpell && !SiphonLife.KnownSpell)
        {
            Immolate.Launch(true, true);
            return;
        }
        else if(!Immolate.KnownSpell)
        {
            ShadowBolt.Launch(true, true);
            return;
        }
        /*
        Thread.Sleep(250 + Usefuls.Latency);
        CombatRotation();
        */
    }

    //Fight
    private void CombatRotation()
    {
        UpdateStatusFrame("Fight");

        while(ObjectManager.Me.CastingSpellId != 0 || GlobalCooldownActive())
        {
            if(!InCombat())
                return;

            UpdateCombatFrame();

            if(ObjectManager.Target.Guid <= 0)
            {
                _lockTarget = false;
            }

            if(ObjectManager.Target.Guid != 0 && TargetIsEnemy() && InCombat() && ObjectManager.Me.CastingSpellId != DrainSoul.Id && ObjectManager.Me.CastingSpellId != DrainLife.Id && ObjectManager.Me.CastingSpellId != HealthFunnel.Id)
            {
                MovementManager.Face(ObjectManager.Target.Position);
                EmergencyCheck();
            }

            if(ObjectManager.Me.Position.DistanceTo(ObjectManager.Target.Position) <= cRange && ObjectManager.Target.IsAttackable && ObjectManager.Pet.IsAlive && ObjectManager.Pet.HealthPercent > 0)
            {
                PetControl();
            }

            Thread.Sleep(25);
        }

        SetTarget();
        CastSpells();
        Thread.Sleep(25);
    }

    //Emergency
    private void EmergencyCheck()
    {
        if(targets == null)
            return;  

        if((ObjectManager.Pet.IsAlive || ObjectManager.Pet.HealthPercent > 0) && (ObjectManager.Me.CastingSpellId == SummonImp.Id || ObjectManager.Me.CastingSpellId == SummonVoidwalker.Id))
            AbortCast();

        if((ObjectManager.Me.CastingSpellId == SummonImp.Id || ObjectManager.Me.CastingSpellId == SummonVoidwalker.Id) && InCombat() && ObjectManager.Me.CastingTimeLeft > 5 / (ObjectManager.GetNumberAttackPlayer() > 0 ? ObjectManager.GetNumberAttackPlayer() : 1))
            AbortCast();

        if((ObjectManager.Pet.HealthPercent >= 95 || ObjectManager.Me.HealthPercent < 15) && ObjectManager.Me.CastingSpellId == HealthFunnel.Id)
            AbortCast();

        double combinedHealth = 0;

        foreach(WoWUnit ele in targets)
        {
            combinedHealth += ele.HealthPercent;
        }

        //Sacrifice
        if(((ObjectManager.Pet.HealthPercent <= 5 && combinedHealth > 10) || ObjectManager.Me.HealthPercent <= 5) && PetIsKnownSpell("Sacrifice") && (ObjectManager.Pet.IsAlive || ObjectManager.Pet.HealthPercent > 0))
            Lua.LuaDoString("CastSpellByName(\"Sacrifice\");");

        //Use Healthstone
        if(ObjectManager.Me.HealthPercent <= 20 && currentHealthstone != null && InCombat() && (ItemsManager.GetItemCountById(currentHealthstone.stoneId) + ItemsManager.GetItemCountById(currentHealthstone.stoneIdTwo) + ItemsManager.GetItemCountById(currentHealthstone.stoneIdThree) > 0))
        {

            TimeSpan diff = DateTime.Now - currentHealthstone.timeStemp;
            int milliseconds = (int)diff.TotalMilliseconds;

            if(milliseconds < 120000)
                return;

            while(IsCasting())
            {
                AbortCast();
                Thread.Sleep(25);
            }

            ItemsManager.UseItem(currentHealthstone.itemName);
            foreach(Stones ele in listHStones)
            {
                if(ele.itemName == currentHealthstone.itemName)
                {
                    ele.timeStemp = DateTime.Now;
                    break;
                }
            }
            currentHealthstone = null;
        }
    }

    //Spells
    private void CastSpells()
    {

        if(_lockSpells)
            return;

        if(targets.Count > 1 && ObjectManager.Target == targets.LastOrDefault())
            return;

        if(ObjectManager.Target.Guid != 0 && TargetIsEnemy() && InCombat() && ObjectManager.Me.CastingSpellId != DrainSoul.Id && ObjectManager.Me.CastingSpellId != DrainLife.Id)
            MovementManager.Face(ObjectManager.Target.Position);

        if(ObjectManager.Target.IsAttackable && !IsCasting() && !GlobalCooldownActive())
        {

            //Fear (multi target)
            if(!fearedTargetExists && targets.Count > 2 && ObjectManager.Target.Guid == targets.LastOrDefault().Guid && !ObjectManager.Target.HaveBuff(Fear.Name) && Fear.MandatoryConditions && _lockTarget)
            {
                if(Fear.IsDistanceGood)
                {
                    Fear.Launch(true, false);
                    return;
                }
                else
                {
                    ManageMovement(Fear.MaxRange - 1);
                    Fear.Launch(true, false);
                    return;
                }
            }

            //Fear (emergency)
            if(((targets.Count == 2 && !targets[0].HaveBuff(Fear.Name) && !targets[1].HaveBuff(Fear.Name)) || (targets.Count == 1 && !targets[0].HaveBuff(Fear.Name))) && Fear.MandatoryConditions && !fearedTargetExists && ((ObjectManager.Me.HealthPercent < 33 && ObjectManager.Target.HealthPercent > 50 && (!ObjectManager.Pet.IsAlive || ObjectManager.Pet.HealthPercent < 10 || GetCurrentPetType().Contains("Imp"))) || ObjectManager.Target.IsElite))
            {
                if(targets.Count == 2)
                {
                    ObjectManager.Me.Target = targets.LastOrDefault().Guid;
                    _lockTarget = true;
                }

                if(Fear.IsDistanceGood)
                {
                    Fear.Launch(true, false);
                    return;
                }
                else
                {
                    ManageMovement(Fear.MaxRange - 1);
                    Fear.Launch(true, false);
                    return;
                }
            }

            //Drain Soul
            if(!TargetIsTrivial() && !ObjectManager.Target.HaveBuff(DrainSoul.Name) && ItemsManager.GetItemCountById(soulShardId) <= 3 && ObjectManager.Target.HealthPercent <= hpEnemyUseSpell && DrainSoul.MandatoryConditions)
            {
                DrainSoul.Launch(true, false);
                return;
            }

            //Health Funnel
            if((ObjectManager.Pet.IsAlive || ObjectManager.Pet.HealthPercent > 0) && (ObjectManager.Pet.HealthPercent <= healthFunnelHpPet || (ObjectManager.Pet.HealthPercent <= healthFunnelHpPet * 2 && targets.Count > 2)) && HealthFunnel.MandatoryConditions && !ObjectManager.Me.HaveBuff(HealthFunnel.Name) && ObjectManager.Me.HealthPercent >= healthFunnelHpMe)
            {
                if(ObjectManager.Me.Position.DistanceTo(ObjectManager.Pet.Position) < HealthFunnel.MaxRange)
                {
                    HealthFunnel.Launch(true, false);
                    return;
                }
                else
                {
                    ManageMovement(HealthFunnel.MaxRange - 1);
                    HealthFunnel.Launch(true, false);
                    return;
                }
            }

            //Life Tap (mandatory)
            if(LifeTap.KnownSpell && ObjectManager.Me.ManaPercentage <= 15 && ObjectManager.Me.HealthPercent >= 33)
            {
                LifeTap.Launch(false, false);
                return;
            }

            //Amplify Curse
            if((targets.Count > 1 || ObjectManager.Target.IsElite) && ObjectManager.Target.HealthPercent > 50 && AmplifyCurse.MandatoryConditions)
            {
                AmplifyCurse.Launch();
                return;
            }

            //Shadow Trance procc
            if(ObjectManager.Me.HaveBuff("Shadow Trance") && ObjectManager.Me.ManaPercentage > 20 && ObjectManager.Target.HealthPercent > 20 && ShadowBolt.MandatoryConditions)
            {
                ShadowBolt.Launch();
                return;
            }

            //Siphon Life
            if(!ObjectManager.Target.HaveBuff(SiphonLife.Name) && ObjectManager.Me.ManaPercentage >= manaUseSpell / 2 && ObjectManager.Target.HealthPercent >= hpEnemyUseSpell / 3 && SiphonLife.MandatoryConditions)
            {
                SiphonLife.Launch(false, false);
                return;
            }

            //Curse of Agony
            if(!ObjectManager.Target.HaveBuff(CurseOfAgony.Name) && ObjectManager.Target.HealthPercent >= hpEnemyUseSpell && CurseOfAgony.MandatoryConditions)
            {
                CurseOfAgony.Launch(false, false);
                return;
            }

            //Corrutpion
            if(!ObjectManager.Target.HaveBuff(Corruption.Name) && ObjectManager.Target.HealthPercent >= WarlockFightClassSettings.CurrentSettings.hpCorruption && Corruption.MandatoryConditions)
            {
                Corruption.Launch(true, false);
                return;
            }

            //Immolate
            if(!SiphonLife.KnownSpell && !ObjectManager.Target.HaveBuff(Immolate.Name) && ObjectManager.Me.ManaPercentage >= manaUseSpell * 2 && ObjectManager.Target.HealthPercent >= hpEnemyUseSpell && Immolate.MandatoryConditions)
            {
                Immolate.Launch(true, false);
                return;
            }

            //Life Tap (efficency)
            if(LifeTap.KnownSpell && ObjectManager.Me.ManaPercentage <= 75 && ObjectManager.Me.HealthPercent >= 50 && targets.Count < 2)
            {
                LifeTap.Launch(false, false);
                return;
            }

            //Drain Life
            if(!ObjectManager.Target.HaveBuff(DrainLife.Name) && ObjectManager.Target.HealthPercent >= hpEnemyUseSpell && ObjectManager.Me.HealthPercent <= 80 && DrainLife.MandatoryConditions)
            {
                if(ObjectManager.Me.Position.DistanceTo(ObjectManager.Pet.Position) < DrainLife.MaxRange)
                {
                    DrainLife.Launch(true, false);
                    return;
                }
                else
                {
                    ManageMovement(DrainLife.MaxRange - 1);
                    DrainLife.Launch(true, false);
                    return;
                }
            }

            //Shadow Bolt (low level)
            if(((ObjectManager.Target.HealthPercent > 20 && ObjectManager.Me.ManaPercentage >= 50 && !Corruption.KnownSpell) || (ObjectManager.Me.ManaPercentage > manaUseSpell && targets.Count > 1 && !CurseOfAgony.KnownSpell)) && ShadowBolt.MandatoryConditions)
            {
                ShadowBolt.Launch(true, false);
                return;
            }

            //Default attack
            if(HaveWand())
            {
                if(!Shoot.IsDistanceGood)
                    return;

                SpellToActionbar("Shoot", 2);
                Lua.LuaDoString("if not IsCurrentAction(2) then UseAction(2) end");
                return;
            }
            else if(!HaveWand() && !ObjectManager.Target.HaveBuff(Fear.Name))
            {
                if(ObjectManager.Me.Position.DistanceTo(ObjectManager.Target.Position) > 5)
                    ManageMovement(5);
                SpellToActionbar("Attack", 2);
                Lua.LuaDoString("if not IsCurrentAction(2) then UseAction(2) end");
            }
        }
    }

    //Pet Behaviour
    private void PetControl()
    {
        
        if(!ObjectManager.Target.InCombatWithMe)
            return;

        if(InCombat() && ObjectManager.Pet.Target == 0)
        {
            ObjectManager.Pet.Target = ObjectManager.Target.Guid;
            Lua.LuaDoString("PetAttack()");
            return;
        }

        if(targets == null)
            return;

        WoWUnit myCurrentTarget = ObjectManager.Target;
        _lockSpells = true;
        petTarget = targets.FirstOrDefault(o => o.IsTargetingMe && !o.IsTargetingMyPet && o.Position.DistanceTo(ObjectManager.Me.Position) <= 25);

        if(ObjectManager.Pet.Target != 0 && !ObjectManager.Pet.IsCast && InCombat() && targets.Count > 0)
        {
            for(int i = 0; i < targets.Count; i++)
            {
                if(targets[i].IsTargetingMe && !targets[i].IsTargetingMyPet)
                {
                    if(ObjectManager.Pet.Target == targets[i].Guid )
                    {
                        if(GetCurrentPetType() == "Voidwalker")
                            Lua.LuaDoString("CastSpellByName(\"Torment\");");
                        
                        _lockSpells = false;
                        return;
                    }

                    if(ObjectManager.Pet.Target != targets[i].Guid)
                    {
                        ObjectManager.Me.Target = targets[i].Guid;
                        Lua.LuaDoString("PetAttack()");
                        ObjectManager.Me.Target = myCurrentTarget.Guid;
                        if(GetCurrentPetType() == "Voidwalker")
                            Lua.LuaDoString("CastSpellByName(\"Torment\");");
                        
                        _lockSpells = false;
                        return;
                    }
                }
            }
        }

        if(fearedTargetExists)
        {
            try
            {
                if(targets.Find(o => o.HaveBuff(Fear.Name)).Guid == ObjectManager.Pet.Target)
                    Lua.LuaDoString("PetAttack()");
            }
            catch(Exception e)
            {

            }
        }
        _lockSpells = false;
    }

    //Buffs & check Pet 
    private void Buffs()
    {
        //manaBuff = 25;

        while((ObjectManager.Me.HaveBuff("Eat") || ObjectManager.Me.HaveBuff("Drink")) && !InCombat())
        {
            if(PetIsKnownSpell("Consume Shadows") && !ObjectManager.Pet.HaveBuff("Consume Shadows") && ObjectManager.Pet.HealthPercent <= 50 && ObjectManager.Pet.ManaPercentage > 20)
                Lua.LuaDoString("CastSpellByName(\"Consume Shadows\");");

            UpdateStatusFrame("Regeneration");
            Thread.Sleep(250);
        }

        if(ObjectManager.Me.IsSwimming && UnendingBreath.MandatoryConditions && !ObjectManager.Me.HaveBuff(UnendingBreath.Name))
            UnendingBreath.Launch(false, true);

        //Check Pet
        if(PetIsKnownSpell("Consume Shadows") && ObjectManager.Pet.HealthPercent <= 40 && ObjectManager.Pet.ManaPercentage >= 20 && !ObjectManager.Pet.HaveBuff("Consume Shadows") && !InCombat() && ObjectManager.Me.Position.DistanceTo(ObjectManager.Pet.Position) <= 9 - rnd.Next(7))
        {
            Lua.LuaDoString("CastSpellByName(\"Consume Shadows\");");
            Pause("Consume Shadows");
            return;
        }

        if(HealthFunnel.KnownSpell && ObjectManager.Me.HealthPercent >= 50 && !ObjectManager.Me.HaveBuff(HealthFunnel.Name) && ObjectManager.Pet.HealthPercent <= 1 && ObjectManager.Pet.IsAlive && ObjectManager.Pet.HealthPercent > 0 && !ObjectManager.Pet.HaveBuff("Consume Shadows"))
        {
            HealthFunnel.Launch(true, false);
            Thread.Sleep((int)HealthFunnel.Cooldown);
            return;
        }

        if(SummonVoidwalker.KnownSpell && ItemsManager.GetItemCountById(soulShardId) > 1 && GetCurrentPetType() == "Imp")
            Lua.LuaDoString("PetDismiss();");

        if((!ObjectManager.Pet.IsAlive || ObjectManager.Pet.HealthPercent <= 0) && SummonImp.KnownSpell && !Usefuls.MapZoneName.Contains("Deeprun") && !ObjectManager.Me.InTransport && !Conditions.ForceIgnoreIsAttacked)
        {
            _stopMovement = true;
            SummonPet();
            _hasFinished = true;
        }

        if(ObjectManager.Pet.IsAlive)
        {
            CheckPetAutocast();
            SetPetStance("Defensive");
        }

        //Actual Buffs
        if(DemonArmor.KnownSpell && !ObjectManager.Me.HaveBuff(DemonArmor.Name) && ObjectManager.Me.ManaPercentage >= manaBuff)
        {
            DemonArmor.Launch(false, true);
        }
        else if(!ObjectManager.Me.HaveBuff(DemonSkin.Name) && !DemonArmor.KnownSpell && ObjectManager.Me.ManaPercentage >= manaBuff)
        {
            DemonSkin.Launch(false, true);
        }

        if(LifeTap.KnownSpell && ObjectManager.Me.ManaPercentage <= 75 && ObjectManager.Me.HealthPercent >= 50 && !Conditions.ForceIgnoreIsAttacked)
            LifeTap.Launch(false, true);

        //Healstone & Soulstone
        if(ObjectManager.Me.ManaPercentage > 35 && ItemsManager.GetItemCountById(soulShardId) > 2 && !MovementManager.InMovement)
            HealthSoulStone();
        
        //Choose HS & SS
        ChooseHealthstone();
        currentSoulstone = listSStones.FirstOrDefault(o => ItemsManager.GetItemCountById(o.stoneId) > 0);
        
        //Use Soulstone
        if(currentSoulstone != null && !InCombat() && ItemsManager.GetItemCountById(currentSoulstone.stoneId) > 0 && !ObjectManager.Me.HaveBuff("Soulstone Resurrection") && !MovementManager.InMovement)
        {
            TimeSpan diff = DateTime.Now - currentSoulstone.timeStemp;
            int milliseconds = (int)diff.TotalMilliseconds;

            if(milliseconds < 900000)
                return;

            while(IsCasting())
            {
                if(!ObjectManager.Me.CastingSpell.Name.Contains("Healthstone") && !ObjectManager.Me.CastingSpell.Name.Contains("Summon"))
                    AbortCast();

                Thread.Sleep(25);
            }

            ItemsManager.UseItem(currentSoulstone.itemName);
            foreach(Stones ele in listSStones)
            {
                if(ele.itemName == currentSoulstone.itemName)
                {
                    ele.timeStemp = DateTime.Now;
                    break;
                }
            }
            currentSoulstone = null;
        }

        //Unending Breath (allies)
        ManageBlacklistedPlayers();
        if(UnendingBreath.KnownSpell && GetListWoWPlayer().Count < 5 && UnendingBreath.MandatoryConditions)
            BuffAllies(GetListWoWPlayer());
    }

    //Find Targets
    private List<WoWUnit> GetAttackers()
    {
        List<WoWUnit> attackersList = ObjectManager.GetWoWUnitAttackables(40);

        if(attackersList.Count <= 0)
            return (List<WoWUnit>)null;

        attackersList.RemoveAll(o => !o.InCombat || !o.IsAlive || (o.IsTaggedByOther && !o.IsTargetingMeOrMyPet));
        attackersList.Sort((x, y) => x.Health != y.Health ? x.Health.CompareTo(y.Health) : x.Guid.CompareTo(y.Guid));

        try
        {
            if(attackersList.Find(o => o.HaveBuff(Fear.Name)).IsValid)
                fearedTargetExists = true;
                
        }
        catch(Exception e)
        {
            fearedTargetExists = false;
        }
       
        return attackersList;
    }

    //Create Healthstone & Soulstone
    private void HealthSoulStone()
    {

        if(ItemsManager.GetItemCountById(soulShardId) > 2 && !IsCasting())
        {
            foreach(Stones ele in listSStones)
            {
                if(ele.spell.KnownSpell)
                {
                    if(ItemsManager.GetItemCountById(ele.stoneId) < 1)
                    {
                        _stopMovement = true;
                        Logging.Write("Creating " + ele.itemName);
                        Thread.Sleep(50);
                        Lua.LuaDoString("CastSpellByName(\"" + ele.castSpellName + "\")");
                        Thread.Sleep(100);
                        _hasFinished = true;
                        return;
                    }
                    else
                    {
                        break;
                    }
                }
            }
            foreach(Stones ele in listHStones)
            {
                if(ele.spell.KnownSpell)
                {
                    if(ItemsManager.GetItemCountById(ele.stoneId) + ItemsManager.GetItemCountById(ele.stoneIdTwo) + ItemsManager.GetItemCountById(ele.stoneIdThree) < 1)
                    {
                        _stopMovement = true;
                        Logging.Write("Creating " + ele.itemName);
                        Thread.Sleep(50);
                        Lua.LuaDoString("CastSpellByName(\"" + ele.castSpellName + "\")");
                        Thread.Sleep(100);
                        _hasFinished = true;
                        return;
                    }
                    else
                    {
                        break;
                    }
                }
            }
        }
    }

    //##Support functions 
    private void ManageBlacklistedPlayers()
    {
        ManageBlacklistedPlayersUnendingBreath();
        ManageBlacklistedPlayersHealthstone();
    }

    private void ManageBlacklistedPlayersUnendingBreath()
    {
        if(blacklistedPlayersUnendingBreath == null || blacklistedPlayersUnendingBreath.Count <= 0)
            return;

        TimeSpan diff;
        int milliseconds;

        try
        {
            foreach(KeyValuePair<WoWPlayer, DateTime> ele in blacklistedPlayersUnendingBreath)
            {
                diff = DateTime.Now - ele.Value;
                milliseconds = (int)diff.TotalMilliseconds;

                if(milliseconds > 900000)
                {
                    blacklistedPlayersUnendingBreath.Remove(ele.Key);
                    Logging.Write("Removing player: " + ele.Key.Name + " with time stamp: " + ele.Value + " from blacklisted players list");
                    break;
                }
            }
        }
        catch(Exception e)
        {
            Logging.Write("Manage blacklisted players: " + e);
        }
    }

    private void ManageBlacklistedPlayersHealthstone()
    {
        if(blacklistedPlayersHealthstones == null || blacklistedPlayersHealthstones.Count <= 0)
            return;

        TimeSpan diff;
        int milliseconds;

        try
        {
            foreach(KeyValuePair<WoWPlayer, DateTime> ele in blacklistedPlayersHealthstones)
            {
                diff = DateTime.Now - ele.Value;
                milliseconds = (int)diff.TotalMilliseconds;

                if(milliseconds > 1800000)
                {
                    blacklistedPlayersHealthstones.Remove(ele.Key);
                    Logging.Write("Removing player: " + ele.Key.Name + " with time stamp: " + ele.Value + " from blacklisted players list");
                    break;
                }
            }
        }
        catch(Exception e)
        {
            Logging.Write("Manage blacklisted players: " + e);
        }
    }

    private void BuffAllies(List<WoWPlayer> alliesList)
    {
        alliesList.RemoveAll(o => o.Position.DistanceTo(ObjectManager.Me.Position) >= 25 || ObjectManager.Me.PlayerFaction != o.PlayerFaction || o.HaveBuff(UnendingBreath.Name) || blacklistedPlayersUnendingBreath.ContainsKey(o));

        if(alliesList == null || alliesList.Count <= 0)
            return;

        ObjectManager.Me.Target = alliesList.FirstOrDefault().Guid;
        UnendingBreath.Launch(false, true);

        if(ObjectManager.Target.HaveBuff(UnendingBreath.Name))
        {
            if(!blacklistedPlayersUnendingBreath.ContainsKey(alliesList.FirstOrDefault()))
            {
                blacklistedPlayersUnendingBreath.Add(alliesList.FirstOrDefault(), DateTime.Now);
                Logging.Write("Adding player: " + alliesList.FirstOrDefault().Name + " with time stamp: " + DateTime.Now + " to blacklist Unending Breath");
            }

            alliesList.Remove(alliesList.FirstOrDefault());
            Lua.LuaDoString("ClearTarget()");
        }
    }

    private void GiveHealthstoneOnTrade(WoWPlayer tradingPlayer)
    {
        if(blacklistedPlayersHealthstones.ContainsKey(tradingPlayer))
        {
            Logging.Write("Current trading partner: " + tradingPlayer.Name + " is still blacklisted. Cancel trade... .");
            Thread.Sleep(rnd.Next(1, 4) * 1000);
            Lua.LuaDoString("CloseTrade()");
            return;
        }

        if((ObjectManager.Target == null || ObjectManager.Target.Guid != tradingPlayer.Guid) && tradingPlayer != null)
            ObjectManager.Me.Target = tradingPlayer.Guid;

        Thread.Sleep(rnd.Next(4, 10) * 1000);
        Lua.LuaDoString("for i=0,4 do for x=1,GetContainerNumSlots(i) do y=GetContainerItemLink(i,x) if y then if string.find(y, \"" + ChooseHealthstone().itemName + "\") then PickupContainerItem(i,x); DropItemOnUnit(\"target\"); return; end end end end");
        Thread.Sleep(rnd.Next(2, 6) * 1000);

        if(!Lua.LuaDoString<bool>("local chatItemLink = GetTradePlayerItemLink(1) if chatItemLink then return true else return false end"))
            return;

        Lua.LuaDoString("AcceptTrade()");

        int counter = 0;
        while(Lua.LuaDoString<bool>("local chatItemLink = GetTradePlayerItemLink(1) if chatItemLink then return true else return false end"))
        {
            if(counter > 5)
            {
                Logging.Write("Accepting trade offer took too long, aborting... .");
                break;
            }

            Thread.Sleep(1000);
            Lua.LuaDoString("AcceptTrade()");
            counter++;
        }
    }

    private List<WoWPlayer> GetListWoWPlayer()
    {
        return ObjectManager.GetObjectWoWPlayer();
    }

    private WoWPlayer GetClosestPlayerHealthstone()
    {
        List<WoWPlayer> allPlayers = GetListWoWPlayer();
        allPlayers.Sort((x, y) => x.Position.DistanceTo(ObjectManager.Me.Position).CompareTo(y.Position.DistanceTo(ObjectManager.Me.Position)));
        allPlayers.RemoveAll(o => ObjectManager.Me.PlayerFaction != o.PlayerFaction);

        if(allPlayers == null || allPlayers.Count <= 0)
            return (WoWPlayer)null;
        else
            return allPlayers.FirstOrDefault();
    }

    private Stones ChooseHealthstone()
    {
        return currentHealthstone = listHStones.FirstOrDefault(o => ItemsManager.GetItemCountById(o.stoneId) > 0 || ItemsManager.GetItemCountById(o.stoneIdTwo) > 0 || ItemsManager.GetItemCountById(o.stoneIdThree) > 0);
    }

    private bool IsDotted(WoWUnit target)
    {
        if(target == null)
        {
            Logging.Write("IsDotted: Currently no target");
            return true;
        }

        if(Immolate.KnownSpell && !target.HaveBuff(Immolate.Name) && target.HealthPercent >= Immolate.HpPercentUse && !SiphonLife.KnownSpell || CurseOfAgony.KnownSpell && !target.HaveBuff(CurseOfAgony.Name) && target.HealthPercent >= CurseOfAgony.HpPercentUse || Corruption.KnownSpell && !target.HaveBuff(Corruption.Name) && target.HealthPercent >= Corruption.HpPercentUse || SiphonLife.KnownSpell && !target.HaveBuff(SiphonLife.Name) && target.HealthPercent >= SiphonLife.HpPercentUse)
            return false;

        return true;
    }

    private void InitializeStones()
    {
        listHStones.Add(healthstoneMinor);
        listHStones.Add(healthstoneLesser);
        listHStones.Add(healthstone);
        listHStones.Add(greaterHealthstone);
        listHStones.Add(majorHealthstone);
        listHStones.Reverse();

        listSStones.Add(minorSoulstone);
        listSStones.Add(lesserSoulstone);
        listSStones.Add(soulstone);
        listSStones.Add(greaterSoulstone);
        listSStones.Add(majorSoulstone);
        listSStones.Reverse();
    }

    private void SummonPet()
    {
        //manaSummon = 50;
        UpdateStatusFrame("Summon Pet");

        if(ItemsManager.GetItemCountById(soulShardId) >= 1 && ObjectManager.Me.ManaPercentage >= manaSummon && SummonVoidwalker.MandatoryConditions)
            SummonVoidwalker.Launch(true, false);
        else if(ObjectManager.Me.ManaPercentage >= 50 && (!ObjectManager.Pet.IsAlive || ObjectManager.Pet.HealthPercent <= 0) && SummonImp.MandatoryConditions)
            SummonImp.Launch(true, false);

        while(ObjectManager.Me.CastingSpellId == SummonImp.Id || ObjectManager.Me.CastingSpellId == SummonVoidwalker.Id)
        {
            if((ObjectManager.Me.CastingSpellId == SummonImp.Id || ObjectManager.Me.CastingSpellId == SummonVoidwalker.Id) && InCombat() && ObjectManager.Me.CastingTimeLeft > 5 / (ObjectManager.GetNumberAttackPlayer() > 0 ? ObjectManager.GetNumberAttackPlayer() : 1))
                AbortCast();

            Thread.Sleep(100);
        }

    }

    private bool SummonMakesSense()
    {
        if(ObjectManager.GetNumberAttackPlayer() <= 1 && !ObjectManager.Target.IsElite && SummonImp.KnownSpell && ObjectManager.Me.HealthPercent > 40 && ObjectManager.Target.HealthPercent > 50 && (!ObjectManager.Pet.IsAlive || ObjectManager.Pet.HealthPercent <= 0))
            return true;
        else
            return false;
    }

    private void SetPetStance(String stance)
    {
        switch(stance)
        {
            case "Aggressive":
                Lua.LuaDoString("PetAggressiveMode()");
                break;
            case "Passive":
                Lua.LuaDoString("PetPassiveMode()");
                break;
            default:
                Lua.LuaDoString("PetDefensiveMode()");
                break;
        }
    }

    private void PetEnableAutocast(String name) // by Fey
    {
        Lua.LuaDoString("local i,g=1,0 while GetSpellName(i,\"pet\") do if GetSpellName(i,\"pet\")==\"" + name + "\" then g=i end i=i+1 end local _,y = GetSpellAutocast(g,\"pet\") if not y then ToggleSpellAutocast(g,\"pet\") end");
    }

    private void PetDisableAutocast(String name) // by Fey
    {
        Lua.LuaDoString("local i,g=1,0 while GetSpellName(i,\"pet\") do if GetSpellName(i,\"pet\")==\"" + name + "\" then g=i end i=i+1 end local _,y = GetSpellAutocast(g,\"pet\") if (y and UnitFactionGroup(\"target\")) then ToggleSpellAutocast(g,\"pet\") end");
    }

    private bool PetIsKnownSpell(String name)
    {
        return Lua.LuaDoString<bool>("local i = 1 while true do local spellName, spellRank = GetSpellName(i, BOOKTYPE_PET) if not spellName then do return false end end if string.find(spellName, \"" + name + "\") then do return true end end i = i + 1 end");
    }

    private String GetCurrentPetType()
    {
        return Lua.LuaDoString<String>("return UnitCreatureFamily(\"pet\");");
    }

    private void CheckPetAutocast()
    {
        switch(GetCurrentPetType())
        {
            case "Voidwalker":

                break;

            case "Imp":
                PetEnableAutocast("Firebolt");
                if(PetIsKnownSpell("Blood Pact"))
                    PetEnableAutocast("Blood Pact");
                if(PetIsKnownSpell("Fire Shield"))
                    PetEnableAutocast("Fire Shield");
                if(PetIsKnownSpell("Phase Shift"))
                    PetEnableAutocast("Phase Shift");
                break;

            default:
                Logging.Write("No pet");
                break;
        }
    }

    public void SpellToActionbar(String spellName, int slotId)
    {
        Lua.LuaDoString("local i = 1 while true do local spellName, spellRank = GetSpellName(i, BOOKTYPE_SPELL) if not spellName then do break end end if string.find(spellName, \"" + spellName + "\") then do PickupSpell(i, BOOKTYPE_SPELL) PlaceAction(" + slotId + ") ClearCursor() break end end i = i + 1 end;");
    }

    private bool HaveWand()
    {
        return Lua.LuaDoString<bool>("return HasWandEquipped();");
    }

    private static bool InCombat()
    {
        return Lua.LuaDoString<bool>("return UnitAffectingCombat(\"player\");") || Lua.LuaDoString<bool>("return UnitAffectingCombat(\"pet\");");
    }

    private bool IsCasting()
    {
        return ObjectManager.Me﻿.CastingSpellId != 0;
    }

    public bool IsTagged(WoWUnit target)
    {
        if(target.IsTaggedByOther && !target.IsTargetingMeOrMyPet)
        {
            Fight.StopFight();
            Lua.LuaDoString("ClearTarget();");
            System.Threading.Thread.Sleep(300 + Usefuls.Latency);
            return true;
        }
        return false;
    }

    //Stopping current casting / channeling of spell
    private void AbortCast()
    {
        if(ObjectManager.Me﻿.CastingSpell﻿ != null)
        {
            UpdateStatusFrame("Abort Cast: " + ObjectManager.Me.CastingSpell.Name);
            Logging.Write("Canceling spell: " + ObjectManager.Me﻿.CastingSpell﻿.Name);
            Lua.LuaDoString("SpellStopCasting();");
            robotManager.Helpful.Keyboard.DownKey(wManager.Wow.Memory.WowMemory.Memory.WindowHandle, System.Windows.Forms.Keys.Down);
            Thread.Sleep(10);
            robotManager.Helpful.Keyboard.UpKey(wManager.Wow.Memory.WowMemory.Memory.WindowHandle, System.Windows.Forms.Keys.Down);
        }
    }

    //Using Shadow Bolt cooldown to determine if GCD is active
    private bool GlobalCooldownActive()
    {
        return Lua.LuaDoString<bool>("local i = 1 while true do local spellName, spellRank = GetSpellName(i, BOOKTYPE_SPELL) if not spellName then do return false end end if string.find(spellName, \"Shadow Bolt\") then local start, duration, enabled = GetSpellCooldown(i, BOOKTYPE_SPELL) if duration > 0 then return true else return false end end i = i + 1 end");
    }

    private bool CooldownActive(Cspell spell)
    {
        return Lua.LuaDoString<bool>("local i = 1 while true do local spellName, spellRank = GetSpellName(i, BOOKTYPE_SPELL) if not spellName then do return false end end if string.find(spellName, \"" + spell.Name + "\") then local start, duration, enabled = GetSpellCooldown(i, BOOKTYPE_SPELL) if duration > 0 then return true else return false end end i = i + 1 end");
    }

    //Wait for Voidwalker to finish Consume Shadows
    private void Pause(String name)
    {
        UpdateStatusFrame("Waiting for " + name);   
        if(!Products.InPause)
        {
            robotManager.Products.Products.InPause = true;
            Thread.Sleep(1000);
        }

        while(!InCombat() && ObjectManager.Pet.HaveBuff(name))
        {
            Thread.Sleep(1000);
        }

        robotManager.Products.Products.InPause = false;
    }

    //Determine if target is hostile
    private bool TargetIsEnemy()
    {
        return Lua.LuaDoString<bool>("return UnitCanAttack(\"player\", \"target\");");
    }

    //Determine if we can get Soulshard from enemy
    private bool TargetIsTrivial()
    {
        return Lua.LuaDoString<bool>("return UnitIsTrivial(\"target\");");
    }

    private void IngameFrame()
    {

        string frame = @"if not CombatStatus then
                    CombatStatus = CreateFrame('Frame')
                    CombatStatus:ClearAllPoints()
                    CombatStatus:SetWidth(340)
                    CombatStatus:SetHeight(130)

                    local texture = CombatStatus:CreateTexture(nil,'BACKGROUND')
                    texture:SetTexture('Interface\\DialogFrame\\UI-DialogBox-Background')
                    texture:SetAllPoints(CombatStatus)
                    CombatStatus.texture = texture
                    CombatStatus:SetPoint('TOP', 0, -20)

                    CombatStatus.status = CombatStatus:CreateFontString(nil, 'OVERLAY', 'GameFontNormal')
                    CombatStatus.status:SetPoint('CENTER', CombatStatus, 'TOP', 0, 10)
                    CombatStatus.status:SetText('Status: Initialize')

                    CombatStatus.text = CombatStatus:CreateFontString(nil, 'OVERLAY', 'GameFontNormal')
                    CombatStatus.text:SetPoint('LEFT', CombatStatus, 'LEFT', 3, 50)
                    CombatStatus.text:SetText('')
                    CombatStatus.text11 = CombatStatus:CreateFontString(nil, 'OVERLAY', 'GameFontNormal')
                    CombatStatus.text11:SetPoint('CENTER', CombatStatus, 'CENTER', 3, 30)
                    CombatStatus.text11:SetText('')

                    CombatStatus.text2 = CombatStatus:CreateFontString(nil, 'OVERLAY', 'GameFontNormal')
                    CombatStatus.text2:SetPoint('LEFT', CombatStatus, 'LEFT', 3, 10)
                    CombatStatus.text2:SetText('')
                    CombatStatus.text21 = CombatStatus:CreateFontString(nil, 'OVERLAY', 'GameFontNormal')
                    CombatStatus.text21:SetPoint('CENTER', CombatStatus, 'CENTER', 3, -10)
                    CombatStatus.text21:SetText('')

                    CombatStatus.text3 = CombatStatus:CreateFontString(nil, 'OVERLAY', 'GameFontNormal')
                    CombatStatus.text3:SetPoint('LEFT', CombatStatus, 'LEFT', 3, -30)
                    CombatStatus.text3:SetText('')
                    CombatStatus.text31 = CombatStatus:CreateFontString(nil, 'OVERLAY', 'GameFontNormal')
                    CombatStatus.text31:SetPoint('CENTER', CombatStatus, 'CENTER', 3, -50)
                    CombatStatus.text31:SetText('')

                    local CloseButton = CreateFrame('Button','CloseButton', CombatStatus, 'UIPanelCloseButton')
                    CloseButton:SetHeight(22)
                    CloseButton:SetWidth(22)
                    CloseButton:SetPoint('TOPRIGHT', CombatStatus, 'TOPRIGHT', 5, 5)
                    CloseButton:SetScript('OnClick', function() CloseButton:GetParent():Hide() end)

                    CombatStatus:SetMovable(true)        
                    CombatStatus:EnableMouse(true)       
                    CombatStatus:SetScript('OnMouseDown', function() CombatStatus:StartMoving() end)        
                    CombatStatus:SetScript('OnMouseUp', function() CombatStatus:StopMovingOrSizing() end)
                    
                    CombatStatus:Show()

                    elseif not CombatStatus:Show() then
                    CombatStatus:Show()
                    end
                    ";

        Lua.LuaDoString(frame);
    }

    private void UpdateStatusFrame(String status)
    {
        if(Lua.LuaDoString<bool>("if CombatStatus then return true else return false end"))
            Lua.LuaDoString("CombatStatus.status:SetText(\"Status: " + status + "\")");
    }

    private void ResetCombatFrame()
    {
        Lua.LuaDoString("CombatStatus.text:SetText('')");
        Lua.LuaDoString("CombatStatus.text11:SetText('')");
        Lua.LuaDoString("CombatStatus.text2:SetText('')");
        Lua.LuaDoString("CombatStatus.text21:SetText('')");
        Lua.LuaDoString("CombatStatus.text3:SetText('')");
        Lua.LuaDoString("CombatStatus.text31:SetText('')");
    }

    private void UpdateCombatFrame()
    {
        try
        {
        /*
            if(setRange == 1)
                UpdateRangeFrame("Melee");
            else if(setRange == 2)
                UpdateRangeFrame("Mid-range");
            else
                UpdateRangeFrame("Default");
                */

            if(targets.Count > 0 && Lua.LuaDoString<bool>("if CombatStatus then return true else return false end"))
            {
                if(targets.Count == 1)
                {
                    Lua.LuaDoString("CombatStatus.text:SetText(\"Target 1:  " + targets[0].Name + "\")");
                    Lua.LuaDoString("CombatStatus.text11:SetText(\"HP:  " + targets[0].HealthPercent + "%  Dotted:  " + IsDotted(targets[0]) + "  Feared:  " + targets[0].HaveBuff(Fear.Name) + "  Pet Target:  " + ObjectManager.Pet.Target.Equals(targets[0].Guid) + "\")");
                    Lua.LuaDoString("CombatStatus.text2:SetText(\" \")");
                    Lua.LuaDoString("CombatStatus.text21:SetText(\"\")");
                    Lua.LuaDoString("CombatStatus.text3:SetText(\" \")");
                    Lua.LuaDoString("CombatStatus.text31:SetText(\"\")");
                }
                if(targets.Count == 2)
                {
                    Lua.LuaDoString("CombatStatus.text:SetText(\"Target 1:  " + targets[0].Name + "\")");
                    Lua.LuaDoString("CombatStatus.text11:SetText(\"HP:  " + targets[0].HealthPercent + "%  Dotted:  " + IsDotted(targets[0]) + "  Feared:  " + targets[0].HaveBuff(Fear.Name) + "  Pet Target:  " + ObjectManager.Pet.Target.Equals(targets[0].Guid) + "\")");
                    Lua.LuaDoString("CombatStatus.text2:SetText(\"Target 2:  " + targets[1].Name + "\")");
                    Lua.LuaDoString("CombatStatus.text21:SetText(\"HP:  " + targets[1].HealthPercent + "%  Dotted:  " + IsDotted(targets[1]) + "  Feared:  " + targets[1].HaveBuff(Fear.Name) + "  Pet Target:  " + ObjectManager.Pet.Target.Equals(targets[1].Guid) + "\")");
                    Lua.LuaDoString("CombatStatus.text3:SetText(\"  \")");
                    Lua.LuaDoString("CombatStatus.text31:SetText(\"\")");
                }
                if(targets.Count >= 3)
                {
                    Lua.LuaDoString("CombatStatus.text:SetText(\"Target 1:  " + targets[0].Name + "\")");
                    Lua.LuaDoString("CombatStatus.text11:SetText(\"HP:  " + targets[0].HealthPercent + "%  Dotted:  " + IsDotted(targets[0]) + "  Feared:  " + targets[0].HaveBuff(Fear.Name) + "  Pet Target:  " + ObjectManager.Pet.Target.Equals(targets[0].Guid) + "\")");
                    Lua.LuaDoString("CombatStatus.text2:SetText(\"Target 2:  " + targets[1].Name + "\")");
                    Lua.LuaDoString("CombatStatus.text21:SetText(\"HP:  " + targets[1].HealthPercent + "%  Dotted:  " + IsDotted(targets[1]) + "  Feared:  " + targets[1].HaveBuff(Fear.Name) + "  Pet Target:  " + ObjectManager.Pet.Target.Equals(targets[1].Guid) + "\")");
                    Lua.LuaDoString("CombatStatus.text3:SetText(\"Target 3:  " + targets[2].Name + "\")");
                    Lua.LuaDoString("CombatStatus.text31:SetText(\"HP:  " + targets[2].HealthPercent + "%  Dotted:  " + IsDotted(targets[2]) + "  Feared:  " + targets[2].HaveBuff(Fear.Name) + "  Pet Target:  " + ObjectManager.Pet.Target.Equals(targets[2].Guid) + "\")");
                }
            }
        }
        catch(Exception e)
        {
            Logging.Write("Something went wrong in CombatStatusUpdate: " + e);
        }
    }

    public void ShowConfiguration()
    {
        WarlockFightClassSettings.Load();
        WarlockFightClassSettings.CurrentSettings.ToForm();
        WarlockFightClassSettings.CurrentSettings.Save();
    }
}

public sealed class WoWPlayerNameComparer : IEqualityComparer<WoWPlayer>
{
    public bool Equals(WoWPlayer x, WoWPlayer y)
    {
        if (ReferenceEquals(x, y)) return true;
        if (ReferenceEquals(x, null)) return false;
        if (ReferenceEquals(y, null)) return false;
        if (x.GetType() != y.GetType()) return false;
        return x.Name == y.Name;
    }

    public int GetHashCode(WoWPlayer obj)
    {
        return (obj.Name != null ? obj.Name.GetHashCode() : 0);
    }
}

public class Cspell : Spell
{
    private robotManager.Helpful.Timer _timer;

    public Cspell(string name, double cooldown, int hpPercentUse, int myManaPercentUse, List<String> immunity) : base(name)
    {
        Name = name;
        this._timer = new robotManager.Helpful.Timer(1);
        this.HpPercentUse = hpPercentUse;
        this.MyManaPercentUse = myManaPercentUse;
        this.Immunity = immunity;
    }

    public double Cooldown { get; set; }
    public int HpPercentUse { get; set; }
    public int MyManaPercentUse { get; set; }
    public List<String> Immunity { get; set; }

    //Spell not on cooldown, enough mana, in range and not immune to spell
    public bool MandatoryConditions
    {
        get
        {
            //Logging.Write("Is usable: " + VanillaIsSpellUsable(this) + " Distance good: " + this.IsDistanceGood + " Target not immune: " + this.TargetIsNotImmune);
            if(VanillaIsSpellUsable(this) && this.IsDistanceGood && this.TargetIsNotImmune)
                return true;
            else
                return false;
        }
    }

    public new bool IsDistanceGood
    {
        get
        {
            WoWUnit target = ObjectManager.Target;

            if(target.Guid != 0)
                return ObjectManager.Me.Position.DistanceTo(target.Position) < this.MaxRange + (ObjectManager.Me.Position.DistanceTo(target.Position) - target.GeHitBoxtDistance) - 3;
            else
                return true;
        }
    }

    public bool TargetIsNotImmune
    {
        get
        {
            WoWUnit target = ObjectManager.Target;

            if(target.Guid <= 0)
                return true;

            foreach(String ele in this.Immunity)
            {
                if(Lua.LuaDoString<String>("return UnitCreatureType(\"target\")") == ele)
                    return false;
            }
            return true;

        }
    }

    public bool IsReady
    {
        get
        {
            return this._timer.IsReady;
        }
    }

    public void Launch(bool stopMove, bool waitIsCast)
    {
        if(!this.IsReady)
            return;

        base.Launch(stopMove, waitIsCast);

        this._timer.Reset();
    }

    private bool VanillaIsSpellUsable(Cspell spell)
    {
        Lua.LuaDoString("local i = 1 while true do local spellName, spellRank = GetSpellName(i, BOOKTYPE_SPELL) if not spellName then do break end end if string.find(spellName, \"" + spell.Name + "\") then do PickupSpell(i, BOOKTYPE_SPELL) PlaceAction(" + 1 + ") ClearCursor() break end end i = i + 1 end;");
        return SpellManager.SpellUsable(spell.Name);
    }

}

public class Stones
{
    //Constructor Healthstones
    public Stones(Cspell spell, String castSpellName, String itemName, uint stoneId, uint stoneIdTwo, uint stoneIdThree, DateTime timeStemp)
    {
        this.spell = spell;
        this.castSpellName = castSpellName;
        this.itemName = itemName;
        this.stoneId = stoneId;
        this.stoneIdTwo = stoneIdTwo;
        this.stoneIdThree = stoneIdThree;
        this.timeStemp = timeStemp;
    }

    //Constructor Soulstones
    public Stones(Cspell spell, String castSpellName, String itemName, uint stoneId, DateTime timeStemp)
    {
        this.spell = spell;
        this.castSpellName = castSpellName;
        this.itemName = itemName;
        this.stoneId = stoneId;
        this.timeStemp = timeStemp;
    }

    public Cspell spell { get; set; }
    public String castSpellName { get; set; }
    public String itemName { get; set; }
    public uint stoneId { get; set; }
    public uint stoneIdTwo { get; set; }
    public uint stoneIdThree { get; set; }
    public DateTime timeStemp { get; set; }
}


[Serializable]
public class WarlockFightClassSettings : Settings
{
    public WarlockFightClassSettings()
    {
        this.hpCurseOfAgony = 10;
        this.hpImmolate = 10;
        this.hpCorruption = 10;
    }

    public static WarlockFightClassSettings CurrentSettings { get; set; }

    public bool Save()
    {
        try
        {
            return Save(AdviserFilePathAndName("WarlockFightClassSettings", ObjectManager.Me.Name + "." + Usefuls.RealmName));
        }
        catch(Exception e)
        {
            Logging.WriteDebug("WarlockFightClassSettings => Save(): " + e);
            return false;
        }
    }

    public static bool Load()
    {
        try
        {
            if(File.Exists(AdviserFilePathAndName("WarlockFightClassSettings", ObjectManager.Me.Name + "." + Usefuls.RealmName)))
            {
                WarlockFightClassSettings.CurrentSettings = Load<WarlockFightClassSettings>(AdviserFilePathAndName("WarlockFightClassSettings", ObjectManager.Me.Name + "." + Usefuls.RealmName));
                return true;
            }

            WarlockFightClassSettings.CurrentSettings = new WarlockFightClassSettings();
        }
        catch(Exception e)
        {
            Logging.WriteDebug("WarlockFightClassSettings => Load(): " + e);
        }
        return false;
    }



    //Values Spells
    //HP target
    [Setting]
    [DefaultValue(10)]
    [Category("DoTs")]
    [DisplayName("HP% Target Curse of Agony")]
    [Description("Lower border to cast Curse of Agony against enemies")]
    public int hpCurseOfAgony { get; set; }

    [Setting]
    [DefaultValue(10)]
    [Category("DoTs")]
    [DisplayName("HP% Target Immolate")]
    [Description("Lower border to cast Immolate against enemies")]
    public int hpImmolate { get; set; }

    [Setting]
    [DefaultValue(10)]
    [Category("DoTs")]
    [DisplayName("HP% Target Corruption")]
    [Description("Lower border to cast Corruption against enemies")]
    public int hpCorruption { get; set; }
}


            /*
            if(!IsDotted(targets[0]) || ObjectManager.Me.ManaPercentage <= 10)
            {
                ObjectManager.Me.Target = targets[0].Guid;
                _lockTarget = true;
                return;
            }

            if(targets.Count > 1)
            {
                if(!IsDotted(targets[1]))
                {
                    ObjectManager.Me.Target = targets[1].Guid;
                    _lockTarget = true;
                    return;
                }
                if(IsDotted(targets[0]) && IsDotted(targets[1]))
                {
                    ObjectManager.Me.Target = targets[0].Guid;
                    _lockTarget = true;
                    return;
                }
            }
            */