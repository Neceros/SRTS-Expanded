using Harmony;
using RimWorld;
using System.Collections.Generic;
using Verse;

namespace SRTS
{
  [HarmonyPatch(typeof (Dialog_Trade), "SetupPlayerCaravanVariables", new System.Type[] {})]
  public static class HarmonyTest_trade
  {
    public static void Postfix(Dialog_Trade __instance)
    {
      Traverse traverse = Traverse.Create((object) __instance);
      List<Thing> thingList1 = traverse.Field("playerCaravanAllPawnsAndItems").GetValue<List<Thing>>();
      List<Thing> thingList2 = new List<Thing>();
      if (thingList1 == null || thingList1.Count <= 0)
        return;
      for (int index = 0; index < thingList1.Count; ++index)
      {
        if (thingList1[index].TryGetComp<CompLaunchableSRTS>() != null)
          thingList2.Add(thingList1[index]);
      }
      traverse.Field("playerCaravanAllPawnsAndItems").SetValue((object) thingList2);
    }
  }
}
