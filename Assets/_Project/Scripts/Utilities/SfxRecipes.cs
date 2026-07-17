using System;
using System.Collections.Generic;

namespace MechaSurvivor.Utilities
{
    /// <summary>
    /// 게임 SFX 전체의 절차 합성 레시피. Id는 그대로 WAV 파일명·SfxLibrary 키가 된다.
    /// 무기 발사음의 Id는 WeaponData.Id와 일치시킨다 (예: "gatling", "missile_pod").
    /// 시드 고정 — 몇 번을 다시 구워도 같은 소리가 나온다.
    /// </summary>
    public static class SfxRecipes
    {
        public readonly struct Recipe
        {
            public readonly string Id;
            public readonly Func<float[]> Render;

            public Recipe(string id, Func<float[]> render)
            {
                Id = id;
                Render = render;
            }
        }

        public static IReadOnlyList<Recipe> All { get; } = new[]
        {
            // 무기 발사음 (Id = WeaponData.Id)
            new Recipe("gatling", Gatling),
            new Recipe("laser_cannon", LaserCannon),
            new Recipe("missile_pod", MissilePod),
            new Recipe("shotgun_cannon", ShotgunCannon),
            new Recipe("cluster_bomb", ClusterBomb),
            new Recipe("beam", Beam),
            new Recipe("railgun", Railgun),
            new Recipe("orbital_strike", OrbitalStrike),
            new Recipe("gravity_well", GravityWell),
            new Recipe("emp_field", EmpField),
            new Recipe("support_drone", SupportDrone),

            // 게임 이벤트음
            new Recipe("enemy_death", EnemyDeath),
            new Recipe("player_hit", PlayerHit),
            new Recipe("player_death", PlayerDeath),
            new Recipe("xp_pickup", XpPickup),
            new Recipe("level_up", LevelUp),
            new Recipe("run_clear", RunClear),
            new Recipe("run_fail", RunFail),

            // 메카 기계음 (표현 레이어)
            new Recipe("thruster_loop", ThrusterLoop),
            new Recipe("dash", Dash),
        };

        // ── 무기 ──────────────────────────────────────────────────────

        /// <summary>짧고 단단한 발포 틱 — 연사되므로 개별음은 아주 짧게.</summary>
        private static float[] Gatling()
        {
            var buf = SfxSynth.Buffer(0.07f);
            SfxSynth.AddNoise(buf, 0.9f, seed: 101);
            SfxSynth.LowPass(buf, 3800f, 1200f);
            SfxSynth.AddSine(buf, 100f, 55f, 0.55f);
            SfxSynth.ApplyDecay(buf, 0.014f);
            SfxSynth.SoftClip(buf, 2.2f);
            SfxSynth.FadeOut(buf, 0.01f);
            SfxSynth.Normalize(buf, 0.75f);
            return buf;
        }

        /// <summary>하강 스윕 구형파 "쀼웅" — 전형적 펄스 레이저.</summary>
        private static float[] LaserCannon()
        {
            var buf = SfxSynth.Buffer(0.12f);
            SfxSynth.AddSquare(buf, 1700f, 320f, 0.5f);
            SfxSynth.AddSine(buf, 2400f, 500f, 0.3f);
            SfxSynth.ApplyDecay(buf, 0.03f);
            SfxSynth.ApplyAttack(buf, 0.004f);
            SfxSynth.FadeOut(buf, 0.02f);
            SfxSynth.Normalize(buf, 0.7f);
            return buf;
        }

        /// <summary>상승 로우패스 노이즈 휙 — 팝업 사출 + 로켓 점화.</summary>
        private static float[] MissilePod()
        {
            var buf = SfxSynth.Buffer(0.38f);
            SfxSynth.AddNoise(buf, 0.85f, seed: 202);
            SfxSynth.LowPass(buf, 450f, 2600f);
            SfxSynth.AddSine(buf, 180f, 320f, 0.2f);
            SfxSynth.ApplyAttack(buf, 0.06f);
            SfxSynth.FadeOut(buf, 0.12f);
            SfxSynth.Normalize(buf, 0.7f);
            return buf;
        }

        /// <summary>묵직한 근접 포성 + 서브베이스 반동.</summary>
        private static float[] ShotgunCannon()
        {
            var buf = SfxSynth.Buffer(0.28f);
            SfxSynth.AddNoise(buf, 1f, seed: 303);
            SfxSynth.LowPass(buf, 1500f, 250f);
            SfxSynth.AddSine(buf, 90f, 45f, 0.8f);
            SfxSynth.ApplyDecay(buf, 0.05f);
            SfxSynth.SoftClip(buf, 3f);
            SfxSynth.FadeOut(buf, 0.05f);
            SfxSynth.Normalize(buf, 0.85f);
            return buf;
        }

        /// <summary>둔탁한 투척 "퉁" — 화려함은 분열 폭발(AreaDamage) 쪽 몫.</summary>
        private static float[] ClusterBomb()
        {
            var buf = SfxSynth.Buffer(0.2f);
            SfxSynth.AddSine(buf, 150f, 55f, 0.9f);
            var click = SfxSynth.Buffer(0.015f);
            SfxSynth.AddNoise(click, 0.6f, seed: 404);
            SfxSynth.LowPass(click, 3000f, 3000f);
            SfxSynth.MixInto(buf, click, 0f, 1f);
            SfxSynth.ApplyDecay(buf, 0.06f);
            SfxSynth.FadeOut(buf, 0.04f);
            SfxSynth.Normalize(buf, 0.75f);
            return buf;
        }

        /// <summary>차징 상승음 → 배음 섞인 굵은 험 — 이중 구조 빔(GDD 3.4 ⑥).</summary>
        private static float[] Beam()
        {
            var buf = SfxSynth.Buffer(0.8f);

            var charge = SfxSynth.Buffer(0.22f);
            SfxSynth.AddSine(charge, 220f, 950f, 0.6f);
            SfxSynth.ApplyAttack(charge, 0.2f);
            SfxSynth.MixInto(buf, charge, 0f, 1f);

            var hum = SfxSynth.Buffer(0.6f);
            SfxSynth.AddSaw(hum, 110f, 110f, 0.35f);
            SfxSynth.AddSaw(hum, 111.7f, 111.7f, 0.35f);   // 디튠 맥놀이 — "출력이 큰" 울림
            SfxSynth.AddSine(hum, 55f, 55f, 0.4f);
            SfxSynth.AddNoise(hum, 0.12f, seed: 505);
            SfxSynth.LowPass(hum, 1800f, 1200f);
            SfxSynth.ApplyAttack(hum, 0.03f);
            SfxSynth.MixInto(buf, hum, 0.2f, 1f);

            SfxSynth.SoftClip(buf, 1.8f);
            SfxSynth.FadeOut(buf, 0.18f);
            SfxSynth.Normalize(buf, 0.8f);
            return buf;
        }

        /// <summary>초고속 크랙 — 순간 임펄스 + 급강하 톤 + 공기 찢는 잔향.</summary>
        private static float[] Railgun()
        {
            var buf = SfxSynth.Buffer(0.32f);

            var crack = SfxSynth.Buffer(0.012f);
            SfxSynth.AddNoise(crack, 1f, seed: 606);
            SfxSynth.MixInto(buf, crack, 0f, 1f);

            SfxSynth.AddSine(buf, 1300f, 80f, 0.6f);

            var tail = SfxSynth.Buffer(0.28f);
            SfxSynth.AddNoise(tail, 0.5f, seed: 607);
            SfxSynth.LowPass(tail, 2500f, 300f);
            SfxSynth.MixInto(buf, tail, 0.01f, 1f);

            SfxSynth.ApplyDecay(buf, 0.05f);
            SfxSynth.SoftClip(buf, 2.5f);
            SfxSynth.FadeOut(buf, 0.06f);
            SfxSynth.Normalize(buf, 0.85f);
            return buf;
        }

        /// <summary>대형 폭발 — 긴 저역 붕괴 + 서브베이스. 궤도 폭격의 무게.</summary>
        private static float[] OrbitalStrike()
        {
            var buf = SfxSynth.Buffer(1.1f);
            SfxSynth.AddNoise(buf, 1f, seed: 707);
            SfxSynth.LowPass(buf, 2800f, 70f);
            SfxSynth.AddSine(buf, 60f, 35f, 0.9f);
            SfxSynth.ApplyDecay(buf, 0.22f);
            SfxSynth.SoftClip(buf, 3.5f);
            SfxSynth.FadeOut(buf, 0.3f);
            SfxSynth.Normalize(buf, 0.9f);
            return buf;
        }

        /// <summary>저역 FM 워블 — 공간이 뒤틀리는 어두운 음.</summary>
        private static float[] GravityWell()
        {
            var buf = SfxSynth.Buffer(0.65f);
            SfxSynth.AddFmSine(buf, 130f, 45f, 6.5f, 0.7f);
            SfxSynth.AddFmSine(buf, 65f, 20f, 6.5f, 0.5f);
            SfxSynth.ApplyAttack(buf, 0.08f);
            SfxSynth.FadeOut(buf, 0.25f);
            SfxSynth.Normalize(buf, 0.7f);
            return buf;
        }

        /// <summary>지직거리는 전기 버즈 — 블록 뮤트로 신호 끊김 질감.</summary>
        private static float[] EmpField()
        {
            var buf = SfxSynth.Buffer(0.45f);
            SfxSynth.AddSquare(buf, 240f, 180f, 0.45f);
            SfxSynth.AddSquare(buf, 1100f, 700f, 0.2f);
            SfxSynth.AddNoise(buf, 0.3f, seed: 808);
            SfxSynth.LowPass(buf, 3200f, 1500f);
            SfxSynth.Gate(buf, 0.012f, 0.35f, seed: 809);
            SfxSynth.ApplyAttack(buf, 0.01f);
            SfxSynth.ApplyDecay(buf, 0.18f);
            SfxSynth.FadeOut(buf, 0.08f);
            SfxSynth.Normalize(buf, 0.65f);
            return buf;
        }

        /// <summary>드론의 가벼운 짧은 잽 — 플레이어 무기보다 존재감을 낮춘다.</summary>
        private static float[] SupportDrone()
        {
            var buf = SfxSynth.Buffer(0.08f);
            SfxSynth.AddSine(buf, 1250f, 650f, 0.5f);
            SfxSynth.ApplyDecay(buf, 0.02f);
            SfxSynth.ApplyAttack(buf, 0.003f);
            SfxSynth.FadeOut(buf, 0.015f);
            SfxSynth.Normalize(buf, 0.5f);
            return buf;
        }

        // ── 게임 이벤트 ───────────────────────────────────────────────

        /// <summary>소형 폭발 팝 — 수십 개가 겹치므로 짧고 가볍게.</summary>
        private static float[] EnemyDeath()
        {
            var buf = SfxSynth.Buffer(0.22f);
            SfxSynth.AddNoise(buf, 0.9f, seed: 909);
            SfxSynth.LowPass(buf, 1400f, 200f);
            SfxSynth.AddSine(buf, 110f, 45f, 0.5f);
            SfxSynth.ApplyDecay(buf, 0.045f);
            SfxSynth.SoftClip(buf, 2f);
            SfxSynth.FadeOut(buf, 0.05f);
            SfxSynth.Normalize(buf, 0.65f);
            return buf;
        }

        /// <summary>금속 타격 + 저역 경고 — 피격을 소리로도 확실히 알린다.</summary>
        private static float[] PlayerHit()
        {
            var buf = SfxSynth.Buffer(0.16f);
            SfxSynth.AddSine(buf, 320f, 300f, 0.5f);
            SfxSynth.AddSine(buf, 487f, 460f, 0.35f);      // 비정수 배음 = 금속성
            SfxSynth.AddNoise(buf, 0.35f, seed: 111);
            SfxSynth.LowPass(buf, 2500f, 800f);
            SfxSynth.ApplyDecay(buf, 0.035f);
            SfxSynth.FadeOut(buf, 0.03f);
            SfxSynth.Normalize(buf, 0.8f);
            return buf;
        }

        /// <summary>대폭발 + 시스템 다운 하강음.</summary>
        private static float[] PlayerDeath()
        {
            var buf = SfxSynth.Buffer(1f);
            SfxSynth.AddNoise(buf, 0.9f, seed: 222);
            SfxSynth.LowPass(buf, 2200f, 100f);
            SfxSynth.AddSine(buf, 420f, 50f, 0.5f);
            SfxSynth.ApplyDecay(buf, 0.25f);
            SfxSynth.SoftClip(buf, 2.5f);
            SfxSynth.FadeOut(buf, 0.3f);
            SfxSynth.Normalize(buf, 0.9f);
            return buf;
        }

        /// <summary>짧은 상승 블립 — 초당 수십 개 먹으므로 존재감 최소.</summary>
        private static float[] XpPickup()
        {
            var buf = SfxSynth.Buffer(0.06f);
            SfxSynth.AddSine(buf, 950f, 1500f, 0.5f);
            SfxSynth.ApplyAttack(buf, 0.005f);
            SfxSynth.ApplyDecay(buf, 0.02f);
            SfxSynth.FadeOut(buf, 0.012f);
            SfxSynth.Normalize(buf, 0.4f);
            return buf;
        }

        /// <summary>장3화음 상행 아르페지오 — 보상의 소리.</summary>
        private static float[] LevelUp()
        {
            return Arpeggio(new[] { 523.25f, 659.25f, 783.99f }, 0.11f, 0.32f, 0.55f);
        }

        /// <summary>옥타브까지 오르는 4음 팡파르.</summary>
        private static float[] RunClear()
        {
            return Arpeggio(new[] { 523.25f, 659.25f, 783.99f, 1046.5f }, 0.16f, 0.5f, 1.1f);
        }

        /// <summary>단3도 하행 — 낮게 가라앉는 실패음.</summary>
        private static float[] RunFail()
        {
            var buf = SfxSynth.Buffer(1.1f);
            var first = Note(392f, 0.5f);
            var second = Note(311.13f, 0.7f);
            SfxSynth.MixInto(buf, first, 0f, 0.8f);
            SfxSynth.MixInto(buf, second, 0.35f, 1f);
            SfxSynth.FadeOut(buf, 0.25f);
            SfxSynth.Normalize(buf, 0.6f);
            return buf;
        }

        // ── 메카 기계음 ───────────────────────────────────────────────

        /// <summary>무이음 루프 스러스터 — 저역 노이즈 + 미세 험. 볼륨·피치는 런타임이 조절.</summary>
        private static float[] ThrusterLoop()
        {
            var buf = SfxSynth.Buffer(1.3f);
            SfxSynth.AddNoise(buf, 0.8f, seed: 333);
            SfxSynth.LowPass(buf, 520f, 520f);
            SfxSynth.AddSine(buf, 84f, 84f, 0.18f);
            var loop = SfxSynth.CrossfadeLoop(buf, 0.15f);
            SfxSynth.Normalize(loop, 0.5f);
            return loop;
        }

        /// <summary>급가속 휙 — 밝은 노이즈가 저역으로 훑고 지나간다.</summary>
        private static float[] Dash()
        {
            var buf = SfxSynth.Buffer(0.28f);
            SfxSynth.AddNoise(buf, 0.9f, seed: 444);
            SfxSynth.LowPass(buf, 3200f, 500f);
            SfxSynth.ApplyAttack(buf, 0.03f);
            SfxSynth.ApplyDecay(buf, 0.09f);
            SfxSynth.FadeOut(buf, 0.06f);
            SfxSynth.Normalize(buf, 0.6f);
            return buf;
        }

        // ── 공용 헬퍼 ─────────────────────────────────────────────────

        private static float[] Note(float freq, float seconds)
        {
            var buf = SfxSynth.Buffer(seconds);
            SfxSynth.AddSine(buf, freq, freq, 0.6f);
            SfxSynth.AddSine(buf, freq * 2f, freq * 2f, 0.15f);
            SfxSynth.ApplyAttack(buf, 0.008f);
            SfxSynth.ApplyDecay(buf, seconds * 0.35f);
            SfxSynth.FadeOut(buf, 0.03f);
            return buf;
        }

        private static float[] Arpeggio(float[] freqs, float interval, float noteLength, float total)
        {
            var buf = SfxSynth.Buffer(total);
            for (int i = 0; i < freqs.Length; i++)
            {
                SfxSynth.MixInto(buf, Note(freqs[i], noteLength), interval * i, 1f);
            }

            SfxSynth.FadeOut(buf, 0.1f);
            SfxSynth.Normalize(buf, 0.6f);
            return buf;
        }
    }
}
