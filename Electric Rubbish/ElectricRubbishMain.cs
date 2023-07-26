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

namespace ElectricRubbish
{
    [BepInPlugin(PLUGIN_GUID, PLUGIN_NAME, PLUGIN_VERSION)]
    public class ElectricRubbishMain : BaseUnityPlugin
    {
        public const string PLUGIN_GUID = "phace.electric_rubbish";
        public const string PLUGIN_NAME = "Electric Rubbish";
        public const string PLUGIN_VERSION = "0.2";

        const float rock_replace_rate = 1.0f;

        public void OnEnable()
        {
            On.ItemSymbol.SymbolDataFromItem += ItemSymbol_SymbolDataFromItem;
            On.ItemSymbol.ColorForItem += ItemSymbol_ColorForItem;
            On.ItemSymbol.SpriteNameForItem += ItemSymbol_SpriteNameForItem;
            On.SaveState.AbstractPhysicalObjectFromString += SaveState_AbstractPhysicalObjectFromString;

            On.Room.AddObject += AddObjHook;

            ElectricRubbishExtnum.RegisterValues();
        }
        public void OnDisable()
        {
            ElectricRubbishExtnum.UnregisterValues();
        }
        //TODO: 
        /*
        public static OptionInterface LoadIO()
        {
            return new ElectricRubbishOptions();
        }

        public class ElectricRubbishOptions: OptionInterface{ 
            public ElectricRubbishOptions()
            {
                config = new ConfigHolder(this);
                config.Bind<float>("Rock Replace Rate", 0.3f);
            }
        }
        */


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
                return Custom.HSL2RGB(UnityEngine.Random.Range(0.6f, 0.65f), UnityEngine.Random.Range(0.85f, 95f), UnityEngine.Random.Range(0.35f, 0.55f));
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

            if(obj.GetType() == typeof(Rock) && obj is Rock r && UnityEngine.Random.value < rock_replace_rate)
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
