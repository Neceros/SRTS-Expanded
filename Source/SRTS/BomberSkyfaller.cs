using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using RimWorld.Planet;
using Verse;
using UnityEngine;
using Harmony;

namespace SRTS
{
    [StaticConstructorOnStartup]
    public class BomberSkyfaller : Thing, IThingHolder
    {
        public BomberSkyfaller()
        {
            this.innerContainer = new ThingOwner<Thing>(this);
            this.droppingBombs = false;
            this.tickCount = this.ticksPerDrop;
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
                        return SkyfallerDrawPosUtilityExtended.DrawPos_Accelerate(base.DrawPos, this.ticksToExit, this.angle, this.def.skyfaller.speed);
                    case SkyfallerMovementType.ConstantSpeed:
                        return SkyfallerDrawPosUtilityExtended.DrawPos_ConstantSpeed(base.DrawPos, this.ticksToExit, this.angle, this.def.skyfaller.speed);
                    case SkyfallerMovementType.Decelerate:
                        return SkyfallerDrawPosUtilityExtended.DrawPos_Decelerate(base.DrawPos, this.ticksToExit, this.angle, this.def.skyfaller.speed);
                    default:
                        Log.ErrorOnce("SkyfallerMovementType not handled: " + this.def.skyfaller.movementType, this.thingIDNumber ^ 1948576711, false);
                        return SkyfallerDrawPosUtilityExtended.DrawPos_Accelerate(base.DrawPos, this.ticksToExit, this.angle, this.def.skyfaller.speed);
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
            Scribe_Values.Look<int>(ref this.ticksToExit, "ticksToExit", 0, false);
            Scribe_Values.Look<float>(ref this.angle, "angle", 0f, false);
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
                this.ticksToExit = this.def.skyfaller.ticksToImpactRange.RandomInRange;
                //this.angle = -60f;
                if(this.def.rotatable && this.innerContainer.Any)
                {
                    //Rotation here?
                }
            }
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
            this.innerContainer.ThingOwnerTick(true);
            this.ticksToExit--;

            if(Math.Abs(this.DrawPosCell.x - bombPos.x) < 5 && Math.Abs(this.DrawPosCell.z - bombPos.z) < 5)
                this.droppingBombs = true;
            if(droppingBombs && this.tickCount < 0 && numberOfBombs > 0 && this.ticksToExit >= this.ticksPerDrop)
            {
                numberOfBombs--;
                this.DropBombs();
                tickCount = ticksPerDrop;
                if(numberOfBombs <= 0)
                    droppingBombs = false;
            }
            if(droppingBombs)
                tickCount--;
            if(this.ticksToExit == 0)
                this.ExitMap();
        }

        private void DropBombs()
        {
            if(innerContainer.Any(x => ((ActiveDropPod)x)?.Contents.innerContainer.Any(y => y.def == ThingDefOf.Shell_HighExplosive || y.def == ThingDefOf.Shell_AntigrainWarhead) ?? false))
            {
                ActiveDropPod srts = (ActiveDropPod)innerContainer.First();

                Thing thing = srts?.Contents.innerContainer.First(y => y.def == ThingDefOf.Shell_HighExplosive || y.def == ThingDefOf.Shell_AntigrainWarhead);
                if (thing is null)
                    return;

                Thing thing2 = srts?.Contents.innerContainer.Take(thing, 1);

                IntVec3 c = (from x in GenRadial.RadialCellsAround(this.bombPos, radius, true)
                             where x.InBounds(this.Map)
                             select x).RandomElementByWeight((IntVec3 x) => 1f - Mathf.Min(x.DistanceTo(this.Position) / radius, 1f) + 0.05f);

                Thing bomb = GenSpawn.Spawn(thing2, c, this.Map);
                AccessTools.Method(type: typeof(CompExplosive), name: "Detonate").Invoke(bomb.TryGetComp<CompExplosive>(), new object[] { thing2.Map });
            }
        }

        private void ExitMap()
        {
            ActiveDropPod activeDropPod = (ActiveDropPod)ThingMaker.MakeThing(ThingDef.Named(this.def.defName.Split('_')[0] + "_Active"), null);
            activeDropPod.Contents = new ActiveDropPodInfo();
            activeDropPod.Contents.innerContainer.TryAddRangeOrTransfer((IEnumerable<Thing>)((ActiveDropPod)innerContainer.First()).Contents.innerContainer, true, true);

            TravelingTransportPods travelingTransportPods = (TravelingTransportPods)WorldObjectMaker.MakeWorldObject(DefDatabase<WorldObjectDef>.GetNamed("TravelingSRTS", true));
            travelingTransportPods.Tile = this.Map.Tile;
            travelingTransportPods.SetFaction(Faction.OfPlayer);
            travelingTransportPods.destinationTile = this.source.First.Tile;
            travelingTransportPods.arrivalAction = new TransportPodsArrivalAction_LandInSpecificCell(this.source.First.Parent, this.source.Second);
            Find.WorldObjects.Add((WorldObject)travelingTransportPods);
            travelingTransportPods.AddPod(activeDropPod.Contents, true);
            this.Destroy();
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

        public int ticksToExit;

        public float angle;

        private Material cachedShadowMaterial;

        private bool anticipationSoundPlayed;

        private static MaterialPropertyBlock shadowPropertyBlock = new MaterialPropertyBlock();

        public const float DefaultAngle = -30f;

        private const int RoofHitPreDelay = 15;

        public IntVec3 bombPos;

        public Pair<Map, IntVec3> source;

        public int ticksPerDrop = 15;

        private int tickCount;

        public int numberOfBombs = 5;

        public int radius = 10;

        private bool droppingBombs = false;
    }
}
