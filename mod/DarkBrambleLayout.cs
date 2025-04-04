﻿using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ArchipelagoRandomizer;

[HarmonyPatch]
internal class DarkBrambleLayout
{
    // same as OuterFogWarpVolume.Name, but without the None value
    public enum DBRoom
    {
        Hub,
        EscapePod,
        AnglerNest,
        Pioneer,
        ExitOnly,
        Vessel,
        Cluster,
        SmallNest,
    }

    // These are just the warps we want to randomize. They exclude things like the recursive zones, most bramble seeds,
    // and all of the "outer" warps that either go back a zone or leave DB entirely.
    public enum DBWarp
    {
        Hub1,
        Hub2,
        Hub3,
        Hub4,
        Cluster1,
        Cluster2,
        EscapePod1,
        AnglerNest1,
        AnglerNest2,
    }

    public static Dictionary<DBRoom, List<DBWarp>> WarpsInRoom = new() {
        { DBRoom.Hub, new(){ DBWarp.Hub1, DBWarp.Hub2, DBWarp.Hub3, DBWarp.Hub4 } },
        { DBRoom.Cluster, new(){ DBWarp.Cluster1, DBWarp.Cluster2 } },
        { DBRoom.EscapePod, new(){ DBWarp.EscapePod1 } },
        { DBRoom.AnglerNest, new(){ DBWarp.AnglerNest1, DBWarp.AnglerNest2 } },
    };

    private record DBLayout
    {
        public DBRoom entrance;
        public Dictionary<DBWarp, DBRoom> warps;
    }
 
    // Prototype generation code, before I ported it to Python.
    /*
     algorithm:
    - call Pioneer, ExitOnly, Vessel and SmallNest the "dead end rooms"
    - call Hub, Cluster, EscapePod, AnglerNest "transit rooms"/non-dead end rooms
    - select a transit room for the entrance, initialize unmapped warps with that room's warps
    - while there are transit rooms unused:
        randomly select one of the unused warps, map it to a random unused transit room, add that room's warps to unmapped warps
    - while there are unused dead end rooms:
        randomly select one of the unused warps, map it to a random unused dead end room
    - while there are still unmapped warps:
        randomly pick any DB room to map them to
     */
    /*private static DBLayout GenerateDBLayout()
    {
        var unusedTransitRooms = new List<DBRoom> { DBRoom.Hub, DBRoom.Cluster, DBRoom.EscapePod, DBRoom.AnglerNest };
        var unusedDeadEndRooms = new List<DBRoom> { DBRoom.Pioneer, DBRoom.ExitOnly, DBRoom.Vessel, DBRoom.SmallNest };

        int Seed = 42;
        var prng = new System.Random(Seed);

        var db = new DBLayout();
        db.warps = new();
        var unmappedWarps = new List<DBWarp>();

        var entranceIndex = prng.Next(unusedTransitRooms.Count);
        db.entrance = unusedTransitRooms[entranceIndex];
        unusedTransitRooms.RemoveAt(entranceIndex);

        unmappedWarps.AddRange(WarpsInRoom[db.entrance]);

        while (unusedTransitRooms.Count > 0)
        {
            var warpIndex = prng.Next(unmappedWarps.Count);
            var warp = unmappedWarps[warpIndex];
            unmappedWarps.RemoveAt(warpIndex);

            var roomIndex = prng.Next(unusedTransitRooms.Count);
            var room = unusedTransitRooms[roomIndex];
            unusedTransitRooms.RemoveAt(roomIndex);

            db.warps[warp] = room;

            unmappedWarps.AddRange(WarpsInRoom[room]);
        }

        while (unusedDeadEndRooms.Count > 0)
        {
            var warpIndex = prng.Next(unmappedWarps.Count);
            var warp = unmappedWarps[warpIndex];
            unmappedWarps.RemoveAt(warpIndex);

            var roomIndex = prng.Next(unusedDeadEndRooms.Count);
            var room = unusedDeadEndRooms[roomIndex];
            unusedDeadEndRooms.RemoveAt(roomIndex);

            db.warps[warp] = room;
        }

        var allRooms = Enum.GetValues(typeof(DBRoom));
        while (unmappedWarps.Count > 0)
        {
            var warpIndex = prng.Next(unmappedWarps.Count);
            var warp = unmappedWarps[warpIndex];
            unmappedWarps.RemoveAt(warpIndex);

            var roomIndex = prng.Next(allRooms.Length);
            var room = (DBRoom)allRooms.GetValue(roomIndex);

            db.warps[warp] = room;
        }

        return db;
    }*/

    private static DBLayout CurrentDBLayout = null;

    private static Dictionary<char, DBRoom> letterToRoom = new Dictionary<char, DBRoom>
    {
        { 'H', DBRoom.Hub },
        { 'E', DBRoom.EscapePod },
        { 'A', DBRoom.AnglerNest },
        { 'P', DBRoom.Pioneer },
        { 'X', DBRoom.ExitOnly },
        { 'V', DBRoom.Vessel },
        { 'C', DBRoom.Cluster },
        { 'S', DBRoom.SmallNest },
    };

    public static void ApplySlotDataLayout(string stringifiedLayout)
    {
        if (stringifiedLayout == "vanilla")
        {
            CurrentDBLayout = null;
            return;
        }

        // In the stringified layout format we want to output for slot data, the vanilla DB layout is:
        // "H->SCEA|C->PX|E->A|A->V"
        // That's what we need to parse and turn into a DBLayout instance.
        try
        {
            var layout = new DBLayout();
            layout.warps = new();

            layout.entrance = letterToRoom[stringifiedLayout[0]];

            var roomStrings = stringifiedLayout.Split('|');

            foreach (var roomString in roomStrings)
            {
                var startRoom = letterToRoom[roomString[0]];
                // 1..2 should be "->"
                var warpTargets = roomString.Substring(3).Select(c => letterToRoom[c]);

                foreach (var (warp, target) in WarpsInRoom[startRoom].Zip(warpTargets, (w, t) => (w, t)))
                    layout.warps[warp] = target;
            }

            CurrentDBLayout = layout;
        }
        catch (Exception e)
        {
            APRandomizer.OWMLModConsole.WriteLine($"ApplySlotDataLayout() failed to deserialize: {stringifiedLayout}\n{e.Message}\n{e.StackTrace}");
        }
    }

    [HarmonyPrefix, HarmonyPatch(typeof(TravelerAudioManager), nameof(TravelerAudioManager.SyncTravelers))]
    public static void TravelerAudioManager_SyncTravelers_Prefix(TravelerAudioManager __instance)
    {
        try
        {
            var signals = __instance._signals;
            if (signals.Any(s => s == null))
            {
                //APRandomizer.OWMLModConsole.WriteLine($"TravelerAudioManager_SyncTravelers_Prefix cleaning up references to vanilla AudioSignals we had to destroy, so they don't NRE later");
                __instance._signals = signals.Where(s => s != null).ToList();
            }
        }
        catch (Exception e) {
            APRandomizer.OWMLModConsole.WriteLine($"TravelerAudioManager_SyncTravelers_Prefix failed: {e.Message}\n{e.StackTrace}");
        }
    }

    private static InnerFogWarpVolume EntranceIFVW;
    private static Dictionary<DBRoom, OuterFogWarpVolume> RoomToOFWV = new();
    private static Dictionary<DBWarp, List<InnerFogWarpVolume>> WarpToIFWVs = new();

    public static void OnCompleteSceneLoad(OWScene _scene, OWScene loadScene)
    {
        if (loadScene != OWScene.SolarSystem) return;

        var pioneerInteractables = GameObject.Find("DB_PioneerDimension_Body/Sector_PioneerDimension/Interactables_PioneerDimension");
        var vesselInteractables = GameObject.Find("DB_VesselDimension_Body/Sector_VesselDimension/Interactables_VesselDimension");
        // base game inconsistency: SmallNest's root GO is DB_SmallNest_Body, not DB_SmallNestDimension_Body
        var smallNestInteractables = GameObject.Find("DB_SmallNest_Body/Sector_SmallNestDimension/Interactables_SmallNestDimension");
        var anglerNestInteractables = GameObject.Find("DB_AnglerNestDimension_Body/Sector_AnglerNestDimension/Interactables_AnglerNestDimension");
        var clusterInteractables = GameObject.Find("DB_ClusterDimension_Body/Sector_ClusterDimension/Interactables_ClusterDimension");
        var exitOnlyInteractables = GameObject.Find("DB_ExitOnlyDimension_Body/Sector_ExitOnlyDimension/Interactables_ExitOnlyDimension");
        var hubInteractables = GameObject.Find("DB_HubDimension_Body/Sector_HubDimension/Interactables_HubDimension");
        var escapePodInteractables = GameObject.Find("DB_EscapePodDimension_Body/Sector_EscapePodDimension/Interactables_EscapePodDimension");

        EntranceIFVW = GameObject.Find("DarkBramble_Body/Sector_DB/Interactables_DB/EntranceWarp_ToHub").GetComponent<InnerFogWarpVolume>();

        RoomToOFWV[DBRoom.Pioneer] = pioneerInteractables.transform.Find("OuterWarp_Pioneer").GetComponent<OuterFogWarpVolume>();
        RoomToOFWV[DBRoom.Vessel] = vesselInteractables.transform.Find("OuterWarp_Vessel").GetComponent<OuterFogWarpVolume>();
        RoomToOFWV[DBRoom.SmallNest] = smallNestInteractables.transform.Find("OuterWarp_SmallNest").GetComponent<OuterFogWarpVolume>();
        RoomToOFWV[DBRoom.AnglerNest] = anglerNestInteractables.transform.Find("OuterWarp_AnglerNest").GetComponent<OuterFogWarpVolume>();
        RoomToOFWV[DBRoom.Cluster] = clusterInteractables.transform.Find("OuterWarp_Cluster").GetComponent<OuterFogWarpVolume>();
        RoomToOFWV[DBRoom.ExitOnly] = exitOnlyInteractables.transform.Find("OuterWarp_ExitOnly").GetComponent<OuterFogWarpVolume>();
        RoomToOFWV[DBRoom.Hub] = hubInteractables.transform.Find("OuterWarp_Hub").GetComponent<OuterFogWarpVolume>();
        RoomToOFWV[DBRoom.EscapePod] = escapePodInteractables.transform.Find("OuterWarp_EscapePod").GetComponent<OuterFogWarpVolume>();

        var hubIFVWs = hubInteractables.transform.GetComponentsInChildren<InnerFogWarpVolume>();
        WarpToIFWVs[DBWarp.Hub1] = hubIFVWs.Where(ifvw => ifvw.name == "InnerWarp_ToCluster").ToList();
        WarpToIFWVs[DBWarp.Hub2] = hubIFVWs.Where(ifvw => ifvw.name == "InnerWarp_ToAnglerNest").ToList();
        WarpToIFWVs[DBWarp.Hub3] = hubIFVWs.Where(ifvw => ifvw.name == "InnerWarp_ToSmallNest").ToList();
        WarpToIFWVs[DBWarp.Hub4] = hubIFVWs.Where(ifvw => ifvw.name == "InnerWarp_ToEscapePod").ToList();

        var clusterIFVWs = clusterInteractables.transform.GetComponentsInChildren<InnerFogWarpVolume>();
        WarpToIFWVs[DBWarp.Cluster1] = clusterIFVWs.Where(ifvw => ifvw.name == "InnerWarp_ToPioneer" || ifvw.name == "SeedWarp_ToPioneer").ToList();
        WarpToIFWVs[DBWarp.Cluster2] = clusterIFVWs.Where(ifvw => ifvw.name == "InnerWarp_ToExitOnly").ToList();

        WarpToIFWVs[DBWarp.EscapePod1] = new List<InnerFogWarpVolume> {
            escapePodInteractables.transform.Find("InnerWarp_ToAnglerNest").GetComponent<InnerFogWarpVolume>()
        };

        var anglerNestIFVWs = anglerNestInteractables.transform.GetComponentsInChildren<InnerFogWarpVolume>();
        WarpToIFWVs[DBWarp.AnglerNest1] = anglerNestIFVWs.Where(ifvw => ifvw.name == "InnerWarp_ToExitOnly").ToList();
        WarpToIFWVs[DBWarp.AnglerNest2] = anglerNestIFVWs.Where(ifvw => ifvw.name == "InnerWarp_ToVessel").ToList();

        ApplyDBLayout();
    }

    private static void ApplyDBLayout()
    {
        if (CurrentDBLayout == null)
        {
            //APRandomizer.OWMLModConsole.WriteLine($"ApplyDBLayout() doing nothing because DB has not been randomized");
            return;
        }

        APRandomizer.OWMLModConsole.WriteLine($"ApplyDBLayout() applying randomized Dark Bramble layout: " +
            $"space -> {CurrentDBLayout.entrance}, {string.Join(",", CurrentDBLayout.warps.Select(wr => $"{wr.Key}->{wr.Value}"))}");

        // Edit the warps between DB rooms

        EntranceIFVW._linkedOuterWarpVolume = RoomToOFWV[CurrentDBLayout.entrance];
        EntranceIFVW._linkedOuterWarpVolume._linkedInnerWarpVolume = EntranceIFVW; // backing out of DB's first room should of course exit DB

        foreach (var (warp, room) in CurrentDBLayout.warps)
        {
            foreach (var ifwv in WarpToIFWVs[warp])
            {
                // This is the kind of warp we usually care about:
                // When you go "into" a spherical portal in one room, which room do you end up in?
                var newOFWVLink = RoomToOFWV[room];
                ifwv._linkedOuterWarpVolume = newOFWVLink;

                // Slightly more complex is "exiting" a DB room. When you pass through the room's OFWV, you emerge from its _linkedInnerWarpVolume.

                // Most commonly, the _linkedInnerWarpVolume is one of DB's exits to space.
                // We don't need to change these, so there's no code for them.

                // Four OFWVs link to a "previous" room (Pioneer goes "two rooms back" in vanilla), so we need to decide what to do with those
                if (newOFWVLink == RoomToOFWV[DBRoom.SmallNest] || newOFWVLink == RoomToOFWV[DBRoom.EscapePod]
                    || newOFWVLink == RoomToOFWV[DBRoom.Cluster] || newOFWVLink == RoomToOFWV[DBRoom.Pioneer])
                {
                    // Unlike the vanilla layout, it's possible for there to be multiple entrances to one of these rooms, but an OFWV can only have one "exit" / linked IFWV,
                    // so there's no unambiguous "previous room" for us to point these to.
                    // Since you need to be able to leave and reenter potentially all of DB after shooting your Scout into the Nomai Grave seed,
                    // we also need to ensure these "exits" can't trap the player in an inescapable warp loop.
                    // I believe the only safe places for these exits to go to are outer space or the first DB room.
                    if (room != CurrentDBLayout.entrance)
                    {
                        // When we have a choice between first room and outer space, we go with first room
                        // because it's slightly more interesting and closer to vanilla's behavior.
                        ifwv._linkedOuterWarpVolume._linkedInnerWarpVolume =
                            // But we don't really care which IFWV we use for the first room, so just pick whatever's first in our lists.
                            WarpToIFWVs[WarpsInRoom[CurrentDBLayout.entrance][0]][0];
                    }
                    // If we've re-entered this layout's first room, then do nothing.
                    // It already exits to space and I don't think we want to change that.
                }
            }
        }

        // Edit the Signalscope signal sources inside DB
        // We can't directly "move" a signal. Instead we have to delete the vanilla signals
        // that layout rando might make incorrect, and create new ones in the correct places.

        // In the vanilla layout, Hub and Cluster are the only two DB rooms with "transitive" signals,
        // which are the ones we need to clean up.
        // Fortunately, these rooms do not contain any "root" signals that we want to leave alone,
        // so we can simply delete every signal in these two rooms.
        foreach (var s in GameObject.FindObjectsOfType<AudioSignal>()) {
            var ofwv = s._outerFogWarpVolume;
            if (ofwv == RoomToOFWV[DBRoom.Hub] || ofwv == RoomToOFWV[DBRoom.Cluster])
                s.gameObject.DestroyAllComponents<AudioSignal>();
        };

        // Figuring out everywhere we need to add a signal is more involved.
        // Since there may be several routes to each "real" signal source with branches diverging and convering,
        // we start from the rooms with the real sources, look for all direct connections to those rooms, and
        // do a sort of fixed point iteration until we can no longer find a room/warp that's missing a signal.
        HashSet<DBRoom> harmonicaRooms = new HashSet<DBRoom> { DBRoom.Pioneer };
        HashSet<DBRoom> ep3Rooms = new HashSet<DBRoom> { DBRoom.EscapePod };
        HashSet<DBWarp> harmonicaWarps = new();
        HashSet<DBWarp> ep3Warps = new();

        bool roomSetsChanged = true;
        while (roomSetsChanged)
        {
            roomSetsChanged = false;

            foreach (var (warp, endRoom) in CurrentDBLayout.warps)
            {
                if (harmonicaRooms.Contains(endRoom) && !harmonicaWarps.Contains(warp))
                {
                    harmonicaWarps.Add(warp);

                    var startRoom = WarpsInRoom.First(roomAndWarps => roomAndWarps.Value.Contains(warp)).Key;
                    if (!harmonicaRooms.Contains(startRoom))
                    {
                        roomSetsChanged |= true;
                        harmonicaRooms.Add(startRoom);
                    }

                    foreach (var ifwv in WarpToIFWVs[warp])
                        AddSignalToGO(ifwv.gameObject, RoomToOFWV[startRoom], SignalFrequency.Traveler, SignalName.Traveler_Feldspar, AudioType.TravelerFeldspar);
                }

                if (ep3Rooms.Contains(endRoom) && !ep3Warps.Contains(warp))
                {
                    ep3Warps.Add(warp);

                    var startRoom = WarpsInRoom.First(roomAndWarps => roomAndWarps.Value.Contains(warp)).Key;
                    if (!ep3Rooms.Contains(startRoom))
                    {
                        roomSetsChanged |= true;
                        ep3Rooms.Add(startRoom);
                    }

                    foreach (var ifwv in WarpToIFWVs[warp])
                        AddSignalToGO(ifwv.gameObject, RoomToOFWV[startRoom], SignalFrequency.EscapePod, SignalName.EscapePod_DB, AudioType.NomaiEscapePodDistressSignal_LP);
                }
            }
        }

        // APRandomizer.OWMLModConsole.WriteLine($"finished adding signals, final warp sets were: {string.Join("|", harmonicaWarps)} and {string.Join("|", ep3Warps)}");
    }

    private static void AddSignalToGO(GameObject go, OuterFogWarpVolume ofwv, SignalFrequency frequency, SignalName signalName, AudioType audioType)
    {
        // Much of this is imitating New Horizons' SignalBuilder.Make() and GeneralAudioBuilder.Make().
        // I'm unsure exactly how much of it is necessary.

        var source = go.AddComponent<AudioSource>();
        var owAudioSource = go.AddComponent<OWAudioSource>();
        owAudioSource._audioSource = source;
        owAudioSource._audioLibraryClip = audioType;
        owAudioSource.playOnAwake = false;
        owAudioSource.SetTrack(OWAudioMixer.TrackName.Signal);

        source.loop = true;
        source.minDistance = 1.5f;
        source.maxDistance = 200;
        source.velocityUpdateMode = AudioVelocityUpdateMode.Fixed;
        source.rolloffMode = AudioRolloffMode.Custom;
        source.spatialBlend = 1f;
        source.volume = 0;
        source.dopplerLevel = 0;

        var signal = go.AddComponent<AudioSignal>();
        signal._onlyAudibleToScope = true;
        signal._outerFogWarpVolume = ofwv;
        signal.SetSector(Locator.GetAstroObject(AstroObject.Name.DarkBramble).GetRootSector());

        signal._frequency = frequency;
        signal._name = signalName;
        signal._owAudioSource = owAudioSource;
    }

    // The vanilla game has an infinite recursion bug that causes an immediate freeze, typically followed by Windows killing the game,
    // when any warpable object (ship, player, scout) enters any fog warp into a Dark Bramble layout with non-trivial recursion.
    // The vanilla DB layout has only trivial (same-room) recursion, which is why it's not affected.
    // Specifically, Inner/OuterFogWarpVolume.PropagateCanvasMarkerOutwards() recursively invoke each other on FWVs,
    // and IFWV.PCMO() has an explicit check for trivial recursion, but only trivial.

    // The PCMO() recursion is always performed synchronously by one of the three methods patched below.
    // Since OuterFWVs often must be visited multiple times to place markers on all of the paths in or out of it, while each
    // InnerFWV only needs to be visited once, we keep a list of all *Inner* FWVs visited since the last "sweep" started...

    private static HashSet<InnerFogWarpVolume> VisitedIFWVs = new();

    [HarmonyPrefix, HarmonyPatch(typeof(CanvasMarker), nameof(CanvasMarker.OnTrackFogWarpVolume))]
    public static void CanvasMarker_OnTrackFogWarpVolume(CanvasMarker __instance, FogWarpVolume warpVolume) => VisitedIFWVs = new();
    [HarmonyPrefix, HarmonyPatch(typeof(CanvasMarker), nameof(CanvasMarker.OnUntrackFogWarpVolume))]
    public static void CanvasMarker_OnUntrackFogWarpVolume(CanvasMarker __instance, FogWarpVolume warpVolume) => VisitedIFWVs = new();
    [HarmonyPrefix, HarmonyPatch(typeof(CanvasMarkerManager), nameof(CanvasMarkerManager.UpdateAllFogMarkers))]
    public static void CanvasMarkerManager_UpdateAllFogMarkers(CanvasMarkerManager __instance) => VisitedIFWVs = new();

    // ...and then patch PCMO() itself to update the IFWV list and skip the vanilla impl if we detect non-trivial recursion.

    [HarmonyPrefix, HarmonyPatch(typeof(InnerFogWarpVolume), nameof(InnerFogWarpVolume.PropagateCanvasMarkerOutwards))]
    public static bool InnerFogWarpVolume_PropagateCanvasMarkerOutwards(InnerFogWarpVolume __instance, CanvasMarker marker, bool addMarker, float warpDist = 0f)
    {
        if (CurrentDBLayout == null)
        {
            //APRandomizer.OWMLModConsole.WriteLine($"IFWV.PCMO() doing nothing because DB has not been randomized");
            return true;
        }

        if (!VisitedIFWVs.Contains(__instance))
        {
            VisitedIFWVs.Add(__instance);
            return true;
        }

        //APRandomizer.OWMLModConsole.WriteLine($"detected duplicate IFWV.PropagateCanvasMarkerOutwards() calls on {__instance}, skipping base game code to prevent infinite loop");
        if (marker.GetSecondaryLabelType() == CanvasMarker.SecondaryLabelType.NONE)
        {
            //APRandomizer.OWMLModConsole.WriteLine($"adding custom duplicate warning to {marker._label} marker since it has none of the vanilla warnings");
            marker._dangerIndicatorRootObj.SetActive(true);
            marker._markerWarningImageObj.SetActive(false);
            marker._secondaryTextField.text = "WARNING: DUPLICATE SIGNAL(S) DETECTED, UNABLE TO PINPOINT DIRECTION";
            marker._secondaryTextField.SetAllDirty();
        }
        return false;
    }
}
