using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace ArchipelagoRandomizer.ItemImpls.FCProgression
{
    class DeepBrambleFixes
    {
        private static System.Random prng = new();
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

            APRandomizer.OWMLModConsole.WriteLine("Patching Deep Bramble dimension");

            // For an unknown reason, the Recursive Node is getting disabled, so we just re-enable it here.
            GameObject.Find("BriarsHollow_Body/Sector/Loop Node").SetActive(true);

            // Randomize Graviton's Folly levers
            FieldInfo beamField = Type.GetType("DeepBramble.MiscBehaviours.Lever, DeepBramble", true).GetField("beamObject", BindingFlags.NonPublic | BindingFlags.Instance);
            List<object> levers = [
                GameObject.Find("GravitonsFolly_Body/Sector/hollowplanet/planet/crystal_core/beams/levers/lever1").GetComponent("Lever"),
                GameObject.Find("GravitonsFolly_Body/Sector/hollowplanet/planet/crystal_core/beams/levers/lever2").GetComponent("Lever"),
                GameObject.Find("GravitonsFolly_Body/Sector/hollowplanet/planet/crystal_core/beams/levers/lever3").GetComponent("Lever"),
                GameObject.Find("GravitonsFolly_Body/Sector/hollowplanet/planet/crystal_core/beams/levers/lever4").GetComponent("Lever"),
                GameObject.Find("GravitonsFolly_Body/Sector/hollowplanet/planet/crystal_core/beams/levers/lever5").GetComponent("Lever"),
                GameObject.Find("GravitonsFolly_Body/Sector/hollowplanet/planet/crystal_core/beams/levers/lever6").GetComponent("Lever"),
            ];
            var beams = levers.Select((l, i) => (beamField.GetValue(l), i + 1)).Cast<(object, int)>().OrderBy(_ => prng.Next()).ToList();

            for (int i = 0; i < levers.Count; i++) {
                beamField.SetValue(levers[i], beams[i].Item1);
            }
            APRandomizer.OWMLModConsole.WriteLine($"Randomized Folly levers: {string.Join(", ", beams.Select(b => $"Beam {b.Item2}"))}");

            //beams.FindIndex()

            // Update the dree text
            NomaiWallText comboHintText = GameObject.Find("GravitonsFolly_Body/Sector/hollowplanet/planet/crystal_core/crystal_lab/Props_NOM_Whiteboard_Shared/combo_hint_text").GetComponent<NomaiWallText>();
            string newText = comboHintText._dictNomaiTextData[2].TextNode.InnerText.Replace("OKALIS: Of course! If I remember correctly, all of the levers should be on, except for the second and the second-to-last.", "OKALIS: TEST TEST.");
            comboHintText._dictNomaiTextData[2].TextNode.InnerText = newText;
        }
    }
}
