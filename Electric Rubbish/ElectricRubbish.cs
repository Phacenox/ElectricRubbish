using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

using Noise;
using RWCustom;
using IL.LizardCosmetics;

namespace ElectricRubbish
{
    public static class ElectricRubbishExtnum
    {
        public static AbstractPhysicalObject.AbstractObjectType ElectricRubbishAbstract;
    }

    public class ElectricRubbishAbstract : AbstractPhysicalObject
    {
        public ElectricRubbishAbstract(World world, AbstractObjectType type, PhysicalObject realizedObject, WorldCoordinate pos, EntityID ID) : base(world, type, realizedObject, pos, ID)
        {
            this.realizedObject = new ElectricRubbish(this, world);
        }
    }

    public class ElectricRubbish : Rock
    {

        public float fluxSpeed;
        public float fluxTimer;

        public Vector2 sparkPoint;

        public int electricCharge = 2;

        public Color electricColor;

        public Color blackColor;

        public float zapPitch;

        public bool didZapCoilCheck;

        public ElectricRubbish(AbstractPhysicalObject abstractPhysicalObject, World world)
            : base(abstractPhysicalObject, world)
        {
            sparkPoint = Vector2.zero;
            UnityEngine.Random.State state = UnityEngine.Random.state;
            electricColor = Custom.HSL2RGB(UnityEngine.Random.Range(0.55f, 0.7f), UnityEngine.Random.Range(0.8f, 1f), UnityEngine.Random.Range(0.3f, 0.6f));
            UnityEngine.Random.InitState(abstractPhysicalObject.ID.RandomSeed);
            ResetFluxSpeed();

            Debug.Log("spawned object!");

            UnityEngine.Random.state = state;
        }

        public override void Update(bool eu)
        {
            base.Update(eu);
            fluxTimer += fluxSpeed;
            if (fluxTimer > (float)Math.PI * 2f)
                ResetFluxSpeed();

            if (UnityEngine.Random.value < 0.025f) Spark();

            didZapCoilCheck = false;

            for (int k = 0; k < room.zapCoils.Count; k++)
            {
                ZapCoil zapCoil = room.zapCoils[k];
                if (zapCoil.turnedOn > 0.5f && zapCoil.GetFloatRect.Vector2Inside(base.firstChunk.pos + rotation * 30f))
                {
                    if (electricCharge == 0)
                    {
                        Recharge();
                    }
                    else
                    {
                        ExplosiveShortCircuit();
                    }

                    didZapCoilCheck = true;
                    break;
                }
            }
        }

        public void Spark()
        {
            if (electricCharge > 0)
            {
                for (int i = 0; i < 10; i++)
                {
                    Vector2 vector = Custom.RNV();
                    room.AddObject(new Spark(sparkPoint + vector * UnityEngine.Random.value * 20f, vector * Mathf.Lerp(4f, 10f, UnityEngine.Random.value), Color.white, null, 4, 18));
                }
            }
        }

        public void Zap()
        {
            if (electricCharge > 0)
            {
                room.AddObject(new ZapCoil.ZapFlash(sparkPoint, 10f));
                room.PlaySound(SoundID.Zapper_Zap, sparkPoint, 1f, (zapPitch == 0f) ? (1.5f + UnityEngine.Random.value * 1.5f) : zapPitch);
                if (base.Submersion > 0.5f)
                {
                    room.AddObject(new UnderwaterShock(room, null, base.firstChunk.pos, 10, 800f, 2f, thrownBy, new Color(0.8f, 0.8f, 1f)));
                }
            }
        }

        public void Electrocute(PhysicalObject otherObject)
        {
            if (!(otherObject is Creature))
            {
                return;
            }

            bool flag = CheckElectricCreature(otherObject as Creature);
            if (flag && electricCharge == 0)
            {
                Recharge();
            }
            else
            {
                if (electricCharge == 0)
                {
                    return;
                }

                if (!(otherObject is BigEel) && !flag)
                {
                    (otherObject as Creature).Violence(base.firstChunk, Custom.DirVec(base.firstChunk.pos, otherObject.firstChunk.pos) * 5f, otherObject.firstChunk, null, Creature.DamageType.Electric, 0.1f, (!(otherObject is Player)) ? (320f * Mathf.Lerp((otherObject as Creature).Template.baseStunResistance, 1f, 0.5f)) : 140f);
                    room.AddObject(new CreatureSpasmer(otherObject as Creature, allowDead: false, (otherObject as Creature).stun));
                }

                if (base.Submersion <= 0.5f && otherObject.Submersion > 0.5f)
                {
                    room.AddObject(new UnderwaterShock(room, null, otherObject.firstChunk.pos, 10, 800f, 2f, thrownBy, new Color(0.8f, 0.8f, 1f)));
                }

                room.PlaySound(SoundID.Jelly_Fish_Tentacle_Stun, base.firstChunk.pos);
                room.AddObject(new Explosion.ExplosionLight(base.firstChunk.pos, 200f, 1f, 4, new Color(0.7f, 1f, 1f)));
                for (int i = 0; i < 15; i++)
                {
                    Vector2 vector = Custom.DegToVec(360f * UnityEngine.Random.value);
                    room.AddObject(new MouseSpark(base.firstChunk.pos + vector * 9f, base.firstChunk.vel + vector * 36f * UnityEngine.Random.value, 20f, new Color(0.7f, 1f, 1f)));
                }

                if (flag)
                {
                    ExplosiveShortCircuit();
                    return;
                }

                ShortCircuit();
            }
        }

        public override void WeaponDeflect(Vector2 inbetweenPos, Vector2 deflectDir, float bounceSpeed)
        {
            base.WeaponDeflect(inbetweenPos, deflectDir, bounceSpeed);
            Zap();
        }

        public override void Thrown(Creature thrownBy, Vector2 thrownPos, Vector2? firstFrameTraceFromPos, IntVector2 throwDir, float frc, bool eu)
        {
            base.Thrown(thrownBy, thrownPos, firstFrameTraceFromPos, throwDir, frc, eu);
            Spark();
        }

        public override void HitSomethingWithoutStopping(PhysicalObject obj, BodyChunk chunk, Appendage appendage)
        {
            base.HitSomethingWithoutStopping(obj, chunk, appendage);
            Spark();
        }

        public Vector2 PointAlongSpear(RoomCamera.SpriteLeaser sLeaser, float percent)
        {
            float height = sLeaser.sprites[0].element.sourceRect.height;
            return new Vector2(base.firstChunk.pos.x, base.firstChunk.pos.y) - Custom.DegToVec(sLeaser.sprites[0].rotation) * height * sLeaser.sprites[0].anchorY + Custom.DegToVec(sLeaser.sprites[0].rotation) * height * percent;
        }

        public void ResetFluxSpeed()
        {
            fluxSpeed = UnityEngine.Random.value * 0.2f + 0.025f;
            while (fluxTimer > (float)Math.PI * 2f)
            {
                fluxTimer -= (float)Math.PI * 2f;
            }
        }

        public override void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            sLeaser.sprites = new FSprite[2];
            sLeaser.sprites[0] = new FSprite("SmallSpear");

            sLeaser.sprites[1] = new FSprite("Pebble" + UnityEngine.Random.Range(1, 12));

            AddToContainer(sLeaser, rCam, null);
        }

        public override void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            base.DrawSprites(sLeaser, rCam, timeStacker, camPos);
            /* todo: check this
            if (blink > 0)
            {
                if (blink > 1 && UnityEngine.Random.value < 0.5f)
                {
                    sLeaser.sprites[0].color = new Color(1f, 1f, 1f);
                }
                else
                {
                    sLeaser.sprites[0].color = color;
                }
            }*/

            sLeaser.sprites[1].x = - camPos.x;
            sLeaser.sprites[1].y = - camPos.y;
            sLeaser.sprites[1].rotation = sLeaser.sprites[0].rotation;
            if (electricCharge == 0)
            {
                sLeaser.sprites[1].color = blackColor;
            }
            else
            {
                sLeaser.sprites[1].color = Color.Lerp(electricColor, Color.white, Mathf.Abs(Mathf.Sin(fluxTimer)));
            }

            sparkPoint = PointAlongSpear(sLeaser, 0.9f);
        }

        public override void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
        {
            base.ApplyPalette(sLeaser, rCam, palette);
            sLeaser.sprites[0].color = color;
            blackColor = palette.blackColor;
        }

        public override void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
        {
            if (newContatiner == null)
            {
                newContatiner = rCam.ReturnFContainer("Items");
            }

            sLeaser.sprites[0].RemoveFromContainer();
            newContatiner.AddChild(sLeaser.sprites[0]);
            for (int num = sLeaser.sprites.Length - 1; num >= 1; num--)
            {
                sLeaser.sprites[num].RemoveFromContainer();
                newContatiner.AddChild(sLeaser.sprites[num]);
            }
        }

        public void ShortCircuit()
        {
            if (electricCharge > 0)
            {
                Vector2 pos = base.firstChunk.pos;
                room.AddObject(new Explosion.ExplosionLight(pos, 40f, 1f, 2, electricColor));
                for (int i = 0; i < 8; i++)
                {
                    Vector2 vector = Custom.RNV();
                    room.AddObject(new Spark(pos + vector * UnityEngine.Random.value * 10f, vector * Mathf.Lerp(6f, 18f, UnityEngine.Random.value), electricColor, null, 4, 18));
                }

                room.AddObject(new ShockWave(pos, 30f, 0.035f, 2));
                room.PlaySound(SoundID.Fire_Spear_Pop, pos);
                room.PlaySound(SoundID.Firecracker_Bang, pos);
                room.InGameNoise(new InGameNoise(pos, 800f, this, 1f));
                vibrate = Math.Max(vibrate, 6);
                electricCharge = 0;
            }
        }

        public void ExplosiveShortCircuit()
        {
            if (electricCharge > 0)
            {
                Vector2 pos = base.firstChunk.pos;
                room.AddObject(new Explosion.ExplosionLight(pos, 40f, 1f, 2, electricColor));
                for (int i = 0; i < 8; i++)
                {
                    Vector2 vector = Custom.RNV();
                    room.AddObject(new Spark(pos + vector * UnityEngine.Random.value * 10f, vector * Mathf.Lerp(6f, 18f, UnityEngine.Random.value), electricColor, null, 4, 18));
                }

                room.AddObject(new ShockWave(pos, 30f, 0.035f, 2));
                room.PlaySound(SoundID.Fire_Spear_Pop, pos);
                room.PlaySound(SoundID.Firecracker_Bang, pos);
                room.InGameNoise(new InGameNoise(pos, 800f, this, 1f));
                vibrate = Math.Max(vibrate, 6);
                Destroy();
            }
        }

        public void Recharge()
        {
            if (electricCharge == 0)
            {
                electricCharge = 2;
                room.PlaySound(SoundID.Jelly_Fish_Tentacle_Stun, base.firstChunk.pos);
                room.AddObject(new Explosion.ExplosionLight(base.firstChunk.pos, 200f, 1f, 4, new Color(0.7f, 1f, 1f)));
                Spark();
                Zap();
                room.AddObject(new ZapCoil.ZapFlash(sparkPoint, 25f));
            }
        }

        public bool CheckElectricCreature(Creature otherObject)
        {
            if (!(otherObject is Centipede) && !(otherObject is MoreSlugcats.BigJellyFish))
            {
                return otherObject is MoreSlugcats.Inspector;
            }

            return true;
        }
    }
}