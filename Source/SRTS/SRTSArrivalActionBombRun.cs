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

        public SRTSArrivalActionBombRun(MapParent mapParent, IntVec3 cell)
        {
            this.mapParent = mapParent;
            this.cell = cell;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look<MapParent>(ref this.mapParent, "mapParent", false);
            Scribe_Values.Look<IntVec3>(ref this.cell, "cell", default(IntVec3), false);
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
            BombRunArrivalUtility.BombWithSRTS(pods, this.cell, this.mapParent.Map);
            Messages.Message("BombRunStarted", lookTarget, MessageTypeDefOf.CautionInput, true);
        }

        public static bool CanBombSpecificCell(IEnumerable<IThingHolder> pods, MapParent mapParent)
        {
            return mapParent != null && mapParent.Spawned && mapParent.HasMap;
        }

        private MapParent mapParent;

        private IntVec3 cell;
    }
}
