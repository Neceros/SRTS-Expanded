using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using Verse.Sound;
using UnityEngine;
using SPExtended;

namespace SRTS
{
    public class BombingTargeter
    {
        public bool IsTargeting => this.action != null;

        public void BeginTargeting(TargetingParameters targetParams, Action<IEnumerable<IntVec3>, Pair<IntVec3, IntVec3>> action, ThingDef bomber, BombingType bombType, Map map, Pawn caster = null, Action actionWhenFinished = null, Texture2D mouseAttachment = null)
        {
            this.action = action;
            this.targetParams = targetParams;
            this.caster = caster;
            this.actionWhenFinished = actionWhenFinished;
            this.mouseAttachment = mouseAttachment;
            this.selections = new List<LocalTargetInfo>();
            this.bomber = bomber;
            this.map = map;
            this.bombType = bombType;
        }

        public void StopTargeting()
        {
            if(this.actionWhenFinished != null)
            {
                Action action = this.actionWhenFinished;
                this.actionWhenFinished = null;
                action();
            }
            this.action = null;
            this.selections.Clear();
        }

        public void ProcessInputEvents()
        {
            this.ConfirmStillValid();
            if(this.IsTargeting)
            {
                if(Event.current.type == EventType.MouseDown && Event.current.button == 0)
                {
                    Event.current.Use();
                    if(selections.Count < 2 && this.action != null)
                    {
                        LocalTargetInfo obj = this.CurrentTargetUnderMouse();
                        if (obj.Cell.InBounds(map) && !selections.Contains(obj))
                            selections.Add(obj);
                        else
                        {
                            SoundDefOf.ClickReject.PlayOneShotOnCamera(null);
                            return;
                        }
                    }
                    SoundDefOf.Tick_High.PlayOneShotOnCamera(null);
                    if(selections.Count == 2)
                    {

                        this.action(this.BombCellsFinalized(), new Pair<IntVec3, IntVec3>(selections[0].Cell, selections[1].Cell));
                        this.StopTargeting();
                    }
                }
                if ((Event.current.type == EventType.MouseDown && Event.current.button == 1) || KeyBindingDefOf.Cancel.KeyDownEvent)
                {
                    if (selections.Any())
                        selections.RemoveLast();
                    else
                        this.StopTargeting();
                    SoundDefOf.CancelMode.PlayOneShotOnCamera(null);
                    Event.current.Use();
                }
            }
        }

        public void TargeterOnGUI()
        {
            if(this.action != null)
            {
                Texture2D icon = this.mouseAttachment ?? Tex2D.LauncherTargeting;
                GenUI.DrawMouseAttachment(icon);
            }
        }

        public void TargeterUpdate()
        {
            if(this.selections.Any())
            {
                GenDraw.DrawLineBetween(selections[0].CenterVector3, UI.MouseMapPosition().ToIntVec3().ToVector3Shifted(), SimpleColor.Red);
                this.DrawTargetingPoints();
            }
        }

        private void DrawTargetingPoints()
        {
            this.targetingLength = Vector3.Distance(selections[0].CenterVector3, UI.MouseMapPosition().ToIntVec3().ToVector3Shifted());
            GenDraw.DrawTargetHighlight(new LocalTargetInfo(selections[0].Cell));
            if(bombType == BombingType.carpet)
            {
                GenDraw.DrawRadiusRing(selections[0].Cell, SRTSMod.GetStatFor<int>(bomber.defName, StatName.radiusDrop));
                
                this.numRings = ((int)(targetingLength / SRTSMod.GetStatFor<float>(this.bomber.defName, StatName.distanceBetweenDrops))).Clamp<int>(0, SRTSMod.GetStatFor<int>(this.bomber.defName, StatName.numberBombs));

                if (SRTSMod.mod.settings.expandBombPoints && numRings >= 1)
                {
                    GenDraw.DrawRadiusRing(UI.MouseMapPosition().ToIntVec3(), SRTSMod.GetStatFor<int>(bomber.defName, StatName.radiusDrop));
                    GenDraw.DrawTargetHighlight(new LocalTargetInfo(UI.MouseMapPosition().ToIntVec3()));
                }
                for (int i = 1; i < numRings - (SRTSMod.mod.settings.expandBombPoints ? 1 : 0); i++)
                {
                    IntVec3 cellTargeted = this.TargeterToCell(i);
                    GenDraw.DrawRadiusRing(cellTargeted, SRTSMod.GetStatFor<int>(bomber.defName, StatName.radiusDrop));
                    GenDraw.DrawTargetHighlight(new LocalTargetInfo(cellTargeted));
                }
            }
            else if(bombType == BombingType.precise)
            {
                IntVec3 centeredTarget = this.TargeterCentered();
                GenDraw.DrawTargetHighlight(new LocalTargetInfo(centeredTarget));
                GenDraw.DrawTargetHighlight(new LocalTargetInfo(UI.MouseMapPosition().ToIntVec3()));
                GenDraw.DrawRadiusRing(centeredTarget, SRTSMod.GetStatFor<int>(bomber.defName, StatName.radiusDrop) * RadiusPreciseMultiplier);
            }
        }

        private IntVec3 TargeterToCell(int bombNumber)
        {
            IntVec3 mousePosition = UI.MouseMapPosition().ToIntVec3();
            IntVec3 targetedCell = new IntVec3(mousePosition.x, selections[0].Cell.y, mousePosition.z);
            double angle = selections[0].Cell.AngleToPoint(targetedCell);
            float distanceToNextBomb = SRTSMod.mod.settings.expandBombPoints ? this.targetingLength / (this.numRings - 1) * bombNumber : SRTSMod.GetStatFor<float>(this.bomber.defName, StatName.distanceBetweenDrops) * bombNumber;
            float xDiff = selections[0].Cell.x + Math.Sign(UI.MouseMapPosition().x - selections[0].CenterVector3.x) *
                (float)(distanceToNextBomb * Math.Cos(angle.DegreesToRadians()));
            float zDiff = selections[0].Cell.z + Math.Sign(UI.MouseMapPosition().z - selections[0].CenterVector3.z) *
                (float)(distanceToNextBomb * Math.Sin(angle.DegreesToRadians()));
            return new IntVec3((int)xDiff, 0, (int)zDiff);
        }

        private IntVec3 TargeterCentered()
        {
            IntVec3 mousePos = UI.MouseMapPosition().ToIntVec3();
            IntVec3 targetedCell = new IntVec3(mousePos.x, selections[0].Cell.y, mousePos.z);
            double angle = selections[0].Cell.AngleToPoint(mousePos);
            float xDiff = selections[0].Cell.x + Math.Sign(targetedCell.x - selections[0].CenterVector3.x) * (float)(this.targetingLength / 2 * Math.Cos(angle.DegreesToRadians()));
            float zDiff = selections[0].Cell.z + Math.Sign(targetedCell.z - selections[0].CenterVector3.z) * (float)(this.targetingLength / 2 * Math.Sin(angle.DegreesToRadians()));
            return new IntVec3((int)xDiff, 0, (int)zDiff);
        }

        private IEnumerable<IntVec3> BombCellsFinalized()
        {
            if(bombType == BombingType.carpet)
            {
                for (int i = 0; i < this.numRings; i++)
                    yield return this.TargeterToCell(i);
            }
            else if(bombType == BombingType.precise)
            {
                yield return TargeterCentered();
                yield break;
            }
        }

        private void ConfirmStillValid()
        {
            if(this.caster != null && (this.caster.Map != Find.CurrentMap || this.caster.Destroyed || !Find.Selector.IsSelected(this.caster)))
            {
                this.StopTargeting();
            }
        }

        private LocalTargetInfo CurrentTargetUnderMouse()
        {
            if(!this.IsTargeting)
                return LocalTargetInfo.Invalid;
            LocalTargetInfo localTarget = LocalTargetInfo.Invalid;
            using(IEnumerator<LocalTargetInfo> enumerator = GenUI.TargetsAtMouse(this.targetParams, false).GetEnumerator())
            {
                if(enumerator.MoveNext())
                {
                    LocalTargetInfo localTarget2 = enumerator.Current;
                    localTarget = localTarget2;
                }
            }
            return localTarget;
        }

        private List<LocalTargetInfo> selections = new List<LocalTargetInfo>();

        private Action<IEnumerable<IntVec3>, Pair<IntVec3, IntVec3>> action;

        private Pawn caster;

        private TargetingParameters targetParams;

        private Action actionWhenFinished;

        private Texture2D mouseAttachment;

        private ThingDef bomber;

        private BombingType bombType;

        private float targetingLength;

        private int numRings;

        private Map map;

        private const float RadiusPreciseMultiplier = 0.6f;
    }
}
