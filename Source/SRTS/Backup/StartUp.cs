// Decompiled with JetBrains decompiler
// Type: Helicopter.StartUp
// Assembly: Helicopter, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 87D55B68-45DE-4FF0-8B2F-4616963FFBF1
// Assembly location: D:\SteamLibrary\steamapps\workshop\content\294100\1833992261\Assemblies\Helicopter.dll

using Harmony;
using System.Reflection;
using Verse;

namespace Helicopter
{
  [StaticConstructorOnStartup]
  public static class StartUp
  {
    static StartUp()
    {
      HarmonyInstance.Create("Helicopter.AKreedz").PatchAll(Assembly.GetExecutingAssembly());
    }
  }
}
