// Decompiled with JetBrains decompiler
// Type: Helicopter.HarmonyTest
// Assembly: Helicopter, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 87D55B68-45DE-4FF0-8B2F-4616963FFBF1
// Assembly location: D:\SteamLibrary\steamapps\workshop\content\294100\1833992261\Assemblies\Helicopter.dll

using Harmony;
using RimWorld;
using Verse;

namespace Helicopter
{
  [HarmonyPatch(typeof (DropPodUtility), "MakeDropPodAt", new System.Type[] {typeof (IntVec3), typeof (Map), typeof (ActiveDropPodInfo)})]
  public static class HarmonyTest
  {
    public static bool Prefix(IntVec3 c, Map map, ActiveDropPodInfo info)
    {
      if (!info.innerContainer.Contains(ThingDef.Named("Building_Helicopter")))
        return true;
      ActiveDropPod activeDropPod = (ActiveDropPod) ThingMaker.MakeThing(ThingDef.Named("ActiveHelicopter"), (ThingDef) null);
      activeDropPod.Contents = info;
      SkyfallerMaker.SpawnSkyfaller(ThingDef.Named("HelicopterIncoming"), (Thing) activeDropPod, c, map);
      return false;
    }
  }
}
