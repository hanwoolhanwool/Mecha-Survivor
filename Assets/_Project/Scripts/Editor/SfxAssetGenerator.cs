using System.IO;
using UnityEditor;
using UnityEngine;
using MechaSurvivor.Utilities;

namespace MechaSurvivor.Editor
{
    /// <summary>
    /// SfxRecipes 전체를 WAV로 구워 Assets/_Project/Audio/SFX 에 저장한다.
    /// 시드 고정 합성이므로 몇 번을 다시 실행해도 같은 파일이 나온다 (재현 가능).
    /// 외부 에셋으로 교체할 때는 SfxLibrary의 클립 참조만 바꾸면 된다.
    /// </summary>
    public static class SfxAssetGenerator
    {
        private const string OutputFolder = "Assets/_Project/Audio/SFX";

        [MenuItem("Mecha Survivor/Generate SFX WAVs")]
        public static void GenerateAll()
        {
            Directory.CreateDirectory(OutputFolder);

            foreach (var recipe in SfxRecipes.All)
            {
                float[] samples = recipe.Render();
                byte[] wav = WavEncoder.EncodePcm16(samples, SfxSynth.SampleRate);
                File.WriteAllBytes($"{OutputFolder}/{recipe.Id}.wav", wav);
            }

            AssetDatabase.Refresh();
            Debug.Log($"[SfxAssetGenerator] WAV {SfxRecipes.All.Count}개 생성 완료 → {OutputFolder}");
        }
    }
}
