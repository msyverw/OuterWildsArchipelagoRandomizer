using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ArchipelagoRandomizer.ItemImpls.FCProgression
{
    class DeepBrambleFixes
    {
        private static IEnumerator deepBrambleFixesCoroutine;
        public static void OnDeepBrambleLoadEvent()
        {
            if (APRandomizer.NewHorizonsAPI == null) return;
            if (APRandomizer.NewHorizonsAPI.GetCurrentStarSystem() != "DeepBramble") return;

            deepBrambleFixesCoroutine = FixDeepBramble();
            APRandomizer.Instance.StartCoroutine(deepBrambleFixesCoroutine);
        }

        private static IEnumerator FixDeepBramble()
        {
            // If we do this too quickly the triggers have issues when re-enabled
            yield return new WaitForSeconds(1f);

            // For an unknown reason, the Recursive Node is getting disabled, so we just re-enable it here.
            GameObject.Find("BriarsHollow_Body/Sector/Loop Node").SetActive(true);

            // TODO:
            GameObject lever1Object = GameObject.Find("GravitonsFolly_Body/Sector/hollowplanet/planet/crystal_core/beams/levers/lever1");
            GameObject lever2Object = GameObject.Find("GravitonsFolly_Body/Sector/hollowplanet/planet/crystal_core/beams/levers/lever2");
            GameObject lever3Object = GameObject.Find("GravitonsFolly_Body/Sector/hollowplanet/planet/crystal_core/beams/levers/lever3");
            GameObject lever4Object = GameObject.Find("GravitonsFolly_Body/Sector/hollowplanet/planet/crystal_core/beams/levers/lever4");
            GameObject lever5Object = GameObject.Find("GravitonsFolly_Body/Sector/hollowplanet/planet/crystal_core/beams/levers/lever5");
            GameObject lever6Object = GameObject.Find("GravitonsFolly_Body/Sector/hollowplanet/planet/crystal_core/beams/levers/lever6");
            // Lever lever1 = lever1Object.GetComponent<DeepBramble.MiscBehaviours.Lever>();
            // GameObject beam1 = lever1.beamObject;

            // Update the dree text
            NomaiWallText comboHintText = GameObject.Find("GravitonsFolly_Body/Sector/hollowplanet/planet/crystal_core/crystal_lab/Props_NOM_Whiteboard_Shared/combo_hint_text").GetComponent<NomaiWallText>();
            string newText = comboHintText._dictNomaiTextData[2].TextNode.InnerText.Replace("OKALIS: Of course! If I remember correctly, all of the levers should be on, except for the second and the second-to-last.", "");
            comboHintText._dictNomaiTextData[2].TextNode.InnerText = newText;
        }
    }
}
