using Harmony;
using RimWorld;
using Verse;

namespace SRTS
{
  [HarmonyPatch(typeof (DropPodUtility), "MakeDropPodAt", new System.Type[] {typeof (IntVec3), typeof (Map), typeof (ActiveDropPodInfo)})]
  public static class HarmonyTest
  {
    public static bool Prefix(IntVec3 c, Map map, ActiveDropPodInfo info)
    {
      for (int index = 0; index < info.innerContainer.Count; index++)
      {
        if (info.innerContainer[index].TryGetComp<CompLaunchableSRTS>() != null)
        {
          string shipType = info.innerContainer[index].def.defName;
          ActiveDropPod activeDropPod = (ActiveDropPod)ThingMaker.MakeThing(ThingDef.Named(shipType + "_Active"), (ThingDef)null);
          activeDropPod.Contents = info;
          SkyfallerMaker.SpawnSkyfaller(ThingDef.Named(shipType + "_Incoming"), (Thing)activeDropPod, c, map);
          return false;
        }
      }
      return true;
    }
  }
}
