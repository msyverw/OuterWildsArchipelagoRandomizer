using HarmonyLib;
using System.Collections;
using UnityEngine;

namespace ArchipelagoRandomizer.ItemImpls.FCProgression
{

    [HarmonyPatch]
    class ExpandedDictionary
    {
        private static IEnumerator renameTextCoroutine;

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

        [HarmonyPostfix, HarmonyPatch(typeof(NomaiWallText), nameof(NomaiWallText.LateInitialize))]
        public static void RenameText(NomaiWallText __instance)
        {
            // We rename the material of Dree text to steal control of the translation from FC
            if (__instance.gameObject.GetComponent<OWRenderer>())
                if (__instance.gameObject.GetComponent<OWRenderer>().sharedMaterial.name.Contains("dree"))
                    __instance.gameObject.GetComponent<OWRenderer>().sharedMaterial.name = "dre_text";
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(NomaiTranslatorProp), nameof(NomaiTranslatorProp.DisplayTextNode))]
        public static bool HideDreeText(NomaiTranslatorProp __instance)
        {
            if (APRandomizer.NewHorizonsAPI == null) return true;

            //This flag checks if the targeted text is Dree text
            bool flag = __instance._scanBeams[0]._nomaiTextLine != null && __instance._scanBeams[0]._nomaiTextLine.gameObject
            .GetComponent<OWRenderer>().sharedMaterial.name.Contains("dre");

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
