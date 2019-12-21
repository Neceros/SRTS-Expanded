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
                Log.Error("Could not add " + innerThing.ToStringSafe<Thing>() + " to a skyfaller.", false);
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

        public static BomberSkyfaller SpawnSkyfaller(ThingDef skyfaller, Thing innerThing, IntVec3 pos, Map map, int idNumber, Thing original)
        {
            BomberSkyfaller thing = BomberSkyfallerMaker.MakeSkyfaller(skyfaller, innerThing);
            thing.source = StartUp.SRTSBombers[idNumber];
            thing.numberOfBombs = original.TryGetComp<CompBombFlyer>().Props.numberBombs;
            thing.speed = original.TryGetComp<CompBombFlyer>().Props.speed;
            thing.radius = original.TryGetComp<CompBombFlyer>().Props.radiusOfDrop;
            thing.sound = original.TryGetComp<CompBombFlyer>().Props.soundFlyBy;

            double angle = pos.AngleThroughOrigin(map);
            if(pos.x == map.Size.x / 2 && pos.z == map.Size.z / 2)
                angle = 0;
            IntVec3 exitPoint = SPTrig.PointFromOrigin(angle, map);
            thing.angle = (float)(angle + 90) * -1;

            StartUp.SRTSBombers.Remove(idNumber);
            BomberSkyfaller bomber = (BomberSkyfaller)GenSpawn.Spawn(thing, exitPoint, map, WipeMode.Vanish);
            bomber.bombPos = pos;
            return bomber;
        }

        public static BomberSkyfaller SpawnSkyfaller(ThingDef skyfaller, IEnumerable<Thing> things, IntVec3 pos, Map map)
        {
            BomberSkyfaller thing = BomberSkyfallerMaker.MakeSkyfaller(skyfaller, things);
            return (BomberSkyfaller)GenSpawn.Spawn(thing, pos, map, WipeMode.Vanish);
        }

        
    }
}
