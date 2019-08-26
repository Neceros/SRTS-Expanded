// Decompiled with JetBrains decompiler
// Type: Helicopter.HarmonyTest_trade
// Assembly: Helicopter, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 87D55B68-45DE-4FF0-8B2F-4616963FFBF1
// Assembly location: D:\SteamLibrary\steamapps\workshop\content\294100\1833992261\Assemblies\Helicopter.dll

using Harmony;
using RimWorld;
using System.Collections.Generic;
using Verse;

namespace Helicopter
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
        if (thingList1[index].def.defName != "Building_Helicopter")
          thingList2.Add(thingList1[index]);
      }
      traverse.Field("playerCaravanAllPawnsAndItems").SetValue((object) thingList2);
    }
  }
}
