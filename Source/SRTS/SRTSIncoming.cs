using System;
using UnityEngine;
using Verse;
using RimWorld;

namespace SRTS
{
    public class SRTSIncoming : Skyfaller, IActiveDropPod, IThingHolder
    {

        public Rot4 SRTSRotation
        {
            get
            {
                return this.rotation;
            }
            set
            {
                if (this.rotation == value)
                    return;
                this.rotation = value;
            }
        }
        public ActiveDropPodInfo Contents
        {
            get
            {
                return ((ActiveDropPod)this.innerContainer[0]).Contents;
            }
            set
            {
                ((ActiveDropPod)this.innerContainer[0]).Contents = value;
            }
        }

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            this.Rotation = Rot4.East;
        }

        protected override void Impact()
        {
            for (int i = 0; i < 6; i++)
            {
                Vector3 loc = Position.ToVector3Shifted() + Gen.RandomHorizontalVector(1f);
                FleckMaker.ThrowDustPuff(loc, base.Map, 1.2f);
            }
            
            FleckMaker.ThrowLightningGlow(Position.ToVector3Shifted(), Map, 2f);
            GenClamor.DoClamor(this, 15f, ClamorDefOf.Impact);
            base.Impact();
        }

        private Rot4 rotation;
    }
}
