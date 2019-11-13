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
            harmony.Patch(original: AccessTools.Method(type: typeof(Dialog_LoadTransporters), name: "CheckForErrors"), prefix: null, postfix: null,
                transpiler: new HarmonyMethod(type: typeof(StartUp),
                name: nameof(ErrorOnNoPawns)));
            harmony.Patch(original: AccessTools.Method(type: typeof(SettlementBase_TraderTracker), name: nameof(SettlementBase_TraderTracker.GiveSoldThingToPlayer)), prefix: null, postfix: null,
                transpiler: new HarmonyMethod(type: typeof(StartUp),
                name: nameof(GiveSoldThingsToSRTS)));
            harmony.Patch(original: AccessTools.Method(type: typeof(Skyfaller), name: nameof(Skyfaller.DrawAt)), prefix: null, postfix: null,
                transpiler: new HarmonyMethod(type: typeof(StartUp),
                name: nameof(RotateSRTSLeaving)));
            harmony.Patch(original: AccessTools.Property(type: typeof(TravelingTransportPods), name: "TraveledPctStepPerTick").GetGetMethod(nonPublic: true),
                prefix: new HarmonyMethod(type: typeof(StartUp),
                name: nameof(CustomTravelSpeedSRTS)));
            harmony.Patch(original: AccessTools.Method(type: typeof(CollectionsMassCalculator), name: nameof(CollectionsMassCalculator.CapacityLeftAfterTradeableTransfer)),
                prefix: new HarmonyMethod(type: typeof(StartUp),
                name: nameof(SRTSMassCapacityCaravan)));
            harmony.Patch(original: AccessTools.Property(type: typeof(Dialog_Trade), name: "MassUsage").GetGetMethod(nonPublic: true), prefix: null, postfix: null,
                transpiler: new HarmonyMethod(type: typeof(StartUp),
                name: nameof(SRTSMassUsageCaravan)));
        }
        public static IEnumerable<CodeInstruction> ErrorOnNoPawns(IEnumerable<CodeInstruction> instructions, ILGenerator ilg)
        {
            List<CodeInstruction> instructionsList = instructions.ToList();

            for (int i = 0; i < instructionsList.Count; i++)
            {
                CodeInstruction instruction = instructionsList[i];

                if(instruction.opcode == OpCodes.Ldc_I4_1 && instructionsList[i+1].opcode == OpCodes.Ret)
                {
                    Label label = ilg.DefineLabel();

                    yield return new CodeInstruction(opcode: OpCodes.Ldarg_1);
                    yield return new CodeInstruction(opcode: OpCodes.Call, operand: AccessTools.Method(type: typeof(StartUp), name: nameof(StartUp.PawnInTransporter)));
                    yield return new CodeInstruction(opcode: OpCodes.Brtrue, label);

                    yield return new CodeInstruction(opcode: OpCodes.Ldstr, operand: "Can't send SRTS without a Pilot");
                    //yield return new CodeInstruction(opcode: OpCodes.Call, operand: AccessTools.Method(type: typeof(Translator), parameters: new Type[] { typeof(string) }, name: nameof(Translator.Translate)));
                    yield return new CodeInstruction(opcode: OpCodes.Ldsfld, operand: AccessTools.Field(type: typeof(MessageTypeDefOf), name: nameof(MessageTypeDefOf.RejectInput)));
                    yield return new CodeInstruction(opcode: OpCodes.Ldc_I4_0);
                    yield return new CodeInstruction(opcode: OpCodes.Call, operand: AccessTools.Method(type: typeof(Messages), parameters: new Type[] { typeof(string), typeof(MessageTypeDef), typeof(bool) },
                        name: nameof(Messages.Message)));

                    yield return new CodeInstruction(opcode: OpCodes.Ldc_I4_0);
                    yield return new CodeInstruction(opcode: OpCodes.Ret);

                    instruction.labels.Add(label);
                }

                yield return instruction;
            }
        }

        public static bool PawnInTransporter(List<Pawn> pawns)
        {
            if (pawns.Any(x => x.IsColonistPlayerControlled))
                return true;
            return false;
        }

        public static IEnumerable<CodeInstruction> GiveSoldThingsToSRTS(IEnumerable<CodeInstruction> instructions, ILGenerator ilg)
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
            caravan.AllThings.First(x => x.TryGetComp<CompLaunchableSRTS>() != null).TryGetComp<CompLaunchableSRTS>()?.AddThingsToSRTS(thing);
        }

        public static IEnumerable<CodeInstruction> RotateSRTSLeaving(IEnumerable<CodeInstruction> instructions)
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
                float num = GenMath.SphericalDistance(start.normalized, end.normalized);
                if(num == 0f)
                {
                    __result = 1f;
                    return false;
                }
                Thing ship = ___pods.Find(x => x.innerContainer.First(y => y.TryGetComp<CompLaunchableSRTS>() != null) != null).innerContainer.First(z => z.TryGetComp<CompLaunchableSRTS>() != null);
                __result = ship.TryGetComp<CompLaunchableSRTS>().TravelSpeed / num;
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

        public static IEnumerable<CodeInstruction> SRTSMassUsageCaravan(IEnumerable<CodeInstruction> instructions, ILGenerator ilg)
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

        public static bool SRTSInCaravan => TradeSession.playerNegotiator.GetCaravan().AllThings.Any(x => x.TryGetComp<CompLaunchableSRTS>() != null);
    }
}
