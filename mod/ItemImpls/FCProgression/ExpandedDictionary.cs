using HarmonyLib;
using System.Collections;
using UnityEngine;
using static StencilPreviewImageEffect;

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

        [HarmonyPostfix, HarmonyPatch(typeof(NomaiTextLine), nameof(NomaiWallText.Update))]
        public static void RenameText(NomaiTextLine __instance)
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

            bool isDreeText = __instance._scanBeams[0]._nomaiTextLine != null && (__instance._scanBeams[0]._nomaiTextLine.gameObject
            .GetComponent<OWRenderer>().sharedMaterial.name.Contains("dre") || __instance._scanBeams[0]._nomaiTextLine.gameObject
            .GetComponent<OWRenderer>().sharedMaterial.name.Contains("IP"));

            //If the text is dree, and the player lacks the upgrade, hide the text
            if (isDreeText && APRandomizer.NewHorizonsAPI.GetCurrentStarSystem() == "DeepBramble")
            {
                if (!hasExpandedDictionary)
                {
                    __instance._textField.text = UITextLibrary.GetString(UITextType.TranslatorUntranslatableWarning);
                    return false;
                }
                else if (__instance._translationTimeElapsed == 0f && !__instance._nomaiTextComponent.IsTranslated(__instance._currentTextID))
                {   // Update the untranslated message when active
                    __instance._textField.text = "<!> Untranslated Dree writing <!>";
                    return false;
                }
            }
            //Otherwise, run normally
            return true;
        }
    }
}
