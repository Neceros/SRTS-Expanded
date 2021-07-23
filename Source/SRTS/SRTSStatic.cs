using RimWorld;
using RimWorld.Planet;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Verse;

namespace SRTS
{
    public static class SRTSStatic
    {
        public static IEnumerable<FloatMenuOption> getFM(
          WorldObject wobj,
          IEnumerable<IThingHolder> ih,
          CompLaunchableSRTS comp,
          Caravan car)
        {
            if (wobj is Caravan)
                return Enumerable.Empty<FloatMenuOption>();
            if (wobj is Site)
                return SRTSStatic.GetSite(wobj as Site, ih, comp, car);
            if (wobj is Settlement)
                return SRTSStatic.GetSettle(wobj as Settlement, ih, comp, car);
            if (wobj is MapParent)
                return SRTSStatic.GetMapParent(wobj as MapParent, ih, comp, car);
            return Enumerable.Empty<FloatMenuOption>();
        }

        public static IEnumerable<FloatMenuOption> GetMapParent(
          MapParent mapparent,
          IEnumerable<IThingHolder> pods,
          CompLaunchableSRTS representative,
          Caravan car)
        {
            if (TransportPodsArrivalAction_LandInSpecificCell.CanLandInSpecificCell(pods, mapparent))
                yield return new FloatMenuOption("LandInExistingMap".Translate(mapparent.Label), () =>
               {
                   Map myMap = car != null ? null : representative.parent.Map;
                   Current.Game.CurrentMap = mapparent.Map;
                   CameraJumper.TryHideWorld();
                   Find.Targeter.BeginTargeting(TargetingParameters.ForDropPodsDestination(), x => representative.TryLaunch(mapparent.Tile, new TransportPodsArrivalAction_LandInSpecificCell(mapparent, x.Cell), car), null, () =>
        {
            if (myMap == null || !Find.Maps.Contains(myMap))
                return;
            Current.Game.CurrentMap = myMap;
        }, CompLaunchable.TargeterMouseAttachment);
               }, MenuOptionPriority.Default, null, null, 0.0f, null, null);
        }

        public static IEnumerable<FloatMenuOption> GetSite(
          Site site,
          IEnumerable<IThingHolder> pods,
          CompLaunchableSRTS representative,
          Caravan car)
        {
            foreach (FloatMenuOption floatMenuOption in SRTSStatic.GetMapParent(site, pods, representative, car))
            {
                FloatMenuOption o = floatMenuOption;
                yield return o;
                o = null;
            }
            foreach (FloatMenuOption floatMenuOption in SRTSStatic.GetVisitSite(representative, pods, site, car))
            {
                FloatMenuOption o2 = floatMenuOption;
                yield return o2;
                o2 = null;
            }
        }

        public static IEnumerable<FloatMenuOption> GetVisitSite(
          CompLaunchableSRTS representative,
          IEnumerable<IThingHolder> pods,
          Site site,
          Caravan car)
        {
            foreach (FloatMenuOption floatMenuOption in SRTSArrivalActionUtility.GetFloatMenuOptions(() => TransportPodsArrivalAction_VisitSite.CanVisit(pods, site), () => new TransportPodsArrivalAction_VisitSite(site, PawnsArrivalModeDefOf.EdgeDrop), "DropAtEdge".Translate(), representative, site.Tile, car))
            {
                FloatMenuOption f = floatMenuOption;
                yield return f;
                f = null;
            }
            foreach (FloatMenuOption floatMenuOption in SRTSArrivalActionUtility.GetFloatMenuOptions(() => TransportPodsArrivalAction_VisitSite.CanVisit(pods, site), () => new TransportPodsArrivalAction_VisitSite(site, PawnsArrivalModeDefOf.CenterDrop), "DropInCenter".Translate(), representative, site.Tile, car))
            {
                FloatMenuOption f2 = floatMenuOption;
                yield return f2;
                f2 = null;
            }
        }

        public static IEnumerable<FloatMenuOption> GetSettle(
          Settlement bs,
          IEnumerable<IThingHolder> pods,
          CompLaunchableSRTS representative,
          Caravan car)
        {
            foreach (FloatMenuOption floatMenuOption in SRTSStatic.GetMapParent(bs, pods, representative, car))
            {
                FloatMenuOption o = floatMenuOption;
                yield return o;
                o = null;
            }
            foreach (FloatMenuOption visitFloatMenuOption in SRTSArrivalActionUtility.GetVisitFloatMenuOptions(representative, pods, bs, car))
            {
                FloatMenuOption f = visitFloatMenuOption;
                yield return f;
                f = null;
            }
            /*Uncomment to allow gifting of Ship and contents to faction -SmashPhil*/
            /*foreach (FloatMenuOption giftFloatMenuOption in SRTSArrivalActionUtility.GetGIFTFloatMenuOptions(representative, pods, bs, car))
            {
              FloatMenuOption f2 = giftFloatMenuOption;
              yield return f2;
              f2 = (FloatMenuOption) null;
            }*/
            foreach (FloatMenuOption atkFloatMenuOption in SRTSArrivalActionUtility.GetATKFloatMenuOptions(representative, pods, bs, car))
            {
                FloatMenuOption f3 = atkFloatMenuOption;
                yield return f3;
                f3 = null;
            }
        }

        public static void SRTSDestroy(Thing thing, DestroyMode mode = DestroyMode.Vanish)
        {
            if (!Thing.allowDestroyNonDestroyable && !thing.def.destroyable)
                Log.Error("Tried to destroy non-destroyable thing " + thing);
            else if (thing.Destroyed)
            {
                Log.Error("Tried to destroy already-destroyed thing " + thing);
            }
            else
            {
                bool spawned = thing.Spawned;
                Map map = thing.Map;
                if (thing.Spawned)
                    thing.DeSpawn(mode);
                System.Type type = typeof(Thing);
                FieldInfo field = type.GetField("mapIndexOrState", BindingFlags.Instance | BindingFlags.NonPublic);
                sbyte num = -2;
                field.SetValue(thing, num);
                if (thing.def.DiscardOnDestroyed)
                    thing.Discard(false);
                if (thing.holdingOwner != null)
                    thing.holdingOwner.Notify_ContainedItemDestroyed(thing);
                type.GetMethod("RemoveAllReservationsAndDesignationsOnThis", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(thing, null);
                if (thing is Pawn)
                    return;
                thing.stackCount = 0;
            }
        }
    }
}