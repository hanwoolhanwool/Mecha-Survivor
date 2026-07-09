using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace MechaSurvivor.Editor
{
    /// <summary>
    /// CLI 배치 빌드용 진입점. 로컬/자체 CI에서 -executeMethod 로 호출한다.
    /// (GitHub Actions는 GameCI 기본 빌더를 사용하므로 이 스크립트가 필요 없다.)
    ///
    /// 예:
    ///   Unity.exe -quit -batchmode -nographics -projectPath "&lt;프로젝트&gt;"
    ///     -executeMethod MechaSurvivor.Editor.BuildScript.PerformWindowsBuild
    ///     -buildPath "Builds/Windows/MechaSurvivor.exe" -logFile -
    /// </summary>
    public static class BuildScript
    {
        private const string DefaultOutput = "Builds/Windows/MechaSurvivor.exe";

        public static void PerformWindowsBuild()
        {
            string buildPath = GetArg("-buildPath") ?? DefaultOutput;

            var options = new BuildPlayerOptions
            {
                scenes = EnabledScenes(),
                locationPathName = buildPath,
                target = BuildTarget.StandaloneWindows64,
                targetGroup = BuildTargetGroup.Standalone,
                options = BuildOptions.None,
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary summary = report.summary;

            if (summary.result == BuildResult.Succeeded)
            {
                Debug.Log($"[BuildScript] 성공: {summary.totalSize} bytes → {buildPath}");
                EditorApplication.Exit(0);
            }
            else
            {
                Debug.LogError($"[BuildScript] 실패: {summary.result} (errors: {summary.totalErrors})");
                EditorApplication.Exit(1);
            }
        }

        /// <summary>빌드 세팅에서 활성화된 씬 경로만 수집.</summary>
        private static string[] EnabledScenes()
        {
            return EditorBuildSettings.scenes
                .Where(scene => scene.enabled)
                .Select(scene => scene.path)
                .ToArray();
        }

        /// <summary>커맨드라인 인자(-name value) 파싱.</summary>
        private static string GetArg(string name)
        {
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (args[i] == name)
                {
                    return args[i + 1];
                }
            }

            return null;
        }
    }
}
