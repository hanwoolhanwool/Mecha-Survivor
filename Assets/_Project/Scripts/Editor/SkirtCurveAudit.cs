// 임시 검증 스크립트 — 스커트(비휴먼) 본 커브가 Humanoid 임포트를 통과하는지 판정.
// 판정이 끝나면 이 파일은 삭제해도 된다.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace MechaSurvivor.EditorTools
{
    public static class SkirtCurveAudit
    {
        const string ModelPath = "Assets/_Project/Art/Models/Mecha/Mecha.fbx";
        const string RiggedDir = "Assets/_Project/Art/Models/Mecha/Rigged";

        // Mixamo 24본 중 Humanoid 매핑이 없는 본. 스커트 3본과 같은 처지다.
        static readonly string[] NonHumanBones = { "headfront", "head_end" };

        [MenuItem("Tools/Mecha/스커트 커브 검증")]
        public static void Run()
        {
            var sb = new StringBuilder();
            sb.AppendLine("# 비휴먼 본 커브 생존 검증");
            sb.AppendLine("Unity " + Application.unityVersion);
            sb.AppendLine();

            // ── 1. 아바타의 human 매핑 vs skeleton ──────────────────────────
            sb.AppendLine("## 1. Mecha.fbx 아바타");
            var importer = AssetImporter.GetAtPath(ModelPath) as ModelImporter;
            if (importer == null)
            {
                sb.AppendLine("!! ModelImporter를 못 얻음: " + ModelPath);
            }
            else
            {
                var hd = importer.humanDescription;
                var humanBoneNames = new HashSet<string>(hd.human.Select(h => h.boneName));
                var skeletonNames = hd.skeleton.Select(s => s.name).ToList();

                sb.AppendLine("animationType   : " + importer.animationType);
                sb.AppendLine("avatarSetup     : " + importer.avatarSetup);
                sb.AppendLine("human 매핑      : " + hd.human.Length + "개");
                sb.AppendLine("skeleton 총계   : " + skeletonNames.Count + "개 (메시·엠프티 포함)");

                var avatar = AssetDatabase.LoadAllAssetsAtPath(ModelPath).OfType<Avatar>().FirstOrDefault();
                sb.AppendLine("Avatar          : " + (avatar == null ? "없음"
                    : avatar.name + " isValid=" + avatar.isValid + " isHuman=" + avatar.isHuman));

                sb.AppendLine();
                sb.AppendLine("### 비휴먼 본이 아바타 skeleton에 있는가");
                foreach (var bone in NonHumanBones)
                {
                    bool inSkel = skeletonNames.Contains(bone);
                    bool inHuman = humanBoneNames.Contains(bone);
                    sb.AppendLine(string.Format("  {0,-12} skeleton={1,-5} human매핑={2}", bone, inSkel, inHuman));
                }
                sb.AppendLine("  (스커트 3본 HipArmor_L/R·HipDetail_L 은 현재 FBX에 아예 없음 — 27본 개편분)");
            }

            // ── 2. 임포트된 AnimationClip의 커브 바인딩 ─────────────────────
            sb.AppendLine();
            sb.AppendLine("## 2. 임포트된 클립의 커브 바인딩");
            sb.AppendLine("Humanoid 클립은 muscle 커브(path=\"\")로 저장된다.");
            sb.AppendLine("비휴먼 본이 살아남았다면 path에 본 경로가 박힌 generic 커브로 나타난다.");
            sb.AppendLine();

            var fbxFiles = Directory.Exists(RiggedDir)
                ? Directory.GetFiles(RiggedDir, "*.fbx").OrderBy(f => f).ToArray()
                : new string[0];

            if (fbxFiles.Length == 0) sb.AppendLine("!! " + RiggedDir + " 에 FBX가 없음");

            int totalClips = 0, clipsWithNonHuman = 0;
            var samplePrinted = false;

            foreach (var file in fbxFiles)
            {
                var assetPath = file.Replace('\\', '/');
                var idx = assetPath.IndexOf("Assets/", StringComparison.Ordinal);
                if (idx >= 0) assetPath = assetPath.Substring(idx);

                var clips = AssetDatabase.LoadAllAssetsAtPath(assetPath).OfType<AnimationClip>()
                            .Where(c => !c.name.StartsWith("__preview__")).ToList();

                foreach (var clip in clips)
                {
                    totalClips++;
                    var bindings = AnimationUtility.GetCurveBindings(clip);
                    var pathed = bindings.Where(b => !string.IsNullOrEmpty(b.path)).ToList();

                    var hitBones = new List<string>();
                    foreach (var bone in NonHumanBones)
                        if (pathed.Any(b => b.path.EndsWith("/" + bone, StringComparison.Ordinal)
                                         || b.path == bone))
                            hitBones.Add(bone);

                    if (hitBones.Count > 0) clipsWithNonHuman++;

                    // 첫 클립은 바인딩을 통째로 덤프해서 실제 구조를 본다
                    if (!samplePrinted)
                    {
                        samplePrinted = true;
                        sb.AppendLine("### 샘플 덤프: " + clip.name + "  (" + Path.GetFileName(file) + ")");
                        sb.AppendLine("  isHumanMotion=" + clip.humanMotion + " legacy=" + clip.legacy
                                      + " length=" + clip.length.ToString("F3") + "s frameRate=" + clip.frameRate);
                        sb.AppendLine("  바인딩 총 " + bindings.Length + "개 / path 있는 것 " + pathed.Count + "개");
                        var byPath = pathed.GroupBy(b => b.path).OrderBy(g => g.Key);
                        foreach (var g in byPath)
                            sb.AppendLine("    path=\"" + g.Key + "\"  커브 " + g.Count() + "개");
                        var muscle = bindings.Where(b => string.IsNullOrEmpty(b.path))
                                             .Select(b => b.propertyName).Take(8);
                        sb.AppendLine("    muscle/root 커브 예시: " + string.Join(", ", muscle.ToArray()));
                        sb.AppendLine();
                    }

                    sb.AppendLine(string.Format("{0,-34} 바인딩={1,4} path있음={2,4} 비휴먼={3}",
                        clip.name, bindings.Length, pathed.Count,
                        hitBones.Count == 0 ? "없음" : string.Join(",", hitBones.ToArray())));
                }
            }

            // ── 3. 판정 ─────────────────────────────────────────────────────
            sb.AppendLine();
            sb.AppendLine("## 3. 판정");
            sb.AppendLine("클립 총 " + totalClips + "개 중 비휴먼 본 커브를 가진 클립: " + clipsWithNonHuman + "개");
            sb.AppendLine();
            if (totalClips == 0)
            {
                sb.AppendLine("판정 불가 — 클립을 하나도 못 읽었다.");
            }
            else if (clipsWithNonHuman > 0)
            {
                sb.AppendLine("=> 비휴먼 본 커브가 Humanoid 임포트를 **통과한다**.");
                sb.AppendLine("   스커트 3본도 같은 방식으로 살아남을 가능성이 높다. 27본 실물 검증으로 진행할 것.");
            }
            else
            {
                sb.AppendLine("=> 비휴먼 본 커브가 **전부 제거됐다**.");
                sb.AppendLine("   Humanoid 파이프라인에서 스커트 오토 스윙은 전달되지 않는다.");
                sb.AppendLine("   27본으로 가는 의미가 없다 — 24본 유지 + Unity 절차 연출이 답이다.");
            }

            var outPath = @"C:\Users\iam12\AppData\Local\Temp\claude\C--Users-iam12-Desktop-Mecha-Survival-Speed-Ver1\aea0262b-8184-4038-8ba0-8d2794ed5b59\scratchpad\skirt_curve_audit.txt";
            Directory.CreateDirectory(Path.GetDirectoryName(outPath));
            File.WriteAllText(outPath, sb.ToString(), new UTF8Encoding(false));
            Debug.Log("[SkirtCurveAudit] 완료 → " + outPath + "\n\n" + sb);
        }
    }
}
