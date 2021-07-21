﻿using System;
using Verse;

namespace SPExtended
{
    public static class SPTrig
    {
        /// <summary>
        /// Rotate point clockwise by angle theta
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="theta"></param>
        /// <returns></returns>
        public static SPTuples.SPTuple2<float, float> RotatePointClockwise(float x, float y, float theta)
        {
            theta = -theta;
            float xPrime = (float)(x * Math.Cos(theta.DegreesToRadians())) - (float)(y * Math.Sin(theta.DegreesToRadians()));
            float yPrime = (float)(x * Math.Sin(theta.DegreesToRadians())) + (float)(y * Math.Cos(theta.DegreesToRadians()));
            return new SPTuples.SPTuple2<float, float>(xPrime, yPrime);
        }

        /// <summary>
        /// Rotate point counter clockwise by angle theta
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="theta"></param>
        /// <returns></returns>
        public static SPTuples.SPTuple2<float, float> RotatePointCounterClockwise(float x, float y, float theta)
        {
            float xPrime = (float)(x * Math.Cos(theta.DegreesToRadians())) - (float)(y * Math.Sin(theta.DegreesToRadians()));
            float yPrime = (float)(x * Math.Sin(theta.DegreesToRadians())) + (float)(y * Math.Cos(theta.DegreesToRadians()));
            return new SPTuples.SPTuple2<float, float>(xPrime, yPrime);
        }

        /// <summary>
        /// Convert degrees (double) to radians
        /// </summary>
        /// <param name="deg"></param>
        /// <returns></returns>
        public static double DegreesToRadians(this double deg)
        {
            return deg * Math.PI / 180;
        }

        /// <summary>
        /// Convert degrees (float) to radians
        /// </summary>
        /// <param name="deg"></param>
        /// <returns></returns>
        public static double DegreesToRadians(this float deg)
        {
            return Convert.ToDouble(deg).DegreesToRadians();
        }

        /// <summary>
        /// Convert Radians to degrees
        /// </summary>
        /// <param name="angle"></param>
        /// <returns></returns>
        public static double RadiansToDegrees(this double radians)
        {
            return radians * (180 / Math.PI);
        }

        /// <summary>
        /// Calculate angle from origin to point on map relative to positive x axis
        /// </summary>
        /// <param name="c"></param>
        /// <param name="map"></param>
        /// <returns></returns>
        public static double AngleThroughOrigin(this IntVec3 c, Map map)
        {
            int xPrime = c.x - (map.Size.x / 2);
            int yPrime = c.z - (map.Size.z / 2);
            double slope = (double)yPrime / (double)xPrime;
            double angleRadians = Math.Atan(slope);
            double angle = Math.Abs(angleRadians.RadiansToDegrees());
            switch (SPExtra.Quadrant.QuadrantOfIntVec3(c, map).AsInt)
            {
                case 2:
                    return 360 - angle;

                case 3:
                    return 180 + angle;

                case 4:
                    return 180 - angle;
            }
            return angle;
        }

        /// <summary>
        /// Calculate angle between 2 points on Cartesian coordinate plane.
        /// </summary>
        /// <param name="pos"></param>
        /// <param name="point"></param>
        /// <returns></returns>
        public static double AngleToPoint(this IntVec3 pos, IntVec3 point)
        {
            int xPrime = pos.x - point.x;
            int yPrime = pos.z - point.z;
            double slope = (double)yPrime / (double)xPrime;
            double angleRadians = Math.Atan(slope);
            double angle = Math.Abs(angleRadians.RadiansToDegrees());
            return angle;
        }

        /// <summary>
        /// Calculate angle between 2 points on Cartesian coordinate plane relative to positive x axis.
        /// </summary>
        /// <param name="pos"></param>
        /// <param name="point"></param>
        /// <returns></returns>
        public static double AngleToPointRelative(this IntVec3 start, IntVec3 end)
        {
            int xPrime = start.x - end.x;
            int yPrime = start.z - end.z;
            double slope = (double)yPrime / (double)xPrime;
            double angleRadians = Math.Atan(slope);
            double angle = Math.Abs(angleRadians.RadiansToDegrees());

            //Opposite of the sign of the slope
            int xDir = Math.Sign((double)xPrime) * -1;
            int yDir = Math.Sign((double)yPrime) * -1;

            //Horizontal
            if (start.z == end.z)
            {
                if (start.x < end.x)
                    return 0;
                else
                    return 180;
            }
            //Vertical
            else if (start.x == end.x)
            {
                if (start.z < end.z)
                    return 90;
                else
                    return 270;
            }

            //Q4
            if (xDir == -1 && yDir == 1)
                angle = 180 - angle;
            //Q3
            else if (xDir == -1 && yDir == -1)
                angle += 180;
            //Q2
            else if (xDir == 1 && yDir == -1)
                angle = 90 - angle + 270;
            //Do nothing if Q1
            return angle;
        }

        /// <summary>
        /// Determine whether point C is left or right of the line from point A looking towards point B
        /// </summary>
        /// <param name="A"></param>
        /// <param name="B"></param>
        /// <param name="C"></param>
        /// <returns></returns>
        public static int LeftRightOfLine(IntVec3 A, IntVec3 B, IntVec3 C)
        {
            return Math.Sign((B.x - A.x) * (C.z - A.z) - (B.z - A.z) * (C.x - A.x));
        }

        /// <summary>
        /// Get point on edge of square map given angle (0 to 360) relative to x axis from origin
        /// </summary>
        /// <param name="angle"></param>
        /// <param name="map"></param>
        /// <returns></returns>
        public static IntVec3 PointFromOrigin(double angle, Map map)
        {
            int a = map.Size.x;
            int b = map.Size.z;

            if (angle < 0 || angle > 360)
                return IntVec3.Invalid;

            Rot4 rayDir = Rot4.Invalid;
            if (angle <= 45 || angle > 315)
                rayDir = Rot4.East;
            else if (angle <= 135 && angle >= 45)
                rayDir = Rot4.North;
            else if (angle <= 225 && angle >= 135)
                rayDir = Rot4.West;
            else if (angle <= 315 && angle >= 225)
                rayDir = Rot4.South;
            else
                return new IntVec3(b / 2, 0, 1);
            var v = Math.Tan(angle.DegreesToRadians());
            switch (rayDir.AsInt)
            {
                case 0: //North
                    return new IntVec3((int)(b / (2 * v) + b / 2), 0, b - 1);

                case 1: //East
                    return new IntVec3(a - 1, 0, (int)(a / 2 * v) + a / 2);

                case 2: //South
                    return new IntVec3((int)(b - (b / (2 * v) + b / 2)), 0, 1);

                case 3: //West
                    return new IntVec3(1, 0, (int)(a - ((a / 2 * v) + a / 2)));
            }

            return IntVec3.Invalid;
        }

        public static IntVec3 ExitPointCustom(double angle, IntVec3 start, Map map)
        {
            if (angle < 0 || angle > 360)
                return IntVec3.Invalid;

            Rot4 rayDir = Rot4.Invalid;
            if (angle <= start.AngleToPointRelative(map.Size) || angle > start.AngleToPointRelative(new IntVec3(map.Size.x, 0, 0)))
                rayDir = Rot4.East;
            else if (angle <= start.AngleToPointRelative(new IntVec3(0, 0, map.Size.z)) && angle >= start.AngleToPointRelative(map.Size))
                rayDir = Rot4.North;
            else if (angle <= start.AngleToPointRelative(IntVec3.Zero) && angle >= start.AngleToPointRelative(new IntVec3(0, 0, map.Size.z)))
                rayDir = Rot4.West;
            else if (angle <= start.AngleToPointRelative(new IntVec3(map.Size.x, 0, 0)) && angle >= start.AngleToPointRelative(IntVec3.Zero))
                rayDir = Rot4.South;

            switch (rayDir.AsInt)
            {
                case 0: //North
                    return new IntVec3((int)((map.Size.z - start.z) / Math.Tan((angle > 90 ? (-(180 - angle)) : angle).DegreesToRadians())) + start.x, 0, map.Size.z - 1);

                case 1: //East
                    return new IntVec3(map.Size.x - 1, 0, (int)((map.Size.x - start.x) * Math.Tan(angle.DegreesToRadians())) + start.z);

                case 2: //South
                    return new IntVec3(start.x + (angle > 270 ? 1 : -1) * (int)(start.z * Math.Tan((angle > 270 ? angle - 270 : 270 - angle).DegreesToRadians())), 0, 0);

                case 3: //West
                    return new IntVec3(0, 0, start.z + (angle > 180 ? -1 : 1) * (int)(start.x * Math.Tan((angle > 180 ? (angle - 180) : (180 - angle)).DegreesToRadians())));
            }
            return IntVec3.Invalid;
        }
    }
}