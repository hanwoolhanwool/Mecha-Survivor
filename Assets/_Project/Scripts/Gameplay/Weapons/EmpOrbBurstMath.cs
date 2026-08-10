using UnityEngine;

namespace MechaSurvivor.Gameplay
{
    /// <summary>
    /// EMP 오브 기폭 연출의 순수 수학. 파츠별 방사 방향·이동·축소 곡선을
    /// MonoBehaviour 밖에 둬서 EditMode 테스트로 검증한다.
    /// </summary>
    public static class EmpOrbBurstMath
    {
        /// <summary>감속 이징 — 초반에 확 퍼지고 끝에서 멎는다.</summary>
        public static float EaseOutCubic(float t)
        {
            t = Mathf.Clamp01(t);
            float inv = 1f - t;
            return 1f - inv * inv * inv;
        }

        /// <summary>
        /// 파츠의 방사 방향. 오브 중심 기준 파츠 위치의 방사 방향을 쓰되,
        /// 중심과 겹치는 파츠는 fallback으로 밀어낸다.
        /// </summary>
        public static Vector3 SpreadDirection(Vector3 restPosition, Vector3 fallback)
        {
            if (restPosition.sqrMagnitude < 0.0001f)
            {
                return fallback.normalized;
            }

            return restPosition.normalized;
        }

        /// <summary>정규화 시간 t(0~1)에서의 파츠 로컬 위치.</summary>
        public static Vector3 FragmentPosition(
            Vector3 restPosition, Vector3 direction, float spreadDistance, float t)
        {
            return restPosition + direction * (spreadDistance * EaseOutCubic(t));
        }

        /// <summary>파츠 스케일 배수 — shrinkStart까지 1 유지, 이후 0으로 수렴.</summary>
        public static float FragmentScale(float t, float shrinkStart)
        {
            t = Mathf.Clamp01(t);
            if (t <= shrinkStart)
            {
                return 1f;
            }

            float span = 1f - shrinkStart;
            if (span <= 0f)
            {
                return 0f;
            }

            return 1f - Mathf.SmoothStep(0f, 1f, (t - shrinkStart) / span);
        }

        /// <summary>중심 몸체 스케일 배수 — coreShrinkEnd 시점에 0이 된다.</summary>
        public static float CoreScale(float t, float coreShrinkEnd)
        {
            if (coreShrinkEnd <= 0f)
            {
                return 0f;
            }

            return 1f - Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / coreShrinkEnd));
        }
    }
}
