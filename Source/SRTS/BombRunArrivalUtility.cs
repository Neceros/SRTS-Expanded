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
        public static void BombWithSRTS(List<ActiveDropPodInfo> srts, IntVec3 targetA, IntVec3 targetB, List<IntVec3> bombCells, BombingType bombType, Map map, Map originalMap, IntVec3 returnSpot)
        {
            if(srts.Count > 1)
                Log.Error("Initiating bomb run with more than 1 SRTS in Drop Pod Group. This should not happen. - Smash Phil");
            for(int i = 0; i < srts.Count; i++)
            {
                MakeSRTSBombingAt(targetA, targetB, bombCells, bombType, map, srts[i], originalMap, returnSpot);
            }
        }

        public static void MakeSRTSBombingAt(IntVec3 c1, IntVec3 c2, List<IntVec3> bombCells, BombingType bombType, Map map, ActiveDropPodInfo info, Map originalMap, IntVec3 returnSpot)
        {
            for (int index = 0; index < info.innerContainer.Count; index++)
            {
                if (info.innerContainer[index].TryGetComp<CompLaunchableSRTS>() != null)
                {
                    Thing ship = info.innerContainer[index];
                    string shipType = ship.def.defName;
                    ActiveDropPod srts = (ActiveDropPod)ThingMaker.MakeThing(ThingDef.Named(shipType + "_Active"), null);
                    srts.Contents = info;
                    BomberSkyfallerMaker.SpawnSkyfaller(ThingDef.Named(shipType + "_BomberRun"), srts, c1, c2, bombCells, bombType, map, ship.thingIDNumber, ship, originalMap, returnSpot);
                }
            }
        }
    }
}
