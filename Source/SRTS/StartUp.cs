using HarmonyLib;
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
      var harmony = new Harmony("SRTSExpanded.smashphil.neceros");
      harmony.PatchAll(Assembly.GetExecutingAssembly());
      //Harmony.DEBUG = true;

      /* Mechanics and Rendering */
      harmony.Patch(original: AccessTools.Method(type: typeof(CompTransporter), name: nameof(CompTransporter.CompGetGizmosExtra)), prefix: null,
          postfix: new HarmonyMethod(typeof(StartUp),
          nameof(NoLaunchGroupForSRTS)));
      harmony.Patch(original: AccessTools.Method(type: typeof(Settlement_TraderTracker), name: nameof(Settlement_TraderTracker.GiveSoldThingToPlayer)), prefix: null, postfix: null,
          transpiler: new HarmonyMethod(typeof(StartUp),
          nameof(GiveSoldThingsToSRTSTranspiler)));
      harmony.Patch(original: AccessTools.Method(type: typeof(WorldDynamicDrawManager), name: nameof(WorldDynamicDrawManager.DrawDynamicWorldObjects)), prefix: null, postfix: null,
          transpiler: new HarmonyMethod(typeof(StartUp),
          nameof(DrawDynamicSRTSObjectsTranspiler)));
      harmony.Patch(original: AccessTools.Method(type: typeof(ExpandableWorldObjectsUtility), name: nameof(ExpandableWorldObjectsUtility.ExpandableWorldObjectsOnGUI)), prefix: null, postfix: null,
          transpiler: new HarmonyMethod(typeof(StartUp),
          nameof(ExpandableIconDetourSRTSTranspiler)));
      harmony.Patch(original: AccessTools.Method(type: typeof(WorldSelector), name: "HandleWorldClicks"), prefix: null,
          postfix: new HarmonyMethod(typeof(StartUp),
          nameof(TravelingSRTSChangeDirection)));

      //Maybe add in the future ...?
      /*harmony.Patch(original: AccessTools.Method(type: typeof(Dialog_Trade), name: "SetupPlayerCaravanVariables"), prefix: null, postfix: null,
          transpiler: new HarmonyMethod(type: typeof(StartUp),
          name: nameof(SRTSAreNotTradeable)));*/

      /* Bomb Runs */
      harmony.Patch(original: AccessTools.Method(type: typeof(TransportPodsArrivalActionUtility), name: nameof(TransportPodsArrivalActionUtility.DropTravelingTransportPods)),
          prefix: new HarmonyMethod(typeof(StartUp),
          nameof(DropSRTSExactSpot)));
      harmony.Patch(original: AccessTools.Method(type: typeof(Targeter), name: nameof(Targeter.TargeterOnGUI)), prefix: null,
          postfix: new HarmonyMethod(typeof(StartUp),
          nameof(DrawBombingTargeter)));
      harmony.Patch(original: AccessTools.Method(type: typeof(Targeter), name: nameof(Targeter.ProcessInputEvents)), prefix: null,
          postfix: new HarmonyMethod(typeof(StartUp),
          nameof(ProcessBombingInputEvents)));
      harmony.Patch(original: AccessTools.Method(type: typeof(Targeter), name: nameof(Targeter.TargeterUpdate)), prefix: null,
          postfix: new HarmonyMethod(typeof(StartUp),
          nameof(BombTargeterUpdate)));

      /* Custom Settings */
      harmony.Patch(original: AccessTools.Property(type: typeof(TravelingTransportPods), name: "TraveledPctStepPerTick").GetGetMethod(nonPublic: true),
          prefix: new HarmonyMethod(typeof(StartUp),
          nameof(CustomTravelSpeedSRTS)));
      harmony.Patch(original: AccessTools.Property(type: typeof(Dialog_LoadTransporters), name: "MassCapacity").GetGetMethod(nonPublic: true),
          prefix: new HarmonyMethod(typeof(StartUp),
          nameof(CustomSRTSMassCapacity)));
      harmony.Patch(original: AccessTools.Property(type: typeof(Dialog_Trade), name: "MassUsage").GetGetMethod(nonPublic: true), prefix: null, postfix: null,
          transpiler: new HarmonyMethod(typeof(StartUp),
          nameof(SRTSMassUsageCaravanTranspiler)));
      harmony.Patch(original: AccessTools.Method(type: typeof(CollectionsMassCalculator), name: nameof(CollectionsMassCalculator.CapacityLeftAfterTradeableTransfer)),
          prefix: new HarmonyMethod(typeof(StartUp),
          nameof(SRTSMassCapacityCaravan)));
      harmony.Patch(original: AccessTools.Method(type: typeof(Dialog_LoadTransporters), name: "AddItemsToTransferables"), prefix: null, postfix: null,
          transpiler: new HarmonyMethod(typeof(StartUp),
          nameof(AddItemsEntireMapNonHomeTranspiler)));

      /*harmony.Patch(original: AccessTools.Method(type: typeof(Dialog_LoadTransporters), name: "CheckForErrors"), prefix: null, postfix: null,
          transpiler: new HarmonyMethod(type: typeof(StartUp),
          name: nameof(ErrorOnNoPawnsTranspiler)));*/
      //harmony.Patch(original: AccessTools.Property(type: typeof(ResearchProjectDef), name: nameof(ResearchProjectDef.CostApparent)).GetGetMethod(),
      //    prefix: new HarmonyMethod(typeof(StartUp),
      //    nameof(ResearchCostApparent)));
      //harmony.Patch(original: AccessTools.Property(type: typeof(ResearchProjectDef), name: nameof(ResearchProjectDef.IsFinished)).GetGetMethod(),
      //    prefix: new HarmonyMethod(typeof(StartUp),
      //    nameof(ResearchIsFinished)));
      //harmony.Patch(original: AccessTools.Property(type: typeof(ResearchProjectDef), name: nameof(ResearchProjectDef.ProgressPercent)).GetGetMethod(),
      //    prefix: new HarmonyMethod(typeof(StartUp),
      //    nameof(ResearchProgressPercent)));
      //harmony.Patch(original: AccessTools.Method(type: typeof(ResearchManager), name: nameof(ResearchManager.FinishProject)), prefix: null, postfix: null,
      //    transpiler: new HarmonyMethod(typeof(StartUp),
      //    nameof(ResearchFinishProjectTranspiler)));
      //harmony.Patch(original: AccessTools.Method(type: typeof(MainTabWindow_Research), name: "DrawLeftRect"), prefix: null, postfix: null,
      //    transpiler: new HarmonyMethod(typeof(StartUp),
      //    nameof(ResearchTranslatedCostTranspiler)));
      //harmony.Patch(original: AccessTools.Method(type: typeof(ResearchManager), name: nameof(ResearchManager.DebugSetAllProjectsFinished)), prefix: null,
      //    postfix: new HarmonyMethod(typeof(StartUp),
      //    nameof(ResearchFinishAllSRTS)));
      //harmony.Patch(original: AccessTools.Property(type: typeof(ResearchProjectDef), name: nameof(ResearchProjectDef.PrerequisitesCompleted)).GetGetMethod(), prefix: null,
      //    postfix: new HarmonyMethod(typeof(StartUp),
      //    nameof(CustomPrerequisitesCompleted)));
      //harmony.Patch(original: AccessTools.Method(type: typeof(MainTabWindow_Research), name: "DrawRightRect"), prefix: null, postfix: null,
      //    transpiler: new HarmonyMethod(typeof(StartUp),
      //    nameof(DrawCustomResearchTranspiler)));
      //harmony.Patch(original: AccessTools.Method(type: typeof(MainTabWindow_Research), name: "DrawResearchPrereqs"), prefix: null,
      //   postfix: new HarmonyMethod(typeof(StartUp),
      //   nameof(DrawCustomResearchPrereqs)));
      harmony.Patch(original: AccessTools.Method(type: typeof(Caravan), name: nameof(Caravan.GetGizmos)), prefix: null,
          postfix: new HarmonyMethod(typeof(StartUp),
          nameof(LaunchAndBombGizmosPassthrough)));


      /* Destructive Patch Fixes */
      bool sos2Flag = false;
      if (ModLister.HasActiveModWithName("Save Our Ship 2"))
      {
        sos2Flag = true;
        Log.Message("[SRTSExpanded] Overriding SOS2 Destructive Patches.");
      }
      harmony.Patch(original: AccessTools.Method(type: typeof(Dialog_LoadTransporters), name: "AddPawnsToTransferables"),
          prefix: sos2Flag ? new HarmonyMethod(typeof(StartUp),
          nameof(CustomOptionsPawnsToTransportOverride)) : null,
          postfix: null,
          transpiler: sos2Flag ? null : new HarmonyMethod(typeof(StartUp),
          nameof(CustomOptionsPawnsToTransportTranspiler)));

      /* Unpatch Save our Ship 2 's destructive and incompetent patch on transporter pawns */
      //harmony.Unpatch(AccessTools.Method(type: typeof(Dialog_LoadTransporters), name: "AddPawnsToTransferables"), HarmonyPatchType.Prefix, "HugsLib.ShipInteriorMod2");
      /*bool flag = harmony.HasAnyPatches("HugsLib.ShipInteriorMod2");
      Log.Message("SoS2: " + flag);*/
    }

    /*public static IEnumerable<CodeInstruction> ErrorOnNoPawnsTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator ilg)
    {
        List<CodeInstruction> instructionsList = instructions.ToList();

        for (int i = 0; i < instructionsList.Count; i++)
        {
            CodeInstruction instruction = instructionsList[i];

            if (instruction.opcode == OpCodes.Ldc_I4_1 && instructionsList[i + 1].opcode == OpCodes.Ret && SRTSMod.mod.settings.passengerLimits)
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
    }*/

    /// <summary>
    /// Insert all items on map (non minifiable) if map is not player home.
    /// </summary>
    /// <param name="instructions"></param>
    /// <param name="ilg"></param>
    /// <returns></returns>
    public static IEnumerable<CodeInstruction> AddItemsEntireMapNonHomeTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator ilg)
    {
      List<CodeInstruction> instructionList = instructions.ToList();

      for (int i = 0; i < instructionList.Count; i++)
      {
        CodeInstruction instruction = instructionList[i];

        if (SRTSMod.mod.settings.displayHomeItems && instruction.opcode == OpCodes.Call && (MethodInfo)instruction.operand == AccessTools.Method(type: typeof(CaravanFormingUtility), name: nameof(CaravanFormingUtility.AllReachableColonyItems)))
        {
          Label label = ilg.DefineLabel();

          ///Check if SRTS is present inside dialog menu transferables
          yield return new CodeInstruction(opcode: OpCodes.Ldarg_0);
          yield return new CodeInstruction(opcode: OpCodes.Ldfld, operand: AccessTools.Field(type: typeof(Dialog_LoadTransporters), name: "transporters"));
          yield return new CodeInstruction(opcode: OpCodes.Call, operand: AccessTools.Method(type: typeof(SRTSHelper), name: nameof(SRTSHelper.SRTSLauncherSelected)));
          yield return new CodeInstruction(opcode: OpCodes.Brfalse, label);

          ///Check if player / SRTS selected is inside non playerhome map
          yield return new CodeInstruction(opcode: OpCodes.Ldarg_0);
          yield return new CodeInstruction(opcode: OpCodes.Ldfld, operand: AccessTools.Field(type: typeof(Dialog_LoadTransporters), name: "map"));
          yield return new CodeInstruction(opcode: OpCodes.Call, operand: AccessTools.Method(type: typeof(SRTSHelper), name: nameof(SRTSHelper.SRTSNonPlayerHomeMap)));
          yield return new CodeInstruction(opcode: OpCodes.Brfalse, label);

          ///Pop top 3 values from stack, which are all false
          yield return new CodeInstruction(opcode: OpCodes.Pop);
          yield return new CodeInstruction(opcode: OpCodes.Pop);
          yield return new CodeInstruction(opcode: OpCodes.Pop);

          ///Push true, true, false onto stack, to change resulting method call parameters
          yield return new CodeInstruction(opcode: OpCodes.Ldc_I4_1);
          yield return new CodeInstruction(opcode: OpCodes.Ldc_I4_1);
          yield return new CodeInstruction(opcode: OpCodes.Ldc_I4_0);

          instruction.labels.Add(label);
        }

        yield return instruction;
      }
    }

    /// <summary>
    /// Add purchased items to list of things contained within SRTS, to drop contents on landing rather than placing inside pawn's inventory.
    /// </summary>
    /// <param name="instructions"></param>
    /// <param name="ilg"></param>
    /// <returns></returns>
    public static IEnumerable<CodeInstruction> GiveSoldThingsToSRTSTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator ilg)
    {
      List<CodeInstruction> instructionList = instructions.ToList();

      for (int i = 0; i < instructionList.Count; i++)
      {
        CodeInstruction instruction = instructionList[i];

        if (instruction.opcode == OpCodes.Stloc_2)
        {
          yield return instruction;
          instruction = instructionList[++i];

          Label label = ilg.DefineLabel();
          yield return new CodeInstruction(opcode: OpCodes.Ldloc_0);
          yield return new CodeInstruction(opcode: OpCodes.Ldloc_1);
          yield return new CodeInstruction(opcode: OpCodes.Call, operand: AccessTools.Method(type: typeof(SRTSHelper), name: nameof(SRTSHelper.AddToSRTSFromCaravan)));
          instruction.labels.Add(label);
        }
        yield return instruction;
      }
    }

    /// <summary>
    /// Draw SRTS textures dynamically to mimic both the flying SRTS texture and its rotation
    /// </summary>
    /// <param name="instructions"></param>
    /// <param name="ilg"></param>
    /// <returns></returns>
    public static IEnumerable<CodeInstruction> DrawDynamicSRTSObjectsTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator ilg)
    {
      List<CodeInstruction> instructionList = instructions.ToList();

      for (int i = 0; i < instructionList.Count; i++)
      {
        CodeInstruction instruction = instructionList[i];

        if (instruction.Calls(AccessTools.Property(typeof(ExpandableWorldObjectsUtility), nameof(ExpandableWorldObjectsUtility.TransitionPct)).GetGetMethod()))
        {
          Label label = ilg.DefineLabel();
          Label brlabel = ilg.DefineLabel();

          ///Check if TravelingTransportPod is SRTS Instance
          yield return new CodeInstruction(opcode: OpCodes.Ldloc_1);
          yield return new CodeInstruction(opcode: OpCodes.Isinst, operand: typeof(TravelingSRTS));
          yield return new CodeInstruction(opcode: OpCodes.Brfalse, label);

          ///Check if dynamic textures mod setting is enabled
          yield return new CodeInstruction(opcode: OpCodes.Call, operand: AccessTools.Property(type: typeof(SRTSHelper), name: nameof(SRTSHelper.DynamicTexturesEnabled)).GetGetMethod());
          yield return new CodeInstruction(opcode: OpCodes.Brfalse, label);

          ///Hook onto SRTS Draw method
          yield return new CodeInstruction(opcode: OpCodes.Ldloc_1);
          yield return new CodeInstruction(opcode: OpCodes.Call, operand: AccessTools.Method(type: typeof(TravelingSRTS), name: nameof(TravelingSRTS.Draw)));
          yield return new CodeInstruction(opcode: OpCodes.Leave, brlabel);

          int j = i;
          while (j < instructionList.Count)
          {
            if (instructionList[j].opcode == OpCodes.Ldloca_S)
            {
              instructionList[j].labels.Add(brlabel);
              break;
            }
            j++;
          }
          instruction.labels.Add(label);
        }
        yield return instruction;
      }
    }

    /// <summary>
    /// Expanding Icon dynamic drawer for SRTS dynamic textures
    /// </summary>
    /// <param name="instructions"></param>
    /// <param name="ilg"></param>
    /// <returns></returns>
    public static IEnumerable<CodeInstruction> ExpandableIconDetourSRTSTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator ilg)
    {
      List<CodeInstruction> instructionList = instructions.ToList();
      Label jumpLabel = ilg.DefineLabel();

      for (int i = 0; i < instructionList.Count; i++)
      {
        CodeInstruction instruction = instructionList[i];

        if (instruction.opcode == OpCodes.Ldloc_2 && instructionList[i + 1].opcode == OpCodes.Ldc_I4_1)
        {
          ///Jump label, for loop
          instruction.labels.Add(jumpLabel);
        }
        if (instruction.Calls(AccessTools.Property(type: typeof(WorldObject), name: nameof(WorldObject.ExpandingIconColor)).GetGetMethod()))
        {
          Label label = ilg.DefineLabel();

          ///Check if TravelingTransportPod is SRTS Instance
          yield return new CodeInstruction(opcode: OpCodes.Ldloc_3);
          yield return new CodeInstruction(opcode: OpCodes.Isinst, operand: typeof(TravelingSRTS));
          yield return new CodeInstruction(opcode: OpCodes.Brfalse, label);

          ///Check if dynamic textures mod setting is enabled
          yield return new CodeInstruction(opcode: OpCodes.Pop);
          yield return new CodeInstruction(opcode: OpCodes.Call, operand: AccessTools.Property(type: typeof(SRTSHelper), name: nameof(SRTSHelper.DynamicTexturesEnabled)).GetGetMethod());
          yield return new CodeInstruction(opcode: OpCodes.Brfalse, label);

          ///Hook onto SRTS Draw method and continue
          yield return new CodeInstruction(opcode: OpCodes.Ldloc_3);
          yield return new CodeInstruction(opcode: OpCodes.Call, operand: AccessTools.Method(type: typeof(TravelingSRTS), name: nameof(TravelingSRTS.Draw)));
          yield return new CodeInstruction(opcode: OpCodes.Br, jumpLabel);

          instruction.labels.Add(label);
        }

        yield return instruction;
      }
    }

    public static void TravelingSRTSChangeDirection(List<WorldObject> ___selected)
    {
      /*if(Event.current.button == 1 && ___selected.Count == 1 && ___selected[0] is TravelingSRTS)
      {
          (___selected[0] as TravelingSRTS).destinationTile = GenWorld.MouseTile(false);
          Vector3 flyingPosition = (___selected[0] as TravelingSRTS).DrawPos;
          Log.Message("1: " + GenWorld.TileAt(new Vector2(flyingPosition.x, flyingPosition.z)));
          Log.Message("2: " + GenWorld.MouseTile(false));
          //AccessTools.Field(type: typeof(TravelingSRTS), name: "initialTile").SetValue((TravelingSRTS)___selected[0], );
      }*/
    }

    public static bool CustomTravelSpeedSRTS(int ___initialTile, int ___destinationTile, List<ActiveDropPodInfo> ___pods, ref float __result)
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
        if (num == 0f)
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
      if (allCurrentThings.Any(x => x.TryGetComp<CompLaunchableSRTS>() != null))
      {
        Thing srts = allCurrentThings.First(x => x.TryGetComp<CompLaunchableSRTS>() != null);
        __result = SRTSMod.GetStatFor<float>(srts.def.defName, StatName.massCapacity);
        return false;
      }
      return true;
    }

    public static IEnumerable<CodeInstruction> SRTSMassUsageCaravanTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator ilg)
    {
      List<CodeInstruction> instructionList = instructions.ToList();

      for (int i = 0; i < instructionList.Count; i++)
      {
        CodeInstruction instruction = instructionList[i];

        if (instruction.opcode == OpCodes.Ldc_I4_0 && instructionList[i + 1].opcode == OpCodes.Ldc_I4_0 && instructionList[i + 2].opcode == OpCodes.Ldc_I4_0)
        {

          yield return instruction;
          instruction = instructionList[++i];

          Label label = ilg.DefineLabel();
          yield return new CodeInstruction(opcode: OpCodes.Call, operand: AccessTools.Property(type: typeof(SRTSHelper), name: nameof(SRTSHelper.SRTSInCaravan)).GetGetMethod());
          continue;
        }

        yield return instruction;
      }
    }

    public static void NoLaunchGroupForSRTS(ref IEnumerable<Gizmo> __result, CompTransporter __instance)
    {
      if (__instance.parent.def.GetCompProperties<CompProperties_LaunchableSRTS>() != null)
      {
        List<Gizmo> gizmos = __result.ToList();
        for (int i = gizmos.Count - 1; i >= 0; i--)
        {
          if ((gizmos[i] as Command_Action)?.defaultLabel == "CommandSelectPreviousTransporter".Translate() || (gizmos[i] as Command_Action)?.defaultLabel == "CommandSelectAllTransporters".Translate() ||
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
      foreach (ActiveDropPodInfo pod in dropPods)
      {
        foreach (Thing t in pod.innerContainer)
        {
          if (DefDatabase<ThingDef>.GetNamedSilentFail(t.def.defName.Split('_')[0])?.GetCompProperties<CompProperties_LaunchableSRTS>() != null)
          {
            TransportPodsArrivalActionUtility.RemovePawnsFromWorldPawns(dropPods);
            foreach (ActiveDropPodInfo pod2 in dropPods)
            {
              DropPodUtility.MakeDropPodAt(near, map, pod2);
            }
            return false;
          }
        }
      }
      return true;
    }

    public static void DrawBombingTargeter()
    {
      SRTSHelper.targeter.TargeterOnGUI();
    }

    public static void ProcessBombingInputEvents()
    {
      SRTSHelper.targeter.ProcessInputEvents();
    }

    public static void BombTargeterUpdate()
    {
      SRTSHelper.targeter.TargeterUpdate();
    }

    public static bool CustomSRTSMassCapacity(ref float __result, List<CompTransporter> ___transporters)
    {
      if (___transporters.Any(x => x.parent.TryGetComp<CompLaunchableSRTS>() != null))
      {
        float num = 0f;
        foreach (CompTransporter comp in ___transporters)
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
      if (SRTSHelper.srtsDefProjects.Any(x => x.Value == __instance))
      {
        __result = SRTSHelper.GetResearchStat(__instance) * __instance.CostFactor(Faction.OfPlayer.def.techLevel);
        return false;
      }
      return true;
    }

    public static bool ResearchIsFinished(ResearchProjectDef __instance, ref bool __result)
    {
      if (SRTSHelper.srtsDefProjects.Any(x => x.Value == __instance))
      {
        __result = __instance.ProgressReal >= SRTSHelper.GetResearchStat(__instance);
        return false;
      }
      return true;
    }

    public static bool ResearchProgressPercent(ResearchProjectDef __instance, ref float __result)
    {
      if (SRTSHelper.srtsDefProjects.Any(x => x.Value == __instance))
      {
        __result = Find.ResearchManager.GetProgress(__instance) / SRTSHelper.GetResearchStat(__instance);
        return false;
      }
      return true;
    }

    public static IEnumerable<CodeInstruction> ResearchFinishProjectTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator ilg)
    {
      List<CodeInstruction> instructionList = instructions.ToList();

      Label prerequisitesLabel = ilg.DefineLabel();
      yield return new CodeInstruction(opcode: OpCodes.Ldarg_1);
      yield return new CodeInstruction(opcode: OpCodes.Call, operand: AccessTools.Method(type: typeof(SRTSHelper), name: nameof(SRTSHelper.ContainedInDefProjects)));
      yield return new CodeInstruction(opcode: OpCodes.Brfalse, prerequisitesLabel);

      yield return new CodeInstruction(opcode: OpCodes.Ldarg_1);
      yield return new CodeInstruction(opcode: OpCodes.Ldarg_0);
      yield return new CodeInstruction(opcode: OpCodes.Call, operand: AccessTools.Method(type: typeof(SRTSHelper), name: nameof(SRTSHelper.FinishCustomPrerequisites)));

      yield return new CodeInstruction(opcode: OpCodes.Nop) { labels = new List<Label> { prerequisitesLabel } };

      for (int i = 0; i < instructionList.Count; i++)
      {
        CodeInstruction instruction = instructionList[i];

        if (instruction.opcode == OpCodes.Ldfld && (FieldInfo)instruction.operand == AccessTools.Field(type: typeof(ResearchManager), name: "progress"))
        {
          yield return instruction;
          instruction = instructionList[++i];

          Label label = ilg.DefineLabel();
          Label brlabel = ilg.DefineLabel();

          yield return new CodeInstruction(opcode: OpCodes.Ldarg_1);
          yield return new CodeInstruction(opcode: OpCodes.Call, operand: AccessTools.Method(type: typeof(SRTSHelper), name: nameof(SRTSHelper.ContainedInDefProjects)));
          yield return new CodeInstruction(opcode: OpCodes.Brfalse, label);

          yield return new CodeInstruction(opcode: OpCodes.Ldarg_1);
          yield return new CodeInstruction(opcode: OpCodes.Ldarg_1);
          yield return new CodeInstruction(opcode: OpCodes.Call, operand: AccessTools.Method(type: typeof(SRTSHelper), name: nameof(SRTSHelper.GetResearchStat)));
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

        if (instruction.opcode == OpCodes.Ldflda && (FieldInfo)instruction.operand == AccessTools.Field(type: typeof(ResearchProjectDef), name: "baseCost"))
        {
          Label label = ilg.DefineLabel();
          Label brlabel = ilg.DefineLabel();

          yield return new CodeInstruction(opcode: OpCodes.Ldarg_0);
          yield return new CodeInstruction(opcode: OpCodes.Ldfld, operand: AccessTools.Field(type: typeof(MainTabWindow_Research), name: "selectedProject"));
          yield return new CodeInstruction(opcode: OpCodes.Call, operand: AccessTools.Method(type: typeof(SRTSHelper), name: nameof(SRTSHelper.ContainedInDefProjects)));
          yield return new CodeInstruction(opcode: OpCodes.Brfalse, label);

          yield return new CodeInstruction(opcode: OpCodes.Call, operand: AccessTools.Method(type: typeof(SRTSHelper), name: nameof(SRTSHelper.GetResearchStatString)));
          yield return new CodeInstruction(opcode: OpCodes.Br, brlabel);

          instruction.labels.Add(label);
          instructionList[i + 4].labels.Add(brlabel);
        }
        yield return instruction;
      }
    }

    public static void ResearchFinishAllSRTS(ResearchManager __instance, ref Dictionary<ResearchProjectDef, float> ___progress)
    {
      foreach (ResearchProjectDef researchProjectDef in SRTSHelper.srtsDefProjects.Values)
      {
        ___progress[researchProjectDef] = SRTSHelper.GetResearchStat(researchProjectDef);
      }
      __instance.ReapplyAllMods();
    }

    public static void CustomPrerequisitesCompleted(ResearchProjectDef __instance, ref bool __result, List<ResearchProjectDef> ___prerequisites)
    {
      if (SRTSHelper.ContainedInDefProjects(__instance) && ___prerequisites != null && __result is true)
      {
        List<ResearchProjectDef> projects = SRTSMod.mod.settings.defProperties[SRTSHelper.srtsDefProjects.FirstOrDefault(x => x.Value == __instance).Key.defName].CustomResearch;
        foreach (ResearchProjectDef proj in projects)
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

        if (instruction.Calls(AccessTools.Method(type: typeof(MainTabWindow_Research), name: "PosX")) && flag)
        {
          flag = false;
          Label label = ilg.DefineLabel();

          yield return new CodeInstruction(opcode: OpCodes.Ldloc_S, 14);
          yield return new CodeInstruction(opcode: OpCodes.Call, operand: AccessTools.Method(type: typeof(SRTSHelper), name: nameof(SRTSHelper.ContainedInDefProjects)));
          yield return new CodeInstruction(opcode: OpCodes.Brfalse, label);

          yield return new CodeInstruction(opcode: OpCodes.Ldloc_S, 14);
          yield return new CodeInstruction(opcode: OpCodes.Ldarg_0);
          yield return new CodeInstruction(opcode: OpCodes.Call, operand: AccessTools.PropertyGetter(type: typeof(MainTabWindow_Research), name: "CurTab"));
          yield return new CodeInstruction(opcode: OpCodes.Ldloc_S, 3);
          yield return new CodeInstruction(opcode: OpCodes.Ldloc_S, 4);
          yield return new CodeInstruction(opcode: OpCodes.Ldarg_0);
          yield return new CodeInstruction(opcode: OpCodes.Ldfld, operand: AccessTools.Field(type: typeof(MainTabWindow_Research), name: "selectedProject"));
          yield return new CodeInstruction(opcode: OpCodes.Ldloc_S, 13);
          yield return new CodeInstruction(opcode: OpCodes.Call, operand: AccessTools.Method(type: typeof(SRTSHelper), name: nameof(SRTSHelper.DrawLinesCustomPrerequisites)));


          instruction.labels.Add(label);
        }
        yield return instruction;
      }
    }

    public static void DrawCustomResearchPrereqs(ResearchProjectDef project, Rect rect, ref float __result)
    {
      if (SRTSHelper.ContainedInDefProjects(project))
      {
        List<ResearchProjectDef> projects = SRTSMod.mod.settings.defProperties[SRTSHelper.srtsDefProjects.FirstOrDefault(x => x.Value == project).Key.defName].CustomResearch;
        float yMin = rect.yMin;
        rect.yMin += rect.height;
        var oldResult = __result;
        foreach (ResearchProjectDef proj in projects)
        {
          if (!project.IsFinished)
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

    public static IEnumerable<Gizmo> LaunchAndBombGizmosPassthrough(IEnumerable<Gizmo> __result, Caravan __instance)
    {
      IEnumerator<Gizmo> enumerator = __result.GetEnumerator();
      while (enumerator.MoveNext())
      {
        var element = enumerator.Current;
        yield return element;
        if ((element as Command_Action)?.defaultLabel == "CommandSettle".Translate() && __instance.PawnsListForReading.Any(x => x.inventory.innerContainer.Any(y => y.TryGetComp<CompLaunchableSRTS>() != null)))
        {
          float massUsage = 0f;
          Thing srts = null;
          foreach (Pawn p in __instance.PawnsListForReading)
          {
            foreach (Thing t in p.inventory?.innerContainer)
            {
              if (t.TryGetComp<CompLaunchableSRTS>() != null)
                srts = t;
              else
              {
                massUsage += t.GetStatValue(StatDefOf.Mass, true) * t.stackCount;
              }
            }
            massUsage += p.GetStatValue(StatDefOf.Mass, true);
            massUsage -= MassUtility.InventoryMass(p) * p.stackCount;
          }
          yield return new Command_Action
          {
            defaultLabel = "CommandLaunchGroup".Translate(),
            defaultDesc = "CommandLaunchGroupDesc".Translate(),
            icon = Tex2D.LaunchSRTS,
            alsoClickIfOtherInGroupClicked = false,
            action = delegate ()
            {
              if (massUsage > SRTSMod.GetStatFor<float>(srts.def.defName, StatName.massCapacity))
                Messages.Message("TooBigTransportersMassUsage".Translate(), MessageTypeDefOf.RejectInput, false);
              else
                srts.TryGetComp<CompLaunchableSRTS>().WorldStartChoosingDestination(__instance);
            }
          };
          /* Not Yet Implemented */
          /*yield return new Command_Action
          {
              defaultLabel = "BombTarget".Translate(),
              defaultDesc = "BombTargetDesc".Translate(),
              icon = TexCommand.Attack,
              action = delegate ()
              {
                  if(SRTSMod.mod.settings.passengerLimits)
                  {
                      if(__instance.PawnsListForReading.Count < SRTSMod.GetStatFor<int>(srts.def.defName, StatName.minPassengers))
                      {
                          Messages.Message("NotEnoughPilots".Translate(), MessageTypeDefOf.RejectInput, false);
                          return;
                      }
                      else if(__instance.PawnsListForReading.Count > SRTSMod.GetStatFor<int>(srts.def.defName, StatName.maxPassengers))
                      {
                          Messages.Message("TooManyPilots".Translate(), MessageTypeDefOf.RejectInput, false);
                          return;
                      }
                  }

                  FloatMenuOption carpetBombing = new FloatMenuOption("CarpetBombing".Translate(), delegate ()
                  {
                      srts.TryGetComp<CompBombFlyer>().bombType = BombingType.carpet;
                      srts.TryGetComp<CompBombFlyer>().StartChoosingWorldDestinationBomb(__instance);
                  });
                  FloatMenuOption preciseBombing = new FloatMenuOption("PreciseBombing".Translate(), delegate ()
                  {
                      srts.TryGetComp<CompBombFlyer>().bombType = BombingType.precise;
                      srts.TryGetComp<CompBombFlyer>().StartChoosingWorldDestinationBomb(__instance);
                  });
                  Find.WindowStack.Add(new FloatMenuGizmo(new List<FloatMenuOption>() { carpetBombing, preciseBombing }, srts, srts.LabelCap, UI.MouseMapPosition()));
              }
          };*/

          Command_Action RefuelSRTS = new Command_Action()
          {
            defaultLabel = "CommandAddFuelSRTS".Translate(srts.TryGetComp<CompRefuelable>().parent.Label),
            defaultDesc = "CommandAddFuelDescSRTS".Translate(),
            icon = Tex2D.FuelSRTS,
            alsoClickIfOtherInGroupClicked = false,
            action = delegate ()
            {
              bool flag = false;
              int count = 0;
              List<Thing> thingList = CaravanInventoryUtility.AllInventoryItems(__instance);
              for (int index = 0; index < thingList.Count; ++index)
              {
                if (thingList[index].def == ThingDefOf.Chemfuel)
                {
                  count = thingList[index].stackCount;
                  Pawn ownerOf = CaravanInventoryUtility.GetOwnerOf(__instance, thingList[index]);
                  float num = srts.TryGetComp<CompRefuelable>().Props.fuelCapacity - srts.TryGetComp<CompRefuelable>().Fuel;
                  if ((double)num < 1.0 && (double)num > 0.0)
                    count = 1;
                  if ((double)count * 1.0 >= (double)num)
                    count = (int)num;
                  if ((double)thingList[index].stackCount * 1.0 <= (double)count)
                  {
                    thingList[index].stackCount -= count;
                    Thing thing = thingList[index];
                    ownerOf.inventory.innerContainer.Remove(thing);
                    thing.Destroy(DestroyMode.Vanish);
                  }
                  else if ((uint)count > 0U)
                    thingList[index].SplitOff(count).Destroy(DestroyMode.Vanish);
                  srts.TryGetComp<CompRefuelable>().GetType().GetField("fuel", BindingFlags.Instance | BindingFlags.NonPublic).SetValue((object)srts.TryGetComp<CompRefuelable>(), (object)(float)((double)srts.TryGetComp<CompRefuelable>().Fuel + (double)count));
                  flag = true;
                  break;
                }
              }
              if (flag)
                Messages.Message("AddFuelSRTSCaravan".Translate(count, srts.LabelCap), MessageTypeDefOf.PositiveEvent, false);
              else
                Messages.Message("NoFuelSRTSCaravan".Translate(), MessageTypeDefOf.RejectInput, false);
            }
          };
          if (srts.TryGetComp<CompRefuelable>().IsFull)
            RefuelSRTS.Disable();
          yield return RefuelSRTS;
          yield return new Gizmo_MapRefuelableFuelStatus
          {

            nowFuel = srts.TryGetComp<CompRefuelable>().Fuel,
            maxFuel = srts.TryGetComp<CompRefuelable>().Props.fuelCapacity,
            compLabel = srts.TryGetComp<CompRefuelable>().Props.FuelGizmoLabel
          };
        }
      }
    }

    public static IEnumerable<CodeInstruction> CustomOptionsPawnsToTransportTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator ilg)
    {
      List<CodeInstruction> instructionList = instructions.ToList();

      for (int i = 0; i < instructionList.Count; i++)
      {
        CodeInstruction instruction = instructionList[i];

        if (instruction.opcode == OpCodes.Call && (MethodInfo)instruction.operand == AccessTools.Method(type: typeof(CaravanFormingUtility), name: nameof(CaravanFormingUtility.AllSendablePawns)))
        {
          Label label = ilg.DefineLabel();

          yield return new CodeInstruction(opcode: OpCodes.Ldarg_0);
          yield return new CodeInstruction(opcode: OpCodes.Ldfld, operand: AccessTools.Field(type: typeof(Dialog_LoadTransporters), name: "transporters"));
          yield return new CodeInstruction(opcode: OpCodes.Call, operand: AccessTools.Method(type: typeof(SRTSHelper), name: nameof(SRTSHelper.SRTSInTransporters)));
          yield return new CodeInstruction(opcode: OpCodes.Brfalse, label);

          ///Remove 4 booleans from the stack, replace with mod settings
          yield return new CodeInstruction(opcode: OpCodes.Pop);
          yield return new CodeInstruction(opcode: OpCodes.Pop);
          yield return new CodeInstruction(opcode: OpCodes.Pop);
          yield return new CodeInstruction(opcode: OpCodes.Pop);

          yield return new CodeInstruction(opcode: OpCodes.Ldsfld, operand: AccessTools.Field(type: typeof(SRTSMod), name: nameof(SRTSMod.mod)));
          yield return new CodeInstruction(opcode: OpCodes.Ldfld, operand: AccessTools.Field(type: typeof(SRTSMod), name: nameof(SRTSMod.settings)));
          yield return new CodeInstruction(opcode: OpCodes.Ldfld, operand: AccessTools.Field(type: typeof(SRTS_ModSettings), name: nameof(SRTS_ModSettings.allowEvenIfDowned)));

          yield return new CodeInstruction(opcode: OpCodes.Ldc_I4_0);

          yield return new CodeInstruction(opcode: OpCodes.Ldsfld, operand: AccessTools.Field(type: typeof(SRTSMod), name: nameof(SRTSMod.mod)));
          yield return new CodeInstruction(opcode: OpCodes.Ldfld, operand: AccessTools.Field(type: typeof(SRTSMod), name: nameof(SRTSMod.settings)));
          yield return new CodeInstruction(opcode: OpCodes.Ldfld, operand: AccessTools.Field(type: typeof(SRTS_ModSettings), name: nameof(SRTS_ModSettings.allowEvenIfPrisonerUnsecured)));

          yield return new CodeInstruction(opcode: OpCodes.Ldsfld, operand: AccessTools.Field(type: typeof(SRTSMod), name: nameof(SRTSMod.mod)));
          yield return new CodeInstruction(opcode: OpCodes.Ldfld, operand: AccessTools.Field(type: typeof(SRTSMod), name: nameof(SRTSMod.settings)));
          yield return new CodeInstruction(opcode: OpCodes.Ldfld, operand: AccessTools.Field(type: typeof(SRTS_ModSettings), name: nameof(SRTS_ModSettings.allowCapturablePawns)));

          instruction.labels.Add(label);
        }

        yield return instruction;
      }
    }

    public static bool CustomOptionsPawnsToTransportOverride(List<CompTransporter> ___transporters, Map ___map, Dialog_LoadTransporters __instance)
    {
      if (___transporters.Any(x => x.parent.TryGetComp<CompLaunchableSRTS>() != null))
      {
        List<Pawn> pawnlist = CaravanFormingUtility.AllSendablePawns(___map, SRTSMod.mod.settings.allowEvenIfDowned, false, SRTSMod.mod.settings.allowEvenIfPrisonerUnsecured, SRTSMod.mod.settings.allowCapturablePawns);
        foreach (Pawn p in pawnlist)
          AccessTools.Method(type: typeof(Dialog_LoadTransporters), name: "AddToTransferables").Invoke(__instance, new object[] { p });
        return false;
      }
      return true;
    }
  }
}
