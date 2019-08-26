// Decompiled with JetBrains decompiler
// Type: Helicopter.HelicopterStatic
// Assembly: Helicopter, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 87D55B68-45DE-4FF0-8B2F-4616963FFBF1
// Assembly location: D:\SteamLibrary\steamapps\workshop\content\294100\1833992261\Assemblies\Helicopter.dll

using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Verse;

namespace Helicopter
{
  public static class HelicopterStatic
  {
    public static IEnumerable<FloatMenuOption> getFM(
      WorldObject wobj,
      IEnumerable<IThingHolder> ih,
      CompLaunchableHelicopter comp,
      Caravan car)
    {
      if (wobj is Caravan)
        return Enumerable.Empty<FloatMenuOption>();
      if (wobj is Site)
        return HelicopterStatic.GetSite(wobj as Site, ih, comp, car);
      if (wobj is SettlementBase)
        return HelicopterStatic.GetSettle(wobj as SettlementBase, ih, comp, car);
      if (wobj is MapParent)
        return HelicopterStatic.GetMapParent(wobj as MapParent, ih, comp, car);
      return Enumerable.Empty<FloatMenuOption>();
    }

    public static IEnumerable<FloatMenuOption> GetMapParent(
      MapParent mapparent,
      IEnumerable<IThingHolder> pods,
      CompLaunchableHelicopter representative,
      Caravan car)
    {
      if (TransportPodsArrivalAction_LandInSpecificCell.CanLandInSpecificCell(pods, mapparent))
        yield return new FloatMenuOption("LandInExistingMap".Translate((object) mapparent.Label), (Action) (() =>
        {
          Map myMap = car != null ? (Map) null : representative.parent.Map;
          Current.Game.CurrentMap = mapparent.Map;
          CameraJumper.TryHideWorld();
          Find.Targeter.BeginTargeting(TargetingParameters.ForDropPodsDestination(), (Action<LocalTargetInfo>) (x => representative.TryLaunch(mapparent.Tile, (TransportPodsArrivalAction) new TransportPodsArrivalAction_LandInSpecificCell(mapparent, x.Cell), car)), (Pawn) null, (Action) (() =>
          {
            if (myMap == null || !Find.Maps.Contains(myMap))
              return;
            Current.Game.CurrentMap = myMap;
          }), CompLaunchable.TargeterMouseAttachment);
        }), MenuOptionPriority.Default, (Action) null, (Thing) null, 0.0f, (Func<Rect, bool>) null, (WorldObject) null);
    }

    public static IEnumerable<FloatMenuOption> GetSite(
      Site site,
      IEnumerable<IThingHolder> pods,
      CompLaunchableHelicopter representative,
      Caravan car)
    {
      foreach (FloatMenuOption floatMenuOption in HelicopterStatic.GetMapParent((MapParent) site, pods, representative, car))
      {
        FloatMenuOption o = floatMenuOption;
        yield return o;
        o = (FloatMenuOption) null;
      }
      foreach (FloatMenuOption floatMenuOption in HelicopterStatic.GetVisitSite(representative, pods, site, car))
      {
        FloatMenuOption o2 = floatMenuOption;
        yield return o2;
        o2 = (FloatMenuOption) null;
      }
    }

    public static IEnumerable<FloatMenuOption> GetVisitSite(
      CompLaunchableHelicopter representative,
      IEnumerable<IThingHolder> pods,
      Site site,
      Caravan car)
    {
      foreach (FloatMenuOption floatMenuOption in HelicoptersArrivalActionUtility.GetFloatMenuOptions<TransportPodsArrivalAction_VisitSite>((Func<FloatMenuAcceptanceReport>) (() => TransportPodsArrivalAction_VisitSite.CanVisit(pods, site)), (Func<TransportPodsArrivalAction_VisitSite>) (() => new TransportPodsArrivalAction_VisitSite(site, PawnsArrivalModeDefOf.EdgeDrop)), "DropAtEdge".Translate(), representative, site.Tile, car))
      {
        FloatMenuOption f = floatMenuOption;
        yield return f;
        f = (FloatMenuOption) null;
      }
      foreach (FloatMenuOption floatMenuOption in HelicoptersArrivalActionUtility.GetFloatMenuOptions<TransportPodsArrivalAction_VisitSite>((Func<FloatMenuAcceptanceReport>) (() => TransportPodsArrivalAction_VisitSite.CanVisit(pods, site)), (Func<TransportPodsArrivalAction_VisitSite>) (() => new TransportPodsArrivalAction_VisitSite(site, PawnsArrivalModeDefOf.CenterDrop)), "DropInCenter".Translate(), representative, site.Tile, car))
      {
        FloatMenuOption f2 = floatMenuOption;
        yield return f2;
        f2 = (FloatMenuOption) null;
      }
    }

    public static IEnumerable<FloatMenuOption> GetSettle(
      SettlementBase bs,
      IEnumerable<IThingHolder> pods,
      CompLaunchableHelicopter representative,
      Caravan car)
    {
      foreach (FloatMenuOption floatMenuOption in HelicopterStatic.GetMapParent((MapParent) bs, pods, representative, car))
      {
        FloatMenuOption o = floatMenuOption;
        yield return o;
        o = (FloatMenuOption) null;
      }
      foreach (FloatMenuOption visitFloatMenuOption in HelicoptersArrivalActionUtility.GetVisitFloatMenuOptions(representative, pods, bs, car))
      {
        FloatMenuOption f = visitFloatMenuOption;
        yield return f;
        f = (FloatMenuOption) null;
      }
      foreach (FloatMenuOption giftFloatMenuOption in HelicoptersArrivalActionUtility.GetGIFTFloatMenuOptions(representative, pods, bs, car))
      {
        FloatMenuOption f2 = giftFloatMenuOption;
        yield return f2;
        f2 = (FloatMenuOption) null;
      }
      foreach (FloatMenuOption atkFloatMenuOption in HelicoptersArrivalActionUtility.GetATKFloatMenuOptions(representative, pods, bs, car))
      {
        FloatMenuOption f3 = atkFloatMenuOption;
        yield return f3;
        f3 = (FloatMenuOption) null;
      }
    }

    public static void HelicopterDestroy(Thing thing, DestroyMode mode = DestroyMode.Vanish)
    {
      if (!Thing.allowDestroyNonDestroyable && !thing.def.destroyable)
        Log.Error("Tried to destroy non-destroyable thing " + (object) thing, false);
      else if (thing.Destroyed)
      {
        Log.Error("Tried to destroy already-destroyed thing " + (object) thing, false);
      }
      else
      {
        bool spawned = thing.Spawned;
        Map map = thing.Map;
        if (thing.Spawned)
          thing.DeSpawn(mode);
        System.Type type = typeof (Thing);
        FieldInfo field = type.GetField("mapIndexOrState", BindingFlags.Instance | BindingFlags.NonPublic);
        sbyte num = -2;
        field.SetValue((object) thing, (object) num);
        if (thing.def.DiscardOnDestroyed)
          thing.Discard(false);
        if (thing.holdingOwner != null)
          thing.holdingOwner.Notify_ContainedItemDestroyed(thing);
        type.GetMethod("RemoveAllReservationsAndDesignationsOnThis", BindingFlags.Instance | BindingFlags.NonPublic).Invoke((object) thing, (object[]) null);
        if (thing is Pawn)
          return;
        thing.stackCount = 0;
      }
    }
  }
}
