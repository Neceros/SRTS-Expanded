using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace SRTS
{
    public class SRTSArrivalActionBombRun : TransportPodsArrivalAction
    {
        public SRTSArrivalActionBombRun()
        {
        }

        public SRTSArrivalActionBombRun(MapParent mapParent, Pair<IntVec3, IntVec3> targetCells, IEnumerable<IntVec3> bombCells, BombingType bombType, Map originalMap, IntVec3 originalLandingSpot)
        {
            this.mapParent = mapParent;
            this.targetCellA = targetCells.First;
            this.targetCellB = targetCells.Second;
            this.bombCells = bombCells.ToList();
            this.originalMap = originalMap;
            this.originalLandingSpot = originalLandingSpot;
            this.bombType = bombType;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look<MapParent>(ref this.mapParent, "mapParent", false);
            Scribe_References.Look(ref originalMap, "originalMap");
            Scribe_Values.Look(ref this.targetCellA, "targetCellA");
            Scribe_Values.Look(ref this.targetCellB, "targetCellB");
            Scribe_Values.Look(ref this.bombType, "bombType");
            Scribe_Collections.Look(ref bombCells, "bombCells");
            Scribe_Values.Look(ref originalLandingSpot, "originalLandingSpot");
        }

        public override FloatMenuAcceptanceReport StillValid(IEnumerable<IThingHolder> pods, int destinationTile)
        {
            FloatMenuAcceptanceReport floatMenuAcceptanceReport = base.StillValid(pods, destinationTile);
            if (!floatMenuAcceptanceReport)
            {
                return floatMenuAcceptanceReport;
            }
            if (this.mapParent != null && this.mapParent.Tile != destinationTile)
            {
                return false;
            }
            return SRTSArrivalActionBombRun.CanBombSpecificCell(pods, this.mapParent);
        }

        public override void Arrived(List<ActiveDropPodInfo> pods, int tile)
        {
            Thing lookTarget = TransportPodsArrivalActionUtility.GetLookTarget(pods);
            BombRunArrivalUtility.BombWithSRTS(pods, this.targetCellA, this.targetCellB, this.bombCells, this.bombType, this.mapParent.Map, originalMap, originalLandingSpot);
            Messages.Message("BombRunStarted".Translate(), lookTarget, MessageTypeDefOf.CautionInput, true);
        }

        public static bool CanBombSpecificCell(IEnumerable<IThingHolder> pods, MapParent mapParent)
        {
            return mapParent != null && mapParent.Spawned && mapParent.HasMap;
        }

        private MapParent mapParent;

        private IntVec3 targetCellA;

        private IntVec3 targetCellB;

        private List<IntVec3> bombCells;

        private Map originalMap;

        private IntVec3 originalLandingSpot;

        private BombingType bombType;
    }
}
