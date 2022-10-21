using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using RimWorld;
using HarmonyLib;
using UnityEngine;

namespace SRTS
{
    [StaticConstructorOnStartup]
    public class FallingBomb : ThingWithComps
    {
        public FallingBomb()
        {
        }

        public FallingBomb(Thing reference, CompExplosive comp, Map map, string texPathShadow)
        {
            this.def = reference.def;
            this.projectile = def.projectileWhenLoaded.projectile;
            this.thingIDNumber = reference.thingIDNumber;
            this.map = map;
            this.factionInt = reference.Faction;
            this.graphicInt = reference.DefaultGraphic;
            this.hitPointsInt = reference.HitPoints;
            props = reference.TryGetComp<CompExplosive>().Props;
            if(projectile?.explosionRadius != null)
                explosionRadius = def.projectileWhenLoaded.projectile.explosionRadius;
            else
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

        protected Material ShadowMaterial
        {
            get
            {
                if(texPathShadow != null && !texPathShadow.NullOrEmpty())
                    cachedShadowMaterial = MaterialPool.MatFrom(texPathShadow, ShaderDatabase.Transparent);
                return cachedShadowMaterial;
            }
        }

        protected void DrawDropSpotShadow()
        {
            if (this.ShadowMaterial is null)
                return;
            FallingBomb.DrawDropSpotShadow(base.DrawPos, base.Rotation, ShadowMaterial, new Vector2(this.RotatedSize.x, this.RotatedSize.z), ticksRemaining);
        }

        protected static void DrawDropSpotShadow(Vector3 center, Rot4 rot, Material material, Vector2 shadowSize, int ticksToImpact)
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
            if(def.projectileWhenLoaded?.projectile != null)
            {
                GenExplosion.DoExplosion(PositionHeld, map, explosionRadius, projectile.damageDef, this, damAmount: projectile.GetDamageAmount(1), armorPenetration: projectile.GetArmorPenetration(1), 
                    explosionSound: projectile.soundExplode, projectile: def.projectileWhenLoaded, 
                    postExplosionSpawnThingDef: projectile.postExplosionSpawnThingDef, postExplosionSpawnChance: projectile.postExplosionSpawnChance,
                    postExplosionSpawnThingCount: projectile.postExplosionSpawnThingCount, applyDamageToExplosionCellsNeighbors: projectile.applyDamageToExplosionCellsNeighbors,
                    preExplosionSpawnThingDef: projectile.preExplosionSpawnThingDef, preExplosionSpawnChance: projectile.preExplosionSpawnChance,
                    preExplosionSpawnThingCount: projectile.preExplosionSpawnThingCount,
                    chanceToStartFire: projectile.explosionChanceToStartFire, damageFalloff: projectile.explosionDamageFalloff);
            }
            else
            {
                GenExplosion.DoExplosion(PositionHeld, map, explosionRadius, props.explosiveDamageType, this, damAmount: props.damageAmountBase, armorPenetration: props.armorPenetrationBase,
                    explosionSound: props.explosionSound,
                    postExplosionSpawnThingDef: props.postExplosionSpawnThingDef, postExplosionSpawnChance: props.postExplosionSpawnChance,
                    postExplosionSpawnThingCount: props.postExplosionSpawnThingCount, applyDamageToExplosionCellsNeighbors: props.applyDamageToExplosionCellsNeighbors,
                    preExplosionSpawnThingDef: props.preExplosionSpawnThingDef, preExplosionSpawnChance: props.preExplosionSpawnChance,
                    preExplosionSpawnThingCount: props.preExplosionSpawnThingCount,
                    chanceToStartFire: props.chanceToStartFire, damageFalloff: props.damageFalloff);
            }
        }

        public static Vector3 DrawBombFalling(Vector3 center, int ticksToImpact, float angle, float speed)
        {
            float dist = ticksToImpact * speed;
            return center + Vector3Utility.FromAngleFlat(angle - 90f) * dist;
        }

        public int ticksRemaining;

        protected Map map;

        protected Graphic graphicInt;

        protected int hitPointsInt = -1;

        private CompProperties_Explosive props;

        private ProjectileProperties projectile;

        private float explosionRadius;

        public float angle;

        public float speed;

        protected string texPathShadow;

        protected Material cachedShadowMaterial;

        protected static MaterialPropertyBlock shadowPropertyBlock = new MaterialPropertyBlock();
    }
}
