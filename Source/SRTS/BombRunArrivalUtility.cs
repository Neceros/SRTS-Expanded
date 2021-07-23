using RimWorld;
using System.Collections.Generic;
using Verse;

namespace SRTS
{
    public static class BombRunArrivalUtility
    {
        public static void BombWithSRTS(List<ActiveDropPodInfo> srts, IntVec3 targetA, IntVec3 targetB, List<IntVec3> bombCells, BombingType bombType, Map map, Map originalMap, IntVec3 returnSpot)
        {
            if (srts.Count > 1)
                Log.Error("Initiating bomb run with more than 1 SRTS in Drop Pod Group. This should not happen. - Smash Phil");
            for (int i = 0; i < srts.Count; i++)
            {
                MakeSRTSBombingAt(targetA, targetB, bombCells, bombType, map, srts[i], originalMap, returnSpot);
            }
        }

        public static void MakeSRTSBombingAt(IntVec3 start, IntVec3 end, List<IntVec3> bombCells, BombingType bombType, Map map, ActiveDropPodInfo activeDropPodInfo, Map originalMap, IntVec3 returnSpot)
        {
            foreach (var ship in activeDropPodInfo.innerContainer)
            {
                if (ship.TryGetComp<CompLaunchableSRTS>() != null)
                {
                    string shipType = ship.def.defName;
                    ActiveDropPod srts = (ActiveDropPod)ThingMaker.MakeThing(ThingDef.Named(shipType + "_Active"));
                    srts.Contents = activeDropPodInfo;
                    BomberSkyfallerMaker.SpawnSkyfaller(ThingDef.Named(shipType + "_BomberRun"), srts, start, end, bombCells, bombType, map, ship.thingIDNumber, ship, originalMap, returnSpot);
                }
            }
        }
    }
}