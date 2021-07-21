﻿using RimWorld;
using Verse;

namespace SRTS
{
    public class SRTSBombing : BomberSkyfaller, IActiveDropPod, IThingHolder
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

        private Rot4 rotation;
    }
}