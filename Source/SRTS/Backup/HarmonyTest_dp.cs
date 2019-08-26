// Decompiled with JetBrains decompiler
// Type: Helicopter.HarmonyTest_dp
// Assembly: Helicopter, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 87D55B68-45DE-4FF0-8B2F-4616963FFBF1
// Assembly location: D:\SteamLibrary\steamapps\workshop\content\294100\1833992261\Assemblies\Helicopter.dll

using Harmony;
using RimWorld;
using System;
using Verse;

namespace Helicopter
{
  [HarmonyPatch(typeof (ActiveDropPod), "PodOpen", new System.Type[] {})]
  public static class HarmonyTest_dp
  {
    public static void Prefix(ActiveDropPod __instance)
    {
      ActiveDropPodInfo activeDropPodInfo = Traverse.Create((object) __instance).Field("contents").GetValue<ActiveDropPodInfo>();
      for (int index = activeDropPodInfo.innerContainer.Count - 1; index >= 0; --index)
      {
        Thing thing = activeDropPodInfo.innerContainer[index];
        if (thing != null && thing.def.defName == "Building_Helicopter")
        {
          Thing lastResultingThing;
          GenPlace.TryPlaceThing(thing, __instance.Position, __instance.Map, ThingPlaceMode.Direct, out lastResultingThing, (Action<Thing, int>) ((placedThing, count) =>
          {
            if (Find.TickManager.TicksGame >= 1200 || !TutorSystem.TutorialMode || placedThing.def.category != ThingCategory.Item)
              return;
            Find.TutorialState.AddStartingItem(placedThing);
          }), (Predicate<IntVec3>) null);
          break;
        }
      }
    }
  }
}
