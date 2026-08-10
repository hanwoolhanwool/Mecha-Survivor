using UnityEngine;

namespace MechaSurvivor.Gameplay
{
    /// <summary>
    /// 전기 테두리 링의 순수 수학. 지그재그 원형 폴리라인 좌표 생성을
    /// MonoBehaviour 밖에 둬서 EditMode 테스트로 검증한다.
    /// </summary>
    public static class ElectricRingMath
    {
        /// <summary>
        /// buffer를 XZ 평면의 지그재그 원 좌표로 채운다. 각 점은 반경 ±radialJitter,
        /// 높이 ±verticalJitter 안에서 흔들린다. rng를 받아 결정적 테스트가 가능하다.
        /// </summary>
        public static void FillRing(
            Vector3[] buffer, float radius, float radialJitter, float verticalJitter,
            System.Random rng)
        {
            FillOrientedRing(buffer, radius, radialJitter, verticalJitter,
                Quaternion.identity, rng);
        }

        /// <summary>
        /// 임의 방향의 대원(great circle) 지그재그 링. orientation이 링 평면을 돌려,
        /// 여러 가닥을 서로 다른 기울기로 겹치면 구형 전기 케이지가 된다.
        /// </summary>
        public static void FillOrientedRing(
            Vector3[] buffer, float radius, float radialJitter, float verticalJitter,
            Quaternion orientation, System.Random rng)
        {
            int count = buffer.Length;
            for (int i = 0; i < count; i++)
            {
                float angle = Mathf.PI * 2f * i / count;
                float r = radius + ((float)rng.NextDouble() * 2f - 1f) * radialJitter;
                float y = ((float)rng.NextDouble() * 2f - 1f) * verticalJitter;
                buffer[i] = orientation *
                    new Vector3(Mathf.Cos(angle) * r, y, Mathf.Sin(angle) * r);
            }
        }

        /// <summary>
        /// 링을 따라 흐르는 파동값 (-1~1). 정수 배수 주파수 3개의 합성이라
        /// 한 바퀴(2π)에서 이음새 없이 이어지고, time에 대해 연속이라 연출이 부드럽다.
        /// </summary>
        public static float FlowWave(float angle, float time, float phase)
        {
            return 0.55f * Mathf.Sin(3f * angle + 1.3f * time + phase)
                 + 0.30f * Mathf.Sin(7f * angle - 2.1f * time + 2.7f * phase)
                 + 0.15f * Mathf.Sin(13f * angle + 3.7f * time + 5.1f * phase);
        }

        /// <summary>
        /// 흐르는 파동으로 일렁이는 대원 링. 무작위 리롤 없이 time만으로 움직여서
        /// 프레임 간 좌표가 연속 — 번개가 지글거리되 튀지 않는다.
        /// </summary>
        public static void FillFlowingRing(
            Vector3[] buffer, float radius, float radialAmplitude, float verticalAmplitude,
            float time, float phase, Quaternion orientation)
        {
            int count = buffer.Length;
            for (int i = 0; i < count; i++)
            {
                float angle = Mathf.PI * 2f * i / count;
                float r = radius + radialAmplitude * FlowWave(angle, time, phase);
                float y = verticalAmplitude * FlowWave(angle, -0.8f * time, phase + 1.9f);
                buffer[i] = orientation *
                    new Vector3(Mathf.Cos(angle) * r, y, Mathf.Sin(angle) * r);
            }
        }

        /// <summary>구면 위 균등 분포 단위 벡터 — 링 평면의 무작위 법선용.</summary>
        public static Vector3 RandomUnitVector(System.Random rng)
        {
            float y = (float)rng.NextDouble() * 2f - 1f;
            float phi = (float)rng.NextDouble() * Mathf.PI * 2f;
            float s = Mathf.Sqrt(Mathf.Max(0f, 1f - y * y));
            return new Vector3(s * Mathf.Cos(phi), y, s * Mathf.Sin(phi));
        }

        /// <summary>무작위 대원 방향 — XZ 링의 법선(+Y)을 무작위 법선으로 돌린다.</summary>
        public static Quaternion RandomRingOrientation(System.Random rng)
        {
            return Quaternion.FromToRotation(Vector3.up, RandomUnitVector(rng));
        }
    }
}
