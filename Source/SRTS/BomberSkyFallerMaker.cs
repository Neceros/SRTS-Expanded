using SPExtended;
using System.Collections.Generic;
using Verse;

namespace SRTS
{
    public static class BomberSkyfallerMaker
    {
        public static BomberSkyfaller MakeSkyfaller(ThingDef skyfallerDef, ThingDef innerThingDef)
        {
            return MakeSkyfaller(skyfallerDef, ThingMaker.MakeThing(innerThingDef));
        }

        public static BomberSkyfaller MakeSkyfaller(ThingDef skyfallerDef, Thing innerThing = null)
        {
            BomberSkyfaller skyfaller = (BomberSkyfaller)ThingMaker.MakeThing(skyfallerDef);
            if (innerThing != null && !skyfaller.innerContainer.TryAdd(innerThing))
            {
                Log.Error("Could not add " + innerThing.ToStringSafe() + " to a skyfaller.");
                innerThing.Destroy();
            }
            return skyfaller;
        }

        public static BomberSkyfaller MakeSkyfaller(ThingDef skyfallerDef, IEnumerable<Thing> things)
        {
            BomberSkyfaller skyfaller = MakeSkyfaller(skyfallerDef);
            if (things != null)
                skyfaller.innerContainer.TryAddRangeOrTransfer(things, false, true);
            return skyfaller;
        }

        public static BomberSkyfaller SpawnSkyfaller(ThingDef skyfallerDef, IntVec3 pos, Map map)
        {
            BomberSkyfaller skyfaller = MakeSkyfaller(skyfallerDef);
            return (BomberSkyfaller)GenSpawn.Spawn(skyfaller, pos, map);
        }

        public static BomberSkyfaller SpawnSkyfaller(ThingDef skyfallerDef, ThingDef innerThing, IntVec3 pos, Map map)
        {
            BomberSkyfaller skyfaller = MakeSkyfaller(skyfallerDef, innerThing);
            return (BomberSkyfaller)GenSpawn.Spawn(skyfaller, pos, map);
        }

        public static BomberSkyfaller SpawnSkyfaller(ThingDef skyfallerDef, Thing innerThing, IntVec3 start, IntVec3 end, List<IntVec3> bombCells, BombingType bombType, Map map, int idNumber, Thing original, Map originalMap, IntVec3 landingSpot)
        {
            BomberSkyfaller skyfaller = MakeSkyfaller(skyfallerDef, innerThing);
            skyfaller.originalMap = originalMap;
            skyfaller.sourceLandingSpot = landingSpot;
            skyfaller.numberOfBombs = SRTSMod.GetStatFor<int>(original.def.defName, StatName.numberBombs);
            skyfaller.precisionBombingNumBombs = SRTSMod.GetStatFor<int>(original.def.defName, StatName.precisionBombingNumBombs);
            skyfaller.speed = SRTSMod.GetStatFor<float>(original.def.defName, StatName.bombingSpeed);
            skyfaller.radius = SRTSMod.GetStatFor<int>(original.def.defName, StatName.radiusDrop);
            skyfaller.sound = original.TryGetComp<CompBombFlyer>().Props.soundFlyBy;
            skyfaller.bombType = bombType;

            double angle = start.AngleToPointRelative(end);
            skyfaller.angle = (float)(angle + 90) * -1;
            IntVec3 exitPoint = SPTrig.ExitPointCustom(angle, start, map);

            BomberSkyfaller spawnedSkyfaller = (BomberSkyfaller)GenSpawn.Spawn(skyfaller, exitPoint, map);
            spawnedSkyfaller.bombCells = bombCells;
            return spawnedSkyfaller;
        }

        public static BomberSkyfaller SpawnSkyfaller(ThingDef skyfallerDef, IEnumerable<Thing> things, IntVec3 pos, Map map)
        {
            BomberSkyfaller skyfaller = MakeSkyfaller(skyfallerDef, things);
            return (BomberSkyfaller)GenSpawn.Spawn(skyfaller, pos, map);
        }
    }
}