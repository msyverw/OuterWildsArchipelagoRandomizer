using HarmonyLib;
using OWML.ModHelper;
using OWML.ModHelper.Events;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ArchipelagoRandomizer.ItemImpls.FCProgression
{
    [HarmonyPatch]
    class DeepBrambleCoordinates
    {
        private static bool _hasDeepBrambleCoordinates = false;

        public static bool hasDeepBrambleCoordinates
        {
            get => _hasDeepBrambleCoordinates;
            set
            {
                if (_hasDeepBrambleCoordinates != value)
                    _hasDeepBrambleCoordinates = value;

                if (_hasDeepBrambleCoordinates)
                {
                    Locator.GetShipLogManager()?.RevealFact("WARP_TO_DB_FACT", true, false);
                    Locator.GetShipLogManager()?.RevealFact("NOMAI_WARP_FACT_FC", true, false);
                }
            }
        }
        [HarmonyPrefix, HarmonyPatch(typeof(ShipLogManager), nameof(ShipLogManager.RevealFact))]
        public static bool RevealFactPatch(ShipLogManager __instance, string id)
        {
            // These log facts control your ability to warp to and from the Deep Bramble. These facts are items as a result.
            if (id == "WARP_TO_DB_FACT" || id == "NOMAI_WARP_FACT_FC")
            {
                if (!hasDeepBrambleCoordinates)
                    return false;
            }
            return true;
        }

        public static void ExitWarpFix()
        {
            APRandomizer.NewHorizonsAPI.DefineStarSystem("DeepBramble", "{ \"factRequiredToExitViaWarpDrive\": \"NOMAI_WARP_FACT_FC\"}", APRandomizer.Instance);
        }

        [HarmonyPrefix, HarmonyPatch(typeof(PlayerData), nameof(PlayerData.SetLastDeathType))]
        public static void PlayerData_SetLastDeathType() // FC sometimes changes the default system, so we reset the default system every time the player dies (for real).
        {
            if (APRandomizer.NewHorizonsAPI is null || !APRandomizer.SlotEnabledMod("enable_fc_mod"))
                return;

            if (Spawn.spawnChoice == Spawn.SpawnChoice.DeepBramble)
                APRandomizer.NewHorizonsAPI?.SetDefaultSystem("DeepBramble");
            else
                APRandomizer.NewHorizonsAPI?.SetDefaultSystem("SolarSystem");
        }
    }
}
