using BepInEx;
using System;
using UnityEngine;
using RWCustom;

namespace ElectricRubbish
{
    [BepInPlugin(PLUGIN_GUID, PLUGIN_NAME, PLUGIN_VERSION)]
    public class ElectricRubbishMain : BaseUnityPlugin
    {
        public const string PLUGIN_GUID = "phace.electricrubbish";
        public const string PLUGIN_NAME = "Electric Rubbish";
        public const string PLUGIN_VERSION = "1.0.3";

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

        public void OnDisable()
        {
            ElectricRubbishExtnum.UnregisterValues();
        }

        private void InitHook(On.RainWorld.orig_OnModsInit orig, RainWorld self)
        {
            orig(self);
            OptionInterface config = new ElectricRubbishOptions();
            MachineConnector.SetRegisteredOI(PLUGIN_GUID, config);
        }

        private IconSymbol.IconSymbolData? ItemSymbol_SymbolDataFromItem(On.ItemSymbol.orig_SymbolDataFromItem orig, AbstractPhysicalObject item)
        {
            if (item is ElectricRubbishAbstract era)
                return new IconSymbol.IconSymbolData() { itemType = ElectricRubbishExtnum.ElectricRubbishAbstract, intData = era.electricCharge };
            return orig(item);
        }

        private Color ItemSymbol_ColorForItem(On.ItemSymbol.orig_ColorForItem orig, AbstractPhysicalObject.AbstractObjectType itemType, int intData)
        {
            if (itemType == ElectricRubbishExtnum.ElectricRubbishAbstract)
            {
                //when the option for total replacement is checked, the shelter sprites reflect the rubbish's charge value.
                if (ElectricRubbishOptions.allRubbishRechargeable && intData == 0)
                {
                    return orig(AbstractPhysicalObject.AbstractObjectType.Rock, 0);
                }
                return Custom.HSL2RGB(UnityEngine.Random.Range(0.55f, 0.7f), UnityEngine.Random.Range(0.8f, 1f), UnityEngine.Random.Range(0.3f, 0.6f));
            }
            return orig(itemType, intData);
        }

        private string ItemSymbol_SpriteNameForItem(On.ItemSymbol.orig_SpriteNameForItem orig, AbstractPhysicalObject.AbstractObjectType itemType, int intData)
        {
            if (itemType == ElectricRubbishExtnum.ElectricRubbishAbstract)
                return orig(AbstractPhysicalObject.AbstractObjectType.Rock, intData);
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
            //needs an extra cast since ElectricRubbish inherits from Rock.
            if(obj.GetType() == typeof(Rock) && obj is Rock r)
            {
                //natural electrified spawns
                if (!self.abstractRoom.shelter && UnityEngine.Random.value < ElectricRubbishOptions.rockReplaceRate)
                {
                    ElectricRubbishAbstract abstr = new ElectricRubbishAbstract(self.world, r.abstractPhysicalObject.pos, self.game.GetNewID(), UnityEngine.Random.value < 0.85f ? 2 : 1);
                    abstr.RealizeInRoom();
                    orig(self, abstr.realizedObject);
                    obj.Destroy();
                    return;
                } //extra conversion
                else if (ElectricRubbishOptions.allRubbishRechargeable)
                {
                    ElectricRubbishAbstract abstr = new ElectricRubbishAbstract(self.world, r.abstractPhysicalObject.pos, self.game.GetNewID(), 0);
                    abstr.RealizeInRoom();
                    orig(self, abstr.realizedObject);
                    obj.Destroy();
                    return;
                }
            }
            orig(self, obj);
        }
    }
}
