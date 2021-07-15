using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using RimWorld.Planet;
using Verse;
using UnityEngine;

namespace SRTS
{
    public class TravelingSRTS : TravelingTransportPods
    {
        private Material SRTSMaterial
        {
            get
            {
                if(flyingThing is null)
                    return this.Material;
                if(material is null)
                {
                    string texPath = flyingThing.def.graphicData.texPath;
                    GraphicRequest graphicRequest = new GraphicRequest(flyingThing.def.graphicData.graphicClass, flyingThing.def.graphicData.texPath, ShaderTypeDefOf.Cutout.Shader, flyingThing.def.graphic.drawSize,
                       Color.white, Color.white, flyingThing.def.graphicData, 0, null, null);
                    if(graphicRequest.graphicClass == typeof(Graphic_Multi))
                        texPath += "_north";
                    material = MaterialPool.MatFrom(texPath, ShaderDatabase.WorldOverlayTransparentLit, WorldMaterials.WorldObjectRenderQueue);
                }
                return ExpandableWorldObjectsUtility.TransitionPct > 0 ? flyingThing.Graphic.MatNorth : material;
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref flyingThing, "flyingThing");
        }
        public override void Draw()
        {
            if(!SRTSMod.mod.settings.dynamicWorldDrawingSRTS)
            {
                base.Draw();
                return;
            }
            
            if (!this.HiddenBehindTerrainNow())
            {

                float averageTileSize = Find.WorldGrid.averageTileSize;
                float transitionPct = ExpandableWorldObjectsUtility.TransitionPct;
            
                if(transitionSize < 1)
                    transitionSize += TransitionTakeoff * (int)Find.TickManager.CurTimeSpeed;
                float drawPct = (1 + (transitionPct * Find.WorldCameraDriver.AltitudePercent * ExpandingResize)) * transitionSize;
                if(directionFacing == default)
                    InitializeFacing();
                
                Vector3 normalized = this.DrawPos.normalized;
                Quaternion quat = Quaternion.LookRotation(Vector3.Cross(normalized, directionFacing), normalized) * Quaternion.Euler(0f, 90f, 0f);
                Vector3 s = new Vector3(averageTileSize * 0.7f * drawPct, 5f, averageTileSize * 0.7f * drawPct);
                
                Matrix4x4 matrix = default;
                matrix.SetTRS(this.DrawPos + normalized * 0.015f, quat, s);
                int layer = WorldCameraManager.WorldLayer;
                Graphics.DrawMesh(MeshPool.plane10, matrix, this.SRTSMaterial, layer);
            }
        }

        private void InitializeFacing()
        {
            Vector3 tileLocation = Find.WorldGrid.GetTileCenter(this.destinationTile).normalized;
            directionFacing = (this.DrawPos - tileLocation).normalized;
        }

        public Thing flyingThing;

        private Material material;

        private const float ExpandingResize = 35f;

        private const float TransitionTakeoff = 0.015f;

        private float transitionSize = 0f;

        Vector3 directionFacing;
    }
}
