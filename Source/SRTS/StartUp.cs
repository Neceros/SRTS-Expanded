using Harmony;
using System;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using Verse;
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
        }
        /*Smash Phil Addition : Disallow launching ship without at least 1 Pawn */
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
    }
}
