using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using RimWorld;

namespace SRTS
{
    [StaticConstructorOnStartup]
    internal static class InitializeDefProperties
    {
        static InitializeDefProperties()
        {
            SRTSMod.mod.settings.CheckDictionarySavedValid();
            SRTS_ModSettings.CheckNewDefaultValues();
        }
    }
}
