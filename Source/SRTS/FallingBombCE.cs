using System;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;
using HarmonyLib;
using UnityEngine;

namespace SRTS
{
    [StaticConstructorOnStartup]
    public class FallingBombCE : FallingBomb
    {
        public FallingBombCE(Thing reference, CompProperties comp, ThingComp CEcomp, Map map, string texPathShadow)
        {
            if (!SRTSHelper.CEModLoaded)
            {
                throw new NotImplementedException("Calling wrong constructor. This is only enabled for Combat Extended calls. - Smash Phil");
            }
            this.def = reference.def;
            this.thingIDNumber = reference.thingIDNumber;
            this.map = map;
            this.factionInt = reference.Faction;
            this.graphicInt = reference.DefaultGraphic;
            this.hitPointsInt = reference.HitPoints;
            this.CEprops = comp;
            this.CEcomp = CEcomp;
            explosionRadius = (float)AccessTools.Field(SRTSHelper.CompProperties_ExplosiveCE, "explosionRadius").GetValue(comp);
            this.texPathShadow = texPathShadow;
        }

        public override void Tick()
        {
            if (ticksRemaining < 0)
            {
                this.ExplodeOnImpact();
            }
            ticksRemaining--;
        }

        protected override void ExplodeOnImpact()
        {
            if (!this.SpawnedOrAnyParentSpawned)
                return;
            AccessTools.Method(type: SRTSHelper.CompExplosiveCE, "Explode").Invoke(this.CEcomp, new object[] { this as Thing, this.Position.ToVector3(), this.Map, 1f});
            this.Destroy();
        }

        private CompProperties CEprops;

        private ThingComp CEcomp;

        private float explosionRadius;
    }
}
