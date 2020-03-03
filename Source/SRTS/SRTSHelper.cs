using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;

namespace SRTS
{
    public static class SRTSHelper
    {
        public static float GetResearchStat(ResearchProjectDef project) => SRTSMod.GetStatFor<float>(srtsDefProjects.FirstOrDefault(x => x.Value == project).Key.defName, StatName.researchPoints);
        public static NamedArgument GetResearchStatString(ResearchProjectDef project) => SRTSMod.GetStatFor<float>(srtsDefProjects.FirstOrDefault(x => x.Value == project).Key.defName, StatName.researchPoints).ToString("F0");
        public static bool ContainedInDefProjects(ResearchProjectDef project) => srtsDefProjects.Any(x => x.Value == project);
        public static bool SRTSInCaravan => TradeSession.playerNegotiator.GetCaravan().AllThings.Any(x => x.TryGetComp<CompLaunchableSRTS>() != null);
        public static bool SRTSInTransporters(List<CompTransporter> transporters) => transporters.Any(x => x.parent.GetComp<CompLaunchableSRTS>() != null);
        public static bool DynamicTexturesEnabled => SRTSMod.mod.settings.dynamicWorldDrawingSRTS;
        public static bool SRTSLauncherSelected(List<CompTransporter> transporters) => transporters.Any(x => x.parent.GetComp<CompLaunchableSRTS>() != null);
        public static bool SRTSNonPlayerHomeMap(Map map) => !map.IsPlayerHome;

        public static void AddToSRTSFromCaravan(Caravan caravan, Thing thing)
        {
            if (caravan.AllThings.Any(x => x.TryGetComp<CompLaunchableSRTS>() != null))
                caravan.AllThings.First(x => x.TryGetComp<CompLaunchableSRTS>() != null).TryGetComp<CompLaunchableSRTS>()?.AddThingsToSRTS(thing);
        }

        public static void FinishCustomPrerequisites(ResearchProjectDef project, ResearchManager instance)
        {
            List<ResearchProjectDef> projects = SRTSMod.mod.settings.defProperties[srtsDefProjects.FirstOrDefault(x => x.Value == project).Key.defName].CustomResearch;
            foreach (ResearchProjectDef proj in projects)
            {
                if (!proj.IsFinished)
                {
                    instance.FinishProject(proj, false, null);
                }
            }
        }

        public static void DrawLinesCustomPrerequisites(ResearchProjectDef project, ResearchTabDef curTab, Vector2 start, Vector2 end, ResearchProjectDef selectedProject, int i)
        {
            List<ResearchProjectDef> projects = new List<ResearchProjectDef>();
            string defName = srtsDefProjects.FirstOrDefault(x => x.Value == project).Key?.defName;
            if (defName is null)
                return;
            projects = SRTSMod.mod.settings.defProperties[defName].CustomResearch;

            start.x = project.ResearchViewX * 190f + 140f;
            start.y = project.ResearchViewY * 100f + 25f;
            foreach (ResearchProjectDef proj in projects)
            {
                if (proj != null && proj.tab == curTab)
                {
                    end.x = proj.ResearchViewX * 190f;
                    end.y = proj.ResearchViewY * 100f + 25f;
                    if (selectedProject !=  null && (selectedProject == project || selectedProject == proj))
                    {
                        if (i == 1)
                            Widgets.DrawLine(start, end, TexUI.HighlightLineResearchColor, 4f);
                    }
                    if (i == 0)
                        Widgets.DrawLine(start, end, new Color(255, 215, 0, 0.25f), 2f);
                }
            }
        }

        /* =========================== Redacted but used as helper methods to ErrorOnNoPawnsTranspiler =========================== */
        public static string MinMaxString(List<CompTransporter> transporters, bool min)
        {
            var srts = transporters.First(x => x.parent.GetComp<CompLaunchableSRTS>() != null).parent;
            return min ? "Minimum Required Pawns for " + srts.def.LabelCap + ": " + (SRTSMod.GetStatFor<int>(srts.def.defName, StatName.minPassengers)) :
                "Maximum Pawns able to board " + srts.def.LabelCap + ": " + (SRTSMod.GetStatFor<int>(srts.def.defName, StatName.maxPassengers));
        }

        public static bool NoPawnInSRTS(List<CompTransporter> transporters, List<Pawn> pawns)
        {
            if (transporters.Any(x => x.parent.GetComp<CompLaunchableSRTS>() != null) && !pawns.Any(x => x.IsColonistPlayerControlled))
                return true;
            return false;
        }

        public static bool MinPawnRestrictionsSRTS(List<CompTransporter> transporters, List<Pawn> pawns)
        {
            if (transporters.Any(x => x.parent.GetComp<CompLaunchableSRTS>() != null))
            {
                int minPawns = transporters.Min(x => SRTSMod.GetStatFor<int>(x.parent.def.defName, StatName.minPassengers));
                if (pawns.Where(x => x.IsColonistPlayerControlled).Count() < minPawns)
                {
                    return true;
                }

            }
            return false;
        }
        public static bool MaxPawnRestrictionsSRTS(List<CompTransporter> transporters, List<Pawn> pawns)
        {
            if (transporters.Any(x => x.parent.GetComp<CompLaunchableSRTS>() != null))
            {
                int maxPawns = transporters.Max(x => SRTSMod.GetStatFor<int>(x.parent.def.defName, StatName.maxPassengers));
                if (pawns.Count > maxPawns)
                {
                    return true;
                }
            }
            return false;
        }
        /* ======================================================================================================================= */

        public static void PopulateDictionary()
        {
            srtsDefProjects = new Dictionary<ThingDef, ResearchProjectDef>();
            List<ThingDef> defs = DefDatabase<ThingDef>.AllDefsListForReading.Where(x => x?.researchPrerequisites?.Count > 0 && x.researchPrerequisites?[0].tab.ToString() == "SRTSE").ToList();
            foreach (ThingDef def in defs)
            {
                srtsDefProjects.Add(def, def.researchPrerequisites[0]);
            }
        }

        public static void PopulateAllowedBombs()
        {
            if (CEModLoaded)
            {
                List<ThingDef> CEthings = DefDatabase<ThingDef>.AllDefsListForReading.FindAll(x => x.HasComp(Type.GetType("CombatExtended.CompExplosiveCE,CombatExtended")));
                if (SRTSMod.mod.settings.allowedBombs is null)
                    SRTSMod.mod.settings.allowedBombs = new List<string>();
                if (SRTSMod.mod.settings.disallowedBombs is null)
                    SRTSMod.mod.settings.disallowedBombs = new List<string>();
                foreach (ThingDef td in CEthings)
                {
                    if (!SRTSMod.mod.settings.allowedBombs.Contains(td.defName) && !SRTSMod.mod.settings.disallowedBombs.Contains(td.defName))
                    {
                        SRTSMod.mod.settings.allowedBombs.Add(td.defName);
                    }
                }
                return;
            }

            List<ThingDef> things = DefDatabase<ThingDef>.AllDefsListForReading.FindAll(x => x.GetCompProperties<CompProperties_Explosive>() != null && x.projectileWhenLoaded != null);
            if (SRTSMod.mod.settings.allowedBombs is null)
                SRTSMod.mod.settings.allowedBombs = new List<string>();
            if (SRTSMod.mod.settings.disallowedBombs is null)
                SRTSMod.mod.settings.disallowedBombs = new List<string>();
            foreach (ThingDef td in things)
            {
                if (!SRTSMod.mod.settings.allowedBombs.Contains(td.defName) && !SRTSMod.mod.settings.disallowedBombs.Contains(td.defName))
                {
                    SRTSMod.mod.settings.allowedBombs.Add(td.defName);
                }
            }
        }
        
        public static Dictionary<ThingDef, ResearchProjectDef> srtsDefProjects = new Dictionary<ThingDef, ResearchProjectDef>();

        public static bool CEModLoaded = false;
        public static BombingTargeter targeter = new BombingTargeter();
        public static Type CompProperties_ExplosiveCE { get; set; }
        public static Type CompExplosiveCE { get; set; }


        public static bool SOS2ModLoaded = false;
        public static WorldObjectDef SpaceSite { get; set; }
        public static Type SpaceSiteType { get; set; }
        public static Type SOS2LaunchableType { get; set; }
    }
}
