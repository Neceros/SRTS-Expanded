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

        protected override void Impact()
        {
            for (int i = 0; i < 6; i++)
            {
                Vector3 loc = base.Position.ToVector3Shifted() + Gen.RandomHorizontalVector(1f);
                MoteMaker.ThrowDustPuff(loc, base.Map, 1.2f);
            }
            MoteMaker.ThrowLightningGlow(base.Position.ToVector3Shifted(), base.Map, 2f);
            GenClamor.DoClamor(this, 15f, ClamorDefOf.Impact);
            base.Impact();
        }

        private Rot4 rotation;
    }
}
