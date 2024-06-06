﻿using HarmonyLib;
using UnityEngine;

namespace ArchipelagoRandomizer;

[HarmonyPatch]
internal class Spawn
{
    enum SpawnChoice
    {
        Vanilla,
        HourglassTwins,
        TimberHearth,
        BrittleHollow,
        GiantsDeep,
    }

    private static SpawnChoice spawnChoice = SpawnChoice.BrittleHollow;// SpawnChoice.Vanilla;

    public static void ApplySlotData(long spawnChoiceSlotData)
    {
        return;
        if (spawnChoiceSlotData == 0)
        {
            // do nothing, let the base game handle vanilla spawn
        }
        else
        {
            switch (spawnChoiceSlotData)
            {
                case /*"hourglass_twins"*/ 1: spawnChoice = SpawnChoice.HourglassTwins; break;
                case /*"timber_hearth"*/   2: spawnChoice = SpawnChoice.TimberHearth; break;
                case /*"brittle_hollow"*/  3: spawnChoice = SpawnChoice.BrittleHollow; break;
                case /*"giants_deep"*/     4: spawnChoice = SpawnChoice.GiantsDeep; break;
            }

            EnsureTimeLoopStarted(); // all non-vanilla spawns need this
        }
    }

    private static void EnsureTimeLoopStarted()
    {
        // For whatever reason, the base game uses the LAUNCH_CODES_GIVEN condition to track
        // the time loop being started, not whether the launch codes have been given yet.

        // We're calling these methods directly on the GameSave instead of PlayerData, because
        // PlayerData::SetPersistentCondition() specifically avoids saving LAUNCH_CODES_GIVEN.
        // See TODO for details.
        if (!PlayerData._currentGameSave.PersistentConditionExists("LAUNCH_CODES_GIVEN"))
        {
            APRandomizer.OWMLModConsole.WriteLine($"Spawn::EnsureTimeLoopStarted() setting LAUNCH_CODES_GIVEN condition to true, since this player has a non-vanilla spawn");
            PlayerData._currentGameSave.SetPersistentCondition("LAUNCH_CODES_GIVEN", true);
        }
    }

    private static bool spawnInSuitNextUpdate = false;

    public static void OnCompleteSceneLoad(OWScene _scene, OWScene loadScene)
    {
        if (loadScene != OWScene.SolarSystem) return;

        if (spawnChoice != SpawnChoice.Vanilla)
        {
            APRandomizer.OWMLModConsole.WriteLine($"Spawn::OnCompleteSceneLoad() setting spawnInSuitNextUpdate to true");
            spawnInSuitNextUpdate = true;
        }
    }

    [HarmonyPrefix, HarmonyPatch(typeof(PlayerSpawner), nameof(PlayerSpawner.Update))]
    public static void PlayerSpawner_Update(PlayerSpacesuit __instance)
    {
        if (spawnInSuitNextUpdate)
        {
            APRandomizer.OWMLModConsole.WriteLine($"instant SuitUp()");
            Locator.GetPlayerSuit().SuitUp(isTrainingSuit: false, instantSuitUp: true, putOnHelmet: true);
            spawnInSuitNextUpdate = false;
        }
    }

    [HarmonyPrefix, HarmonyPatch(typeof(AlignPlayerWithForce), nameof(AlignPlayerWithForce.OnSuitUp))]
    public static bool AlignPlayerWithForce_OnSuitUp(AlignPlayerWithForce __instance)
    {
        if (spawnInSuitNextUpdate)
        {
            APRandomizer.OWMLModConsole.WriteLine($"skipping AlignPlayerWithForce::OnSuitUp() call so the player wakes up facing the sky despite wearing the spacesuit");
            return false;
        }
        APRandomizer.OWMLModConsole.WriteLine($"normal AlignPlayerWithForce::OnSuitUp() call");
        return true;
    }

    [HarmonyPrefix, HarmonyPatch(typeof(PlayerSpawner), nameof(PlayerSpawner.SpawnPlayer))]
    public static void PlayerSpawner_SpawnPlayer(PlayerSpawner __instance)
    {
        if (spawnChoice == SpawnChoice.Vanilla || spawnChoice == SpawnChoice.TimberHearth)
        {
            APRandomizer.OWMLModConsole.WriteLine($"PlayerSpawner_SpawnPlayer doing nothing, since we're spawning in TH village");
            return;
        }

        //APRandomizer.OWMLModConsole.WriteLine($"_spawnList: {string.Join("\n",
        //    __instance._spawnList.Select(sp => $"{sp?.transform.parent.name}/{sp?.name}|{sp?._spawnLocation}|{sp?._isShipSpawn}|{sp?._triggerVolumes?.Count()}|{sp?._attachedBody?.name}|{sp?.transform.position}"))}");

        if (spawnChoice == SpawnChoice.HourglassTwins)
        {
            var emberTwinGO = Locator.GetAstroObject(AstroObject.Name.CaveTwin).gameObject;

            var sp = emberTwinGO.transform.Find("SPAWNS/Spawn_ChertsCamp").GetComponent<SpawnPoint>();
            __instance._initialSpawnPoint = sp;
            APRandomizer.OWMLModConsole.WriteLine($"PlayerSpawner_SpawnPlayer set player spawn {sp.transform.position}");

            OWRigidbody owrigidbody = Locator.GetShipBody();
            var offsetFromPlanet = new Vector3(9, 152.45f, 16); // from in-game testing
            var shipPos = emberTwinGO.transform.TransformPoint(offsetFromPlanet);
            owrigidbody.WarpToPositionRotation(shipPos, sp.transform.rotation);
            owrigidbody.SetVelocity(sp.GetPointVelocity());
            owrigidbody.GetRequiredComponent<MatchInitialMotion>().SetBodyToMatch(sp?._attachedBody);
            APRandomizer.OWMLModConsole.WriteLine($"PlayerSpawner_SpawnPlayer set ship spawn {offsetFromPlanet} / {shipPos} / {owrigidbody.transform.position}");
            return;
        }

        if (spawnChoice == SpawnChoice.BrittleHollow)
        {
            var riebeckOldCampfireGO = GameObject.Find("BrittleHollow_Body/Sector_BH/Sector_Crossroads/Interactables_Crossroads/VisibleFrom_BH/Prefab_HEA_Campfire/");

            var brittleHollowGO = Locator.GetAstroObject(AstroObject.Name.BrittleHollow).gameObject;

            // how do we spawn the player at a location that isn't one of the built-in SpawnPoints???
            var sp = brittleHollowGO.transform.Find("SPAWNS_PLAYER/SPAWN_OldCamp").GetComponent<SpawnPoint>();
            __instance._initialSpawnPoint = sp;
            APRandomizer.OWMLModConsole.WriteLine($"PlayerSpawner_SpawnPlayer set player spawn {sp.transform.position}");

            OWRigidbody owrigidbody = Locator.GetShipBody();
            var offsetFromPlanet = new Vector3(-6, 10, 285); // from in-game testing
            var shipPos = brittleHollowGO.transform.TransformPoint(offsetFromPlanet);
            owrigidbody.WarpToPositionRotation(shipPos, riebeckOldCampfireGO.transform.rotation);
            owrigidbody.SetVelocity(sp.GetPointVelocity());
            owrigidbody.GetRequiredComponent<MatchInitialMotion>().SetBodyToMatch(sp?._attachedBody);
            APRandomizer.OWMLModConsole.WriteLine($"PlayerSpawner_SpawnPlayer set ship spawn {offsetFromPlanet} / {shipPos} / {owrigidbody.transform.position}");
            return;
        }

        if (spawnChoice == SpawnChoice.GiantsDeep)
        {
            return;
        }
    }
}
