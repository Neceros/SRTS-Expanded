using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using Verse;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;

namespace SRTS
{
    public enum BombingType { carpet, precise, missile }
    public class CompBombFlyer : ThingComp
    {
        public Building SRTS_Launcher => this.parent as Building;
        public CompLaunchableSRTS CompLauncher => SRTS_Launcher.GetComp<CompLaunchableSRTS>();
        public CompProperties_BombsAway Props => (CompProperties_BombsAway)this.props;
        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            if(SRTS_Launcher.GetComp<CompLaunchableSRTS>().LoadingInProgressOrReadyToLaunch)
            {
                yield return new Command_Action()
                {
                    defaultLabel = "BombTarget".Translate(),
                    defaultDesc = "BombTargetDesc".Translate(),
                    icon = TexCommand.Attack,
                    action = delegate()
                    {
                        int num = 0;
                        foreach (Thing t in CompLauncher.Transporter.innerContainer)
                        {
                            if (t is Pawn && (t as Pawn).IsColonist)
                            {
                                num++;
                            }
                        }
                        if(SRTSMod.mod.settings.passengerLimits)
                        {
                            if (num < SRTSMod.GetStatFor<int>(this.parent.def.defName, StatName.minPassengers))
                            {
                                Messages.Message("NotEnoughPilots".Translate(), MessageTypeDefOf.RejectInput, false);
                                return;
                            }
                            else if (num > SRTSMod.GetStatFor<int>(this.parent.def.defName, StatName.maxPassengers))
                            {
                                Messages.Message("TooManyPilots".Translate(), MessageTypeDefOf.RejectInput, false);
                                return;
                            }
                        }
                        
                        FloatMenuOption carpetBombing = new FloatMenuOption("CarpetBombing".Translate(), delegate ()
                        {
                            bombType = BombingType.carpet;
                            if (CompLauncher.AnyInGroupHasAnythingLeftToLoad)
                            {
                                Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation("ConfirmSendNotCompletelyLoadedPods".Translate(CompLauncher.FirstThingLeftToLoadInGroup.LabelCapNoCount,
                                    CompLauncher.FirstThingLeftToLoadInGroup), StartChoosingDestinationBomb));
                            }
                            this.StartChoosingDestinationBomb();
                        });
                        FloatMenuOption preciseBombing = new FloatMenuOption("PreciseBombing".Translate(), delegate ()
                        {
                            bombType = BombingType.precise;
                            if (CompLauncher.AnyInGroupHasAnythingLeftToLoad)
                            {
                                Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation("ConfirmSendNotCompletelyLoadedPods".Translate(CompLauncher.FirstThingLeftToLoadInGroup.LabelCapNoCount,
                                    CompLauncher.FirstThingLeftToLoadInGroup), StartChoosingDestinationBomb));
                            }
                            this.StartChoosingDestinationBomb();
                        });
                        Find.WindowStack.Add(new FloatMenuGizmo(new List<FloatMenuOption>() { carpetBombing, preciseBombing }, this.parent, this.parent.LabelCap, UI.MouseMapPosition()));
                    }
                };
            }
        }

        public void StartChoosingDestinationBomb()
        {
            CameraJumper.TryJump(CameraJumper.GetWorldTarget(this.parent));
            Find.WorldSelector.ClearSelection();
            int tile = this.parent.Map.Tile;

            Find.WorldTargeter.BeginTargeting(new Func<GlobalTargetInfo, bool>(this.ChoseWorldTargetToBomb), false, Tex2D.LauncherTargeting, true, delegate
            {
                GenDraw.DrawWorldRadiusRing(tile, CompLauncher.MaxLaunchDistance);
            }, delegate (GlobalTargetInfo target)
            {
                if (!target.IsValid)
                    return null;
                int num = Find.WorldGrid.TraversalDistanceBetween(tile, target.Tile);
                if (num > CompLauncher.MaxLaunchDistance)
                {
                    GUI.color = Color.red;
                    if (num > CompLauncher.MaxLaunchDistanceEverPossible)
                    {
                        return "TransportPodDestinationBeyondMaximumRange".Translate();
                    }
                    return "TransportPodNotEnoughFuel".Translate();
                }
                else
                {
                    IEnumerable<FloatMenuOption> transportPodsFloatMenuOptionsAt = CompLauncher.GetTransportPodsFloatMenuOptionsAt(target.Tile);
                    if (!transportPodsFloatMenuOptionsAt.Any<FloatMenuOption>())
                    {
                        return string.Empty;
                    }
                    if (transportPodsFloatMenuOptionsAt.Count<FloatMenuOption>() == 1)
                    {
                        if (transportPodsFloatMenuOptionsAt.First<FloatMenuOption>().Disabled)
                        {
                            GUI.color = Color.red;
                        }
                        return transportPodsFloatMenuOptionsAt.First<FloatMenuOption>().Label;
                    }
                    MapParent mapParent = target.WorldObject as MapParent;
                    if (mapParent != null)
                    {
                        return "ClickToSeeAvailableOrders_WorldObject".Translate(mapParent.LabelCap); //change
                    }
                    return "ClickToSeeAvailableOrders_Empty".Translate(); //change here
                }
            });
        }

        private bool ChoseWorldTargetToBomb(GlobalTargetInfo target)
        {
            if(!target.IsValid)
            {
                Messages.Message("MessageTransportPodsDestinationIsInvalid".Translate(), MessageTypeDefOf.RejectInput, false);
                return false;
            }
            
            int num = Find.WorldGrid.TraversalDistanceBetween(this.parent.Map.Tile, target.Tile);
            if(num > CompLauncher.MaxLaunchDistance)
            {
                Messages.Message("MessageTransportPodsDestinationIsTooFar".Translate(CompLaunchableSRTS.FuelNeededToLaunchAtDist((float)num, this.parent.GetComp<CompLaunchableSRTS>().BaseFuelPerTile).ToString("0.#")), MessageTypeDefOf.RejectInput, false);
                return false;
            }
            if(Find.WorldObjects.AnyMapParentAt(target.Tile))
            {
                MapParent targetMapParent = Find.WorldObjects.MapParentAt(target.Tile);
                if(SRTSArrivalActionBombRun.CanBombSpecificCell(null, targetMapParent))
                {
                    Map targetMap = targetMapParent.Map;
                    Current.Game.CurrentMap = targetMap;
                    CameraJumper.TryHideWorld();

                    TargetingParameters bombingTargetingParams = new TargetingParameters();
                    bombingTargetingParams.canTargetLocations = true;
                    bombingTargetingParams.canTargetSelf = false;
                    bombingTargetingParams.canTargetPawns = false;
                    bombingTargetingParams.canTargetFires = true;
                    bombingTargetingParams.canTargetBuildings = true;
                    bombingTargetingParams.canTargetItems = true;
                    bombingTargetingParams.validator = ((TargetInfo x) => x.Cell.InBounds(targetMap) && (!x.Cell.GetRoof(targetMap)?.isThickRoof ?? true));

                    SRTSHelper.targeter.BeginTargeting(bombingTargetingParams, delegate (IEnumerable<IntVec3> cells, Pair<IntVec3, IntVec3> targetPoints)
                    {
                        TryLaunchBombRun(target.Tile, targetPoints, cells, targetMapParent);
                    }, this.parent.def, bombType, targetMap, null, delegate ()
                    {
                        if (Find.Maps.Contains(this.parent.Map))
                        {
                            Current.Game.CurrentMap = this.parent.Map;
                        }
                    }, Tex2D.LauncherTargeting);
                    return true;
                }
            }
            Messages.Message("CannotBombMap".Translate(), MessageTypeDefOf.RejectInput, false);
            return false;
        }

        private void TryLaunchBombRun(int destTile, Pair<IntVec3, IntVec3> targetPoints, IEnumerable<IntVec3> bombCells, MapParent mapParent)
        {
            if (!this.parent.Spawned)
            {
                Log.Error("Tried to launch " + this.parent + ", but it's unspawned.");
                return;
            }

            if (!CompLauncher.LoadingInProgressOrReadyToLaunch || !CompLauncher.AllInGroupConnectedToFuelingPort || !CompLauncher.AllFuelingPortSourcesInGroupHaveAnyFuel)
            {
                return;
            }

            Map map = this.parent.Map;
            int num = Find.WorldGrid.TraversalDistanceBetween(map.Tile, destTile);
            if(num > CompLauncher.MaxLaunchDistance)
            {
                return;
            }
            CompLauncher.Transporter.TryRemoveLord(map);
            int groupID = CompLauncher.Transporter.groupID;
            float amount = Mathf.Max(CompLaunchableSRTS.FuelNeededToLaunchAtDist((float)num, this.parent.GetComp<CompLaunchableSRTS>().BaseFuelPerTile), 1f);
            CompTransporter comp1 = CompLauncher.FuelingPortSource.TryGetComp<CompTransporter>();
            Building fuelPortSource = CompLauncher.FuelingPortSource;
            if (fuelPortSource != null)
                fuelPortSource.TryGetComp<CompRefuelable>().ConsumeFuel(amount);
            ThingOwner directlyHeldThings = comp1.GetDirectlyHeldThings();

            Thing thing = ThingMaker.MakeThing(ThingDef.Named(parent.def.defName), null);
            thing.SetFactionDirect(Faction.OfPlayer);
            thing.Rotation = CompLauncher.FuelingPortSource.Rotation;
            CompRefuelable comp2 = thing.TryGetComp<CompRefuelable>();
            comp2.GetType().GetField("fuel", BindingFlags.Instance | BindingFlags.NonPublic).SetValue((object)comp2, (object)fuelPortSource.TryGetComp<CompRefuelable>().Fuel);
            comp2.TargetFuelLevel = fuelPortSource.TryGetComp<CompRefuelable>().TargetFuelLevel;
            thing.stackCount = 1;
            directlyHeldThings.TryAddOrTransfer(thing, true);

            ActiveDropPod activeDropPod = (ActiveDropPod)ThingMaker.MakeThing(ThingDef.Named(parent.def.defName + "_Active"), null);
            activeDropPod.Contents = new ActiveDropPodInfo();
            activeDropPod.Contents.innerContainer.TryAddRangeOrTransfer((IEnumerable<Thing>)directlyHeldThings, true, true);

            SRTSLeaving srtsLeaving = (SRTSLeaving)SkyfallerMaker.MakeSkyfaller(ThingDef.Named(parent.def.defName + "_Leaving"), (Thing)activeDropPod);
            srtsLeaving.rotation = CompLauncher.FuelingPortSource.Rotation;
            srtsLeaving.groupID = groupID;
            srtsLeaving.destinationTile = destTile;
            srtsLeaving.arrivalAction = new SRTSArrivalActionBombRun(mapParent, targetPoints, bombCells, this.bombType, map, CompLauncher.FuelingPortSource.Position);

            comp1.CleanUpLoadingVars(map);
            IntVec3 position = fuelPortSource.Position;
            SRTSStatic.SRTSDestroy((Thing)fuelPortSource, DestroyMode.Vanish);
            GenSpawn.Spawn((Thing)srtsLeaving, position, map, WipeMode.Vanish);
            CameraJumper.TryHideWorld();
        }

        public BombingType bombType;
    }
}
