using Harmony;
using RimWorld;
using RimWorld.Planet;
using System.Collections.Generic;
using System.Reflection;
using Verse;

namespace SRTS
{
  class HarmonyTest_TS
  {
    [HarmonyPatch(typeof(Dialog_LoadTransporters), "AddPawnsToTransferables", new System.Type[] { })]
    public static class HarmonyTest_C
    {
      public static bool Prefix(Dialog_LoadTransporters __instance)
      {

        return true;
      }
    }
  }
}
