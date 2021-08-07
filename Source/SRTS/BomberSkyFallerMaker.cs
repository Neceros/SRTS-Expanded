using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using RimWorld;
using SPExtended;

namespace SRTS
{
    public static class BomberSkyfallerMaker
    {
        public static BomberSkyfaller MakeSkyfaller(ThingDef skyfaller)
        {
            return (BomberSkyfaller)ThingMaker.MakeThing(skyfaller);
        }

        public static BomberSkyfaller MakeSkyfaller(ThingDef skyfaller, ThingDef innerThing)
        {
            Thing innerThing2 = ThingMaker.MakeThing(innerThing, null);
            return BomberSkyfallerMaker.MakeSkyfaller(skyfaller, innerThing2);
        }

        public static BomberSkyfaller MakeSkyfaller(ThingDef skyfaller, Thing innerThing)
        {
            BomberSkyfaller skyfaller2 = BomberSkyfallerMaker.MakeSkyfaller(skyfaller);
            if(innerThing != null && !skyfaller2.innerContainer.TryAdd(innerThing, true))
            {
                Log.Error("Could not add " + innerThing.ToStringSafe<Thing>() + " to a skyfaller.");
                innerThing.Destroy(DestroyMode.Vanish);
            }
            return skyfaller2;
        }

        public static BomberSkyfaller MakeSkyfaller(ThingDef skyfaller, IEnumerable<Thing> things)
        {
            BomberSkyfaller skyfaller2 = BomberSkyfallerMaker.MakeSkyfaller(skyfaller);
            if(things != null)
                skyfaller2.innerContainer.TryAddRangeOrTransfer(things, false, true);
            return skyfaller2;
        }

        public static BomberSkyfaller SpawnSkyfaller(ThingDef skyfaller, IntVec3 pos, Map map)
        {
            BomberSkyfaller thing = BomberSkyfallerMaker.MakeSkyfaller(skyfaller);
            return (BomberSkyfaller)GenSpawn.Spawn(thing, pos, map, WipeMode.Vanish);
        }

        public static BomberSkyfaller SpawnSkyfaller(ThingDef skyfaller, ThingDef innerThing, IntVec3 pos, Map map)
        {
            BomberSkyfaller thing = BomberSkyfallerMaker.MakeSkyfaller(skyfaller, innerThing);
            return (BomberSkyfaller)GenSpawn.Spawn(thing, pos, map, WipeMode.Vanish);
        }

        public static BomberSkyfaller SpawnSkyfaller(ThingDef skyfaller, Thing innerThing, IntVec3 start, IntVec3 end, List<IntVec3> bombCells, BombingType bombType, Map map, int idNumber, Thing original, Map originalMap, IntVec3 landingSpot)
        {
            BomberSkyfaller thing = BomberSkyfallerMaker.MakeSkyfaller(skyfaller, innerThing);
            thing.originalMap = originalMap;
            thing.sourceLandingSpot = landingSpot;
            thing.numberOfBombs = SRTSMod.GetStatFor<int>(original.def.defName, StatName.numberBombs);
            thing.precisionBombingNumBombs = SRTSMod.GetStatFor<int>(original.def.defName, StatName.precisionBombingNumBombs);
            thing.speed = SRTSMod.GetStatFor<float>(original.def.defName, StatName.bombingSpeed);
            thing.radius = SRTSMod.GetStatFor<int>(original.def.defName, StatName.radiusDrop);
            thing.sound = original.TryGetComp<CompBombFlyer>().Props.soundFlyBy;
            thing.bombType = bombType;

            double angle = start.AngleToPointRelative(end);
            thing.angle = (float)(angle + 90) * -1;
            IntVec3 exitPoint = SPTrig.ExitPointCustom(angle, start, map);

            BomberSkyfaller bomber = (BomberSkyfaller)GenSpawn.Spawn(thing, exitPoint, map, WipeMode.Vanish);
            bomber.bombCells = bombCells;
            return bomber;
        }

        public static BomberSkyfaller SpawnSkyfaller(ThingDef skyfaller, IEnumerable<Thing> things, IntVec3 pos, Map map)
        {
            BomberSkyfaller thing = BomberSkyfallerMaker.MakeSkyfaller(skyfaller, things);
            return (BomberSkyfaller)GenSpawn.Spawn(thing, pos, map, WipeMode.Vanish);
        }
    }
}
