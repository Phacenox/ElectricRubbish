using BepInEx;
using System;
using UnityEngine;
using RWCustom;
using System.Collections.Generic;
using MoreSlugcats;

namespace ElectricRubbish
{
    [BepInPlugin(PLUGIN_GUID, PLUGIN_NAME, PLUGIN_VERSION)]
    public class ElectricRubbishMain : BaseUnityPlugin
    {
        public const string PLUGIN_GUID = "phace.electricrubbish";
        public const string PLUGIN_NAME = "Electric Rubbish";
        public const string PLUGIN_VERSION = "1.3.4";
        public static string plugin_live_version => PLUGIN_VERSION;

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
            On.Player.SwallowObject += SwallowObjectHook;

            ElectricRubbishExtnum.RegisterValues();
        }

        //allow arti to treat electricrubbish like rocks for crafting
        private void SwallowObjectHook(On.Player.orig_SwallowObject orig, Player self, int grasp)
        {
            if (grasp < 0 || self.grasps[grasp] == null) return;
            AbstractPhysicalObject abstractPhysicalObject = self.grasps[grasp].grabbed.abstractPhysicalObject;
            if (abstractPhysicalObject.type == ElectricRubbishExtnum.ElectricRubbishAbstract)
            {
                self.objectInStomach = abstractPhysicalObject;
                if (ModManager.MMF && self.room.game.session is StoryGameSession)
                    (self.room.game.session as StoryGameSession).RemovePersistentTracker(self.objectInStomach);
                self.ReleaseGrasp(grasp);
                self.objectInStomach.realizedObject.RemoveFromRoom();
                self.objectInStomach.Abstractize(self.abstractCreature.pos);
                self.objectInStomach.Room.RemoveEntity(self.objectInStomach);
                if (ModManager.MSC && self.SlugCatClass == MoreSlugcatsEnums.SlugcatStatsName.Artificer && self.FoodInStomach > 0)
                {
                    abstractPhysicalObject = new AbstractPhysicalObject(self.room.world,
                        AbstractPhysicalObject.AbstractObjectType.ScavengerBomb, null,
                        self.room.GetWorldCoordinate(self.mainBodyChunk.pos), self.room.game.GetNewID());
                    self.SubtractFood(1);
                    self.objectInStomach = abstractPhysicalObject;
                    self.objectInStomach.Abstractize(self.abstractCreature.pos);
                }
                BodyChunk mainBodyChunk = self.mainBodyChunk;
                mainBodyChunk.vel.y += 2f;
                self.room.PlaySound(SoundID.Slugcat_Swallow_Item, self.mainBodyChunk);
            }
            else
                orig(self, grasp);
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
            //Format: ID.-103.7068<oB>0<oA>ElectricRubbishAbstract<oA>DS_S02l.26.17.0<oA>1
            var data = objString.Split(new[] { "<oA>", "<oB>" }, StringSplitOptions.None);
            var type = data[2];
            if (type == "ElectricRubbishAbstract")
            {
                int electricCharge = data.Length >= 5 ? int.Parse(data[4]) : 0;
                return new ElectricRubbishAbstract(world, WorldCoordinate.FromString(data[3]), EntityID.FromString(data[0]), electricCharge);
            }
            #region legacy parsing
            //Format: ID.- 1.4778 < oA > ElectricRubbishAbstract < oA > SU_S01.24.16.1 < oA > 2
            type = data[1];
            if (type == "ElectricRubbishAbstract")
            {
                int electricCharge = data.Length >= 4 ? int.Parse(data[3]) : 0;
                return new ElectricRubbishAbstract(world, WorldCoordinate.FromString(data[2]), EntityID.FromString(data[0]), electricCharge);
            }
            #endregion
            return orig(world, objString);
        }

        readonly List<EntityID> addedrocks = new List<EntityID>();

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
                    ElectricRubbishAbstract abstr = new ElectricRubbishAbstract(self.world, r.abstractPhysicalObject.pos, r.abstractPhysicalObject.ID, UnityEngine.Random.value < 0.85f ? 2 : 1);
                    abstr.RealizeInRoom();
                    obj.Destroy();
                } //extra conversion
                else if (ElectricRubbishOptions.AllRubbishRechargeable)
                {
                    ElectricRubbishAbstract abstr = new ElectricRubbishAbstract(self.world, r.abstractPhysicalObject.pos, r.abstractPhysicalObject.ID, 0);
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
