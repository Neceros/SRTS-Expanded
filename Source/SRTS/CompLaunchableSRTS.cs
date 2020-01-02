using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Verse;

namespace SRTS
{
  [StaticConstructorOnStartup]
  public class CompLaunchableSRTS : ThingComp
  {
    public static readonly Texture2D TargeterMouseAttachment = ContentFinder<Texture2D>.Get("UI/Overlays/LaunchableMouseAttachment", true);
    private static readonly Texture2D LaunchCommandTex = ContentFinder<Texture2D>.Get("UI/Commands/LaunchShip", true);
    private CompTransporter cachedCompTransporter;
    private const float FuelPerTile = 2.25f;
    private Caravan carr;

    public CompProperties_LaunchableSRTS SRTSProps => (CompProperties_LaunchableSRTS)this.props;
    public Building FuelingPortSource
    {
      get
      {
        return (Building) this.parent;
      }
    }

    public bool ConnectedToFuelingPort
    {
      get
      {
        return this.FuelingPortSource != null;
      }
    }

    public bool FuelingPortSourceHasAnyFuel
    {
      get
      {
        return this.ConnectedToFuelingPort && this.FuelingPortSource.GetComp<CompRefuelable>().HasFuel;
      }
    }

    public bool LoadingInProgressOrReadyToLaunch
    {
      get
      {
        return this.Transporter.LoadingInProgressOrReadyToLaunch;
      }
    }

    public bool AnythingLeftToLoad
    {
      get
      {
        return this.Transporter.AnythingLeftToLoad;
      }
    }

    public Thing FirstThingLeftToLoad
    {
      get
      {
        return this.Transporter.FirstThingLeftToLoad;
      }
    }

    public List<CompTransporter> TransportersInGroup
    {
      get
      {
        return new List<CompTransporter>()
        {
          this.parent.TryGetComp<CompTransporter>()
        };
      }
    }

    public bool AnyInGroupHasAnythingLeftToLoad
    {
      get
      {
        return this.Transporter.AnyInGroupHasAnythingLeftToLoad;
      }
    }

    public Thing FirstThingLeftToLoadInGroup
    {
      get
      {
        return this.Transporter.FirstThingLeftToLoadInGroup;
      }
    }

    public bool AnyInGroupIsUnderRoof
    {
      get
      {
        List<CompTransporter> transportersInGroup = this.TransportersInGroup;
        for (int index = 0; index < transportersInGroup.Count; ++index)
        {
          if (transportersInGroup[index].parent.Position.Roofed(this.parent.Map))
            return true;
        }
        return false;
      }
    }

    public CompTransporter Transporter
    {
      get
      {
        if (this.cachedCompTransporter == null)
          this.cachedCompTransporter = this.parent.GetComp<CompTransporter>();
        return this.cachedCompTransporter;
      }
    }

    public float FuelingPortSourceFuel
    {
      get
      {
        if (!this.ConnectedToFuelingPort)
          return 0.0f;
        return this.parent.GetComp<CompRefuelable>().Fuel;
      }
    }

    public bool AllInGroupConnectedToFuelingPort
    {
      get
      {
        return true;
      }
    }

    public bool AllFuelingPortSourcesInGroupHaveAnyFuel
    {
      get
      {
        return true;
      }
    }

    private float FuelInLeastFueledFuelingPortSource
    {
      get
      {
        float num = 0.0f;
        bool flag = false;
        float fuelingPortSourceFuel = this.FuelingPortSourceFuel;
        if (!flag || (double) fuelingPortSourceFuel < (double) num)
        {
          num = fuelingPortSourceFuel;
          flag = true;
        }
        if (!flag)
          return 0.0f;
        return num;
      }
    }

    public int MaxLaunchDistance
    {
      get
      {
        if (this.parent.Spawned && !this.LoadingInProgressOrReadyToLaunch)
          return 0;
        return CompLaunchableSRTS.MaxLaunchDistanceAtFuelLevel(this.FuelInLeastFueledFuelingPortSource);
      }
    }

    public int MaxLaunchDistanceEverPossible
    {
      get
      {
        if (!this.LoadingInProgressOrReadyToLaunch)
          return 0;
        float num = 0.0f;
        Building fuelingPortSource = this.FuelingPortSource;
        if (fuelingPortSource != null)
          num = Mathf.Max(num, fuelingPortSource.GetComp<CompRefuelable>().Props.fuelCapacity);
        return CompLaunchableSRTS.MaxLaunchDistanceAtFuelLevel(num);
      }
    }

    private bool PodsHaveAnyPotentialCaravanOwner
    {
      get
      {
        List<CompTransporter> transportersInGroup = this.TransportersInGroup;
        for (int index1 = 0; index1 < transportersInGroup.Count; ++index1)
        {
          ThingOwner innerContainer = transportersInGroup[index1].innerContainer;
          for (int index2 = 0; index2 < innerContainer.Count; ++index2)
          {
            Pawn pawn = innerContainer[index2] as Pawn;
            if (pawn != null && CaravanUtility.IsOwner(pawn, Faction.OfPlayer))
              return true;
          }
        }
        return false;
      }
    }

    public void AddThingsToSRTS(Thing thing)
    {
        thingsInsideShip.Add(thing);
    }

    public override IEnumerable<Gizmo> CompGetGizmosExtra()
    {
      foreach (Gizmo gizmo in base.CompGetGizmosExtra())
      {
        Gizmo g = gizmo;
        yield return g;
        g = (Gizmo) null;
      }
      if (this.LoadingInProgressOrReadyToLaunch)
      {
        Command_Action launch = new Command_Action();
        launch.defaultLabel = "CommandLaunchGroup".Translate();
        launch.defaultDesc = "CommandLaunchGroupDesc".Translate();
        launch.icon = CompLaunchableSRTS.LaunchCommandTex;
        launch.alsoClickIfOtherInGroupClicked = false;
        launch.action = (Action) (() =>
        {
            int num = 0;
            foreach(Thing t in this.Transporter.innerContainer)
            {
                if(t is Pawn && (t as Pawn).IsColonist)
                {
                    num++;
                }
            }
            if(SRTSMod.mod.settings.passengerLimits)
            {
                if (num < this.SRTSProps.minPassengers)
                {
                    Messages.Message("Not enough pilots to launch", MessageTypeDefOf.RejectInput, false);
                    return;
                }
                else if (num > this.SRTSProps.maxPassengers)
                {
                    Messages.Message("Too many colonists to launch", MessageTypeDefOf.RejectInput, false);
                    return;
                }
            }

          if (this.AnyInGroupHasAnythingLeftToLoad)
            Find.WindowStack.Add((Window) Dialog_MessageBox.CreateConfirmation("ConfirmSendNotCompletelyLoadedPods".Translate(this.FirstThingLeftToLoadInGroup.LabelCapNoCount), new Action(this.StartChoosingDestination), false, (string) null));
          else
            this.StartChoosingDestination();
        });
        if (!this.AllInGroupConnectedToFuelingPort)
          launch.Disable("CommandLaunchGroupFailNotConnectedToFuelingPort".Translate());
        else if (!this.AllFuelingPortSourcesInGroupHaveAnyFuel)
          launch.Disable("CommandLaunchGroupFailNoFuel".Translate());
        else if (this.AnyInGroupIsUnderRoof)
          launch.Disable("CommandLaunchGroupFailUnderRoof".Translate());
        yield return (Gizmo) launch;
        launch = (Command_Action) null;
      }
    }

    public override string CompInspectStringExtra()
    {
      if (!this.LoadingInProgressOrReadyToLaunch)
        return (string) null;
      if (!this.AllInGroupConnectedToFuelingPort)
        return "NotReadyForLaunch".Translate() + ": " + "NotAllInGroupConnectedToFuelingPort".Translate() + ".";
      if (!this.AllFuelingPortSourcesInGroupHaveAnyFuel)
        return "NotReadyForLaunch".Translate() + ": " + "NotAllFuelingPortSourcesInGroupHaveAnyFuel".Translate() + ".";
      if (this.AnyInGroupHasAnythingLeftToLoad)
        return "NotReadyForLaunch".Translate() + ": " + "TransportPodInGroupHasSomethingLeftToLoad".Translate() + ".";
      return "ReadyForLaunch".Translate();
    }

    private void StartChoosingDestination()
    {
      CameraJumper.TryJump(CameraJumper.GetWorldTarget((GlobalTargetInfo) ((Thing) this.parent)));
      Find.WorldSelector.ClearSelection();
      int tile = this.parent.Map.Tile;
      this.carr = (Caravan) null;
      Find.WorldTargeter.BeginTargeting(new Func<GlobalTargetInfo, bool>(this.ChoseWorldTarget), true, CompLaunchableSRTS.TargeterMouseAttachment, true, (Action) (() => GenDraw.DrawWorldRadiusRing(tile, this.MaxLaunchDistance)), (Func<GlobalTargetInfo, string>) (target =>
      {
        if (!target.IsValid)
          return (string) null;
        int num = Find.WorldGrid.TraversalDistanceBetween(tile, target.Tile, true, int.MaxValue);
        if (num > this.MaxLaunchDistance)
        {
          GUI.color = Color.red;
          if (num > this.MaxLaunchDistanceEverPossible)
            return "TransportPodDestinationBeyondMaximumRange".Translate();
          return "TransportPodNotEnoughFuel".Translate();
        }
        IEnumerable<FloatMenuOption> floatMenuOptionsAt = this.GetTransportPodsFloatMenuOptionsAt(target.Tile, (Caravan) null);
        if (!floatMenuOptionsAt.Any<FloatMenuOption>())
        {
          if (Find.WorldGrid[target.Tile].biome.impassable || Find.World.Impassable(target.Tile))
            return "MessageTransportPodsDestinationIsInvalid".Translate();
          return string.Empty;
        }
        if (floatMenuOptionsAt.Count<FloatMenuOption>() == 1)
        {
          if (floatMenuOptionsAt.First<FloatMenuOption>().Disabled)
            GUI.color = Color.red;
          return floatMenuOptionsAt.First<FloatMenuOption>().Label;
        }
        MapParent worldObject = target.WorldObject as MapParent;
        if (worldObject == null)
          return "ClickToSeeAvailableOrders_Empty".Translate();
        return "ClickToSeeAvailableOrders_WorldObject".Translate(worldObject.LabelCap);
      }));
    }

    public void WorldStartChoosingDestination(Caravan car)
    {
      CameraJumper.TryJump(CameraJumper.GetWorldTarget((GlobalTargetInfo) ((WorldObject) car)));
      Find.WorldSelector.ClearSelection();
      int tile = car.Tile;
      this.carr = car;
      Find.WorldTargeter.BeginTargeting(new Func<GlobalTargetInfo, bool>(this.ChoseWorldTarget), true, CompLaunchableSRTS.TargeterMouseAttachment, false, (Action) (() => GenDraw.DrawWorldRadiusRing(car.Tile, this.MaxLaunchDistance)), (Func<GlobalTargetInfo, string>) (target =>
      {
        if (!target.IsValid)
          return (string) null;
        int num = Find.WorldGrid.TraversalDistanceBetween(tile, target.Tile, true, int.MaxValue);
        if (num > this.MaxLaunchDistance)
        {
          GUI.color = Color.red;
          if (num > this.MaxLaunchDistanceEverPossible)
            return "TransportPodDestinationBeyondMaximumRange".Translate();
          return "TransportPodNotEnoughFuel".Translate();
        }
        IEnumerable<FloatMenuOption> floatMenuOptionsAt = this.GetTransportPodsFloatMenuOptionsAt(target.Tile, car);
        if (!floatMenuOptionsAt.Any<FloatMenuOption>())
        {
          if (Find.WorldGrid[target.Tile].biome.impassable || Find.World.Impassable(target.Tile))
            return "MessageTransportPodsDestinationIsInvalid".Translate();
          return string.Empty;
        }
        if (floatMenuOptionsAt.Count<FloatMenuOption>() == 1)
        {
          if (floatMenuOptionsAt.First<FloatMenuOption>().Disabled)
            GUI.color = Color.red;
          return floatMenuOptionsAt.First<FloatMenuOption>().Label;
        }
        MapParent worldObject = target.WorldObject as MapParent;
        if (worldObject == null)
          return "ClickToSeeAvailableOrders_Empty".Translate();
        return "ClickToSeeAvailableOrders_WorldObject".Translate(worldObject.LabelCap);
      }));
    }

    private bool ChoseWorldTarget(GlobalTargetInfo target)
    {
      if (this.carr == null && !this.LoadingInProgressOrReadyToLaunch)
        return true;
      if (!target.IsValid)
      {
        Messages.Message("MessageTransportPodsDestinationIsInvalid".Translate(), MessageTypeDefOf.RejectInput, false);
        return false;
      }
      int num = Find.WorldGrid.TraversalDistanceBetween(this.carr != null ? this.carr.Tile : this.parent.Map.Tile, target.Tile, true, int.MaxValue);
      if (num > this.MaxLaunchDistance)
      {
        Messages.Message("MessageTransportPodsDestinationIsTooFar".Translate(CompLaunchableSRTS.FuelNeededToLaunchAtDist((float) num).ToString("0.#")), MessageTypeDefOf.RejectInput, false);
        return false;
      }
      if (Find.WorldGrid[target.Tile].biome.impassable || Find.World.Impassable(target.Tile))
      {
        Messages.Message("MessageTransportPodsDestinationIsInvalid".Translate(), MessageTypeDefOf.RejectInput, false);
        return false;
      }
      Find.WorldObjects.MapParentAt(target.Tile);
      IEnumerable<FloatMenuOption> floatMenuOptionsAt = this.GetTransportPodsFloatMenuOptionsAt(target.Tile, this.carr);
      if (!floatMenuOptionsAt.Any<FloatMenuOption>())
      {
        if (Find.WorldGrid[target.Tile].biome.impassable || Find.World.Impassable(target.Tile))
        {
          Messages.Message("MessageTransportPodsDestinationIsInvalid".Translate(), MessageTypeDefOf.RejectInput, false);
          return false;
        }
        this.TryLaunch(target.Tile, (TransportPodsArrivalAction) null, (Caravan) null);
        return true;
      }
      if (floatMenuOptionsAt.Count<FloatMenuOption>() == 1)
      {
        if (!floatMenuOptionsAt.First<FloatMenuOption>().Disabled)
          floatMenuOptionsAt.First<FloatMenuOption>().action();
        return false;
      }
      Find.WindowStack.Add((Window) new FloatMenu(floatMenuOptionsAt.ToList<FloatMenuOption>()));
      return false;
    }

    public void TryLaunch(int destinationTile, TransportPodsArrivalAction arrivalAction, Caravan cafr = null)
    {
      if (cafr == null && !this.parent.Spawned)
      {
        Log.Error("Tried to launch " + (object) this.parent + ", but it's unspawned.", false);
      }
      else
      {
        if (this.parent.Spawned && !this.LoadingInProgressOrReadyToLaunch || (!this.AllInGroupConnectedToFuelingPort || !this.AllFuelingPortSourcesInGroupHaveAnyFuel))
          return;
        if (cafr == null)
        {
          Map map = this.parent.Map;
          int num = Find.WorldGrid.TraversalDistanceBetween(map.Tile, destinationTile, true, int.MaxValue);
          if (num > this.MaxLaunchDistance)
            return;
          this.Transporter.TryRemoveLord(map);
          int groupId = this.Transporter.groupID;
          float amount = Mathf.Max(CompLaunchableSRTS.FuelNeededToLaunchAtDist((float) num), 1f);
          CompTransporter comp1 = this.FuelingPortSource.TryGetComp<CompTransporter>();
          Building fuelingPortSource = this.FuelingPortSource;
          if (fuelingPortSource != null)
            fuelingPortSource.TryGetComp<CompRefuelable>().ConsumeFuel(amount);
          ThingOwner directlyHeldThings = comp1.GetDirectlyHeldThings();

          // Neceros Edit
          Thing thing = ThingMaker.MakeThing(ThingDef.Named(parent.def.defName), (ThingDef) null);
          thing.SetFactionDirect(Faction.OfPlayer);
          thing.Rotation = this.FuelingPortSource.Rotation;
          CompRefuelable comp2 = thing.TryGetComp<CompRefuelable>();
          comp2.GetType().GetField("fuel", BindingFlags.Instance | BindingFlags.NonPublic).SetValue((object) comp2, (object) fuelingPortSource.TryGetComp<CompRefuelable>().Fuel);
          comp2.TargetFuelLevel = fuelingPortSource.TryGetComp<CompRefuelable>().TargetFuelLevel;
          thing.stackCount = 1;
          directlyHeldThings.TryAddOrTransfer(thing, true);

          // Neceros Edit
          ActiveDropPod activeDropPod = (ActiveDropPod) ThingMaker.MakeThing(ThingDef.Named(parent.def.defName + "_Active"), null);
          activeDropPod.Contents = new ActiveDropPodInfo();
          activeDropPod.Contents.innerContainer.TryAddRangeOrTransfer((IEnumerable<Thing>) directlyHeldThings, true, true);

          // Neceros Edit
          SRTSLeaving srtsLeaving = (SRTSLeaving) SkyfallerMaker.MakeSkyfaller(ThingDef.Named(parent.def.defName + "_Leaving"), (Thing) activeDropPod);
          srtsLeaving.rotation = this.FuelingPortSource.Rotation;
          srtsLeaving.groupID = groupId;
          srtsLeaving.destinationTile = destinationTile;
          srtsLeaving.arrivalAction = arrivalAction;
          comp1.CleanUpLoadingVars(map);
          IntVec3 position = fuelingPortSource.Position;
          SRTSStatic.SRTSDestroy((Thing) fuelingPortSource, DestroyMode.Vanish);
          GenSpawn.Spawn((Thing) srtsLeaving, position, map, WipeMode.Vanish);
          CameraJumper.TryHideWorld();
        }
        else
        {
          int num = Find.WorldGrid.TraversalDistanceBetween(this.carr.Tile, destinationTile, true, int.MaxValue);
          if (num > this.MaxLaunchDistance)
            return;
          float amount = Mathf.Max(CompLaunchableSRTS.FuelNeededToLaunchAtDist((float) num), 1f);
          if (this.FuelingPortSource != null)
            this.FuelingPortSource.TryGetComp<CompRefuelable>().ConsumeFuel(amount);
          ThingOwner<Pawn> directlyHeldThings = (ThingOwner<Pawn>) cafr.GetDirectlyHeldThings();
          Thing thing = null;
          foreach (Pawn pawn in directlyHeldThings.InnerListForReading)
          {
            Pawn_InventoryTracker inventory = pawn.inventory;
            for (int index = 0; index < inventory.innerContainer.Count; ++index)
            {
              // Neceros Edit
              if (inventory.innerContainer[index].TryGetComp<CompLaunchableSRTS>() != null)
              {
                thing = inventory.innerContainer[index];
                inventory.innerContainer[index].holdingOwner.Remove(inventory.innerContainer[index]);
                break;
              }
            }
          }
          /*Add caravan items to SRTS - SmashPhil */
          foreach(Pawn p in directlyHeldThings.InnerListForReading)
          {
            p.inventory.innerContainer.InnerListForReading.ForEach(x => AddThingsToSRTS(x));
            p.inventory.innerContainer.Clear();
          }

          ThingOwner<Thing> thingOwner = new ThingOwner<Thing>();
          foreach (Pawn pawn in directlyHeldThings.AsEnumerable<Pawn>().ToList<Pawn>())
            thingOwner.TryAddOrTransfer((Thing) pawn, true);
          if (thing != null && thing.holdingOwner == null)
            thingOwner.TryAddOrTransfer(thing, false);

          // Neceros Edit
          ActiveDropPod activeDropPod = (ActiveDropPod) ThingMaker.MakeThing(ThingDef.Named(parent.def.defName + "_Active"), (ThingDef) null);
          activeDropPod.Contents = new ActiveDropPodInfo();
          activeDropPod.Contents.innerContainer.TryAddRangeOrTransfer((IEnumerable<Thing>) thingOwner, true, true);
          activeDropPod.Contents.innerContainer.TryAddRangeOrTransfer((IEnumerable<Thing>) thingsInsideShip, true, true);
          thingsInsideShip.Clear();

          cafr.RemoveAllPawns();
          if (cafr.Spawned)
            Find.WorldObjects.Remove((WorldObject) cafr);
          TravelingTransportPods travelingTransportPods = (TravelingTransportPods) WorldObjectMaker.MakeWorldObject(DefDatabase<WorldObjectDef>.GetNamed("TravelingSRTS", true));
          travelingTransportPods.Tile = cafr.Tile;
          travelingTransportPods.SetFaction(Faction.OfPlayer);
          travelingTransportPods.destinationTile = destinationTile;
          travelingTransportPods.arrivalAction = arrivalAction;
          Find.WorldObjects.Add((WorldObject) travelingTransportPods);
          travelingTransportPods.AddPod(activeDropPod.Contents, true);
          activeDropPod.Contents = (ActiveDropPodInfo) null;
          activeDropPod.Destroy(DestroyMode.Vanish);
          Find.WorldTargeter.StopTargeting();
        }
      }
    }

    public void Notify_FuelingPortSourceDeSpawned()
    {
      if (!this.Transporter.CancelLoad())
        return;
      Messages.Message("MessageTransportersLoadCanceled_FuelingPortGiverDeSpawned".Translate(), (LookTargets) ((Thing) this.parent), MessageTypeDefOf.NegativeEvent, true);
    }

    public static int MaxLaunchDistanceAtFuelLevel(float fuelLevel)
    {
      return Mathf.FloorToInt(fuelLevel / FuelPerTile);
    }

    public static float FuelNeededToLaunchAtDist(float dist)
    {
      return FuelPerTile * dist;
    }

    public IEnumerable<FloatMenuOption> GetTransportPodsFloatMenuOptionsAt(
      int tile,
      Caravan car = null)
    {
      bool anything = false;
      IEnumerable<IThingHolder> pods = this.TransportersInGroup.Cast<IThingHolder>();
      if (car != null)
      {
        List<Caravan> rliss = new List<Caravan>();
        rliss.Add(car);
        pods = rliss.Cast<IThingHolder>();
        rliss = (List<Caravan>) null;
      }
      if (car == null)
      {
        if(TransportPodsArrivalAction_FormCaravan.CanFormCaravanAt(pods, tile) && !Find.WorldObjects.AnySettlementBaseAt(tile) && !Find.WorldObjects.AnySiteAt(tile))
        {
          anything = true;
          yield return new FloatMenuOption("FormCaravanHere".Translate(), (Action) (() => this.TryLaunch(tile, (TransportPodsArrivalAction) new TransportPodsArrivalAction_FormCaravan(), car)), MenuOptionPriority.Default, (Action) null, (Thing) null, 0.0f, (Func<Rect, bool>) null, (WorldObject) null);
        }
      }
      else if (!Find.WorldObjects.AnySettlementBaseAt(tile) && !Find.WorldObjects.AnySiteAt(tile) && !Find.World.Impassable(tile))
      {
        anything = true;
        yield return new FloatMenuOption("FormCaravanHere".Translate(), (Action) (() => this.TryLaunch(tile, (TransportPodsArrivalAction) new TransportPodsArrivalAction_FormCaravan(), car)), MenuOptionPriority.Default, (Action) null, (Thing) null, 0.0f, (Func<Rect, bool>) null, (WorldObject) null);
      }
      List<WorldObject> worldObjects = Find.WorldObjects.AllWorldObjects;
      for (int i = 0; i < worldObjects.Count; ++i)
      {
        if (worldObjects[i].Tile == tile)
        {
          IEnumerable<FloatMenuOption> nowre = SRTSStatic.getFM(worldObjects[i], pods, this, car);
          if (nowre.ToList<FloatMenuOption>().Count < 1)
          {
            yield return new FloatMenuOption("FormCaravanHere".Translate(), (Action) (() => this.TryLaunch(tile, (TransportPodsArrivalAction) new TransportPodsArrivalAction_FormCaravan(), car)), MenuOptionPriority.Default, (Action) null, (Thing) null, 0.0f, (Func<Rect, bool>) null, (WorldObject) null);
          }
          else
          {
            foreach (FloatMenuOption floatMenuOption in nowre)
            {
              FloatMenuOption o = floatMenuOption;
              anything = true;
              yield return o;
              o = (FloatMenuOption) null;
            }
          }
          nowre = (IEnumerable<FloatMenuOption>) null;
        }
      }
      if (!anything && !Find.World.Impassable(tile))
        yield return new FloatMenuOption("TransportPodsContentsWillBeLost".Translate(), (Action) (() => this.TryLaunch(tile, (TransportPodsArrivalAction) null, (Caravan) null)), MenuOptionPriority.Default, (Action) null, (Thing) null, 0.0f, (Func<Rect, bool>) null, (WorldObject) null);
    }


    List<Thing> thingsInsideShip = new List<Thing>();
  }
}
