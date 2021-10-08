using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using RimWorld.Planet;
using Verse;
using UnityEngine;
using SPExtended;
using Verse.Sound;

namespace SRTS
{
    [StaticConstructorOnStartup]
    public class BomberSkyfaller : Thing, IThingHolder
    {
        public BomberSkyfaller()
        {
            this.innerContainer = new ThingOwner<Thing>(this);
            this.bombCells = new List<IntVec3>();
        }

        public override Graphic Graphic
        {
            get
            {
                Thing thingForGraphic = this.GetThingForGraphic();
                if(thingForGraphic == this)
                    return base.Graphic;
                return thingForGraphic.Graphic.ExtractInnerGraphicFor(thingForGraphic).GetShadowlessGraphic();
            }
        }

        public override Vector3 DrawPos
        {
            get
            {
                switch (this.def.skyfaller.movementType)
                {
                    case SkyfallerMovementType.Accelerate:
                        return SkyfallerDrawPosUtility.DrawPos_Accelerate(base.DrawPos, this.ticksToExit, this.angle, this.speed);
                    case SkyfallerMovementType.ConstantSpeed:
                        return SkyfallerDrawPosUtility.DrawPos_ConstantSpeed(base.DrawPos, this.ticksToExit, this.angle, this.speed);
                    case SkyfallerMovementType.Decelerate:
                        return SkyfallerDrawPosUtility.DrawPos_Decelerate(base.DrawPos, this.ticksToExit, this.angle, this.speed);
                    default:
                        Log.ErrorOnce("SkyfallerMovementType not handled: " + this.def.skyfaller.movementType, this.thingIDNumber ^ 1948576711);
                        return SkyfallerDrawPosUtility.DrawPos_Accelerate(base.DrawPos, this.ticksToExit, this.angle, this.speed);
                }
            }
        }

        public IntVec3 DrawPosCell
        {
            get
            {
                return new IntVec3((int)this.DrawPos.x, (int)this.DrawPos.y, (int)this.DrawPos.z);
            }
        }

        private Material ShadowMaterial
        {
            get
            {
                if(this.cachedShadowMaterial is null && !this.def.skyfaller.shadow.NullOrEmpty())
                    this.cachedShadowMaterial = MaterialPool.MatFrom(this.def.skyfaller.shadow, ShaderDatabase.Transparent);
                return this.cachedShadowMaterial;
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Deep.Look<ThingOwner>(ref this.innerContainer, "innerContainer", new object[]
            {
                this
            });
            Scribe_References.Look(ref originalMap, "originalMap");
            Scribe_Values.Look<int>(ref ticksToExit, "ticksToExit", 0, false);
            Scribe_Values.Look<float>(ref angle, "angle", 0f, false);
            Scribe_Values.Look(ref sourceLandingSpot, "sourceLandingSpot");
            Scribe_Collections.Look<IntVec3>(ref bombCells, "bombCells", LookMode.Value);

            Scribe_Values.Look(ref numberOfBombs, "numberOfBombs");
            Scribe_Values.Look(ref speed, "speed");
            Scribe_Values.Look(ref radius, "radius");
            Scribe_Defs.Look(ref sound, "sound");
        }

        public override void PostMake()
        {
            base.PostMake();
        }

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            if(!respawningAfterLoad)
            {
                this.ticksToExit = Mathf.CeilToInt((float)SPExtra.Distance(new IntVec3(map.Size.x/2, map.Size.y, map.Size.z/2), this.Position)*2 / this.speed);
            }
            if(sound != null)
                sound.PlayOneShotOnCamera(this.Map);
        }

        public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
        {
            base.Destroy(mode);
            this.innerContainer.ClearAndDestroyContents(DestroyMode.Vanish);
        }

        public override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            Thing thingForGraphic = this.GetThingForGraphic();
            float extraRotation = this.angle;
            this.Graphic.Draw(drawLoc, !flip ? thingForGraphic.Rotation.Opposite : thingForGraphic.Rotation, thingForGraphic, extraRotation);
            this.DrawDropSpotShadow();
        }

        public override void Tick()
        {
			try
			{
                this.innerContainer.ThingOwnerTick(true);
                this.ticksToExit--;
                if (bombCells.Any() && Math.Abs(this.DrawPosCell.x - bombCells.First().x) < 3 && Math.Abs(this.DrawPosCell.z - bombCells.First().z) < 3)
                {
                    this.DropBomb();
                }
                if (this.ticksToExit == 0)
                {
                    this.ExitMap();
                }
            }
            catch (Exception ex)
			{
                Log.Error($"Exception thrown while ticking {this}. Immediately sending to world to avoid loss of contents. Ex=\"{ex.Message}\"");
                ExitMap();
			}
        }

        private void DropBomb()
        {
            for(int i = 0; i < (bombType == BombingType.precise ? this.precisionBombingNumBombs : 1); ++i)
            {
                if (innerContainer.Any(x => ((ActiveDropPod)x)?.Contents.innerContainer.Any(y => SRTSMod.mod.settings.allowedBombs.Contains(y.def.defName)) ?? false))
                {
                    ActiveDropPod srts = (ActiveDropPod)innerContainer.First();

                    Thing thing = srts?.Contents.innerContainer.FirstOrDefault(y => SRTSMod.mod.settings.allowedBombs.Contains(y.def.defName));
                    if (thing is null)
                        return;

                    Thing thing2 = srts?.Contents.innerContainer.Take(thing, 1);

                    IntVec3 bombPos = bombCells[0];
                    if(bombType == BombingType.carpet)
                        bombCells.RemoveAt(0);
                    int timerTickExplode = 20 + Rand.Range(0, 5); //Change later to allow release timer
                    if (SRTSHelper.CEModLoaded)
                        goto Block_CEPatched;
                    FallingBomb bombThing = new FallingBomb(thing2, thing2.TryGetComp<CompExplosive>(), this.Map, this.def.skyfaller.shadow);
                    bombThing.HitPoints = int.MaxValue;
                    bombThing.ticksRemaining = timerTickExplode;

                    IntVec3 c = (from x in GenRadial.RadialCellsAround(bombPos, GetCurrentTargetingRadius(), true)
                                 where x.InBounds(this.Map)
                                 select x).RandomElementByWeight((IntVec3 x) => 1f - Mathf.Min(x.DistanceTo(this.Position) / GetCurrentTargetingRadius(), 1f) + 0.05f);
                    bombThing.angle = this.angle + (SPTrig.LeftRightOfLine(this.DrawPosCell, this.Position, c) * -10);
                    bombThing.speed = (float)SPExtra.Distance(this.DrawPosCell, c) / bombThing.ticksRemaining;
                    Thing t = GenSpawn.Spawn(bombThing, c, this.Map);
                    GenExplosion.NotifyNearbyPawnsOfDangerousExplosive(t, thing2.TryGetComp<CompExplosive>().Props.explosiveDamageType, null);
                    continue;

                Block_CEPatched:;
                    ThingComp CEComp = (thing2 as ThingWithComps)?.AllComps.Find(x => x.GetType().Name == "CompExplosiveCE");
                    FallingBombCE CEbombThing = new FallingBombCE(thing2, CEComp.props, CEComp, this.Map, this.def.skyfaller.shadow);
                    CEbombThing.HitPoints = int.MaxValue;
                    CEbombThing.ticksRemaining = timerTickExplode;
                    IntVec3 c2 = (from x in GenRadial.RadialCellsAround(bombPos, GetCurrentTargetingRadius(), true)
                                  where x.InBounds(this.Map)
                                  select x).RandomElementByWeight((IntVec3 x) => 1f - Mathf.Min(x.DistanceTo(this.Position) / GetCurrentTargetingRadius(), 1f) + 0.05f);
                    CEbombThing.angle = this.angle + (SPTrig.LeftRightOfLine(this.DrawPosCell, this.Position, c2) * -10);
                    CEbombThing.speed = (float)SPExtra.Distance(this.DrawPosCell, c2) / CEbombThing.ticksRemaining;
                    Thing CEt = GenSpawn.Spawn(CEbombThing, c2, this.Map);
                    //GenExplosion.NotifyNearbyPawnsOfDangerousExplosive(CEt, DamageDefOf., null); /*Is GenExplosion CE compatible?*/
                }
            }
            if(bombType == BombingType.precise && bombCells.Any())
                bombCells.Clear();
        }

        private void ExitMap()
        {
            ActiveDropPod activeDropPod = (ActiveDropPod)ThingMaker.MakeThing(ThingDef.Named(this.def.defName.Split('_')[0] + "_Active"), null);
            activeDropPod.Contents = new ActiveDropPodInfo();
            activeDropPod.Contents.innerContainer.TryAddRangeOrTransfer((IEnumerable<Thing>)((ActiveDropPod)innerContainer.First()).Contents.innerContainer, true, true);

            TravelingSRTS travelingTransportPods = (TravelingSRTS)WorldObjectMaker.MakeWorldObject(DefDatabase<WorldObjectDef>.GetNamed("TravelingSRTS", true));
            travelingTransportPods.Tile = this.Map.Tile;
            travelingTransportPods.SetFaction(Faction.OfPlayer);
            travelingTransportPods.destinationTile = this.originalMap.Tile;
            travelingTransportPods.arrivalAction = new TransportPodsArrivalAction_LandInSpecificCell(this.originalMap.Parent, this.sourceLandingSpot);
            travelingTransportPods.flyingThing = this;
            Find.WorldObjects.Add((WorldObject)travelingTransportPods);
            travelingTransportPods.AddPod(activeDropPod.Contents, true);
            this.Destroy();
        }

        private int GetCurrentTargetingRadius()
        {
            switch(bombType)
            {
                case BombingType.carpet:
                    return radius;
                case BombingType.precise:
                    return (int)(radius * 0.6f);
                case BombingType.missile:
                    throw new NotImplementedException("BombingType");
                default:
                    throw new NotImplementedException("BombingType");
            }
        }

        public ThingOwner GetDirectlyHeldThings()
        {
            return this.innerContainer;
        }

        public void GetChildHolders(List<IThingHolder> outChildren)
        {
            ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, this.GetDirectlyHeldThings());
        }

        private Thing GetThingForGraphic()
        {
            if (this.def.graphicData != null || !this.innerContainer.Any)
            {
                return this;
            }
            return this.innerContainer[0];
        }

        private void DrawDropSpotShadow()
        {
            Material shadowMaterial = this.ShadowMaterial;
            if (shadowMaterial == null)
            {
                return;
            }
            Skyfaller.DrawDropSpotShadow(base.DrawPos, base.Rotation, shadowMaterial, this.def.skyfaller.shadowSize, this.ticksToExit);
        }

        public static void DrawBombSpotShadow(Vector3 loc, Rot4 rot, Material material, Vector2 shadowSize, int ticksToExit)
        {
            if(rot.IsHorizontal)
                Gen.Swap<float>(ref shadowSize.x, ref shadowSize.y);
            ticksToExit = Mathf.Max(ticksToExit, 0);
            Vector3 pos = loc;
            pos.y = AltitudeLayer.Shadows.AltitudeFor();
            float num = 1f + (float)ticksToExit / 100f;
            Vector3 s = new Vector3(num * shadowSize.x, 1f, num * shadowSize.y);
            Color white = Color.white;
            if (ticksToExit > 150)
                white.a = Mathf.InverseLerp(200f, 150f, (float)ticksToExit);
            shadowPropertyBlock.SetColor(ShaderPropertyIDs.Color, white);
            Matrix4x4 matrix = default(Matrix4x4);
            matrix.SetTRS(pos, rot.AsQuat, s);
            Graphics.DrawMesh(MeshPool.plane10Back, matrix, material, 0, null, 0, shadowPropertyBlock);
        }

        public ThingOwner innerContainer;

        private int ticksToExit;

        public float angle;

        private Material cachedShadowMaterial;

        public SoundDef sound;

        private static MaterialPropertyBlock shadowPropertyBlock = new MaterialPropertyBlock();

        public List<IntVec3> bombCells = new List<IntVec3>();

        public Map originalMap;

        public IntVec3 sourceLandingSpot;

        public int numberOfBombs;

        public int radius;

        public int precisionBombingNumBombs;

        public float speed;

        public BombingType bombType;
    }
}
