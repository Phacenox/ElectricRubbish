using BepInEx;
using IL;
using IL.MoreSlugcats;
using MoreSlugcats;
using On;
using On.MoreSlugcats;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using RWCustom;
using IL.Menu.Remix.MixedUI;

namespace ElectricRubbish
{
    [BepInPlugin(PLUGIN_GUID, PLUGIN_NAME, PLUGIN_VERSION)]
    public class ElectricRubbishMain : BaseUnityPlugin
    {
        public const string PLUGIN_GUID = "phace.electric_rubbish";
        public const string PLUGIN_NAME = "Electric Rubbish";
        public const string PLUGIN_VERSION = "0.3";

        public OptionInterface config;

        public void OnEnable()
        {
            On.RainWorld.OnModsInit += InitHook;
            On.ItemSymbol.SymbolDataFromItem += ItemSymbol_SymbolDataFromItem;
            On.ItemSymbol.ColorForItem += ItemSymbol_ColorForItem;
            On.ItemSymbol.SpriteNameForItem += ItemSymbol_SpriteNameForItem;
            On.SaveState.AbstractPhysicalObjectFromString += SaveState_AbstractPhysicalObjectFromString;

            On.Room.AddObject += AddObjHook;

            ElectricRubbishExtnum.RegisterValues();
        }

        private void InitHook(On.RainWorld.orig_OnModsInit orig, RainWorld self)
        {
            orig(self);
            Logger.LogDebug("initting config");


            try
            {
                OptionInterface config = new ElectricRubbishOptions();
                MachineConnector.SetRegisteredOI(PLUGIN_GUID, config);
                Logger.LogDebug("config working");
            }
            catch (Exception err)
            {
                Logger.LogError(err);
                Logger.LogDebug("config not working");
            }
        }

        public void OnDisable()
        {
            ElectricRubbishExtnum.UnregisterValues();
        }

        public class ElectricRubbishOptions: OptionInterface{
            public static Configurable<int> Percent_Rock_Replace_Rate;
            public ElectricRubbishOptions()
            {
                Percent_Rock_Replace_Rate = config.Bind<int>("Percent_Rock_Replace_Rate", 15, new ConfigurableInfo("When set to 1, all rubbish will be electrified."));
            }

            public override void Initialize()
            {
                base.Initialize();
                this.Tabs = new[]
                {
                    new Menu.Remix.MixedUI.OpTab(this)
                };
                Menu.Remix.MixedUI.OpLabel Label = new Menu.Remix.MixedUI.OpLabel(0f, 550f, "Percent Electrification Rate");
                Menu.Remix.MixedUI.OpSlider slider = new Menu.Remix.MixedUI.OpSlider(Percent_Rock_Replace_Rate, new Vector2(0f, 520f), 200)
                {
                    min = 0,
                    max = 100
                };

                Tabs[0].AddItems(new Menu.Remix.MixedUI.UIelement[]
                {
                    Label, slider
                });
            }
        }


        private IconSymbol.IconSymbolData? ItemSymbol_SymbolDataFromItem(On.ItemSymbol.orig_SymbolDataFromItem orig, AbstractPhysicalObject item)
        {
            if (item is ElectricRubbishAbstract)
            {
                return new IconSymbol.IconSymbolData() { itemType = ElectricRubbishExtnum.ElectricRubbishAbstract };
            }
            return orig(item);
        }

        private Color ItemSymbol_ColorForItem(On.ItemSymbol.orig_ColorForItem orig, AbstractPhysicalObject.AbstractObjectType itemType, int intData)
        {
            if (itemType == ElectricRubbishExtnum.ElectricRubbishAbstract)
            {
                return Custom.HSL2RGB(UnityEngine.Random.Range(0.55f, 0.7f), UnityEngine.Random.Range(0.8f, 1f), UnityEngine.Random.Range(0.3f, 0.6f));
            }
            return orig(itemType, intData);
        }

        private string ItemSymbol_SpriteNameForItem(On.ItemSymbol.orig_SpriteNameForItem orig, AbstractPhysicalObject.AbstractObjectType itemType, int intData)
        {
            if (itemType == ElectricRubbishExtnum.ElectricRubbishAbstract)
            {
                Debug.Log(orig(AbstractPhysicalObject.AbstractObjectType.Rock, intData));
                return orig(AbstractPhysicalObject.AbstractObjectType.Rock, intData);
            }
            return orig(itemType, intData);
        }

        private AbstractPhysicalObject SaveState_AbstractPhysicalObjectFromString(On.SaveState.orig_AbstractPhysicalObjectFromString orig, World world, string objString)
        {
            //samples
            //ID.- 341.4782 < oA > Spear < oA > SU_A24.4.15.1 < oA > 2 < oA > 0 < oA > 0 < oA > 0 < oA > 0 < oA > 0
            //ID.- 1.4778 < oA > ElectricRubbishAbstract < oA > SU_S01.24.16.1
            var data = objString.Split(new[] { "<oA>" }, StringSplitOptions.None);
            var type = data[1];
            if (type == "ElectricRubbishAbstract")
            {
                int customData = data.Length >= 4 ? int.Parse(data[3]) : 0;
                return new ElectricRubbishAbstract(world, WorldCoordinate.FromString(data[2]), EntityID.FromString(data[0]), customData);
            }
            return orig(world, objString);
        }

        

        private void AddObjHook(On.Room.orig_AddObject orig, Room self, UpdatableAndDeletable obj)
        {

            if(obj.GetType() == typeof(Rock) && obj is Rock r && UnityEngine.Random.value < (float)ElectricRubbishOptions.Percent_Rock_Replace_Rate.Value/100f)
            {
                ElectricRubbishAbstract abstr = new ElectricRubbishAbstract(self.world, r.abstractPhysicalObject.pos, self.game.GetNewID(), 2);
                abstr.realizedObject = (PhysicalObject)new ElectricRubbish(abstr, self.world); ;
                abstr.RealizeInRoom();
                orig(self, abstr.realizedObject);
                obj.Destroy();
            }
            orig(self, obj);
        }
    }
}
