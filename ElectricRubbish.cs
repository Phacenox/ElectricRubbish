using System;
using UnityEngine;

using Noise;
using RWCustom;
using MoreSlugcats;

namespace ElectricRubbish
{
    //this class copies a lot of code from moreslugcats' ElectricSpear
    public class ElectricRubbish : Rock
    {
        public ElectricRubbishAbstract rubbishAbstract;
        public float fluxSpeed;
        public float fluxTimer;

        public Color electricColor;

        public Color blackColor;

        public float zapPitch;

        public ElectricRubbish(AbstractPhysicalObject abstractPhysicalObject, World world)
            : base(abstractPhysicalObject, world)
        {
            UnityEngine.Random.State state = UnityEngine.Random.state;
            electricColor = Custom.HSL2RGB(UnityEngine.Random.Range(0.55f, 0.7f), UnityEngine.Random.Range(0.8f, 1f), UnityEngine.Random.Range(0.3f, 0.6f));
            UnityEngine.Random.InitState(abstractPhysicalObject.ID.RandomSeed);
            rubbishAbstract = (ElectricRubbishAbstract)abstractPhysicalObject;
            ResetFluxSpeed();

            UnityEngine.Random.state = state;
        }

        public override void Update(bool eu)
        {
            base.Update(eu);
            fluxTimer += fluxSpeed;
            if (fluxTimer > (float)Math.PI * 2f)
                ResetFluxSpeed();
            if (UnityEngine.Random.value < 0.0125f * rubbishAbstract.electricCharge) Spark();

            if(rubbishAbstract.electricCharge == 2)
            {
                float diff_x, diff_y;
                foreach (var i in room.abstractRoom.creatures)
                {
                    diff_x = Mathf.Abs(i.pos.x - abstractPhysicalObject.pos.x);
                    diff_y = Mathf.Abs(i.pos.y - abstractPhysicalObject.pos.y);
                    if(Mathf.Sqrt(Mathf.Pow(diff_x, 2) +  Mathf.Pow(diff_y,2)) < 1)
                    {
                        if(i.realizedCreature is Player p && ElectricRubbishOptions.OverchargeLethatlity >= ElectricRubbishOptions.LETHALITY.Kills_Artificer)
                        {
                            if(p.slugcatStats.name == MoreSlugcatsEnums.SlugcatStatsName.Artificer)
                            {
                                ExplosiveShortCircuit();
                                p.PyroDeath();
                                return;
                            }
                        }
                        if(ElectricRubbishOptions.OverchargeLethatlity >= ElectricRubbishOptions.LETHALITY.Kills_Anything)
                        {
                            i.realizedCreature.Violence(base.firstChunk, Vector2.zero, i.realizedCreature.firstChunk, null, Creature.DamageType.Electric, 1.5f, 0f);
                        }
                        Electrocute(i.realizedObject);
                    }
                }
            }

            for (int k = 0; k < room.zapCoils.Count; k++)
            {
                ZapCoil zapCoil = room.zapCoils[k];
                if (zapCoil.turnedOn > 0.5f && zapCoil.GetFloatRect.Vector2Inside(base.firstChunk.pos + rotation * 30f))
                {
                    if (rubbishAbstract.electricCharge == 0)
                        Recharge();
                    else
                        ExplosiveShortCircuit();
                    break;
                }
            }
        }

        public void Spark()
        {
            if (rubbishAbstract.electricCharge > 0)
            {
                int numSparks = rubbishAbstract.electricCharge > 1 ? 10 : 2;
                for (int i = 0; i < numSparks; i++)
                {
                    Vector2 vector = Custom.RNV();
                    room.AddObject(new Spark(base.firstChunk.pos + vector * UnityEngine.Random.value * 20f, vector * Mathf.Lerp(2f, 5f, UnityEngine.Random.value), Color.white, null, 4, 18));
                }
            }
        }

        public void Zap()
        {
            if (rubbishAbstract.electricCharge > 0)
            {
                room.AddObject(new ZapCoil.ZapFlash(base.firstChunk.pos, 10f));
                room.PlaySound(SoundID.Zapper_Zap, base.firstChunk.pos, 1f, (zapPitch == 0f) ? (1.5f + UnityEngine.Random.value * 1.5f) : zapPitch);
                if (base.Submersion > 0.5f)
                    room.AddObject(new UnderwaterShock(room, null, base.firstChunk.pos, 10, 800f, 2f, thrownBy, new Color(0.8f, 0.8f, 1f)));
            }
        }

        public void Electrocute(PhysicalObject otherObject, float stunScalar = 1)
        {
            if (!(otherObject is Creature))
                return;

            bool flag = CheckElectricCreature(otherObject as Creature);
            if (flag && rubbishAbstract.electricCharge == 0)
            {
                Recharge();
            }
            else
            {
                if (rubbishAbstract.electricCharge == 0)
                    return;

                if (!(otherObject is BigEel) && !flag)
                {
                    (otherObject as Creature).Violence(base.firstChunk, Custom.DirVec(base.firstChunk.pos, otherObject.firstChunk.pos) * 5f, otherObject.firstChunk, null, Creature.DamageType.Electric, 0.1f, (!(otherObject is Player)) ? stunScalar * (320f * Mathf.Lerp((otherObject as Creature).Template.baseStunResistance, 1f, 0.5f)) : stunScalar * 140f);
                    room.AddObject(new CreatureSpasmer(otherObject as Creature, allowDead: false, (otherObject as Creature).stun));
                }

                if (base.Submersion <= 0.5f && otherObject.Submersion > 0.5f)
                    room.AddObject(new UnderwaterShock(room, null, otherObject.firstChunk.pos, 10, 800f, 2f, thrownBy, new Color(0.8f, 0.8f, 1f)));

                room.PlaySound(SoundID.Jelly_Fish_Tentacle_Stun, base.firstChunk.pos, Mathf.Max(stunScalar, 0.6f), 1);
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

                if (rubbishAbstract.electricCharge > 0)
                    rubbishAbstract.electricCharge--;
            }
        }

        public override void WeaponDeflect(Vector2 inbetweenPos, Vector2 deflectDir, float bounceSpeed)
        {
            base.WeaponDeflect(inbetweenPos, deflectDir, bounceSpeed);
            Zap();
        }

        public override void Grabbed(Creature.Grasp grasp)
        {
            base.Grabbed(grasp);
            if (rubbishAbstract.electricCharge == 2)
                Electrocute(grasp.grabber, 0.1f);
        }

        public override void Thrown(Creature thrownBy, Vector2 thrownPos, Vector2? firstFrameTraceFromPos, IntVector2 throwDir, float frc, bool eu)
        {
            base.Thrown(thrownBy, thrownPos, firstFrameTraceFromPos, throwDir, frc, eu);
            Spark();
        }

        public override bool HitSomething(SharedPhysics.CollisionResult result, bool eu)
        {
            if (result.obj != null)
            {
                Electrocute(result.obj);
                Spark();
            }
            return base.HitSomething(result, eu);
        }

        public void ResetFluxSpeed()
        {
            fluxSpeed = UnityEngine.Random.value * 0.2f + rubbishAbstract.electricCharge == 2 ? 0.05f : 0.025f;
            while (fluxTimer > (float)Math.PI * 2f) 
                fluxTimer -= (float)Math.PI * 2f;
        }

        public override void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            base.DrawSprites(sLeaser, rCam, timeStacker, camPos);

            if (rubbishAbstract.electricCharge == 0)
                sLeaser.sprites[0].color = blackColor;
            else
                sLeaser.sprites[0].color = Color.Lerp(electricColor,
                    rubbishAbstract.electricCharge == 2 ? Color.white : electricColor + new Color(0.6f, 0.6f, 0.6f, 0),
                    rubbishAbstract.electricCharge * Mathf.Abs(Mathf.Sin(fluxTimer)));
        }

        public override void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
        {
            base.ApplyPalette(sLeaser, rCam, palette);
            sLeaser.sprites[0].color = color;
            blackColor = palette.blackColor;
        }

        public void ExplosiveShortCircuit()
        {
            if (rubbishAbstract.electricCharge > 0)
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
            if (rubbishAbstract.electricCharge == 0)
            {
                rubbishAbstract.electricCharge = 2;
                room.PlaySound(SoundID.Jelly_Fish_Tentacle_Stun, base.firstChunk.pos);
                room.AddObject(new Explosion.ExplosionLight(base.firstChunk.pos, 200f, 1f, 4, new Color(0.7f, 1f, 1f)));
                Spark();
                Zap();
                room.AddObject(new ZapCoil.ZapFlash(base.firstChunk.pos, 25f));
            }
        }

        public static bool CheckElectricCreature(Creature otherObject)
        {
            if (!(otherObject is Centipede) && !(otherObject is BigJellyFish))
                return otherObject is Inspector;
            return true;
        }
    }
}