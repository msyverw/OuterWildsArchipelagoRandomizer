using OWML.ModHelper.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchipelagoRandomizer.ItemImpls.FCProgression
{
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
                    Locator.GetShipLogManager().RevealFact("WARP_TO_DB_FACT", true, false);
                }
            }
        }

        public static void OnCompleteSceneLoad()
        {
            if (hasDeepBrambleCoordinates)
                Locator.GetShipLogManager().RevealFact("WARP_TO_DB_FACT", true, false);
            else
                Locator.GetShipLogManager().GetFact("WARP_TO_DB_FACT")._save.revealOrder = -1;
        }
    }
}
