using RimWorld;
using RimWorld.Planet;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.AI.Group;
using Verse.Sound;

namespace SRTS
{
    public class SRTSLeaving : Skyfaller, IActiveDropPod, IThingHolder
    {
        private static List<Thing> tmpActiveDropPods = new List<Thing>();
        public int groupID = -1;
        public int destinationTile = -1;
        public TransportPodsArrivalAction arrivalAction;
        private bool alreadyLeft;
        public Rot4 rotation;

        public ActiveDropPodInfo Contents
        {
            get
            {
                return ((ActiveDropPod) this.innerContainer[0]).Contents;
            }
            set
            {
                ((ActiveDropPod) this.innerContainer[0]).Contents = value;
            }
        }

        /*public override Vector3 DrawPos
        {
            get
            {
                switch(def.skyfaller.movementType)
                {
                    case SkyfallerMovementType.Accelerate:
                        if (initiatingTakeoff)
                            return SkyfallerDrawPosUtilityExtended.DrawPos_AccelerateSRTSDirectional(originalDrawPos, ticksToImpact, rotation, def.skyfaller.speed);
                        else
                            return SkyfallerDrawPosUtilityExtended.DrawPos_TakeoffUpward(originalDrawPos, takeoffTicks);
                    case SkyfallerMovementType.ConstantSpeed:
                        return base.DrawPos;// SkyfallerDrawPosUtilityExtended.DrawPos_AccelerateSRTSDirectional(base.DrawPos, ticksToImpact, rotation, def.skyfaller.speed);
                    *//*case SkyfallerMovementType.Decelerate:
                        return;*//*
                    default:
                        Log.Error("SkyfallerMovementType is not consistent with Vanilla enum options. Defaulting to vanilla's accelerate mechanic. - Smash Phil");
                        return SkyfallerDrawPosUtility.DrawPos_Accelerate(base.DrawPos, ticksToImpact, angle, def.skyfaller.speed);
                }
            }
        }*/

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            originalDrawPos = base.DrawPos;
            this.Rotation = Rot4.West; //rotation; going to be for directional lift off
        }

        public override void ExposeData()
        {
            base.ExposeData();

            Scribe_Values.Look<int>(ref this.groupID, "groupID", 0, false);
            Scribe_Values.Look<int>(ref this.destinationTile, "destinationTile", 0, false);
            Scribe_Deep.Look<TransportPodsArrivalAction>(ref this.arrivalAction, "arrivalAction");
            Scribe_Values.Look<bool>(ref this.alreadyLeft, "alreadyLeft", false, false);
            Scribe_Values.Look<Rot4>(ref this.rotation, "rotation", Rot4.North);
        }

        protected override void LeaveMap()
        {
            if (this.alreadyLeft)
                base.LeaveMap();
            else if (this.groupID < 0)
            {
                Log.Error("Drop pod left the map, but its group ID is " + (object) this.groupID);
                this.Destroy(DestroyMode.Vanish);
            }
            else if (this.destinationTile < 0)
            {
                Log.Error("Drop pod left the map, but its destination tile is " + (object) this.destinationTile);
                this.Destroy(DestroyMode.Vanish);
            }
            else
            {
                Lord lord = TransporterUtility.FindLord(this.groupID, this.Map);
                if (lord != null)
                    this.Map.lordManager.RemoveLord(lord);
                TravelingSRTS travelingTransportPods = (TravelingSRTS)WorldObjectMaker.MakeWorldObject(DefDatabase<WorldObjectDef>.GetNamed("TravelingSRTS", true));
                travelingTransportPods.Tile = this.Map.Tile;
                travelingTransportPods.SetFaction(Faction.OfPlayer);
                travelingTransportPods.destinationTile = this.destinationTile;
                travelingTransportPods.arrivalAction = this.arrivalAction;

                Find.WorldObjects.Add((WorldObject) travelingTransportPods);
                SRTSLeaving.tmpActiveDropPods.Clear();
                SRTSLeaving.tmpActiveDropPods.AddRange((IEnumerable<Thing>) this.Map.listerThings.ThingsInGroup(ThingRequestGroup.ActiveDropPod));
                travelingTransportPods.flyingThing = tmpActiveDropPods.Find(x => (x as SRTSLeaving)?.groupID == this.groupID);
                for (int index = 0; index < SRTSLeaving.tmpActiveDropPods.Count; ++index)
                {
                    SRTSLeaving tmpActiveDropPod = SRTSLeaving.tmpActiveDropPods[index] as SRTSLeaving;
                    if (tmpActiveDropPod != null && tmpActiveDropPod.groupID == this.groupID)
                    {
                        tmpActiveDropPod.alreadyLeft = true;
                        travelingTransportPods.AddPod(tmpActiveDropPod.Contents, true);
                        tmpActiveDropPod.Contents = (ActiveDropPodInfo) null;
                        tmpActiveDropPod.Destroy(DestroyMode.Vanish);
                    }
                }
            }
        }

        public override void Tick()
        {
            innerContainer.ThingOwnerTick(true);
            takeoffTicks++;
            if (takeoffTicks >= TakeoffCountTicks && !initiatingTakeoff)
            {
                initiatingTakeoff = true;
                originalDrawPos = SkyfallerDrawPosUtilityExtended.DrawPos_TakeoffUpward(originalDrawPos, TakeoffCountTicks);
            }
            if (initiatingTakeoff)
            {
                ticksToImpact++;

                if (!soundPlayed && def.skyfaller.anticipationSound != null && ticksToImpact > def.skyfaller.anticipationSoundTicks)
                {
                    soundPlayed = true;
                    def.skyfaller.anticipationSound.PlayOneShot(new TargetInfo(base.Position, base.Map, false));
                }
                if (ticksToImpact == 220)
                    LeaveMap();
            }
        }

        private const int TakeoffCountTicks = 300;

        private int takeoffTicks = 0;

        private bool soundPlayed = false;

        private bool initiatingTakeoff = false;

        private Vector3 originalDrawPos = Vector3.zero;
    }
}
