using Harmony;
using System.Reflection;
using Verse;

namespace SRTS
{
  [StaticConstructorOnStartup]
  public static class StartUp
  {
    static StartUp()
    {
      HarmonyInstance.Create("SRTSExpanded").PatchAll(Assembly.GetExecutingAssembly());
    }
  }
}
