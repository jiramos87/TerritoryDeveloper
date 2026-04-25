using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using Territory.Audio;

namespace Territory.EditorTools.Audio
{
    /// <summary>
    /// Bakes <c>tools/fixtures/blip/{id}-v0.json</c> from the live C# kernel.
    ///
    /// Replaces the legacy TS port at <c>tools/scripts/blip-bake-fixtures.ts</c>
    /// which drifted vs the Unity DSP path because TS Math runs at double
    /// precision while the C# kernel runs at float32 throughout the per-sample
    /// chain (oscSum, envLevel, gainMult, x, filterZ1 all <c>float</c>).
    ///
    /// Bit-exact provenance — fixtures match <see cref="BlipGoldenFixtureTests"/>
    /// re-render to within 1e-6 by construction (same kernel call).
    ///
    /// Invoke from Unity menu (<b>Tools/Audio/Bake Blip Golden Fixtures</b>) or
    /// via batch:
    /// <code>
    /// $UNITY_EDITOR_PATH -batchmode -nographics -projectPath . \
    ///   -executeMethod Territory.EditorTools.Audio.BlipGoldenFixtureBaker.BakeAll \
    ///   -quit
    /// </code>
    /// </summary>
    public static class BlipGoldenFixtureBaker
    {
        private const int  SampleRate    = 48000;
        private const int  Seconds       = 1;
        private const int  VariantIndex  = 0;
        private const string OutDir      = "tools/fixtures/blip";

        private static readonly BlipId[] Ids =
        {
            BlipId.UiButtonHover,
            BlipId.UiButtonClick,
            BlipId.ToolRoadTick,
            BlipId.ToolRoadComplete,
            BlipId.ToolBuildingPlace,
            BlipId.ToolBuildingDenied,
            BlipId.WorldCellSelected,
            BlipId.EcoMoneyEarned,
            BlipId.EcoMoneySpent,
            BlipId.SysSaveGame,
        };

        [MenuItem("Tools/Audio/Bake Blip Golden Fixtures")]
        public static void BakeAll()
        {
            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            string outDir = Path.Combine(projectRoot, OutDir);
            Directory.CreateDirectory(outDir);

            int written = 0;
            foreach (BlipId id in Ids)
            {
                string assetPath = $"Assets/Audio/Blip/Patches/BlipPatch_{id}.asset";
                var patch = AssetDatabase.LoadAssetAtPath<BlipPatch>(assetPath);
                if (patch == null)
                {
                    Debug.LogError($"[BlipGoldenFixtureBaker] Missing SO at {assetPath}");
                    continue;
                }

                BlipPatchFlat flat = BlipPatchFlat.FromSO(patch);
                int sampleCount = SampleRate * Seconds;
                float[] buffer = new float[sampleCount];
                BlipVoiceState state = default;
                BlipVoice.Render(buffer, 0, buffer.Length, SampleRate, in flat, VariantIndex, ref state);

                // Inline mirrors of BlipTestFixtures.SumAbsHash + CountZeroCrossings
                // — duplicated here because BlipTestFixtures lives in the test asmdef
                // (Blip.Tests.EditMode) which Editor asmdef cannot reference.
                double sumAbsHash = 0.0;
                foreach (float s in buffer) sumAbsHash += Math.Abs(s);

                int zeroCrossings = 0;
                float prev = 0f;
                for (int i = 0; i < buffer.Length; i++)
                {
                    float s = buffer[i];
                    if (s == 0f) continue;
                    if (prev != 0f && ((prev < 0f) != (s < 0f))) zeroCrossings++;
                    prev = s;
                }

                // Stable JSON (no Unity JsonUtility — handcrafted to match
                // tools/scripts/blip-bake-fixtures.ts key order + style).
                var sb = new StringBuilder();
                sb.AppendLine("{");
                sb.AppendLine($"  \"id\": \"{id}\",");
                sb.AppendLine($"  \"variant\": {VariantIndex},");
                sb.AppendLine($"  \"patchHash\": {patch.PatchHash},");
                sb.AppendLine($"  \"sampleRate\": {SampleRate},");
                sb.AppendLine($"  \"sampleCount\": {sampleCount},");
                sb.AppendLine($"  \"sumAbsHash\": {sumAbsHash.ToString("R", System.Globalization.CultureInfo.InvariantCulture)},");
                sb.AppendLine($"  \"zeroCrossings\": {zeroCrossings}");
                sb.Append("}");
                sb.Append('\n');

                string outPath = Path.Combine(outDir, $"{id}-v0.json");
                File.WriteAllText(outPath, sb.ToString());
                Debug.Log($"[BlipGoldenFixtureBaker] Wrote {id}-v0.json sumAbsHash={sumAbsHash:R} zc={zeroCrossings} patchHash={patch.PatchHash}");
                written++;
            }

            Debug.Log($"[BlipGoldenFixtureBaker] Baked {written}/{Ids.Length} fixtures → {outDir}");
        }
    }
}
