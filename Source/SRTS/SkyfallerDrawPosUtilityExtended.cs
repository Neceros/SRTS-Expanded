using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;
using RimWorld;

namespace SRTS
{
    public static class SkyfallerDrawPosUtilityExtended
    {
        public static Vector3 DrawPos_AccelerateSRTSDirectional(Vector3 center, int ticksToImpact, Rot4 direction, float speed)
        {
            ticksToImpact = Mathf.Max(ticksToImpact, 0);
            float dist = Mathf.Pow((float)ticksToImpact, 0.95f) * 1.7f * speed;
            return SRTSPosAtDist(center, dist, direction);
        }

        public static Vector3 DrawPos_TakeoffUpward(Vector3 center, int ticksTillTakeoff)
        {
            Vector3 pos = new Vector3(center.x, center.y, center.z + (0.03f * ticksTillTakeoff));
            return pos;
        }

        private static Vector3 SRTSPosAtDist(Vector3 center, float dist, Rot4 dir)
        {
            Vector2 angle = dir.AsVector2 * dist;
            Vector3 pos = center + new Vector3(angle.x, 0, angle.y);
            return pos;
        }
    }
}
