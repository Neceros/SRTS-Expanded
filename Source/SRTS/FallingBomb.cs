using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using RimWorld;
using Harmony;
using UnityEngine;

namespace SRTS
{
    [StaticConstructorOnStartup]
    public class FallingBomb : ThingWithComps
    {
        public FallingBomb(Thing reference, CompExplosive comp, Map map, string texPathShadow)
        {
            this.def = reference.def;
            this.thingIDNumber = reference.thingIDNumber;
            this.map = map;
            this.positionInt = reference.Position;
            this.rotationInt = reference.Rotation;
            this.factionInt = reference.Faction;
            this.graphicInt = reference.DefaultGraphic;
            this.hitPointsInt = reference.HitPoints;
            props = reference.TryGetComp<CompExplosive>().Props;
            explosionRadius = comp.ExplosiveRadius();
            this.texPathShadow = texPathShadow;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<int>(ref this.ticksRemaining, "ticksRemaining");
            Scribe_Values.Look<float>(ref this.angle, "angle");
            Scribe_Values.Look<string>(ref texPathShadow, "cachedShadowMaterial");
        }

        public override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            float angleDropped = this.angle - 45f;
            this.Graphic.Draw(drawLoc, !flip ? base.Rotation.Opposite : base.Rotation, this as Thing, angleDropped);
            this.DrawDropSpotShadow();
        }

        public override Vector3 DrawPos
        {
            get
            {
                return FallingBomb.DrawBombFalling(base.DrawPos, this.ticksRemaining, this.angle, this.speed);
            }
        }

        private Material ShadowMaterial
        {
            get
            {
                if(texPathShadow != null && !texPathShadow.NullOrEmpty())
                    cachedShadowMaterial = MaterialPool.MatFrom(texPathShadow, ShaderDatabase.Transparent);
                return cachedShadowMaterial;
            }
        }

        private void DrawDropSpotShadow()
        {
            if (this.ShadowMaterial is null)
                return;
            FallingBomb.DrawDropSpotShadow(base.DrawPos, base.Rotation, ShadowMaterial, new Vector2(this.RotatedSize.x, this.RotatedSize.z), ticksRemaining);
        }

        private static void DrawDropSpotShadow(Vector3 center, Rot4 rot, Material material, Vector2 shadowSize, int ticksToImpact)
        {
            if(rot.IsHorizontal)
                Gen.Swap<float>(ref shadowSize.x, ref shadowSize.y);
            ticksToImpact = Mathf.Max(ticksToImpact, 0);
            Vector3 pos = center;
            pos.y = AltitudeLayer.Shadows.AltitudeFor();
            float num = 1f + (float)ticksToImpact / 100f;
            Vector3 s = new Vector3(num * shadowSize.x, 1f, num * shadowSize.y);
            Color white = Color.white;
            if (ticksToImpact > 150)
            {
                white.a = Mathf.InverseLerp(200f, 150f, (float)ticksToImpact);
            }
            FallingBomb.shadowPropertyBlock.SetColor(ShaderPropertyIDs.Color, white);
            Matrix4x4 matrix = default;
            matrix.SetTRS(pos, rot.AsQuat, s);
            Graphics.DrawMesh(MeshPool.plane10Back, matrix, material, 0, null, 0, FallingBomb.shadowPropertyBlock);
        }

        public override void Tick()
        {
            if(ticksRemaining < 0)
            {
                this.ExplodeOnImpact();
            }
            ticksRemaining--;
        }

        protected virtual void ExplodeOnImpact()
        {
            if (!this.SpawnedOrAnyParentSpawned)
                return;
            if (props.destroyThingOnExplosionSize <= explosionRadius && !this.Destroyed)
            {
                this.Kill(null, null);
            }
            if (props.explosionEffect != null)
            {
                Effecter effecter = props.explosionEffect.Spawn();
                effecter.Trigger(new TargetInfo(this.PositionHeld, map, false), new TargetInfo(this.PositionHeld, map, false));
                effecter.Cleanup();
            }
            GenExplosion.DoExplosion(this.PositionHeld, map, explosionRadius, props.explosiveDamageType, this, props.damageAmountBase, props.armorPenetrationBase, props.explosionSound, null, null, null, props.postExplosionSpawnThingDef,
                props.postExplosionSpawnChance, props.postExplosionSpawnThingCount, props.applyDamageToExplosionCellsNeighbors, props.preExplosionSpawnThingDef, props.preExplosionSpawnChance, props.preExplosionSpawnThingCount, props.chanceToStartFire,
                props.damageFalloff);
        }

        public static Vector3 DrawBombFalling(Vector3 center, int ticksToImpact, float angle, float speed)
        {
            float dist = (float)ticksToImpact * speed;
            return center + Vector3Utility.FromAngleFlat(angle - 90f) * dist;
        }

        public int ticksRemaining;

        private Map map;

        private IntVec3 positionInt = IntVec3.Invalid;

        private Rot4 rotationInt = Rot4.North;

        private Graphic graphicInt;

        private int hitPointsInt = -1;

        private CompProperties_Explosive props;

        private float explosionRadius;

        public float angle;

        public float speed;

        private string texPathShadow;

        private Material cachedShadowMaterial;

        private static MaterialPropertyBlock shadowPropertyBlock = new MaterialPropertyBlock();
    }
}
