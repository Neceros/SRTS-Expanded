using Harmony;
using RimWorld;
using System;
using Verse;

namespace SRTS
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
        if (thing != null && thing.TryGetComp<CompLaunchableSRTS>() != null)
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
