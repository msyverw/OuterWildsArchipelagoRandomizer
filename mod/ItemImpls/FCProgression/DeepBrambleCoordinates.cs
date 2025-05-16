using HarmonyLib;
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
                }
            }
        }
        [HarmonyPrefix, HarmonyPatch(typeof(ShipLogManager), nameof(ShipLogManager.RevealFact))]
        public static bool RevealFactPatch(ShipLogManager __instance, string id)
        {
            // This log fact controlls your ability to warp to the Deep Bramble. This fact is an item, rather than a logsanity location, as a result.
            if (id == "WARP_TO_DB_FACT")
            {
                if (!hasDeepBrambleCoordinates)
                    return false;
            }
            return true;
        }
    }
}
