using robotManager.Helpful;
using robotManager.Products;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using wManager.Plugin;
using wManager.Events;
using wManager.Wow.Bot.Tasks;
using wManager.Wow.Enums;
using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Configuration;
using wManager.Wow.Class;


public class Main : IPlugin
{
    private string version = "1.6.2";
    public static int timer = 0;
    public static bool _isLaunched;
    private static float saveDistance;
    public static Vector3 destinationVector = new Vector3(0, 0, 0);
    public static bool inProcessing = false;
    public static bool _takenTaxi = false;
    static FlightMasterDB from = (FlightMasterDB)null;
    static FlightMasterDB to = (FlightMasterDB)null;
    static FlightMasterDB discoverTaxiNode = (FlightMasterDB)null;
    public static bool _timer = false;
    public static bool _discoverTaxiTimer = false;
    public static bool changer = true;
    public static bool _updateNodes;
    public static bool checkPath = true;
    public static bool checkPathActive = false;
    public static FlightMasterDB checkPathActiveFM = (FlightMasterDB)null;
    public static bool cancelCheckPathThread = false;
    public static bool pauseCheckPathThread = false;
    public static string status = "";
    public static string statusDiscover = "";
    public static bool _copySettings { get; set; }
    public static bool _runScan = false;
    public static FlightMasterDB taxiToDiscover = (FlightMasterDB)null;
    public static bool _taxiToDiscover = false;
    public static bool _discoverInProessing = false;
    public static int stuckCounter = 0;

    public void Initialize()
    {
        Logging.Write("[VanillaFlightMaster]: Flight Master initialized - " + version);
        _isLaunched = true;
        inProcessing = false;
        _copySettings = true;
        _runScan = true;
        _updateNodes = false;
        cancelCheckPathThread = false;

        ingameSettings();
        watchForEvents();
        FNVFlightMasterSettings.Load();
        applyDefaultNodes();
        MovementEvents.OnMovementPulse += MovementEventsOnOnMovementPulse;
        MovementEvents.OnSeemStuck += MovementEventsOnOnSeemStuck;

        scanNearbyTaxi.Start();
        flightMasterLoop();
    }

    public void Dispose()
    {
        _runScan = false;
        cancelCheckPathThread = true;
        _isLaunched = false;
        _updateNodes = false;
        MovementEvents.OnMovementPulse -= MovementEventsOnOnMovementPulse;
        MovementEvents.OnSeemStuck -= MovementEventsOnOnSeemStuck;
        FNVFlightMasterSettings.CurrentSettings.Save();
        Logging.Write("[VanillaFlightMaster]: Flight Master disposed");
    }

    public void Settings()
    {
        FNVFlightMasterSettings.Load();
        FNVFlightMasterSettings.CurrentSettings.ToForm();
        FNVFlightMasterSettings.CurrentSettings.Save();
    }

    public static void ingameSettings()
    {
        if(wManager.wManagerSetting.CurrentSetting.FlightMasterTaxiUse)
        {
            Logging.Write("[VanillaFlightMaster]: WRobots Taxi is enabled, going to disable it...");
            wManager.wManagerSetting.CurrentSetting.FlightMasterTaxiUse = false;
        }
    }

    public static void applyDefaultNodes()
    {
        if(ObjectManager.Me.PlayerRace == PlayerFactions.NightElf)
        {
            FNVFlightMasterSettings.CurrentSettings.Darkshore = true;
            FNVFlightMasterSettings.CurrentSettings.Teldrassil = true;
        }

        if(ObjectManager.Me.PlayerRace == PlayerFactions.Human)
            FNVFlightMasterSettings.CurrentSettings.Stormwind = true;

        if(ObjectManager.Me.PlayerRace == PlayerFactions.Dwarf || ObjectManager.Me.PlayerRace == PlayerFactions.Gnome)
            FNVFlightMasterSettings.CurrentSettings.Ironforge = true;

    }

    private void flightMasterLoop()
    {

        while(Products.IsStarted && _isLaunched)
        {
            if(!Products.InPause && _takenTaxi || _timer)
            {
                while(ObjectManager.Me.IsOnTaxi)
                {
                    Thread.Sleep(1000);
                }


                for(int timer = FNVFlightMasterSettings.CurrentSettings.pauseTaxiTime; timer > 0 && _timer; timer -= 1000)
                {
                    Thread.Sleep(1000);
                }


                if(!scanNearbyTaxi.IsAlive)
                {
                    Logging.Write("Taxi scan not running, restarting...");
                    scanNearbyTaxi.Start();
                }

                resetTaxi();
            }

            Thread.Sleep(5000);
        }

        Dispose();
    }

    private static void resetTaxi()
    {
        while(ObjectManager.Me.IsOnTaxi)
        {
            Thread.Sleep(5000);
        }

        Thread.Sleep(Usefuls.Latency * 3 + 1500);

        Logging.Write("[VanillaFlightMaster]: Reset taxi");
        _takenTaxi = false;
        from = (FlightMasterDB)null;
        to = (FlightMasterDB)null;
        _timer = false;
        checkPath = true;
        checkPathActive = false;
        checkPathActiveFM = (FlightMasterDB)null;
    }

    private void watchForEvents()
    {

        EventsLuaWithArgs.OnEventsLuaWithArgs += (LuaEventsId id, List<string> args) =>
        {
            if(id == wManager.Wow.Enums.LuaEventsId.TAXIMAP_OPENED && FNVFlightMasterSettings.CurrentSettings.updateTaxi)
            {
                if(!_updateNodes)
                {
                    _updateNodes = true;
                    List<FlightMasterDB> dbUpdate = fillDB();
                    int node = -1;

                    foreach(var temp in dbUpdate)
                    {
                        if(temp.continent.Equals(checkContinent()))
                        {
                            node = -1;
                            node = wManager.Wow.Helpers.Lua.LuaDoString<int>("for i=0,30 do if string.find(TaxiNodeName(i),'" + temp.name + "') then return i end end return -1");

                            if(node == -1 && temp.alreadyDiscovered)
                            {
                                Logging.Write("[VanillaFlightMaster]: Taxi node " + temp.name + " has not been discovered so far");
                                temp.alreadyDiscovered = false;
                                FNVFlightMasterSettings.flightMasterSaveChanges(temp, false);
                            }
                            else if(node != -1 && !temp.alreadyDiscovered)
                            {
                                Logging.Write("[VanillaFlightMaster]: Taxi node " + temp.name + " has already been discovered");
                                temp.alreadyDiscovered = true;
                                FNVFlightMasterSettings.flightMasterSaveChanges(temp, true);
                            }

                        }
                    }
                    _updateNodes = false;
                    Thread.Sleep(Usefuls.Latency * 5 + 5000);
                }
            }
        };
    }

    private static void MovementEventsOnOnSeemStuck()
    {
        Vector3 searingGorgeGate = new Vector3(-6033.529f, -2490.157f, 310.9456f);

        if((Usefuls.MapZoneName.Contains("Loch Modan") || Usefuls.MapZoneName.Contains("Searing Gorge")) && ObjectManager.Me.Position.DistanceTo2D(searingGorgeGate) < 50 && FNVFlightMasterSettings.CurrentSettings.pauseSearingGorge)
        {
            stuckCounter++;
            if(stuckCounter >= 5)
            {
                Logging.Write("[VanillaFlightMaster]: Repeated stucks detected at the locked gate between Loch Modan and Searing Gorge. Going to stop bot, to prevent getting caught");
                stuckCounter = 0;
                Products.ProductStop();
            }
        }
        else
        {
            stuckCounter = 0;
        }

        if(_timer || _takenTaxi)
        {
            Logging.Write("[VanillaFlightMaster]: SeemStuck detected, reset taxi to help solving it");
            resetTaxi();
        }
    }


    private static void MovementEventsOnOnMovementPulse(List<Vector3> points, CancelEventArgs cancelable)
    {

        statusDiscover = Logging.Status;
        if(_taxiToDiscover && !discoverTaxiNode.Equals((FlightMasterDB)null) && !_discoverInProessing && !_updateNodes && !statusDiscover.Contains("Boat") && !statusDiscover.Contains("Ship"))
        {
            _discoverInProessing = true;
            Thread.Sleep(Usefuls.Latency + 500);
            cancelable.Cancel = true;
            checkPathActive = true;
            checkPathActiveFM = discoverTaxiNode;

            discoverTaxi(discoverTaxiNode);

            Thread.Sleep(Usefuls.Latency * 3);
            cancelable.Cancel = false;
            checkPathActive = false;
        }

        if(changer && !_updateNodes && !inProcessing && ObjectManager.Me.IsAlive)
        {
            changer = false;
            if(!_taxiToDiscover && !_timer && !_takenTaxi && ObjectManager.Me.Position.DistanceTo(points.Last<Vector3>()) > FNVFlightMasterSettings.CurrentSettings.taxiTriggerDistance)
            {

                status = Logging.Status;
                if(FNVFlightMasterSettings.CurrentSettings.skipIfFollowPath && status.Contains("Follow Path") && !status.Contains("Resurrect") && calculateRealDistance(ObjectManager.Me.Position, points.Last<Vector3>()) < FNVFlightMasterSettings.CurrentSettings.skipIfFollowPathDistance)
                {
                    Logging.Write("[VanillaFlightMaster]: Currently following path or distance to start (" + calculateRealDistance(ObjectManager.Me.Position, points.Last<Vector3>()) + " yards) is smaller than setting value (" + FNVFlightMasterSettings.CurrentSettings.skipIfFollowPathDistance + " yards)");
                    Thread.Sleep(1000);
                    cancelable.Cancel = false;
                    inProcessing = false;
                    checkPathActive = true;
                    changer = true;
                    _timer = true;
                    return;
                }

                destinationVector = points.Last<Vector3>();
                saveDistance = calculateRealDistance(ObjectManager.Me.Position, points.Last<Vector3>());

                Thread.Sleep(Usefuls.Latency + 500);
                cancelable.Cancel = true;

                if(!inProcessing)
                {
                    from = getClosestFlightMasterFrom();
                    to = getClosestFlightMasterTo();
                }

                Thread.Sleep(1000);

                if(!from.name.Contains(to.name) && !to.name.Contains("null") && !to.name.Contains("FlightMaster") && !from.name.Contains("null") && !from.Equals(to) && calculateRealDistance(ObjectManager.Me.Position, from.position) + calculateRealDistance(to.position, destinationVector) + FNVFlightMasterSettings.CurrentSettings.shorterMinDistance <= saveDistance)
                {
                    Logging.Write("[VanillaFlightMaster]: Shorter path detected, taking Taxi from " + from.name + " to " + to.name);
                    inProcessing = true;
                    checkPathActive = true;
                    checkPathActiveFM = from;
                    takeTaxi(from, to);

                    Thread.Sleep(1000);
                    cancelable.Cancel = false;
                    inProcessing = false;
                    checkPathActive = true;
                }
                else
                {
                    Logging.Write("[VanillaFlightMaster]: No shorter path available, skip flying");
                    cancelable.Cancel = false;
                    _timer = true;
                    inProcessing = false;
                }
            }
            changer = true;
        }
    }

    public static bool inCombat()
    {
        return Lua.LuaDoString<bool>("return UnitAffectingCombat('player');");
    }

    public static bool inCombatPet()
    {
        return Lua.LuaDoString<bool>("return UnitAffectingCombat('pet');");
    }


    Thread scanNearbyTaxi = new Thread(() =>
    {
        int scanTimer = 10000;
        List<FlightMasterDB> npcScan = fillDB();
        Logging.Write("[VanillaFlightMaster]: Taxi scan started");

        while(robotManager.Products.Products.IsStarted)
        {
            if(_discoverTaxiTimer || _discoverInProessing)
            {
                Logging.Write("[VanillaFlightMaster]: Discover in processing or scan for nearby nodes paused");
                for(int i = FNVFlightMasterSettings.CurrentSettings.pauseTaxiTime; i > 0; i -= 1000)
                {
                    Thread.Sleep(1000);
                }
                _discoverTaxiTimer = false;
            }

            while(inCombat() || inCombatPet())
            {
                Thread.Sleep(5000);
            }

            //Pause while training First Aid in Darnassus, to avoid conflicts with HumanMasterPlugin
            string status = Logging.Status;
            while(status.Contains("First Aid") && Usefuls.MapZoneName.Contains("Teldrassil"))
            {
                Logging.Write("[VanillaFlightMaster]: HumanMasterPlugin trying to train First Aid. Pausing undiscovered node scan for five minutes to avoid conflicts");
                Thread.Sleep(300000);
            }

            if(Conditions.InGameAndConnectedAndAliveAndProductStartedNotInPause && !_taxiToDiscover && !ObjectManager.Me.IsOnTaxi)
            {
                foreach(var temp in npcScan)
                {
                    if(checkContinent() == temp.continent && !temp.alreadyDiscovered && ObjectManager.Me.Position.DistanceTo(temp.position) < FNVFlightMasterSettings.CurrentSettings.detectTaxiDistance)
                    {
                        taxiToDiscover = temp;
                        discoverTaxiNode = temp;
                        _taxiToDiscover = true;
                        Logging.Write("[VanillaFlightMaster]: Near undiscovered Taxi node found: " + temp.name);

                        Thread.Sleep(1000 + Usefuls.Latency);

                        while(!MovementManager.InMovement)
                        {
                            Thread.Sleep(100);
                        }

                        Reenable();
                    }
                }
            }
            Thread.Sleep(Usefuls.Latency * 10);
            npcScan = fillDB();
            Thread.Sleep(scanTimer);
        }

    });


    //By Matenia
    private static async void Reenable()
    {
        await Task.Run(() =>
        {
            Products.InPause = true;
            if(ObjectManager.Me.WowClass == WoWClass.Hunter)
                Lua.LuaDoString("RotaOn = false");
            MovementManager.StopMove();
            MovementManager.CurrentPath.Clear();
            MovementManager.CurrentPathOrigine.Clear();
            Thread.Sleep(5000);
            Products.InPause = false;
            if(ObjectManager.Me.WowClass == WoWClass.Hunter)
                Lua.LuaDoString("RotaOn = true");
            Logging.Write("[VanillaFlightMaster]: Resetting pathing");
        });
    }

    private static float calculateRealDistance(Vector3 startVector, Vector3 destinationVector)
    {
        float distance = 0;

        List<Vector3> realDistance = new List<Vector3>();

        realDistance = PathFinder.FindPath(startVector, destinationVector);

        for(int i = 0; i < realDistance.Count - 1; i++)
        {
            distance = distance + realDistance[i].DistanceTo2D(realDistance[i + 1]);
        }
        return distance;
    }

    public static FlightMasterDB getClosestFlightMasterFrom()
    {
        List<FlightMasterDB> FMLnfmd = fillDB();
        float tempDistance = 99999;
        FlightMasterDB returnObject = new FlightMasterDB("null", 0, new Vector3(0, 0, 0), false, false);

        foreach(var a in FMLnfmd)
        {
            if(a.alreadyDiscovered && a.position.DistanceTo(ObjectManager.Me.Position) < tempDistance && (a.continent == checkContinent()))
            {
                tempDistance = a.position.DistanceTo(ObjectManager.Me.Position);
                returnObject = a;
            }
        }
        return returnObject;
    }

    public static FlightMasterDB getClosestFlightMasterTo()
    {
        List<FlightMasterDB> FMLgcfmt = fillDB();
        float tempDistance = 99999;
        FlightMasterDB returnObject = new FlightMasterDB("null", 0, new Vector3(0, 0, 0), false, false);

        foreach(var a in FMLgcfmt)
        {
            if(a.alreadyDiscovered && a.position.DistanceTo(destinationVector) < tempDistance && (a.continent == checkContinent()))
            {
                tempDistance = a.position.DistanceTo(destinationVector);
                returnObject = a;
            }
        }
        return returnObject;
    }

    public static bool checkContinent()
    {
        if(Usefuls.ContinentId == (int)ContinentId.Kalimdor)
        {
            //Kalimdor
            return true;
        }
        else
        {
            //Eastern Kingdoms
            return false;
        }
    }

    public static void waitFlying(string destinationFlightMaster)
    {
        while(ObjectManager.Me.IsOnTaxi)
        {
            Logging.Write("[VanillaFlightMaster]: On taxi, waiting");
            Thread.Sleep(30000);
        }
        _takenTaxi = true;
        inProcessing = false;
        Thread.Sleep(5000);
        Reenable();
        Logging.Write("[VanillaFlightMaster]: Arrived at destination " + destinationFlightMaster + " , finished waiting");
    }

    public static List<FlightMasterDB> fillDB()
    {
        //True = Kalimdor ; False = Eastern Kingdoms
        List<FlightMasterDB> FMListe = new List<FlightMasterDB>();
        FlightMasterDB Stormwind = new FlightMasterDB("Stormwind", 352, new Vector3(-8835.76f, 490.084f, 109.6157f), false, FNVFlightMasterSettings.CurrentSettings.Stormwind);
        FMListe.Add(Stormwind);
        FlightMasterDB ArathiHighlands = new FlightMasterDB("Arathi", 2835, new Vector3(-1240.03f, -2513.96f, 21.92969f), false, FNVFlightMasterSettings.CurrentSettings.ArathiHighlands);
        FMListe.Add(ArathiHighlands);
        FlightMasterDB Ashenvale = new FlightMasterDB("Ashenvale", 4267, new Vector3(2828.4f, -284.3f, 106.7f), true, FNVFlightMasterSettings.CurrentSettings.Ashenvale);
        FMListe.Add(Ashenvale);
        FlightMasterDB Darkshore = new FlightMasterDB("Darkshore", 3841, new Vector3(6343.2f, 561.651f, 15.79876f), true, FNVFlightMasterSettings.CurrentSettings.Darkshore);
        FMListe.Add(Darkshore);
        FlightMasterDB Stranglethorn = new FlightMasterDB("Stranglethorn", 2859, new Vector3(-14477.9f, 464.101f, 36.38163f), false, FNVFlightMasterSettings.CurrentSettings.StranglethornValley);
        FMListe.Add(Stranglethorn);
        FlightMasterDB Duskwood = new FlightMasterDB("Duskwood", 2409, new Vector3(-10513.8f, -1258.79f, 41.43174f), false, FNVFlightMasterSettings.CurrentSettings.Duskwood);
        FMListe.Add(Duskwood);
        FlightMasterDB FeralasFeathermoon = new FlightMasterDB("Feathermoon", 8019, new Vector3(-4370.5f, 3340f, 12f), true, FNVFlightMasterSettings.CurrentSettings.FeralasFeathermoon);
        FMListe.Add(FeralasFeathermoon);
        FlightMasterDB FeralasThalanaar = new FlightMasterDB("Thalanaar", 4319, new Vector3(-4491f, -781f, -40f), true, FNVFlightMasterSettings.CurrentSettings.FeralasThalanaar);
        FMListe.Add(FeralasThalanaar);
        FlightMasterDB Tanaris = new FlightMasterDB("Tanaris", 7823, new Vector3(-7224.9f, -3738.2f, 8.4f), true, FNVFlightMasterSettings.CurrentSettings.Tanaris);
        FMListe.Add(Tanaris);
        FlightMasterDB Hinterlands = new FlightMasterDB("Hinterlands", 8018, new Vector3(282.1f, -2001.3f, 194.1f), false, FNVFlightMasterSettings.CurrentSettings.TheHinterlands);
        FMListe.Add(Hinterlands);
        FlightMasterDB Ironforge = new FlightMasterDB("Ironforge", 1573, new Vector3(-4821.13f, -1152.4f, 502.2116f), false, FNVFlightMasterSettings.CurrentSettings.Ironforge);
        FMListe.Add(Ironforge);
        FlightMasterDB Menethil = new FlightMasterDB("Wetlands", 1571, new Vector3(-3793.2f, -782.052f, 9.014864f), false, FNVFlightMasterSettings.CurrentSettings.Wetlands);
        FMListe.Add(Menethil);
        FlightMasterDB TheBarrens = new FlightMasterDB("Barrens", 16227, new Vector3(-898.246f, -3769.65f, 11.71021f), true, FNVFlightMasterSettings.CurrentSettings.TheBarrens);
        FMListe.Add(TheBarrens);
        FlightMasterDB Redridge = new FlightMasterDB("Redridge", 931, new Vector3(-9435.8f, -2234.79f, 69.43174f), false, FNVFlightMasterSettings.CurrentSettings.RedridgeMountains);
        FMListe.Add(Redridge);
        FlightMasterDB Teldrassil = new FlightMasterDB("Teldrassil", 3838, new Vector3(8640.58f, 841.118f, 23.26363f), true, FNVFlightMasterSettings.CurrentSettings.Teldrassil);
        FMListe.Add(Teldrassil);
        FlightMasterDB Southshore = new FlightMasterDB("Hillsbrad", 2432, new Vector3(-715.146f, -512.134f, 26.54455f), false, FNVFlightMasterSettings.CurrentSettings.HillsbradFoothills);
        FMListe.Add(Southshore);
        FlightMasterDB Stonetalon = new FlightMasterDB("Stonetalon", 4407, new Vector3(2682.83f, 1466.45f, 233.6483f), true, FNVFlightMasterSettings.CurrentSettings.StonetalonMountains);
        FMListe.Add(Stonetalon);
        FlightMasterDB Thelsamar = new FlightMasterDB("Thelsamar", 1572, new Vector3(-5424.85f, -2929.87f, 347.5623f), false, FNVFlightMasterSettings.CurrentSettings.LochModan);
        FMListe.Add(Thelsamar);
        FlightMasterDB Theramore = new FlightMasterDB("Dustwallow", 4321, new Vector3(-3828.88f, -4517.51f, 10.66067f), true, FNVFlightMasterSettings.CurrentSettings.DustwallowMarsh);
        FMListe.Add(Theramore);
        FlightMasterDB WesternP = new FlightMasterDB("Chillwind", 12596, new Vector3(928.3f, -1429.1f, 64.8f), false, FNVFlightMasterSettings.CurrentSettings.WesternPlaguelands);
        FMListe.Add(WesternP);
        FlightMasterDB Westfall = new FlightMasterDB("Westfall", 523, new Vector3(-10628.8f, 1037.79f, 34.43174f), false, FNVFlightMasterSettings.CurrentSettings.Westfall);
        FMListe.Add(Westfall);
        FlightMasterDB EasternP = new FlightMasterDB("Chapel", 12617, new Vector3(2269.9f, -5345.4f, 86.9f), false, FNVFlightMasterSettings.CurrentSettings.EasternPlaguelands);
        FMListe.Add(EasternP);
        FlightMasterDB SearingGorge = new FlightMasterDB("Searing", 2941, new Vector3(-6559.1f, -1169.4f, 309.8f), false, FNVFlightMasterSettings.CurrentSettings.SearingGorge);
        FMListe.Add(SearingGorge);
        FlightMasterDB BurningSteppes = new FlightMasterDB("Steppes", 2299, new Vector3(-8365.1f, -2758.5f, 185.6f), false, FNVFlightMasterSettings.CurrentSettings.BurningSteppes);
        FMListe.Add(BurningSteppes);
        FlightMasterDB BlastedLands = new FlightMasterDB("Blasted", 8609, new Vector3(-11110.2f, -3437.1f, 79.2f), false, FNVFlightMasterSettings.CurrentSettings.BlastedLands);
        FMListe.Add(BlastedLands);
        FlightMasterDB Azshara = new FlightMasterDB("Azshara", 12577, new Vector3(2718.2f, -3880.8f, 101.4f), true, FNVFlightMasterSettings.CurrentSettings.Azshara);
        FMListe.Add(Azshara);
        FlightMasterDB Felwood = new FlightMasterDB("Felwood", 12578, new Vector3(6204.2f, -1951.4f, 571.3f), true, FNVFlightMasterSettings.CurrentSettings.Felwood);
        FMListe.Add(Felwood);
        FlightMasterDB Winterspring = new FlightMasterDB("Winterspring", 11138, new Vector3(6800.5f, -4742.4f, 701.5f), true, FNVFlightMasterSettings.CurrentSettings.Winterspring);
        FMListe.Add(Winterspring);
        FlightMasterDB UngoroCreater = new FlightMasterDB("Crater", 10583, new Vector3(-6110.5f, -1140.4f, -186.9f), true, FNVFlightMasterSettings.CurrentSettings.UngoroCrater);
        FMListe.Add(UngoroCreater);
        FlightMasterDB Silithus = new FlightMasterDB("Silithus", 15177, new Vector3(-6758.6f, 775.6f, 89f), true, FNVFlightMasterSettings.CurrentSettings.Silithus);
        FMListe.Add(Silithus);
        FlightMasterDB Desolace = new FlightMasterDB("Desolace", 6706, new Vector3(136f, 1326f, 193f), true, FNVFlightMasterSettings.CurrentSettings.Desolace);
        FMListe.Add(Desolace);
        return FMListe;
    }


    private static void takeTaxi(FlightMasterDB from, FlightMasterDB to)
    {

        if(wManager.Wow.Bot.Tasks.GoToTask.ToPosition(from.position, 3.5f, false, context => Conditions.InGameAndConnectedAndAliveAndProductStartedNotInPause && !Conditions.IsAttackedAndCannotIgnore))
        {
            if(wManager.Wow.Bot.Tasks.GoToTask.ToPositionAndIntecractWithNpc(from.position, from.NPCId, -1, false, context => Conditions.InGameAndConnectedAndAliveAndProductStartedNotInPause && !Conditions.IsAttackedAndCannotIgnore, false))
            {
                while(!ObjectManager.Me.IsOnTaxi)
                {
                    if(ObjectManager.Me.IsMounted)
                        wManager.Wow.Bot.Tasks.MountTask.DismountMount(false, false, 100);


                    Usefuls.SelectGossipOption(GossipOptionsType.taxi);

                    Thread.Sleep(Usefuls.Latency + 1500);

                    while(_updateNodes)
                    {
                        Logging.Write("[VanillaFlightMaster]: Taxi node update in progress, waiting...");
                        Thread.Sleep(10000);
                    }

                    int node = Lua.LuaDoString<int>("for i=0,30 do if string.find(TaxiNodeName(i),'" + to.name + "') then return i end end");
                    Lua.LuaDoString("TakeTaxiNode(" + node + ")");
                    Logging.Write("[VanillaFlightMaster]: Taking Taxi from " + from.name + " to " + to.name);
                    Thread.Sleep(Usefuls.Latency + 500);
                    robotManager.Helpful.Keyboard.DownKey(wManager.Wow.Memory.WowMemory.Memory.WindowHandle, System.Windows.Forms.Keys.Escape);
                    Thread.Sleep(Usefuls.Latency + 2500);
                    if(!ObjectManager.Me.IsOnTaxi)
                        wManager.Wow.Bot.Tasks.GoToTask.ToPositionAndIntecractWithNpc(from.position, from.NPCId, -1, false, context => Conditions.InGameAndConnectedAndAliveAndProductStartedNotInPause && !Conditions.IsAttackedAndCannotIgnore, false);
                }

                if(ObjectManager.Me.IsOnTaxi)
                {
                    waitFlying(to.name);
                }
            }
        }
    }

    private static void discoverTaxi(FlightMasterDB flightMasterToDiscover)
    {

        FNVFlightMasterSettings.Load();
        List<FlightMasterDB> FMLdt = fillDB();

        if(wManager.Wow.Bot.Tasks.GoToTask.ToPosition(flightMasterToDiscover.position, 3.5f, false, context => Conditions.InGameAndConnectedAndAliveAndProductStartedNotInPause && !Conditions.IsAttackedAndCannotIgnore))
        {

            wManager.Wow.Bot.Tasks.GoToTask.ToPosition(flightMasterToDiscover.position, 3.5f, false, context => Conditions.InGameAndConnectedAndAliveAndProductStartedNotInPause && !Conditions.IsAttackedAndCannotIgnore);

            if(wManager.Wow.Bot.Tasks.GoToTask.ToPositionAndIntecractWithNpc(flightMasterToDiscover.position, flightMasterToDiscover.NPCId, -1, false, context => Conditions.InGameAndConnectedAndAliveAndProductStartedNotInPause && !Conditions.IsAttackedAndCannotIgnore, false))
            {
                wManager.wManagerSetting.ClearBlacklistOfCurrentProductSession();
                wManager.Wow.Bot.Tasks.GoToTask.ToPositionAndIntecractWithNpc(flightMasterToDiscover.position, flightMasterToDiscover.NPCId, -1, false, context => Conditions.InGameAndConnectedAndAliveAndProductStartedNotInPause && !Conditions.IsAttackedAndCannotIgnore, false);

                if(ObjectManager.Me.IsMounted)
                    wManager.Wow.Bot.Tasks.MountTask.DismountMount(false, false, 100);

                Usefuls.SelectGossipOption(GossipOptionsType.taxi);
                Thread.Sleep(Usefuls.Latency + 1500);

                while(_updateNodes)
                {
                    Logging.Write("[VanillaFLightMaster]: Taxi node update in progress...");
                    Thread.Sleep(10000);
                }

                Logging.Write("[VanillaFlightMaster]: Flight Master " + flightMasterToDiscover.name + " discovered");
                flightMasterToDiscover.alreadyDiscovered = true;
                FNVFlightMasterSettings.flightMasterSaveChanges(flightMasterToDiscover, true);

                Thread.Sleep(Usefuls.Latency * 5);

                timer = 0;
                //_timer = true;
                discoverTaxiNode = (FlightMasterDB)null;
                _taxiToDiscover = false;
                _discoverInProessing = false;
                _discoverTaxiTimer = true;
                Reenable();
                return;
            }
        }
        _discoverInProessing = false;
        return;
    }
}


    public class FlightMasterDB
    {
        public FlightMasterDB(String name, int NPCId, Vector3 position, bool continent, bool alreadyDiscovered)
        {
            this.name = name;
            this.NPCId = NPCId;
            this.position = position;
            this.continent = continent;
            this.alreadyDiscovered = alreadyDiscovered;
        }

        public int NPCId { get; set; }
        public Vector3 position { get; set; }
        public String name { get; set; }
        public bool continent { get; set; }
        public bool alreadyDiscovered { get; set; }

    }

[Serializable]
public class FNVFlightMasterSettings : Settings
{
    public FNVFlightMasterSettings()
    {
        //Settings
        this.taxiTriggerDistance = 1000;
        this.pauseTaxiTime = 50000;
        this.detectTaxiDistance = 50;
        this.shorterMinDistance = 1000;
        this.skipIfFollowPath = true;
        this.updateTaxi = true;
        this.skipIfFollowPathDistance = 5000;
        this.pauseSearingGorge = true;

        //FlightMaster discovered
        //Eastern Kingdoms
        this.ArathiHighlands = false;
        this.Wetlands = false;
        this.WesternPlaguelands = false;
        this.EasternPlaguelands = false;
        this.HillsbradFoothills = false;
        this.TheHinterlands = false;
        this.LochModan = false;
        this.Ironforge = false;
        this.SearingGorge = false;
        this.BurningSteppes = false;
        this.RedridgeMountains = false;
        this.Stormwind = false;
        this.Westfall = false;
        this.Duskwood = false;
        this.StranglethornValley = false;
        this.BlastedLands = false;

        //Kalimdor
        this.Teldrassil = false;
        this.Darkshore = false;
        this.Winterspring = false;
        this.Azshara = false;
        this.Ashenvale = false;
        this.StonetalonMountains = false;
        this.Desolace = false;
        this.TheBarrens = false;
        this.Tanaris = false;
        this.FeralasFeathermoon = false;
        this.FeralasThalanaar = false;
        this.UngoroCrater = false;
        this.DustwallowMarsh = false;
        this.Silithus = false;
        this.Moonglade = false;
        this.Felwood = false;

    }

    public static void flightMasterSaveChanges(FlightMasterDB needToChange, bool value)
    {

        if(needToChange.name.Contains("Arathi"))
            CurrentSettings.ArathiHighlands = value;

        if(needToChange.name.Contains("Wetlands"))
            CurrentSettings.Wetlands = value;

        if(needToChange.name.Contains("Chillwind"))
            CurrentSettings.WesternPlaguelands = value;

        if(needToChange.name.Contains("Chapel"))
            CurrentSettings.EasternPlaguelands = value;

        if(needToChange.name.Contains("Hillsbrad"))
            CurrentSettings.HillsbradFoothills = value;

        if(needToChange.name.Contains("Hinterlands"))
            CurrentSettings.TheHinterlands = value;

        if(needToChange.name.Contains("Thelsamar"))
            CurrentSettings.LochModan = value;

        if(needToChange.name.Contains("Ironforge"))
            CurrentSettings.Ironforge = value;

        if(needToChange.name.Contains("Searing"))
            CurrentSettings.SearingGorge = value;

        if(needToChange.name.Contains("Steppes"))
            CurrentSettings.BurningSteppes = value;

        if(needToChange.name.Contains("Redridge"))
            CurrentSettings.RedridgeMountains = value;

        if(needToChange.name.Contains("Stormwind"))
            CurrentSettings.Stormwind = value;

        if(needToChange.name.Contains("Westfall"))
            CurrentSettings.Westfall = value;

        if(needToChange.name.Contains("Duskwood"))
            CurrentSettings.Duskwood = value;

        if(needToChange.name.Contains("Stranglethorn"))
            CurrentSettings.StranglethornValley = value;

        if(needToChange.name.Contains("Blasted"))
            CurrentSettings.BlastedLands = value;

        if(needToChange.name.Contains("Teldrassil"))
            CurrentSettings.Teldrassil = value;

        if(needToChange.name.Contains("Darkshore"))
            CurrentSettings.Darkshore = value;

        if(needToChange.name.Contains("Winterspring"))
            CurrentSettings.Winterspring = value;

        if(needToChange.name.Contains("Azshara"))
            CurrentSettings.Azshara = value;

        if(needToChange.name.Contains("Ashenvale"))
            CurrentSettings.Ashenvale = value;

        if(needToChange.name.Contains("Stonetalon"))
            CurrentSettings.StonetalonMountains = value;

        if(needToChange.name.Contains("Desolace"))
            CurrentSettings.Desolace = value;

        if(needToChange.name.Contains("Tanaris"))
            CurrentSettings.Tanaris = value;

        if(needToChange.name.Contains("Barrens"))
            CurrentSettings.TheBarrens = value;

        if(needToChange.name.Contains("Feathermoon"))
            CurrentSettings.FeralasFeathermoon = value;

        if(needToChange.name.Contains("Thalanaar"))
            CurrentSettings.FeralasThalanaar = value;

        if(needToChange.name.Contains("Crater"))
            CurrentSettings.UngoroCrater = value;

        if(needToChange.name.Contains("Dustwallow"))
            CurrentSettings.DustwallowMarsh = value;

        if(needToChange.name.Contains("Silithus"))
            CurrentSettings.Silithus = value;

        if(needToChange.name.Contains("Felwood"))
            CurrentSettings.Felwood = value;

        FNVFlightMasterSettings.CurrentSettings.Save();
        Thread.Sleep(2500);

        try
        {
            FNVFlightMasterSettings.CurrentSettings = Load<FNVFlightMasterSettings>(AdviserFilePathAndName("VanillaFlightMaster_DB", ObjectManager.Me.Name + "." + Usefuls.RealmName));
        }
        catch(Exception e)
        {
            Logging.Write("[VanillaFlightMaster]: Error when trying to reload DB file -> " + e);
        }

        Logging.Write("[VanillaFlightMaster]: Settings saved of Flight Master " + needToChange.name);
        return;
    }


    public static FNVFlightMasterSettings CurrentSettings { get; set; }

    public bool Save()
    {
        try
        {
            return Save(AdviserFilePathAndName("VanillaFlightMaster_DB", ObjectManager.Me.Name + "." + Usefuls.RealmName));
        }
        catch(Exception e)
        {
            Logging.WriteDebug("VanillaFlightMaster_DB => Save(): " + e);
            return false;
        }
    }


    public static bool Load()
    {
        try
        {
            if(File.Exists(AdviserFilePathAndName("VanillaFlightMaster_DB", ObjectManager.Me.Name + "." + Usefuls.RealmName)))
            {
                FNVFlightMasterSettings.CurrentSettings = Load<FNVFlightMasterSettings>(AdviserFilePathAndName("VanillaFlightMaster_DB", ObjectManager.Me.Name + "." + Usefuls.RealmName));
                return true;
            }

            FNVFlightMasterSettings.CurrentSettings = new FNVFlightMasterSettings();
        }
        catch(Exception e)
        {
            Logging.WriteDebug("VanillaFlightMaster_DB => Load(): " + e);
        }
        return false;
    }

    [Setting]
    [DefaultValue(1000)]
    [Category("1 - Main")]
    [DisplayName("Trigger Distance")]
    [Description("Sets how long your distance to your destination has to be, to trigger use of taxi")]
    public int taxiTriggerDistance { get; set; }

    [Setting]
    [DefaultValue(50000)]
    [Category("1 - Main")]
    [DisplayName("Pause Taxi Time")]
    [Description("Sets how long taxi is paused after use, to avoid loops. Only change it, if you experience issues")]

    public int pauseTaxiTime { get; set; }
    [Setting]
    [DefaultValue(50)]
    [Category("1 - Main")]
    [DisplayName("Discover Distance")]
    [Description("Min distance to discover an undiscovered taxi node")]

    public int detectTaxiDistance { get; set; }
    [Setting]
    [DefaultValue(1000)]
    [Category("1 - Main")]
    [DisplayName("Shorter Path Min")]
    [Description("Sets how much shorter a path has to be, to trigger taxi")]
    public int shorterMinDistance { get; set; }


    [Setting]
    [DefaultValue(true)]
    [Category("2 - Useful")]
    [DisplayName("1. Skip if Follow Path / Boat step")]
    [Description("Skips take taxi, if currently executing a Follow Path or Boat Quester step. When running a profile with dedicated paths")]
    public bool skipIfFollowPath { get; set; }

    [Setting]
    [DefaultValue(true)]
    [Category("2 - Useful")]
    [DisplayName("2. Update taxi nodes")]
    [Description("Scans and updates all entries on the taxi map of the current continent, if they have already been discovered. Triggers, when the taxi map is opened")]
    public bool updateTaxi { get; set; }

    [Setting]
    [DefaultValue(5000)]
    [Category("2 - Useful")]
    [DisplayName("1.1 Skip if ... min distance")]
    [Description("Won't skip taxi min distance to destination")]
    public float skipIfFollowPathDistance { get; set; }

    [Setting]
    [DefaultValue(true)]
    [Category("2 - Useful")]
    [DisplayName("3. Stop bot at Searing Gorge gate")]
    [Description("Stops the bot, to prevent it from running into the Searing Gorge gate from Loch Modan and getting stuck over and over again")]
    public bool pauseSearingGorge { get; set; }


    //FlightMaster
    //Eastern Kingdoms
    public bool Stormwind { get; set; }
    public bool Westfall { get; set; }
    public bool RedridgeMountains { get; set; }
    public bool Duskwood { get; set; }
    public bool StranglethornValley { get; set; }
    public bool Ironforge { get; set; }
    public bool BurningSteppes { get; set; }
    public bool BlastedLands { get; set; }
    public bool SearingGorge { get; set; }
    public bool LochModan { get; set; }
    public bool Wetlands { get; set; }
    public bool ArathiHighlands { get; set; }
    public bool HillsbradFoothills { get; set; }
    public bool WesternPlaguelands { get; set; }
    public bool EasternPlaguelands { get; set; }
    public bool TheHinterlands { get; set; }

    //Kalimdor

    public bool Ashenvale { get; set; }
    public bool Azshara { get; set; }
    public bool Darkshore { get; set; }
    public bool Teldrassil { get; set; }
    public bool Desolace { get; set; }
    public bool DustwallowMarsh { get; set; }
    public bool Felwood { get; set; }
    public bool FeralasFeathermoon { get; set; }
    public bool FeralasThalanaar { get; set; }
    public bool Moonglade { get; set; }
    public bool Silithus { get; set; }
    public bool StonetalonMountains { get; set; }
    public bool Tanaris { get; set; }
    public bool TheBarrens { get; set; }
    public bool UngoroCrater { get; set; }
    public bool Winterspring { get; set; }

}

    