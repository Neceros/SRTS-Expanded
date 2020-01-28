using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using RimWorld;
using UnityEngine;
namespace SRTS
{
    public class FloatMenuGizmo : FloatMenu
    {
        public FloatMenuGizmo(List<FloatMenuOption> options, Thing srtsSelected, string title, Vector3 clickPos) : base(options, title, false)
        {
            this.clickPos = clickPos;
            this.srtsSelected = srtsSelected;
        }

        public override void DoWindowContents(Rect rect)
        {
            if(srtsSelected is null || srtsSelected.Destroyed)
            {
                Find.WindowStack.TryRemove(this, true);
                return;
            }
            if (Time.frameCount % RevalidateEveryFrame == 0)
            {
                for (int i = 0; i < this.options.Count; i++)
                {
                    /*if(!this.options[i].Disabled && !FloatMenuGizmo.StillValid(this.options[i], this.selPawns, this.clickedPawn))
                    {
                        this.options[i].Disabled = true;
                    }*/
                }
            }
            base.DoWindowContents(rect);
        }

        private Vector3 clickPos;

        private Thing srtsSelected;

        public const int RevalidateEveryFrame = 3;
    }
}
