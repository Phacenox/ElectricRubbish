using BepInEx;
using System;
using UnityEngine;
using RWCustom;
using System.Collections.Generic;

namespace ElectricRubbish
{
    [BepInPlugin(PLUGIN_GUID, PLUGIN_NAME, PLUGIN_VERSION)]
    public class ElectricRubbishMain : BaseUnityPlugin
    {
        public const string PLUGIN_GUID = "phace.electricrubbish";
        public const string PLUGIN_NAME = "Electric Rubbish";
        public const string PLUGIN_VERSION = "1.3.0";

        public OptionInterface config;

        public void OnEnable()
        {
            On.RainWorld.OnModsInit += InitHook;

            On.ItemSymbol.SymbolDataFromItem += ItemSymbol_SymbolDataFromItem;
            On.ItemSymbol.ColorForItem += ItemSymbol_ColorForItem;
            On.ItemSymbol.SpriteNameForItem += ItemSymbol_SpriteNameForItem;
            On.SaveState.AbstractPhysicalObjectFromString += SaveState_AbstractPhysicalObjectFromString;

            On.Room.AddObject += AddObjHook;
            On.UpdatableAndDeletable.Destroy += DestroyHook;
            On.ScavengerAI.CollectScore_PhysicalObject_bool += ScavCollectScoreHook;

            ElectricRubbishExtnum.RegisterValues();
        }

        public void OnDisable()
        {
            ElectricRubbishExtnum.UnregisterValues();
        }

        private void InitHook(On.RainWorld.orig_OnModsInit orig, RainWorld self)
        {
            orig(self);
            config = new ElectricRubbishOptions();
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
                if (ElectricRubbishOptions.AllRubbishRechargeable && intData == 0)
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
            //ID.- 1.4778 < oA > ElectricRubbishAbstract < oA > SU_S01.24.16.1 < oA > 2
            var data = objString.Split(new[] { "<oA>" }, StringSplitOptions.None);
            var type = data[1];
            if (type == "ElectricRubbishAbstract")
            {
                int customData = data.Length >= 4 ? int.Parse(data[3]) : 0;
                return new ElectricRubbishAbstract(world, WorldCoordinate.FromString(data[2]), EntityID.FromString(data[0]), customData);
            }
            return orig(world, objString);
        }

        List<EntityID> addedrocks = new List<EntityID>();

        private void AddObjHook(On.Room.orig_AddObject orig, Room self, UpdatableAndDeletable obj)
        {
            orig(self, obj);
            //needs an extra cast since ElectricRubbish inherits from Rock.
            if (obj.GetType() == typeof(Rock) && obj is Rock r && !addedrocks.Contains(r.abstractPhysicalObject.ID))
            {
                addedrocks.Add(r.abstractPhysicalObject.ID);
                //natural electrified spawns
                if (!self.abstractRoom.shelter && UnityEngine.Random.value < ElectricRubbishOptions.RockReplaceRate)
                {
                    ElectricRubbishAbstract abstr = new ElectricRubbishAbstract(self.world, r.abstractPhysicalObject.pos, (obj as PhysicalObject).abstractPhysicalObject.ID, UnityEngine.Random.value < 0.85f ? 2 : 1);
                    self.abstractRoom.AddEntity(abstr);
                    abstr.RealizeInRoom();
                    obj.Destroy();
                } //extra conversion
                else if (ElectricRubbishOptions.AllRubbishRechargeable)
                {
                    ElectricRubbishAbstract abstr = new ElectricRubbishAbstract(self.world, r.abstractPhysicalObject.pos, (obj as PhysicalObject).abstractPhysicalObject.ID, 0);
                    self.abstractRoom.AddEntity(abstr);
                    abstr.RealizeInRoom();
                    obj.Destroy();
                }
            }
        }

        private void DestroyHook(On.UpdatableAndDeletable.orig_Destroy orig, UpdatableAndDeletable self)
        {
            if(self.GetType() == typeof(Rock) && self is Rock r)
                addedrocks.Remove(r.abstractPhysicalObject.ID);
            orig(self);
        }


        private int ScavCollectScoreHook(On.ScavengerAI.orig_CollectScore_PhysicalObject_bool orig, ScavengerAI self, PhysicalObject obj, bool weaponFiltered)
        {
            var original_score = orig(self, obj, weaponFiltered);
            if (obj is ElectricRubbish er)
            {
                //if has no charge, treat as normal rock.
                if (er.rubbishAbstract.electricCharge == 0)
                    return original_score;
                if (weaponFiltered)
                {
                    return original_score * 3 / 2;
                }
                else
                {
                    //if scav would already want this as a normal rock, prioritize this.
                    if (original_score > 0)
                        return 3;
                    //otherwise, try to replace any held uncharged rocks, or fill an empty hand.
                    for (int i = 0; i < self.scavenger.grasps.Length; i++)
                    {
                        if(self.scavenger.grasps[i] == null)
                        {
                            return 2;
                        }
                        if (self.scavenger.grasps[i].grabbed != obj)
                        {
                            if (self.scavenger.grasps[i].grabbed.GetType() == typeof(Rock)
                                || (self.scavenger.grasps[i].grabbed is ElectricRubbish scavs_er && scavs_er.rubbishAbstract.electricCharge == 0))
                            {
                                return 3;
                            }
                        }
                    }
                }
            }
            return original_score;
        }
    }
}
