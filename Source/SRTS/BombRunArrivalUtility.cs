using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace SRTS
{
    public static class BombRunArrivalUtility
    {
        public static void BombWithSRTS(List<ActiveDropPodInfo> srts, IntVec3 target, Map map)
        {
            if(srts.Count > 1)
                Log.Error("Initiating bomb run with more than 1 SRTS in Drop Pod Group. This should not happen. - Smash Phil");
            for(int i = 0; i < srts.Count; i++)
            {
                MakeSRTSBombingAt(target, map, srts[i]);
            }
        }

        public static void MakeSRTSBombingAt(IntVec3 c, Map map, ActiveDropPodInfo info)
        {
            for (int index = 0; index < info.innerContainer.Count; index++)
            {
                if (info.innerContainer[index].TryGetComp<CompLaunchableSRTS>() != null)
                {
                    Thing ship = info.innerContainer[index];
                    string shipType = ship.def.defName;
                    ActiveDropPod srts = (ActiveDropPod)ThingMaker.MakeThing(ThingDef.Named(shipType + "_Active"), null);
                    srts.Contents = info;
                    BomberSkyfallerMaker.SpawnSkyfaller(ThingDef.Named(shipType + "_BomberRun"), srts, c, map, ship.thingIDNumber);
                }
            }
        }
    }
}
