// Decompiled with JetBrains decompiler
// Type: Helicopter.HarmonyTest_AJ
// Assembly: Helicopter, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 87D55B68-45DE-4FF0-8B2F-4616963FFBF1
// Assembly location: D:\SteamLibrary\steamapps\workshop\content\294100\1833992261\Assemblies\Helicopter.dll

using Harmony;
using RimWorld;
using RimWorld.Planet;
using System.Collections.Generic;
using Verse;

namespace Helicopter
{
  [HarmonyPatch(typeof (TransportPodsArrivalAction_LandInSpecificCell), "Arrived", new System.Type[] {typeof (List<ActiveDropPodInfo>), typeof (int)})]
  public static class HarmonyTest_AJ
  {
    public static bool Prefix(
      TransportPodsArrivalAction_LandInSpecificCell __instance,
      List<ActiveDropPodInfo> pods,
      int tile)
    {
      foreach (ActiveDropPodInfo pod in pods)
      {
        if (pod.innerContainer.Contains(ThingDef.Named("Building_Helicopter")))
        {
          Thing lookTarget = TransportPodsArrivalActionUtility.GetLookTarget(pods);
          Traverse traverse = Traverse.Create((object) __instance);
          IntVec3 c = traverse.Field("cell").GetValue<IntVec3>();
          Map map = traverse.Field("mapParent").GetValue<MapParent>().Map;
          TransportPodsArrivalActionUtility.RemovePawnsFromWorldPawns(pods);
          for (int index = 0; index < pods.Count; ++index)
            DropPodUtility.MakeDropPodAt(c, map, pods[index]);
          Messages.Message("MessageTransportPodsArrived".Translate(), (LookTargets) lookTarget, MessageTypeDefOf.TaskCompletion, true);
          return false;
        }
      }
      return true;
    }
  }
}
