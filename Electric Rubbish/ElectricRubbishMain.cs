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
            On.PhysicalObject.Grabbed += ItemGrabbedHook;
            On.Room.Loaded += RoomLoadedPatch;
        }


        private void ItemGrabbedHook(On.PhysicalObject.orig_Grabbed orig, PhysicalObject self, Creature.Grasp grasp)
        {
            orig(self, grasp);
            if(self is ElectricRubbish)
            {
                /*
                grasp.Release();
                Electrocute(p, self.room);*/
            }

        }
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
                                ElectricRubbishAbstract entity = new ElectricRubbishAbstract(room.world, ElectricRubbishExtnum.ElectricRubbishAbstract, null, new WorldCoordinate(room.abstractRoom.index, spawnTile.x, spawnTile.y, -1), newID);
                                room.abstractRoom.AddEntity(entity);
                            }
                        }
                    }
                }
            }

            orig(room);
        }

        public void Electrocute(Creature otherObject, Room room)
        {

            if (!(otherObject is BigEel))
            {
                (otherObject as Creature).Violence(otherObject.firstChunk, Vector2.one, otherObject.firstChunk, null, Creature.DamageType.Electric, 0.1f, (!(otherObject is Player)) ? (320f * Mathf.Lerp((otherObject as Creature).Template.baseStunResistance, 1f, 0.5f)) : 140f);
                room.AddObject(new CreatureSpasmer(otherObject as Creature, allowDead: false, (otherObject as Creature).stun));
            }

            bool flag2 = false;
            if (otherObject.Submersion > 0.5f)
            {
                room.AddObject(new UnderwaterShock(room, null, otherObject.firstChunk.pos, 10, 800f, 2f, otherObject, new Color(0.8f, 0.8f, 1f)));
                flag2 = true;
            }

            room.PlaySound(SoundID.Jelly_Fish_Tentacle_Stun, otherObject.firstChunk.pos);
            room.AddObject(new Explosion.ExplosionLight(otherObject.firstChunk.pos, 200f, 1f, 4, new Color(0.7f, 1f, 1f)));
            for (int i = 0; i < 15; i++)
            {
                Vector2 vector = Vector2.one;
                room.AddObject(new MouseSpark(otherObject.firstChunk.pos + vector * 9f, otherObject.firstChunk.vel, 20f, new Color(0.7f, 1f, 1f)));
            }

        }

        private void ItemRealizeHook(On.AbstractPhysicalObject.orig_Realize orig, AbstractPhysicalObject self)
        {
            orig(self);
            if(self.type == AbstractPhysicalObject.AbstractObjectType.Rock)
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
