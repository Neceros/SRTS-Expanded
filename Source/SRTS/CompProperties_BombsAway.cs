using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace SRTS
{
    public class CompProperties_BombsAway : CompProperties
    {
        public CompProperties_BombsAway()
        {
            this.compClass = typeof(CompBombFlyer);
        }

        public int numberBombs = 3;
        public float accuracy = 0.8f;
    }
}
