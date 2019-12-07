using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;

namespace SRTS
{
    public class CompBombFlyer : ThingComp
    {
        public Building SRTS_Launcher => this.parent as Building;

        public CompLaunchableSRTS CompLauncher => SRTS_Launcher.GetComp<CompLaunchableSRTS>();

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            if(SRTS_Launcher.GetComp<CompLaunchableSRTS>().LoadingInProgressOrReadyToLaunch)
            {
                yield break;
                yield return new Command_Action()
                {
                    defaultLabel = "Bomb Target",
                    defaultDesc = "Select a target on another map to drop bombs on.",
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
                        if (num < CompLauncher.SRTSProps.minPassengers)
                        {
                            Messages.Message("Not enough pilots to launch", MessageTypeDefOf.RejectInput, false);
                            return;
                        }
                        else if (num > CompLauncher.SRTSProps.maxPassengers)
                        {
                            Messages.Message("Too many colonists to launch", MessageTypeDefOf.RejectInput, false);
                            return;
                        }

                        if (CompLauncher.AnyInGroupHasAnythingLeftToLoad)
                        {
                            Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation("ConfirmSendNotCompletelyLoadedPods".Translate(CompLauncher.FirstThingLeftToLoadInGroup.LabelCapNoCount, 
                                CompLauncher.FirstThingLeftToLoadInGroup), new Action(this.StartChoosingDestinationBomb), false, null));
                        }
                        this.StartChoosingDestinationBomb();
                    }
                };
            }
        }

        private void StartChoosingDestinationBomb()
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
                if(num > CompLauncher.MaxLaunchDistance)
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
                Messages.Message("MessageTransportPodsDestinationIsTooFar".Translate(CompLaunchableSRTS.FuelNeededToLaunchAtDist((float)num).ToString("0.#")), MessageTypeDefOf.RejectInput, false);
                return false;
            }
            if(Find.WorldObjects.AnySettlementBaseAt(target.Tile))
            {
                MapParent targetMapParent = Find.WorldObjects.MapParentAt(target.Tile);
                if (TransportPodsArrivalAction_LandInSpecificCell.CanLandInSpecificCell(null, targetMapParent))
                {
                    Map targetMap = targetMapParent.Map;
                    Current.Game.CurrentMap = targetMap;
                    CameraJumper.TryHideWorld();
                    Find.Targeter.BeginTargeting(TargetingParameters.ForDropPodsDestination(), delegate (LocalTargetInfo x)
                    {
                        Log.Message("BOMB RUN");
                    }, null, delegate ()
                    {
                        if (Find.Maps.Contains(this.parent.Map))
                        {
                            Current.Game.CurrentMap = this.parent.Map;
                        }
                    }, Tex2D.LauncherTargeting);
                    return true;
                }
            }
            Messages.Message("Cannot Bomb map unless loaded.", MessageTypeDefOf.RejectInput, false);
            return false;
        }

        public static IEnumerable<FloatMenuOption> GetMapParent(
      MapParent mapparent,
      IEnumerable<IThingHolder> pods,
      CompLaunchableSRTS representative,
      Caravan car)
        {
            if (TransportPodsArrivalAction_LandInSpecificCell.CanLandInSpecificCell(pods, mapparent))
                yield return new FloatMenuOption("LandInExistingMap".Translate(mapparent.Label), (Action)(() =>
                {
                    Map myMap = car != null ? (Map)null : representative.parent.Map;
                    Current.Game.CurrentMap = mapparent.Map;
                    CameraJumper.TryHideWorld();
                    Find.Targeter.BeginTargeting(TargetingParameters.ForDropPodsDestination(), (Action<LocalTargetInfo>)(x => representative.TryLaunch(mapparent.Tile, (TransportPodsArrivalAction)new TransportPodsArrivalAction_LandInSpecificCell(mapparent, x.Cell), car)), (Pawn)null, (Action)(() =>
                    {
                        if (myMap == null || !Find.Maps.Contains(myMap))
                            return;
                        Current.Game.CurrentMap = myMap;
                    }), CompLaunchable.TargeterMouseAttachment);
                }), MenuOptionPriority.Default, (Action)null, (Thing)null, 0.0f, (Func<Rect, bool>)null, (WorldObject)null);
        }

        private void TryLaunchBombRun(int destTile)
        {
            if (!this.parent.Spawned)
            {
                Log.Error("Tried to launch " + this.parent + ", but it's unspawned.", false);
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

        }
    }
}
