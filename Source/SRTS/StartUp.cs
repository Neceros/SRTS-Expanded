using Harmony;
using System;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using Verse;
using UnityEngine;
using RimWorld;
using RimWorld.Planet;
using OpCodes = System.Reflection.Emit.OpCodes;

namespace SRTS
{
    [StaticConstructorOnStartup]
    public static class StartUp
    {
        static StartUp()
        {
            var harmony = HarmonyInstance.Create("SRTSExpanded");
            harmony.PatchAll(Assembly.GetExecutingAssembly());

            /* Smash Phil Addition */

            //Mechanics and Rendering 
            harmony.Patch(original: AccessTools.Method(type: typeof(CompTransporter), name: nameof(CompTransporter.CompGetGizmosExtra)), prefix: null,
                postfix: new HarmonyMethod(type: typeof(StartUp),
                name: nameof(NoLaunchGroupForSRTS)));
            harmony.Patch(original: AccessTools.Method(type: typeof(Skyfaller), name: nameof(Skyfaller.DrawAt)), prefix: null, postfix: null,
                transpiler: new HarmonyMethod(type: typeof(StartUp),
                name: nameof(RotateSRTSLeavingTranspiler)));
            harmony.Patch(original: AccessTools.Method(type: typeof(SettlementBase_TraderTracker), name: nameof(SettlementBase_TraderTracker.GiveSoldThingToPlayer)), prefix: null, postfix: null,
                transpiler: new HarmonyMethod(type: typeof(StartUp),
                name: nameof(GiveSoldThingsToSRTSTranspiler)));

            //Bomb Runs
            harmony.Patch(original: AccessTools.Method(type: typeof(TransportPodsArrivalActionUtility), name: nameof(TransportPodsArrivalActionUtility.DropTravelingTransportPods)),
                prefix: new HarmonyMethod(type: typeof(StartUp),
                name: nameof(DropSRTSExactSpot)));

            //Custom Settings
            harmony.Patch(original: AccessTools.Property(type: typeof(TravelingTransportPods), name: "TraveledPctStepPerTick").GetGetMethod(nonPublic: true),
                prefix: new HarmonyMethod(type: typeof(StartUp),
                name: nameof(CustomTravelSpeedSRTS)));
            harmony.Patch(original: AccessTools.Property(type: typeof(Dialog_LoadTransporters), name: "MassCapacity").GetGetMethod(nonPublic: true),
                prefix: new HarmonyMethod(type: typeof(StartUp),
                name: nameof(CustomSRTSMassCapacity)));
            harmony.Patch(original: AccessTools.Property(type: typeof(Dialog_Trade), name: "MassUsage").GetGetMethod(nonPublic: true), prefix: null, postfix: null,
                transpiler: new HarmonyMethod(type: typeof(StartUp),
                name: nameof(SRTSMassUsageCaravanTranspiler)));
            harmony.Patch(original: AccessTools.Method(type: typeof(CollectionsMassCalculator), name: nameof(CollectionsMassCalculator.CapacityLeftAfterTradeableTransfer)),
                prefix: new HarmonyMethod(type: typeof(StartUp),
                name: nameof(SRTSMassCapacityCaravan)));
            harmony.Patch(original: AccessTools.Method(type: typeof(Dialog_LoadTransporters), name: "AddItemsToTransferables"), prefix: null, postfix: null,
                transpiler: new HarmonyMethod(type: typeof(StartUp),
                name: nameof(AddItemsEntireMapNonHomeTranspiler)));
            harmony.Patch(original: AccessTools.Method(type: typeof(Dialog_LoadTransporters), name: "CheckForErrors"), prefix: null, postfix: null,
                transpiler: new HarmonyMethod(type: typeof(StartUp),
                name: nameof(ErrorOnNoPawnsTranspiler)));
            harmony.Patch(original: AccessTools.Property(type: typeof(ResearchProjectDef), name: nameof(ResearchProjectDef.CostApparent)).GetGetMethod(),
                prefix: new HarmonyMethod(type: typeof(StartUp),
                name: nameof(ResearchCostApparent)));
            harmony.Patch(original: AccessTools.Property(type: typeof(ResearchProjectDef), name: nameof(ResearchProjectDef.IsFinished)).GetGetMethod(),
                prefix: new HarmonyMethod(type: typeof(StartUp),
                name: nameof(ResearchIsFinished)));
            harmony.Patch(original: AccessTools.Property(type: typeof(ResearchProjectDef), name: nameof(ResearchProjectDef.ProgressPercent)).GetGetMethod(),
                prefix: new HarmonyMethod(type: typeof(StartUp),
                name: nameof(ResearchProgressPercent)));
            harmony.Patch(original: AccessTools.Method(type: typeof(ResearchManager), name: nameof(ResearchManager.FinishProject)), prefix: null, postfix: null,
                transpiler: new HarmonyMethod(type: typeof(StartUp),
                name: nameof(ResearchFinishProjectTranspiler)));
            harmony.Patch(original: AccessTools.Method(type: typeof(MainTabWindow_Research), name: "DrawLeftRect"), prefix: null, postfix: null,
                transpiler: new HarmonyMethod(type: typeof(StartUp),
                name: nameof(ResearchTranslatedCostTranspiler)));
            harmony.Patch(original: AccessTools.Method(type: typeof(ResearchManager), name: nameof(ResearchManager.DebugSetAllProjectsFinished)), prefix: null,
                postfix: new HarmonyMethod(type: typeof(StartUp),
                name: nameof(ResearchFinishAllSRTS)));
            harmony.Patch(original: AccessTools.Property(type: typeof(ResearchProjectDef), name: nameof(ResearchProjectDef.PrerequisitesCompleted)).GetGetMethod(), prefix: null,
                postfix: new HarmonyMethod(type: typeof(StartUp),
                name: nameof(CustomPrerequisitesCompleted)));
            harmony.Patch(original: AccessTools.Method(type: typeof(MainTabWindow_Research), name: "DrawRightRect"), prefix: null, postfix: null,
                transpiler: new HarmonyMethod(type: typeof(StartUp),
                name: nameof(DrawCustomResearchTranspiler)));
            harmony.Patch(original: AccessTools.Method(type: typeof(MainTabWindow_Research), name: "DrawResearchPrereqs"), prefix: null,
               postfix: new HarmonyMethod(type: typeof(StartUp),
               name: nameof(DrawCustomResearchPrereqs)));
        }
        public static IEnumerable<CodeInstruction> ErrorOnNoPawnsTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator ilg)
        {
            List<CodeInstruction> instructionsList = instructions.ToList();

            for (int i = 0; i < instructionsList.Count; i++)
            {
                CodeInstruction instruction = instructionsList[i];

                if(instruction.opcode == OpCodes.Ldc_I4_1 && instructionsList[i+1].opcode == OpCodes.Ret && SRTSMod.mod.settings.passengerLimits)
                {
                    Label label = ilg.DefineLabel();
                    Label label2 = ilg.DefineLabel();
                    Label brlabelMin = ilg.DefineLabel();
                    Label brlabelMax = ilg.DefineLabel();

                    yield return new CodeInstruction(opcode: OpCodes.Ldarg_0);
                    yield return new CodeInstruction(opcode: OpCodes.Ldfld, operand: AccessTools.Field(type: typeof(Dialog_LoadTransporters), name: "transporters"));
                    yield return new CodeInstruction(opcode: OpCodes.Ldarg_1);
                    yield return new CodeInstruction(opcode: OpCodes.Call, operand: AccessTools.Method(type: typeof(StartUp), name: nameof(StartUp.NoPawnInSRTS)));
                    yield return new CodeInstruction(opcode: OpCodes.Brfalse_S, label);

                    yield return new CodeInstruction(opcode: OpCodes.Ldstr, operand: "SRTSNoPilot");
                    yield return new CodeInstruction(opcode: OpCodes.Call, operand: AccessTools.Method(type: typeof(Translator), parameters: new Type[] { typeof(string) }, name: nameof(Translator.Translate)));
                    yield return new CodeInstruction(opcode: OpCodes.Ldsfld, operand: AccessTools.Field(type: typeof(MessageTypeDefOf), name: nameof(MessageTypeDefOf.RejectInput)));
                    yield return new CodeInstruction(opcode: OpCodes.Ldc_I4_0);
                    yield return new CodeInstruction(opcode: OpCodes.Call, operand: AccessTools.Method(type: typeof(Messages), parameters: new Type[] { typeof(string), typeof(MessageTypeDef), typeof(bool) },
                        name: nameof(Messages.Message)));

                    yield return new CodeInstruction(opcode: OpCodes.Ldc_I4_0);
                    yield return new CodeInstruction(opcode: OpCodes.Ret);

                    yield return new CodeInstruction(opcode: OpCodes.Ldarg_0) { labels = new List<Label> { label } };
                    yield return new CodeInstruction(opcode: OpCodes.Ldfld, operand: AccessTools.Field(type: typeof(Dialog_LoadTransporters), name: "transporters"));
                    yield return new CodeInstruction(opcode: OpCodes.Ldarg_1);
                    yield return new CodeInstruction(opcode: OpCodes.Call, operand: AccessTools.Method(type: typeof(StartUp), name: nameof(StartUp.MinPawnRestrictionsSRTS)));
                    yield return new CodeInstruction(opcode: OpCodes.Brtrue, brlabelMin);

                    yield return new CodeInstruction(opcode: OpCodes.Ldarg_0);
                    yield return new CodeInstruction(opcode: OpCodes.Ldfld, operand: AccessTools.Field(type: typeof(Dialog_LoadTransporters), name: "transporters"));
                    yield return new CodeInstruction(opcode: OpCodes.Ldarg_1);
                    yield return new CodeInstruction(opcode: OpCodes.Call, operand: AccessTools.Method(type: typeof(StartUp), name: nameof(StartUp.MaxPawnRestrictionsSRTS)));
                    yield return new CodeInstruction(opcode: OpCodes.Brtrue, brlabelMax);

                    yield return new CodeInstruction(opcode: OpCodes.Br, label2);

                    yield return new CodeInstruction(opcode: OpCodes.Ldarg_0) { labels = new List<Label> { brlabelMin } };
                    yield return new CodeInstruction(opcode: OpCodes.Ldfld, operand: AccessTools.Field(type: typeof(Dialog_LoadTransporters), name: "transporters"));
                    yield return new CodeInstruction(opcode: OpCodes.Ldc_I4_1);
                    yield return new CodeInstruction(opcode: OpCodes.Call, operand: AccessTools.Method(type: typeof(StartUp), name: nameof(StartUp.MinMaxString)));
                    yield return new CodeInstruction(opcode: OpCodes.Ldsfld, operand: AccessTools.Field(type: typeof(MessageTypeDefOf), name: nameof(MessageTypeDefOf.RejectInput)));
                    yield return new CodeInstruction(opcode: OpCodes.Ldc_I4_0);
                    yield return new CodeInstruction(opcode: OpCodes.Call, operand: AccessTools.Method(type: typeof(Messages), parameters: new Type[] { typeof(string), typeof(MessageTypeDef), typeof(bool) },
                        name: nameof(Messages.Message)));
                    yield return new CodeInstruction(opcode: OpCodes.Ldc_I4_0);
                    yield return new CodeInstruction(opcode: OpCodes.Ret);

                    yield return new CodeInstruction(opcode: OpCodes.Ldarg_0) { labels = new List<Label> { brlabelMax } };
                    yield return new CodeInstruction(opcode: OpCodes.Ldfld, operand: AccessTools.Field(type: typeof(Dialog_LoadTransporters), name: "transporters"));
                    yield return new CodeInstruction(opcode: OpCodes.Ldc_I4_0);
                    yield return new CodeInstruction(opcode: OpCodes.Call, operand: AccessTools.Method(type: typeof(StartUp), name: nameof(StartUp.MinMaxString)));
                    yield return new CodeInstruction(opcode: OpCodes.Ldsfld, operand: AccessTools.Field(type: typeof(MessageTypeDefOf), name: nameof(MessageTypeDefOf.RejectInput)));
                    yield return new CodeInstruction(opcode: OpCodes.Ldc_I4_0);
                    yield return new CodeInstruction(opcode: OpCodes.Call, operand: AccessTools.Method(type: typeof(Messages), parameters: new Type[] { typeof(string), typeof(MessageTypeDef), typeof(bool) },
                        name: nameof(Messages.Message)));
                    yield return new CodeInstruction(opcode: OpCodes.Ldc_I4_0);
                    yield return new CodeInstruction(opcode: OpCodes.Ret);

                    instruction.labels.Add(label2);
                }

                yield return instruction;
            }
        }

        public static string MinMaxString(List<CompTransporter> transporters, bool min)
        {
            var srts = transporters.First(x => x.parent.GetComp<CompLaunchableSRTS>() != null).parent;
            return min ? "Minimum Required Pawns for " + srts.def.LabelCap + ": " + (SRTSMod.GetStatFor<int>(srts.def.defName, StatName.minPassengers)) :
                "Maximum Pawns able to board " + srts.def.LabelCap + ": " + (SRTSMod.GetStatFor<int>(srts.def.defName, StatName.maxPassengers));
        }

        public static bool NoPawnInSRTS(List<CompTransporter> transporters, List<Pawn> pawns)
        {
            if(transporters.Any(x => x.parent.GetComp<CompLaunchableSRTS>() != null) && !pawns.Any(x => x.IsColonistPlayerControlled))
                return true;
            return false;
        }

        public static bool MinPawnRestrictionsSRTS(List<CompTransporter> transporters, List<Pawn> pawns)
        {
            if(transporters.Any(x => x.parent.GetComp<CompLaunchableSRTS>() != null))
            {
                int minPawns = transporters.Min(x => SRTSMod.GetStatFor<int>(x.parent.def.defName, StatName.minPassengers));
                if(pawns.Where(x => x.IsColonistPlayerControlled).Count() < minPawns)
                {
                    return true;
                }
                
            }
            return false;
        }
        public static bool MaxPawnRestrictionsSRTS(List<CompTransporter> transporters, List<Pawn> pawns)
        {
            if(transporters.Any(x => x.parent.GetComp<CompLaunchableSRTS>() != null))
            {
                int maxPawns = transporters.Max(x => SRTSMod.GetStatFor<int>(x.parent.def.defName, StatName.maxPassengers));
                if (pawns.Count > maxPawns)
                {
                    return true;
                }
            }
            return false;
        }

        public static IEnumerable<CodeInstruction> AddItemsEntireMapNonHomeTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator ilg)
        {
            List<CodeInstruction> instructionList = instructions.ToList();

            for (int i = 0; i < instructionList.Count; i++)
            {
                CodeInstruction instruction = instructionList[i];

                if(SRTSMod.mod.settings.displayHomeItems && instruction.opcode == OpCodes.Call && instruction.operand == AccessTools.Method(type: typeof(CaravanFormingUtility), name: nameof(CaravanFormingUtility.AllReachableColonyItems)))
                {
                    Label label = ilg.DefineLabel();

                    yield return new CodeInstruction(opcode: OpCodes.Ldarg_0);
                    yield return new CodeInstruction(opcode: OpCodes.Ldfld, operand: AccessTools.Field(type: typeof(Dialog_LoadTransporters), name: "transporters"));
                    yield return new CodeInstruction(opcode: OpCodes.Call, operand: AccessTools.Method(type: typeof(StartUp), name: nameof(StartUp.SRTSLauncherSelected)));
                    yield return new CodeInstruction(opcode: OpCodes.Brfalse, label);

                    yield return new CodeInstruction(opcode: OpCodes.Ldarg_0);
                    yield return new CodeInstruction(opcode: OpCodes.Ldfld, operand: AccessTools.Field(type: typeof(Dialog_LoadTransporters), name: "map"));
                    yield return new CodeInstruction(opcode: OpCodes.Call, operand: AccessTools.Method(type: typeof(StartUp), name: nameof(StartUp.SRTSNonPlayerHomeMap)));
                    yield return new CodeInstruction(opcode: OpCodes.Brfalse, label);

                    yield return new CodeInstruction(opcode: OpCodes.Pop);
                    yield return new CodeInstruction(opcode: OpCodes.Pop);
                    yield return new CodeInstruction(opcode: OpCodes.Pop);

                    yield return new CodeInstruction(opcode: OpCodes.Ldc_I4_1);
                    yield return new CodeInstruction(opcode: OpCodes.Ldc_I4_1);
                    yield return new CodeInstruction(opcode: OpCodes.Ldc_I4_0);

                    instruction.labels.Add(label);
                }

                yield return instruction;
            }
        }

        public static bool SRTSLauncherSelected(List<CompTransporter> transporters)
        {
            if(transporters.Any(x => x.parent.GetComp<CompLaunchableSRTS>() != null))
            {
                return true;
            }
            return false;
        }

        public static bool SRTSNonPlayerHomeMap(Map map)
        {
            return !map.IsPlayerHome;
        }

        public static IEnumerable<CodeInstruction> GiveSoldThingsToSRTSTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator ilg)
        {
            List<CodeInstruction> instructionList = instructions.ToList();

            for (int i = 0; i < instructionList.Count; i++)
            {
                CodeInstruction instruction = instructionList[i];

                if(instruction.opcode == OpCodes.Stloc_2)
                {
                    yield return instruction;
                    instruction = instructionList[++i];

                    Label label = ilg.DefineLabel();
                    yield return new CodeInstruction(opcode: OpCodes.Ldloc_0);
                    yield return new CodeInstruction(opcode: OpCodes.Ldloc_1);
                    yield return new CodeInstruction(opcode: OpCodes.Call, operand: AccessTools.Method(type: typeof(StartUp), name: nameof(StartUp.AddToSRTSFromCaravan)));
                    instruction.labels.Add(label);
                }
                yield return instruction;
            }
        }

        public static void AddToSRTSFromCaravan(Caravan caravan, Thing thing)
        {
            if(caravan.AllThings.Any(x => x.TryGetComp<CompLaunchableSRTS>() != null))
                caravan.AllThings.First(x => x.TryGetComp<CompLaunchableSRTS>() != null).TryGetComp<CompLaunchableSRTS>()?.AddThingsToSRTS(thing);
        }

        public static IEnumerable<CodeInstruction> RotateSRTSLeavingTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> instructionList = instructions.ToList();

            for(int i = 0; i < instructionList.Count; i++)
            {
                CodeInstruction instruction = instructionList[i];

                if(instruction.opcode == OpCodes.Stloc_0)
                {
                    yield return instruction;
                    instruction = instructionList[++i];

                    yield return new CodeInstruction(opcode: OpCodes.Ldloc_0);
                    yield return new CodeInstruction(opcode: OpCodes.Ldarg_0);
                    yield return new CodeInstruction(opcode: OpCodes.Ldflda, operand: AccessTools.Field(type: typeof(Skyfaller), name: nameof(Skyfaller.innerContainer)));
                    yield return new CodeInstruction(opcode: OpCodes.Call, operand: AccessTools.Method(type: typeof(StartUp), name: nameof(StartUp.RotateSRTS)));
                    yield return new CodeInstruction(opcode: OpCodes.Stloc_0);
                }
                yield return instruction;
            }
        }

        public static Thing RotateSRTS(Thing t, ref ThingOwner ic)
        {
            string[] nameSplit = t.Label.Split(' ');
            if(nameSplit[0] == "Superpod")
                return t;

            t.Rotation = (t as SRTSLeaving) is null ? ( (t as SRTSIncoming) is null ? Rot4.North : Rot4.East) : Rot4.West;
            ic[0].Rotation = t.Rotation;
            return t;
        }

        private static bool CustomTravelSpeedSRTS(int ___initialTile, int ___destinationTile, List<ActiveDropPodInfo> ___pods, ref float __result)
        {
            if (___pods.Any(x => x.innerContainer.Any(y => y.TryGetComp<CompLaunchableSRTS>() != null)))
            {
                Vector3 start = Find.WorldGrid.GetTileCenter(___initialTile);
                Vector3 end = Find.WorldGrid.GetTileCenter(___destinationTile);

                if (start == end)
                {
                    __result = 1f;
                    return false;
                }
                float num = GenMath.SphericalDistance(start.normalized, end.normalized) * 100000;
                if(num == 0f)
                {
                    __result = 1f;
                    return false;
                }
                Thing ship = ___pods.Find(x => x.innerContainer.First(y => y.TryGetComp<CompLaunchableSRTS>() != null) != null).innerContainer.First(z => z.TryGetComp<CompLaunchableSRTS>() != null);
                __result = SRTSMod.GetStatFor<float>(ship.def.defName, StatName.flightSpeed) / num;
                return false;
            }
            return true;
        }

        public static bool SRTSMassCapacityCaravan(List<Thing> allCurrentThings, List<Tradeable> tradeables, StringBuilder explanation, ref float __result)
        {
            Thing ship = null;
            if(TradeSession.playerNegotiator.GetCaravan().AllThings.Any(x => x.TryGetComp<CompLaunchableSRTS>() != null))
            {
                ship = TradeSession.playerNegotiator.GetCaravan().AllThings.First(x => x.TryGetComp<CompLaunchableSRTS>() != null);
            }
            if (ship != null)
            {
                __result = ship.def.GetCompProperties<CompProperties_Transporter>().massCapacity;
                return false;
            }
            return true;
        }

        public static IEnumerable<CodeInstruction> SRTSMassUsageCaravanTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator ilg)
        {
            List<CodeInstruction> instructionList = instructions.ToList();

            for(int i = 0; i < instructionList.Count; i++)
            {
                CodeInstruction instruction = instructionList[i];

                if(instruction.opcode == OpCodes.Ldc_I4_0 && instructionList[i+1].opcode == OpCodes.Ldc_I4_0 && instructionList[i+2].opcode == OpCodes.Ldc_I4_0)
                {
                    
                    yield return instruction;
                    instruction = instructionList[++i];

                    Label label = ilg.DefineLabel();
                    yield return new CodeInstruction(opcode: OpCodes.Call, operand: AccessTools.Property(type: typeof(StartUp), name: nameof(StartUp.SRTSInCaravan)).GetGetMethod());
                    continue;
                }

                yield return instruction;
            }
        }

        public static void NoLaunchGroupForSRTS(ref IEnumerable<Gizmo> __result, CompTransporter __instance)
        {
            if(__instance.parent.def.GetCompProperties<CompProperties_LaunchableSRTS>() != null)
            {
                List<Gizmo> gizmos = __result.ToList();
                for(int i = gizmos.Count - 1; i >= 0; i--)
                {
                    if( (gizmos[i] as Command_Action)?.defaultLabel == "CommandSelectPreviousTransporter".Translate() || (gizmos[i] as Command_Action)?.defaultLabel == "CommandSelectAllTransporters".Translate() ||
                        (gizmos[i] as Command_Action)?.defaultLabel == "CommandSelectNextTransporter".Translate())
                    {
                        gizmos.Remove(gizmos[i]);
                    }
                }
                __result = gizmos;
            }
        }

        public static bool DropSRTSExactSpot(List<ActiveDropPodInfo> dropPods, IntVec3 near, Map map)
        {
            foreach(ActiveDropPodInfo pod in dropPods)
            {
                foreach(Thing t in pod.innerContainer)
                {
                    if(ThingDef.Named(t.def.defName.Split('_')[0])?.GetCompProperties<CompProperties_LaunchableSRTS>() != null)
                    {
                        TransportPodsArrivalActionUtility.RemovePawnsFromWorldPawns(dropPods);
                        foreach(ActiveDropPodInfo pod2 in dropPods)
                        {
                            DropPodUtility.MakeDropPodAt(near, map, pod2);
                        }
                        return false;
                    }
                }
            }
            return true;
        }

        public static bool CustomSRTSMassCapacity(ref float __result, List<CompTransporter> ___transporters)
        {
            if(___transporters.Any(x => x.parent.TryGetComp<CompLaunchableSRTS>() != null))
            {
                float num = 0f;
                foreach(CompTransporter comp in ___transporters)
                {
                    num += SRTSMod.GetStatFor<float>(comp.parent.def.defName, StatName.massCapacity);   
                }
                __result = num;
                return false;
            }
            return true;
        }

        public static bool ResearchCostApparent(ResearchProjectDef __instance, ref float __result)
        {
            if(srtsDefProjects.Any(x => x.Value == __instance))
            {
                __result = GetResearchStat(__instance) * __instance.CostFactor(Faction.OfPlayer.def.techLevel);
                return false;
            }
            return true;
        }

        public static bool ResearchIsFinished(ResearchProjectDef __instance, ref bool __result)
        {
            if(srtsDefProjects.Any(x => x.Value == __instance))
            {
                __result = __instance.ProgressReal >= GetResearchStat(__instance);
                return false;
            }
            return true;
        }

        public static bool ResearchProgressPercent(ResearchProjectDef __instance, ref float __result)
        {
            if(srtsDefProjects.Any(x => x.Value == __instance))
            {
                __result = Find.ResearchManager.GetProgress(__instance) / GetResearchStat(__instance);
                return false;
            }
            return true;
        }

        public static IEnumerable<CodeInstruction> ResearchFinishProjectTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator ilg)
        {
            List<CodeInstruction> instructionList = instructions.ToList();

            Label prerequisitesLabel = ilg.DefineLabel();
            yield return new CodeInstruction(opcode: OpCodes.Ldarg_1);
            yield return new CodeInstruction(opcode: OpCodes.Call, operand: AccessTools.Method(type: typeof(StartUp), name: nameof(StartUp.ContainedInDefProjects)));
            yield return new CodeInstruction(opcode: OpCodes.Brfalse, prerequisitesLabel);

            yield return new CodeInstruction(opcode: OpCodes.Ldarg_1);
            yield return new CodeInstruction(opcode: OpCodes.Ldarg_0);
            yield return new CodeInstruction(opcode: OpCodes.Call, operand: AccessTools.Method(type: typeof(StartUp), name: nameof(StartUp.FinishCustomPrerequisites)));

            yield return new CodeInstruction(opcode: OpCodes.Nop) { labels = new List<Label> { prerequisitesLabel } };

            for(int i = 0; i < instructionList.Count; i++)
            {
                CodeInstruction instruction = instructionList[i];

                if (instruction.opcode == OpCodes.Ldfld && instruction.operand == AccessTools.Field(type: typeof(ResearchManager), name: "progress"))
                {
                    yield return instruction;
                    instruction = instructionList[++i];

                    Label label = ilg.DefineLabel();
                    Label brlabel = ilg.DefineLabel();

                    yield return new CodeInstruction(opcode: OpCodes.Ldarg_1);
                    yield return new CodeInstruction(opcode: OpCodes.Call, operand: AccessTools.Method(type: typeof(StartUp), name: nameof(StartUp.ContainedInDefProjects)));
                    yield return new CodeInstruction(opcode: OpCodes.Brfalse, label);

                    yield return new CodeInstruction(opcode: OpCodes.Ldarg_1);
                    yield return new CodeInstruction(opcode: OpCodes.Ldarg_1);
                    yield return new CodeInstruction(opcode: OpCodes.Call, operand: AccessTools.Method(type: typeof(StartUp), name: nameof(StartUp.GetResearchStat)));
                    yield return new CodeInstruction(opcode: OpCodes.Br, brlabel);

                    int x = i;
                    while (x < instructionList.Count)
                    {
                        if (instructionList[x].opcode == OpCodes.Callvirt)
                        {
                            instructionList[x].labels.Add(brlabel);
                            break;
                        }
                        x++;
                    }

                    instruction.labels.Add(label);
                }
                yield return instruction;
            }
        }

        public static IEnumerable<CodeInstruction> ResearchTranslatedCostTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator ilg)
        {
            List<CodeInstruction> instructionList = instructions.ToList();
            for (int i = 0; i < instructionList.Count; i++)
            {
                CodeInstruction instruction = instructionList[i];

                if (instruction.opcode == OpCodes.Ldflda && instruction.operand == AccessTools.Field(type: typeof(ResearchProjectDef), name: "baseCost"))
                {
                    Label label = ilg.DefineLabel();
                    Label brlabel = ilg.DefineLabel();

                    yield return new CodeInstruction(opcode: OpCodes.Ldarg_0);
                    yield return new CodeInstruction(opcode: OpCodes.Ldfld, operand: AccessTools.Field(type: typeof(MainTabWindow_Research), name: "selectedProject"));
                    yield return new CodeInstruction(opcode: OpCodes.Call, operand: AccessTools.Method(type: typeof(StartUp), name: nameof(StartUp.ContainedInDefProjects)));
                    yield return new CodeInstruction(opcode: OpCodes.Brfalse, label);

                    yield return new CodeInstruction(opcode: OpCodes.Call, operand: AccessTools.Method(type: typeof(StartUp), name: nameof(StartUp.GetResearchStatString)));
                    yield return new CodeInstruction(opcode: OpCodes.Br, brlabel);

                    instruction.labels.Add(label);
                    instructionList[i + 4].labels.Add(brlabel);
                }
                yield return instruction;
            }
        }

        public static void ResearchFinishAllSRTS(ResearchManager __instance, ref Dictionary<ResearchProjectDef, float> ___progress)
        {
            foreach(ResearchProjectDef researchProjectDef in srtsDefProjects.Values)
            {
                ___progress[researchProjectDef] = GetResearchStat(researchProjectDef);
            }
            __instance.ReapplyAllMods();
        }

        public static void CustomPrerequisitesCompleted(ResearchProjectDef __instance, ref bool __result, List<ResearchProjectDef> ___prerequisites)
        {
            if(ContainedInDefProjects(__instance) && ___prerequisites != null && __result is true)
            {
                List<ResearchProjectDef> projects = SRTSMod.mod.settings.defProperties[srtsDefProjects.FirstOrDefault(x => x.Value == __instance).Key.defName].ResearchPrerequisites;
                foreach(ResearchProjectDef proj in projects)
                {
                    if (!proj.IsFinished)
                    {
                        __result = false;
                    }
                }
            }
        }

        public static IEnumerable<CodeInstruction> DrawCustomResearchTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator ilg)
        {
            List<CodeInstruction> instructionList = instructions.ToList();
            bool flag = true;
            for (int i = 0; i < instructionList.Count; i++)
            {
                CodeInstruction instruction = instructionList[i];

                if(instruction.opcode == OpCodes.Call && instruction.operand == AccessTools.Method(type: typeof(MainTabWindow_Research), name: "PosX") && flag)
                {
                    flag = false;
                    Label label = ilg.DefineLabel();

                    yield return new CodeInstruction(opcode: OpCodes.Ldloc_S, 19);
                    yield return new CodeInstruction(opcode: OpCodes.Call, operand: AccessTools.Method(type: typeof(StartUp), name: nameof(StartUp.ContainedInDefProjects)));
                    yield return new CodeInstruction(opcode: OpCodes.Brfalse, label);

                    yield return new CodeInstruction(opcode: OpCodes.Ldloc_S, 19);
                    yield return new CodeInstruction(opcode: OpCodes.Ldarg_0);
                    yield return new CodeInstruction(opcode: OpCodes.Call, operand: AccessTools.Property(type: typeof(MainTabWindow_Research), name: "CurTab").GetGetMethod(nonPublic: true));
                    yield return new CodeInstruction(opcode: OpCodes.Ldloc_S, 14);
                    yield return new CodeInstruction(opcode: OpCodes.Ldloc_S, 15);
                    yield return new CodeInstruction(opcode: OpCodes.Ldarg_0);
                    yield return new CodeInstruction(opcode: OpCodes.Ldfld, operand: AccessTools.Field(type: typeof(MainTabWindow_Research), name: "selectedProject"));
                    yield return new CodeInstruction(opcode: OpCodes.Ldloc_S, 17);
                    yield return new CodeInstruction(opcode: OpCodes.Call, operand: AccessTools.Method(type: typeof(StartUp), name: nameof(StartUp.DrawLinesCustomPrerequisites)));
                    
                    instruction.labels.Add(label);
                }
                yield return instruction;
            }
        }

        public static void DrawCustomResearchPrereqs(ResearchProjectDef project, Rect rect, ref float __result)
        {
            if(ContainedInDefProjects(project))
            {
                List<ResearchProjectDef> projects = SRTSMod.mod.settings.defProperties[srtsDefProjects.FirstOrDefault(x => x.Value == project).Key.defName].CustomResearch;
                float yMin = rect.yMin;
                rect.yMin += rect.height;
                var oldResult = __result;
                foreach(ResearchProjectDef proj in projects)
                {
                    if(!project.IsFinished)
                    {
                        if (proj.IsFinished)
                            GUI.color = Color.green;
                        else
                            GUI.color = Color.red;
                    }
                    Widgets.LabelCacheHeight(ref rect, "  " + proj.LabelCap, true, false);
                    rect.yMin += rect.height;
                }
                GUI.color = Color.white;
                __result = rect.yMin - yMin + oldResult;
            }
        }

        /*=========================== Helper Methods ===================================*/

        public static float GetResearchStat(ResearchProjectDef project) => SRTSMod.GetStatFor<float>(srtsDefProjects.FirstOrDefault(x => x.Value == project).Key.defName, StatName.researchPoints);
        public static NamedArgument GetResearchStatString(ResearchProjectDef project) => SRTSMod.GetStatFor<float>(srtsDefProjects.FirstOrDefault(x => x.Value == project).Key.defName, StatName.researchPoints).ToString("F0");
        public static bool ContainedInDefProjects(ResearchProjectDef project) => srtsDefProjects.Any(x => x.Value == project);

        public static void FinishCustomPrerequisites(ResearchProjectDef project, ResearchManager instance)
        {
            List<ResearchProjectDef> projects = SRTSMod.mod.settings.defProperties[srtsDefProjects.FirstOrDefault(x => x.Value == project).Key.defName].CustomResearch;
            foreach(ResearchProjectDef proj in projects)
            {
                if(!proj.IsFinished)
                {
                    instance.FinishProject(proj, false, null);
                } 
            }
        }

        public static void DrawLinesCustomPrerequisites(ResearchProjectDef project, ResearchTabDef curTab, Vector2 start, Vector2 end, ResearchProjectDef selectedProject, int i)
        {
            List<ResearchProjectDef> projects = SRTSMod.mod.settings.defProperties[srtsDefProjects.FirstOrDefault(x => x.Value == project).Key.defName].CustomResearch;

            start.x = project.ResearchViewX * 190f + 140f;
            start.y = project.ResearchViewY * 100f + 25f;
            foreach(ResearchProjectDef proj in projects)
            {
                if(proj != null && proj.tab == curTab)
                {
                    end.x = proj.ResearchViewX * 190f;
                    end.y = proj.ResearchViewY * 100f + 25f;
                    if(selectedProject == project || selectedProject == proj)
                    {
                        if(i == 1)
                            Widgets.DrawLine(start, end, TexUI.HighlightLineResearchColor, 4f);
                    }
                    if(i == 0)
                        Widgets.DrawLine(start, end, new Color(255, 215, 0, 0.25f), 2f);
                }
            }
        }

        public static void PopulateDictionary()
        {
            srtsDefProjects = new Dictionary<ThingDef, ResearchProjectDef>();
            List<ThingDef> defs = DefDatabase<ThingDef>.AllDefsListForReading.Where(x => x.researchPrerequisites?[0].tab.ToString() == "SRTSE").ToList();
            foreach (ThingDef def in defs)
            {
                srtsDefProjects.Add(def, def.researchPrerequisites[0]);
            }
        }

        public static bool SRTSInCaravan => TradeSession.playerNegotiator.GetCaravan().AllThings.Any(x => x.TryGetComp<CompLaunchableSRTS>() != null);
        public static Dictionary<int, Pair<Map, IntVec3>> SRTSBombers = new Dictionary<int, Pair<Map, IntVec3>>();
        private static Dictionary<ThingDef, ResearchProjectDef> srtsDefProjects = new Dictionary<ThingDef, ResearchProjectDef>();
    }
}
