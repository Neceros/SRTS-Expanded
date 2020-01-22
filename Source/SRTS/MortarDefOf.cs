using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;

namespace SRTS
{
    [DefOf]
    public static class MortarDefOf
    {
        static MortarDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(MortarDefOf));
        }

        public static ThingCategoryDef MortarShells;
    }
}
