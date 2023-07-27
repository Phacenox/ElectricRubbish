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
        public const string PLUGIN_GUID = "phace.electricrubbish";
        public const string PLUGIN_NAME = "Electric Rubbish";
        public const string PLUGIN_VERSION = "1.0.2";

        public OptionInterface config;

        public void OnEnable()
        {
            On.RainWorld.OnModsInit += InitHook;
            On.ItemSymbol.SymbolDataFromItem += ItemSymbol_SymbolDataFromItem;
            On.ItemSymbol.ColorForItem += ItemSymbol_ColorForItem;
            On.ItemSymbol.SpriteNameForItem += ItemSymbol_SpriteNameForItem;
            On.SaveState.AbstractPhysicalObjectFromString += SaveState_AbstractPhysicalObjectFromString;

            On.Room.AddObject += AddObjHook;

            On.Rock.HitSomething += RockHitHook;

            ElectricRubbishExtnum.RegisterValues();
        }

        private bool RockHitHook(On.Rock.orig_HitSomething orig, Rock self, SharedPhysics.CollisionResult result, bool eu)
        {
            if (ElectricRubbishOptions.All_Rubbish_Rechargable.Value && result.obj != null && !(self is ElectricRubbish))
            {
                if (result.obj is Creature && ElectricRubbish.CheckElectricCreature(result.obj as Creature))
                {
                    ElectricRubbishAbstract abstr = new ElectricRubbishAbstract(self.room.world, self.abstractPhysicalObject.pos, self.room.game.GetNewID(), 0);
                    abstr.RealizeInRoom();
                    (abstr.realizedObject as ElectricRubbish).RechargeNextFrame();
                    //StartCoroutine("slowrecharge", abstr.realizedObject as ElectricRubbish);
                    self.Destroy();
                }
            }
            return orig(self, result, eu);
        }
        IEnumerable<object> slowrecharge(ElectricRubbish r)
        {
            yield return null;
            r.Recharge();
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
            public static Configurable<bool> All_Rubbish_Rechargable;
            public ElectricRubbishOptions()
            {
                Percent_Rock_Replace_Rate = config.Bind<int>("Percent_Rock_Replace_Rate", 10, new ConfigurableInfo("When set to 1, all rubbish will be electrified."));
                All_Rubbish_Rechargable = config.Bind<bool>("All_Rubbish_Rechargable", false, new ConfigurableInfo("When true, any normal rubbish will be electrified if thrown at an electric creature."));
            }

            public override void Initialize()
            {
                base.Initialize();
                this.Tabs = new[]
                {
                    new Menu.Remix.MixedUI.OpTab(this)
                };
                Menu.Remix.MixedUI.OpLabel Label = new Menu.Remix.MixedUI.OpLabel(0f, 550f, "Percent Natural Electrification Rate");
                Menu.Remix.MixedUI.OpSlider slider = new Menu.Remix.MixedUI.OpSlider(Percent_Rock_Replace_Rate, new Vector2(0f, 520f), 200)
                {
                    min = 0,
                    max = 100
                };
                Menu.Remix.MixedUI.OpLabel Label2 = new Menu.Remix.MixedUI.OpLabel(0f, 450f, "Recharge any Rubbish");
                Menu.Remix.MixedUI.OpCheckBox checkbox = new Menu.Remix.MixedUI.OpCheckBox(All_Rubbish_Rechargable, new Vector2(0f, 420f));

                Tabs[0].AddItems(new Menu.Remix.MixedUI.UIelement[]
                {
                    Label, slider, Label2, checkbox
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

            if(!self.abstractRoom.shelter && obj.GetType() == typeof(Rock) && obj is Rock r && UnityEngine.Random.value < (float)ElectricRubbishOptions.Percent_Rock_Replace_Rate.Value/100f)
            {
                ElectricRubbishAbstract abstr = new ElectricRubbishAbstract(self.world, r.abstractPhysicalObject.pos, self.game.GetNewID(), UnityEngine.Random.value < 0.85f ? 2 : 1);
                abstr.RealizeInRoom();
                orig(self, abstr.realizedObject);
                obj.Destroy();
            }
            orig(self, obj);
        }
    }
}
