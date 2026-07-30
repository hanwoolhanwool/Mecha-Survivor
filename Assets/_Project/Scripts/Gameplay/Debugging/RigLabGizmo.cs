using UnityEngine;

namespace MechaSurvivor.Gameplay
{
    /// <summary>
    /// RigLab 선택 항목 기즈모 — 게임 뷰에서도 보이도록 GL 라인으로 그린다 (Docs/06 §4.2).
    /// 마운트: XYZ 축. 총구: 구체 링 + 전방 화살표 (발사 방향 확인이 핵심).
    /// </summary>
    public sealed class RigLabGizmo : MonoBehaviour
    {
        [SerializeField] private float _axisLength = 0.5f;

        private Material _lineMaterial;

        /// <summary>표시 대상 (null이면 그리지 않음). 컨트롤러가 선택 시 지정.</summary>
        public Transform Target { get; set; }

        /// <summary>총구 모드 — 전방 화살표 + 링 표시.</summary>
        public bool IsMuzzle { get; set; }

        private void OnRenderObject()
        {
            if (Target == null)
            {
                return;
            }

            if (_lineMaterial == null)
            {
                _lineMaterial = new Material(Shader.Find("Hidden/Internal-Colored"))
                {
                    hideFlags = HideFlags.HideAndDontSave,
                };
                _lineMaterial.SetInt("_ZTest", (int)UnityEngine.Rendering.CompareFunction.Always);
            }

            _lineMaterial.SetPass(0);
            GL.PushMatrix();
            GL.Begin(GL.LINES);

            Vector3 origin = Target.position;

            // 부모 스케일에 눌리지 않도록 월드 축 길이 고정.
            DrawLine(origin, origin + Target.right * _axisLength, Color.red);
            DrawLine(origin, origin + Target.up * _axisLength, Color.green);
            DrawLine(origin, origin + Target.forward * _axisLength, Color.blue);

            if (IsMuzzle)
            {
                // 전방 화살표 (긴 시안색) — 투사체가 나갈 방향.
                Vector3 tip = origin + Target.forward * (_axisLength * 3f);
                var cyan = new Color(0.2f, 1f, 1f);
                DrawLine(origin, tip, cyan);
                Vector3 back = Target.forward * (_axisLength * 0.5f);
                DrawLine(tip, tip - back + Target.right * (_axisLength * 0.25f), cyan);
                DrawLine(tip, tip - back - Target.right * (_axisLength * 0.25f), cyan);

                // 생성 위치 링.
                const int segments = 24;
                float radius = _axisLength * 0.25f;
                for (int i = 0; i < segments; i++)
                {
                    float a0 = i * Mathf.PI * 2f / segments;
                    float a1 = (i + 1) * Mathf.PI * 2f / segments;
                    DrawLine(
                        origin + (Target.right * Mathf.Cos(a0) + Target.up * Mathf.Sin(a0)) * radius,
                        origin + (Target.right * Mathf.Cos(a1) + Target.up * Mathf.Sin(a1)) * radius,
                        cyan);
                }
            }

            GL.End();
            GL.PopMatrix();
        }

        private static void DrawLine(Vector3 from, Vector3 to, Color color)
        {
            GL.Color(color);
            GL.Vertex(from);
            GL.Vertex(to);
        }
    }
}
