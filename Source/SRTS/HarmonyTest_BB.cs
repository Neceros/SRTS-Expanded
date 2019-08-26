using Harmony;
using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Verse;

namespace SRTS
{
  [HarmonyPatch(typeof (Caravan), "GetGizmos", new System.Type[] {})]
  public static class HarmonyTest_BB
  {
    public static void Postfix(Caravan __instance, ref IEnumerable<Gizmo> __result)
    {
      float masss = 0.0f;
      foreach (Pawn pawn in __instance.pawns.InnerListForReading)
      {
        for (int index = 0; index < pawn.inventory.innerContainer.Count; ++index)
        {
          if (pawn.inventory.innerContainer[index].def.defName != "SRTSMkII" &&
              pawn.inventory.innerContainer[index].def.defName != "SRTSMkIII")
            masss += pawn.inventory.innerContainer[index].def.BaseMass * (float) pawn.inventory.innerContainer[index].stackCount;
        }
      }
      foreach (Pawn pawn in __instance.pawns.InnerListForReading)
      {
        Pawn_InventoryTracker pinv = pawn.inventory;
        for (int i = 0; i < pinv.innerContainer.Count; i++)
        {
          if (pinv.innerContainer[i].def.defName == "SRTSMkII" ||
              pinv.innerContainer[i].def.defName == "SRTSMkIII")
          {
            Command_Action commandAction1 = new Command_Action();
            commandAction1.defaultLabel = "CommandLaunchGroup".Translate();
            commandAction1.defaultDesc = "CommandLaunchGroupDesc".Translate();
            commandAction1.icon = ContentFinder<Texture2D>.Get("UI/Commands/LaunchShip", true);
            commandAction1.alsoClickIfOtherInGroupClicked = false;
            commandAction1.action = (Action) (() =>
            {
              float massCapacity = pinv.innerContainer[i].TryGetComp<CompTransporter>().Props.massCapacity;
              if ((double) masss <= (double) massCapacity)
                pinv.innerContainer[i].TryGetComp<CompLaunchableSRTS>().WorldStartChoosingDestination(__instance);
              else
                Messages.Message("TooBigTransportersMassUsage".Translate() + "(" + (object) (float) ((double) massCapacity - (double) masss) + "KG)", MessageTypeDefOf.RejectInput, false);
            });
            List<Gizmo> list = __result.ToList<Gizmo>();
            list.Add((Gizmo) commandAction1);
            Command_Action commandAction2 = new Command_Action();
            commandAction2.defaultLabel = "CommandAddFuel".Translate();
            commandAction2.defaultDesc = "CommandAddFuelDesc".Translate();
            commandAction2.icon = ContentFinder<Texture2D>.Get("Things/Item/Resource/Chemfuel", true);
            commandAction2.alsoClickIfOtherInGroupClicked = false;
            commandAction2.action = (Action) (() =>
            {
              bool flag = false;
              int count = 0;
              CompRefuelable comp = pinv.innerContainer[i].TryGetComp<CompRefuelable>();
              List<Thing> thingList = CaravanInventoryUtility.AllInventoryItems(__instance);
              for (int index = 0; index < thingList.Count; ++index)
              {
                if (thingList[index].def == ThingDefOf.Chemfuel)
                {
                  count = thingList[index].stackCount;
                  Pawn ownerOf = CaravanInventoryUtility.GetOwnerOf(__instance, thingList[index]);
                  float num = comp.Props.fuelCapacity - comp.Fuel;
                  if ((double) num < 1.0 && (double) num > 0.0)
                    count = 1;
                  if ((double) count * 1.0 >= (double) num)
                    count = (int) num;
                  if ((double) thingList[index].stackCount * 1.0 <= (double) count)
                  {
                    thingList[index].stackCount -= count;
                    Thing thing = thingList[index];
                    ownerOf.inventory.innerContainer.Remove(thing);
                    thing.Destroy(DestroyMode.Vanish);
                  }
                  else if ((uint) count > 0U)
                    thingList[index].SplitOff(count).Destroy(DestroyMode.Vanish);
                  comp.GetType().GetField("fuel", BindingFlags.Instance | BindingFlags.NonPublic).SetValue((object) comp, (object) (float) ((double) comp.Fuel + (double) count));
                  flag = true;
                  break;
                }
              }
              if (flag)
                Messages.Message("AddFuelDoneMsg".Translate((object) count, (object) comp.Fuel), MessageTypeDefOf.PositiveEvent, false);
              else
                Messages.Message("NonOilMsg".Translate(), MessageTypeDefOf.RejectInput, false);
            });
            list.Add((Gizmo) commandAction2);
            Gizmo_MapRefuelableFuelStatus refuelableFuelStatus = new Gizmo_MapRefuelableFuelStatus()
            {
              nowFuel = pinv.innerContainer[i].TryGetComp<CompRefuelable>().Fuel,
              maxFuel = pinv.innerContainer[i].TryGetComp<CompRefuelable>().Props.fuelCapacity,
              compLabel = pinv.innerContainer[i].TryGetComp<CompRefuelable>().Props.FuelGizmoLabel
            };
            list.Add((Gizmo) refuelableFuelStatus);
            __result = (IEnumerable<Gizmo>) list;
            return;
          }
        }
      }
    }
  }
}
