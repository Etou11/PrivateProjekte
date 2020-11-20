using robotManager.Helpful;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using wManager.Plugin;
using wManager.Wow.Enums;
using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;

public class Main : IPlugin
{
    private bool _isLaunched;
    private bool _isSubscribed;
    private bool _goingToTown;
    private string currentFood; 
    private string currentDrink; 

    readonly List<FoodAndDrink> FoodAndDrinkList = new List<FoodAndDrink>()
    {
            //Drink - Vanilla, BC, WotLK
            new FoodAndDrink("Refreshing Spring Water", 159U, 0U, "Drink"),
            new FoodAndDrink("Ice Cold Milk", 1179U, 5U, "Drink"),
            new FoodAndDrink("Melon Juice", 1205U, 15U, "Drink"),
            new FoodAndDrink("Sweet Nectar", 1708U, 25U, "Drink"),
            new FoodAndDrink("Moonberry Juice", 1645U, 35U, "Drink"),
            new FoodAndDrink("Morning Glory Dew", 8766U, 45U, "Drink"),
            new FoodAndDrink("Filtered Draenic Water", 28399U, 55U, "Drink"),
            new FoodAndDrink("Purified Draenic Water", 27860U, 65U, "Drink"),
            new FoodAndDrink("Sweetened Goat\'s Milk", 35954U, 65U, "Drink"),
            new FoodAndDrink("Pungent Seal Whey", 33444U, 70U, "Drink"),
            new FoodAndDrink("Honeymint Tea", 33445U, 75U, "Drink"),

            //Food - Bread, Meat, Cheese, Fungus, Fruit, Fish - Vanilla, BC, WotLK
            new FoodAndDrink("Tough Hunk of Bread", 4540U, 0U, "Food"),
            new FoodAndDrink("Freshly Baked Bread", 4541U, 5U, "Food"),
            new FoodAndDrink("Moist Cornbread", 4542U, 15U, "Food"),
            new FoodAndDrink("Mulgore Spice Bread", 4544U, 25U, "Food"),
            new FoodAndDrink("Soft Banana Bread", 4601U, 35U, "Food"),
            new FoodAndDrink("Hommade Cherry Pie", 8950U, 45U, "Food"),
            new FoodAndDrink("Mag\'har Grainbread", 19232U, 55U, "Food"),
            new FoodAndDrink("Bladespire Bagel", 29449U, 65U, "Food"),
            new FoodAndDrink("Crusty Flatbread", 33449U, 65U, "Food"),
            new FoodAndDrink("Sweet Potato Bread", 35950U, 75U, "Food"),
            new FoodAndDrink("Tough Jerky", 117U, 0U, "Food"),
            new FoodAndDrink("Haunch of Meat", 2287U, 5U, "Food"),
            new FoodAndDrink("Mutton Chop", 3770U, 15U, "Food"),
            new FoodAndDrink("Wild Hog Shank", 3771U, 25U, "Food"),
            new FoodAndDrink("Cured Ham Steak", 4599U, 35U, "Food"),
            new FoodAndDrink("Roasted Quail", 8952U, 45U, "Food"),
            new FoodAndDrink("Smoked Talbuk Venison", 278540U, 55U, "Food"),
            new FoodAndDrink("Clefthoof Ribs", 29451U, 65U, "Food"),
            new FoodAndDrink("Salted Venison", 33454U, 65U, "Food"),
            new FoodAndDrink("Mead Basted Caribou", 35953U, 75U, "Food"),
            new FoodAndDrink("Slitherskin Mackerel", 787U, 0U, "Food"),
            new FoodAndDrink("Longjaw Mud Snapper", 4592U, 5U, "Food"),
            new FoodAndDrink("Bristle Whisker Catfish", 4593U, 15U, "Food"),
            new FoodAndDrink("Rockscale Cod", 4594U, 25U, "Food"),
            new FoodAndDrink("Yellowtail", 6887U, 35U, "Food"),
            new FoodAndDrink("Spinefin Halibut", 8957U, 45U, "Food"),
            new FoodAndDrink("Sunspring Carp", 27858U, 55U, "Food"),
            new FoodAndDrink("Zangar Trout", 29452U, 65U, "Food"),
            new FoodAndDrink("Fillet of Icefin", 33451U, 65U, "Food"),
            new FoodAndDrink("Poached Emperor Salmon", 35951U, 75U, "Food"),
            new FoodAndDrink("Speared Emperor Salmon", 44049U, 75U, "Food"),
            new FoodAndDrink("Roasted Eel", 44071U, 75U, "Food"),
            new FoodAndDrink("Shiny Red Apple", 4536U, 0U, "Food"),
            new FoodAndDrink("Abim Banana", 4537U, 5U, "Food"),
            new FoodAndDrink("Snapvine Watermelon", 4538U, 15U, "Food"),
            new FoodAndDrink("Goldenbark Apple", 4539U, 25U, "Food"),
            new FoodAndDrink("Moon Harvest Pumpkin", 4602U, 35U, "Food"),
            new FoodAndDrink("Heaven Peach", 16168U, 35U, "Food"),
            new FoodAndDrink("Deep Fried Plantains", 8953U, 45U, "Food"),
            new FoodAndDrink("Skethyl Berries", 27856U, 55U, "Food"),
            new FoodAndDrink("Telaari Grapes", 29450U, 65U, "Food"),
            new FoodAndDrink("Tundra Berries", 35949U, 65U, "Food"),
            new FoodAndDrink("Savory Snowplum", 35948U, 75U, "Food"),
            new FoodAndDrink("Forest Mushroom Cap", 4604U, 0U, "Food"),
            new FoodAndDrink("Red-speckled Mushroom", 4605U, 5U, "Food"),
            new FoodAndDrink("Spongy Morel", 4606U, 15U, "Food"),
            new FoodAndDrink("Delicious Cave Mold", 4607U, 25U, "Food"),
            new FoodAndDrink("Raw Black Truffle", 4608U, 35U, "Food"),
            new FoodAndDrink("Dried King Bolete", 8948U, 45U, "Food"),
            new FoodAndDrink("Zangar Caps", 27859U, 55U, "Food"),
            new FoodAndDrink("Sporeggar Mushroom", 29453U, 65U, "Food"),
            new FoodAndDrink("Spiced Lichen", 33452U, 65U, "Food"),
            new FoodAndDrink("Sparkling Frostcap", 35947U, 75U, "Food"),
            new FoodAndDrink("Darnassian Bleu", 2070U, 0U, "Food"),
            new FoodAndDrink("Dalaran Sharp", 414U, 5U, "Food"),
            new FoodAndDrink("Dwarven Mild", 422U, 15U, "Food"),
            new FoodAndDrink("Stormwind Brie", 1707U, 25U, "Food"),
            new FoodAndDrink("Fine Aged Cheddar", 3927U, 35U, "Food"),
            new FoodAndDrink("Alterac Swiss", 8932U, 45U, "Food"),
            new FoodAndDrink("Garadar Sharp", 27857U, 55U, "Food"),
            new FoodAndDrink("Mag\'har Mild  Cheese", 29448U, 65U, "Food"),
            new FoodAndDrink("Sour Goat Cheese", 33443U, 65U, "Food"),
            new FoodAndDrink("Briny Hardcheese", 35952U, 75U, "Food")
    };

    public void Initialize()
    {
        _isLaunched = true;
        _isSubscribed = true;
        BuyAnyFoodSettings.Load();
        ToTownWatcher();
        LuaEvents();
        Logging.Write("[BuyAnyFood] Loaded");
    }

    public void Dispose()
    {
        _isLaunched = false;
        _isSubscribed = false;
        BuyAnyFoodSettings.CurrentSettings.Save();
        Logging.Write("[BuyAnyFood] Disposed");
    }

    public void Settings()
    {
        BuyAnyFoodSettings.Load();
        BuyAnyFoodSettings.CurrentSettings.ToForm();
        BuyAnyFoodSettings.CurrentSettings.Save();
    }

    private void LuaEvents()
    {
        EventsLuaWithArgs.OnEventsLuaWithArgs += (LuaEventsId id, List<string> args) =>
        {
            if(_isSubscribed && _goingToTown && id == wManager.Wow.Enums.LuaEventsId.MERCHANT_SHOW)
            {
                PluginExecution();
            }
        };
    }

    private void ToTownWatcher()
    {
        robotManager.Events.FiniteStateMachineEvents.OnRunState += (engine, state, cancelable) =>
        {
            if(state != null && state.DisplayName == "To Town")
            {
                _goingToTown = true;
            }
        };

        robotManager.Events.FiniteStateMachineEvents.OnAfterRunState += (engine, state) =>
        {
            if(state != null && state.DisplayName == "To Town")
            {
                _goingToTown = false;
            }
        };
    }

    private void PluginExecution()
    {
        currentDrink = wManager.wManagerSetting.CurrentSetting.DrinkName;
        currentFood = wManager.wManagerSetting.CurrentSetting.FoodName;

        if(!wManager.wManagerSetting.CurrentSetting.FoodIsSpell && currentFood != "" && BuyAnyFoodSettings.CurrentSettings.ManageFood)
            ManageFood();
        if(!wManager.wManagerSetting.CurrentSetting.DrinkIsSpell && currentDrink != "" && BuyAnyFoodSettings.CurrentSettings.ManageDrink)
            ManageDrink();
    }

    private void ManageFood()
    {
        foreach(string ele in GetAllVendorItems())
        {
            if(ele.Contains(currentFood))
            {
                var currentItem = FoodAndDrinkList.Find(o => o.Name == currentFood);
                if(currentItem.Level <= ObjectManager.Me.Level && (ObjectManager.Me.Level >= 10 ? (currentItem.Level > ObjectManager.Me.Level - (10 + BuyAnyFoodSettings.CurrentSettings.LevelOffset)) : (currentItem.Level > ObjectManager.Me.Level - (5 + BuyAnyFoodSettings.CurrentSettings.LevelOffset))) && currentItem.Type == "Food")
                {
                    Logging.Write("[BuyAnyFood] Vendor sells fitting food item " + currentItem.Name);
                    return;
                }
            }
        }

        //Remove any high and low level food
        var temp = FoodAndDrinkList.FindAll(o => o.Level <= ObjectManager.Me.Level && (ObjectManager.Me.Level >= 10 ? (o.Level > ObjectManager.Me.Level - (10 + BuyAnyFoodSettings.CurrentSettings.LevelOffset)) : (o.Level > ObjectManager.Me.Level - (5 + BuyAnyFoodSettings.CurrentSettings.LevelOffset))) && o.Type == "Food");
        bool extendedBorder = false;
label_4:
        foreach(FoodAndDrink element in temp)
        {
            foreach(string ele in GetAllVendorItems())
            {
                if(ele.Contains(element.Name))
                {
                    Logging.Write("[BuyAnyFood] Vendor sells similar / better food item " + element.Name + ". Chaning food item to " + element.Name);
                    ManageItemsBlacklist(element.Name, "Add");
                    ManageItemsBlacklist(currentFood, "Remove");
                    wManager.wManagerSetting.CurrentSetting.FoodName = element.Name;
                    return;
                }
            }
        }
        if(!extendedBorder)
        {
            //Increase level border if no food found
            temp = FoodAndDrinkList.FindAll(o => o.Level <= ObjectManager.Me.Level && (ObjectManager.Me.Level >= 10 ? (o.Level > ObjectManager.Me.Level - (10 + BuyAnyFoodSettings.CurrentSettings.LevelOffset)) : (o.Level > ObjectManager.Me.Level - (5 + BuyAnyFoodSettings.CurrentSettings.LevelOffset))) && o.Type == "Food");
            extendedBorder = true;
            goto label_4;
        }
    }

    private void ManageDrink()
    {
        foreach(string ele in GetAllVendorItems())
        {
            if(ele.Contains(currentDrink))
            {
                var currentItem = FoodAndDrinkList.Find(o => o.Name == currentDrink);
                if(currentItem.Level <= ObjectManager.Me.Level && (ObjectManager.Me.Level >= 10 ? (currentItem.Level > ObjectManager.Me.Level - (10 + BuyAnyFoodSettings.CurrentSettings.LevelOffset)) : (currentItem.Level > ObjectManager.Me.Level - (5 + BuyAnyFoodSettings.CurrentSettings.LevelOffset))) && currentItem.Type == "Drink")
                {
                    Logging.Write("[BuyAnyFood] Vendor sells fitting drink item " + currentItem.Name);
                    return;
                }
            }
        }

        //Remove any high and low level drink
        var temp = FoodAndDrinkList.FindAll(o => o.Level <= ObjectManager.Me.Level && (ObjectManager.Me.Level >= 10 ? (o.Level > ObjectManager.Me.Level - (10 + BuyAnyFoodSettings.CurrentSettings.LevelOffset)) : (o.Level > ObjectManager.Me.Level - (5 + BuyAnyFoodSettings.CurrentSettings.LevelOffset))) && o.Type == "Drink");
        bool extendedBorder = false;
label_5:
        foreach(FoodAndDrink element in temp)
        {
            foreach(string ele in GetAllVendorItems())
            {
                if(ele.Contains(element.Name))
                {
                    Logging.Write("[BuyAnyFood] Vendor sells similar / better drink item " + element.Name + ". Changing drink item to " + element.Name);
                    ManageItemsBlacklist(element.Name, "Add");
                    ManageItemsBlacklist(currentDrink, "Remove");
                    wManager.wManagerSetting.CurrentSetting.DrinkName = element.Name;
                    return;
                }
            }
        }
        if(!extendedBorder)
        {
            //Increase level border if no drink found
            temp = FoodAndDrinkList.FindAll(o => o.Level <= ObjectManager.Me.Level && (ObjectManager.Me.Level >= 10 ? (o.Level > ObjectManager.Me.Level - (10 + BuyAnyFoodSettings.CurrentSettings.LevelOffset)) : (o.Level > ObjectManager.Me.Level - (5 + BuyAnyFoodSettings.CurrentSettings.LevelOffset))) && o.Type == "Drink");
            extendedBorder = true;
            goto label_5;
        }
    }

    private void ManageItemsBlacklist(string itemName, string action)
    {
        switch(action)
        {
            case "Add":
                if(!BuyAnyFoodSettings.CurrentSettings.DoNotSellAdd)
                    return;

                wManager.wManagerSetting.CurrentSetting.DoNotSellList.Add(itemName);
                Logging.Write("[BuyAnyFood] Adding " + itemName + " to DoNotSellList");
                break;

            case "Remove":
                if(!BuyAnyFoodSettings.CurrentSettings.DoNotSellRemove)
                    return;

                wManager.wManagerSetting.CurrentSetting.DoNotSellList.RemoveAll(o => o == itemName);
                Logging.Write("[BuyAnyFood] Removing " + itemName + " from DoNotSellList");
                break;

            default:
                Logging.Write("[BuyAnyFood] No action selected");
                break;
        }
    }

    private List<string> GetAllVendorItems()
    {
        List<string> vendorItems = new List<string>();

        for(int i = 1; Lua.LuaDoString<bool>("if GetMerchantItemLink(" + i + ") then return true else return false end"); i++)
        {
            vendorItems.Add(Lua.LuaDoString<string>("return GetMerchantItemLink(" + i + ")"));
        }
        return vendorItems;
    }

}


public class FoodAndDrink
{
    //Constructor
    public FoodAndDrink(string name, uint id, uint level, string type)
    {
        this.Name = name;
        this.Id = id;
        this.Level = level;
        this.Type = type;
    }

    public string Name { get; set; }
    public uint Id { get; set; }
    public uint Level { get; set; }
    public string Type { get; set; }

}



[Serializable]
public class BuyAnyFoodSettings : Settings
{

    public BuyAnyFoodSettings()
    {
        this.ManageFood = true;
        this.ManageDrink = true;
        this.LevelOffset = 0;
        this.DoNotSellAdd = true;
        this.DoNotSellRemove = true;
    }

    public static BuyAnyFoodSettings CurrentSettings { get; set; }

    public bool Save()
    {
        try
        {
            return Save(AdviserFilePathAndName("BuyAnyFoodSettings", ObjectManager.Me.Name + "." + Usefuls.RealmName));
        }
        catch(Exception e)
        {
            Logging.WriteDebug("BuyAnyFoodSettings => Save(): " + e);
            return false;
        }
    }

    public static bool Load()
    {
        try
        {
            if(File.Exists(AdviserFilePathAndName("BuyAnyFoodSettings", ObjectManager.Me.Name + "." + Usefuls.RealmName)))
            {
                BuyAnyFoodSettings.CurrentSettings = Load<BuyAnyFoodSettings>(AdviserFilePathAndName("BuyAnyFoodSettings", ObjectManager.Me.Name + "." + Usefuls.RealmName));
                return true;
            }

            BuyAnyFoodSettings.CurrentSettings = new BuyAnyFoodSettings();
        }
        catch(Exception e)
        {
            Logging.WriteDebug("BuyAnyFoodSettings => Load(): " + e);
        }
        return false;
    }

    [Setting]
    [DefaultValue(true)]
    [Category("Main")]
    [DisplayName("Manage Food")]
    [Description("Allows plugin to change food item")]
    public bool ManageFood { get; set; }

    [Setting]
    [DefaultValue(true)]
    [Category("Main")]
    [DisplayName("Manage Drink")]
    [Description("Allows plugin to change drink item")]
    public bool ManageDrink { get; set; }

    [Setting]
    [DefaultValue(0)]
    [Category("Main")]
    [DisplayName("Level offset")]
    [Description("Change level difference for bought food / drink items. Higher level -> buy next food level later")]
    public int LevelOffset { get; set; }

    [Setting]
    [DefaultValue(true)]
    [Category("Main")]
    [DisplayName("DoNotSellList Add")]
    [Description("Allows plugin to add items to DoNotSellList")]
    public bool DoNotSellAdd { get; set; }

    [Setting]
    [DefaultValue(true)]
    [Category("Main")]
    [DisplayName("DoNotSellList Remove")]
    [Description("Allows plugin to remove items from DoNotSellList")]
    public bool DoNotSellRemove { get; set; }

}

