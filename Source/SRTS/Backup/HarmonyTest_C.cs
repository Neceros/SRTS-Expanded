// Decompiled with JetBrains decompiler
// Type: Helicopter.HarmonyTest_C
// Assembly: Helicopter, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 87D55B68-45DE-4FF0-8B2F-4616963FFBF1
// Assembly location: D:\SteamLibrary\steamapps\workshop\content\294100\1833992261\Assemblies\Helicopter.dll

using Harmony;
using RimWorld;
using RimWorld.Planet;
using System.Collections.Generic;
using System.Reflection;
using Verse;

namespace Helicopter
{
  [HarmonyPatch(typeof (Dialog_LoadTransporters), "AddPawnsToTransferables", new System.Type[] {})]
  public static class HarmonyTest_C
  {
    public static bool Prefix(Dialog_LoadTransporters __instance)
    {
      Traverse traverse = Traverse.Create((object) __instance);
      foreach (ThingComp thingComp in traverse.Field("transporters").GetValue<List<CompTransporter>>())
      {
        if (thingComp.parent.TryGetComp<CompLaunchableHelicopter>() != null)
        {
          List<Pawn> pawnList = CaravanFormingUtility.AllSendablePawns(traverse.Field("map").GetValue<Map>(), true, true, true, true);
          for (int index = 0; index < pawnList.Count; ++index)
            __instance.GetType().GetMethod("AddToTransferables", BindingFlags.Instance | BindingFlags.NonPublic).Invoke((object) __instance, new object[1]
            {
              (object) pawnList[index]
            });
          return false;
        }
      }
      return true;
    }
  }
}
