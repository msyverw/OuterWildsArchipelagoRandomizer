using HarmonyLib;
using UnityEngine;

namespace ArchipelagoRandomizer.ItemImpls.FCProgression
{

    [HarmonyPatch]
    class ExpandedDictionary
    {
        private static bool _hasExpandedDictionary = false;

        public static bool hasExpandedDictionary
        {
            get => _hasExpandedDictionary;
            set
            {
                _hasExpandedDictionary = value;

                if (_hasExpandedDictionary)
                {
                    var nd = new NotificationData(NotificationTarget.Player, "RECONFIGURING TRANSLATOR TO INCLUDE DREE TRANSLATION DICTIONARY.", 10);
                    NotificationManager.SharedInstance.PostNotification(nd, false);
                }
            }
        }

        public static void OnDeepBrambleLoadEvent()
        {
            if (APRandomizer.NewHorizonsAPI == null) return;
            if (APRandomizer.NewHorizonsAPI.GetCurrentStarSystem() != "DeepBramble") return;

            // We rename the material of Dree text to steal control of the translation from FC
            GameObject.Find("GravitonsFolly_Body/Sector/hollowplanet/planet/crystal_core/crystal_lab/Props_NOM_Whiteboard_Shared/combo_hint_text/Arc 2 - Child of 1").GetComponent<OWRenderer>().sharedMaterial.name = "bramble_text";
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(NomaiTranslatorProp), nameof(NomaiTranslatorProp.DisplayTextNode))]
        public static bool HideDreeText(NomaiTranslatorProp __instance)
        {
            if (APRandomizer.NewHorizonsAPI == null) return true;

            //This flag checks if the targeted text is Dree text
            bool flag = __instance._scanBeams[0]._nomaiTextLine != null && __instance._scanBeams[0]._nomaiTextLine.gameObject
            .GetComponent<OWRenderer>().sharedMaterial.name.Contains("bramble");

            //If the text is dree, and the player lacks the upgrade, hide the text
            if (flag && APRandomizer.NewHorizonsAPI.GetCurrentStarSystem() == "DeepBramble" && !hasExpandedDictionary)
            {
                __instance._textField.text = UITextLibrary.GetString(UITextType.TranslatorUntranslatableWarning);
                return false;
            }
            //Otherwise, run normally
            return true;
        }
    }
}
