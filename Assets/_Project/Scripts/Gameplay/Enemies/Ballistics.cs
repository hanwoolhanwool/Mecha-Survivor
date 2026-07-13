using UnityEngine;

namespace MechaSurvivor.Gameplay
{
    /// <summary>포탑형 적의 예측 사격 순수 계산 (GDD 5.3 — 착지 방해 역할).</summary>
    public static class Ballistics
    {
        /// <summary>
        /// 등속 이동 타깃에 대한 요격 방향. 해가 없으면(타깃이 더 빠름 등) 현재 위치 direct 조준으로
        /// 폴백하고 false를 반환한다.
        /// </summary>
        public static bool TryPredictInterceptDirection(
            Vector3 origin, Vector3 targetPosition, Vector3 targetVelocity, float projectileSpeed,
            out Vector3 direction)
        {
            Vector3 toTarget = targetPosition - origin;

            float a = targetVelocity.sqrMagnitude - projectileSpeed * projectileSpeed;
            float b = 2f * Vector3.Dot(toTarget, targetVelocity);
            float c = toTarget.sqrMagnitude;

            float interceptTime = -1f;

            if (Mathf.Abs(a) < 1e-4f)
            {
                // 속도가 같으면 1차식: b·t + c = 0.
                if (Mathf.Abs(b) > 1e-4f)
                {
                    interceptTime = -c / b;
                }
            }
            else
            {
                float discriminant = b * b - 4f * a * c;
                if (discriminant >= 0f)
                {
                    float sqrt = Mathf.Sqrt(discriminant);
                    float t1 = (-b - sqrt) / (2f * a);
                    float t2 = (-b + sqrt) / (2f * a);

                    // 가장 이른 양의 해.
                    interceptTime = Mathf.Min(t1, t2);
                    if (interceptTime < 0f)
                    {
                        interceptTime = Mathf.Max(t1, t2);
                    }
                }
            }

            if (interceptTime > 0f)
            {
                Vector3 predicted = toTarget + targetVelocity * interceptTime;
                if (predicted.sqrMagnitude > 1e-6f)
                {
                    direction = predicted.normalized;
                    return true;
                }
            }

            direction = toTarget.sqrMagnitude > 1e-6f ? toTarget.normalized : Vector3.forward;
            return false;
        }
    }
}
