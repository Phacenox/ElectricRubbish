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
        public const string PLUGIN_VERSION = "0.0.1";

        Player p;

        public void OnEnable()
        {
            On.Player.Update += PlayerUpdateHook;
            On.AbstractPhysicalObject.Realize += ItemRealizeHook;
            On.Room.Loaded += RoomLoadedPatch;
            On.ItemSymbol.SymbolDataFromItem += ItemSymbol_SymbolDataFromItem;
            On.ItemSymbol.ColorForItem += ItemSymbol_ColorForItem;
            On.ItemSymbol.SpriteNameForItem += ItemSymbol_SpriteNameForItem;
            On.SaveState.AbstractPhysicalObjectFromString += SaveState_AbstractPhysicalObjectFromString;
            //On.Rock.InitiateSprites += RockHook;
        }

        private IconSymbol.IconSymbolData? ItemSymbol_SymbolDataFromItem(On.ItemSymbol.orig_SymbolDataFromItem orig, AbstractPhysicalObject item)
        {
            if(item is ElectricRubbishAbstract)
            {
                return orig(new AbstractPhysicalObject(item.world, AbstractPhysicalObject.AbstractObjectType.Rock, null, item.pos, item.ID));
            }
            return orig(item);
        }

        private AbstractPhysicalObject SaveState_AbstractPhysicalObjectFromString(On.SaveState.orig_AbstractPhysicalObjectFromString orig, World world, string objString)
        {
            var data = objString.Split(new[] { "<oA>" }, StringSplitOptions.None);
            var type = data[1];
            if (type == "ElectricRubbish")
            {
                return new ElectricRubbishAbstract(world, ElectricRubbishExtnum.ElectricRubbishAbstract, null, WorldCoordinate.FromString(data[2]), EntityID.FromString(data[0]), int.Parse(data[3]));
            }
            return orig(world, objString);
        }

        private string ItemSymbol_SpriteNameForItem(On.ItemSymbol.orig_SpriteNameForItem orig, AbstractPhysicalObject.AbstractObjectType itemType, int intData)
        {
            Debug.Log(itemType);
            if(itemType == ElectricRubbishExtnum.ElectricRubbishAbstract)
            {
                return orig(AbstractPhysicalObject.AbstractObjectType.Rock, intData);
            }
            return orig(itemType, intData);
        }

        private Color ItemSymbol_ColorForItem(On.ItemSymbol.orig_ColorForItem orig, AbstractPhysicalObject.AbstractObjectType itemType, int intData)
        {
            if (itemType == ElectricRubbishExtnum.ElectricRubbishAbstract)
            {
                return orig(AbstractPhysicalObject.AbstractObjectType.Rock, intData);
            }
            return orig(itemType, intData);
        }


        /*
private void RockHook(On.Rock.orig_InitiateSprites orig, Rock self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
{
   self.Destroy();
}*/

        private void RoomLoadedPatch(On.Room.orig_Loaded orig, Room room)
        {
            if (room.abstractRoom.firstTimeRealized)
            {
                if (!room.abstractRoom.shelter && !room.abstractRoom.gate && room.game != null && (!room.game.IsArenaSession || room.game.GetArenaGameSession.GameTypeSetup.levelItems))
                {
                    for (int i = 100; i >= 0; i--)
                    {
                        IntVector2 spawnTile = room.RandomTile();
                        if (!room.GetTile(spawnTile).Solid)
                        {
                            bool canSpawnHere = true;
                            for (int j = -1; j < 2; j++)
                            {
                                if (!room.GetTile(spawnTile + new IntVector2(j, -1)).Solid)
                                {
                                    canSpawnHere = false;
                                    break;
                                }
                            }

                            if (canSpawnHere)
                            {
                                p.AddQuarterFood();
                                EntityID newID = room.game.GetNewID(-room.abstractRoom.index);
                                ElectricRubbishAbstract entity = new ElectricRubbishAbstract(room.world, ElectricRubbishExtnum.ElectricRubbishAbstract, null, new WorldCoordinate(room.abstractRoom.index, spawnTile.x, spawnTile.y, -1), newID, 2);
                                room.abstractRoom.AddEntity(entity);
                            }
                        }
                    }
                }
            }

            orig(room);
        }

        private void ItemRealizeHook(On.AbstractPhysicalObject.orig_Realize orig, AbstractPhysicalObject self)
        {
            orig(self);
            if(self.realizedObject is Rock)
            {
                self.Destroy();
                /*
                EntityID new_id = self.Room.realizedRoom.game.GetNewID(-self.Room.index);
                var e = new AbstractPhysicalObject(self.Room.world, ElectricRubbishExtnum.ElectricRubbish, null, self.pos, new_id);
                self.Room.AddEntity(e);*/
            }
        }



        void PlayerUpdateHook(On.Player.orig_Update orig, Player self, bool eu)
        {
            p = self;
            orig(self, eu);
        }
    }
}
