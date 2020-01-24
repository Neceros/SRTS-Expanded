using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using RimWorld;
using Harmony;

namespace SRTS
{
    [StaticConstructorOnStartup]
    internal static class InitializeDefProperties
    {
        static InitializeDefProperties()
        {
            SRTSMod.mod.settings.CheckDictionarySavedValid();
            SRTS_ModSettings.CheckNewDefaultValues();
            StartUp.PopulateDictionary();
            StartUp.PopulateAllowedBombs();

            CombatExtendedInitialized();
        }

        private static void CombatExtendedInitialized()
        {
            List<ModMetaData> mods = ModLister.AllInstalledMods.ToList();

            foreach(ModMetaData mod in mods)
            {
                if(ModLister.HasActiveModWithName(mod.Name) && mod.Identifier == "1631756268" && !StartUp.CEModLoaded)
                {
                    Log.Message("[SRTS Expanded] Initializing Combat Extended patch for Bombing Runs.");
                    if(!SRTSMod.mod.settings.CEPreviouslyInitialized)
                        SRTSMod.mod.ResetBombList();
                    SRTSMod.mod.settings.CEPreviouslyInitialized = true;

                    StartUp.CEModLoaded = true;
                    StartUp.CompProperties_ExplosiveCE = AccessTools.TypeByName("CompProperties_ExplosiveCE");
                    StartUp.CompExplosiveCE = AccessTools.TypeByName("CompExplosiveCE");
                }
            }
            if(SRTSMod.mod.settings.CEPreviouslyInitialized && !StartUp.CEModLoaded)
            {
                SRTSMod.mod.settings.CEPreviouslyInitialized = false;
                SRTSMod.mod.ResetBombList();
            }
        }
    }
}
