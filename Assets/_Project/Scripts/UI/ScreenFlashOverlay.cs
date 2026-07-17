using UnityEngine;
using UnityEngine.UI;
using MechaSurvivor.Core;

namespace MechaSurvivor.UI
{
    /// <summary>
    /// 대형 착탄(HeavyImpact) 화면 플래시 (GDD 3.4-8 궤도 폭격).
    /// 가시성 규칙 준수: 중앙이 뚫린 비네트 형태라 크로스헤어를 가리지 않고(3.6-1),
    /// 지속은 0.1초대로 짧다(3.6-4). 강도 슬라이더 0이면 완전히 꺼진다(3.6-6).
    /// </summary>
    public sealed class ScreenFlashOverlay : MonoBehaviour
    {
        [Range(0f, 1f)]
        [Tooltip("0 = 플래시 끔")]
        [SerializeField] private float _intensity = 1f;

        [Tooltip("피크 알파 — 낮게. 화면을 하얗게 덮으면 조준이 죽는다")]
        [SerializeField] private float _peakAlpha = 0.4f;

        [Tooltip("플래시 지속(초). GDD 3.6-4: 0.1초 이내 권장")]
        [SerializeField] private float _duration = 0.12f;

        [Tooltip("비네트 안쪽 투명 반경 (0~1, 화면 중심 기준) — 크로스헤어 주변을 비운다")]
        [SerializeField] private float _clearRadius = 0.3f;

        private Image _image;
        private float _flashEndTime;
        private float _currentPeak;
        private float _lastAppliedAlpha = -1f;

        private void Awake()
        {
            BuildOverlay();
        }

        private void OnEnable() => EventBus<HeavyImpactEvent>.Subscribe(OnHeavyImpact);
        private void OnDisable() => EventBus<HeavyImpactEvent>.Unsubscribe(OnHeavyImpact);

        private void OnHeavyImpact(HeavyImpactEvent evt) => Flash(evt.Magnitude);

        /// <summary>플래시 트리거. magnitude 0~1이 피크 알파에 곱해진다.</summary>
        public void Flash(float magnitude)
        {
            float peak = _peakAlpha * _intensity * Mathf.Clamp01(magnitude);
            if (peak <= 0f)
            {
                return;
            }

            _currentPeak = Mathf.Max(_currentPeak, peak);
            _flashEndTime = Time.unscaledTime + _duration;
        }

        private void Update()
        {
            float remaining = _flashEndTime - Time.unscaledTime;
            float alpha = remaining > 0f
                ? _currentPeak * (remaining / _duration)
                : 0f;

            if (alpha <= 0f)
            {
                _currentPeak = 0f;
            }

            if (!Mathf.Approximately(alpha, _lastAppliedAlpha))
            {
                var color = _image.color;
                color.a = alpha;
                _image.color = color;
                _lastAppliedAlpha = alpha;
            }
        }

        /// <summary>전용 캔버스 + 풀스크린 비네트 이미지를 런타임 구축 (UiFactory 방식과 동일 — 프리팹 없음).</summary>
        private void BuildOverlay()
        {
            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 200;   // HUD 위, 어떤 UI보다 위 — 하지만 중앙은 뚫려 있다

            var imageGo = new GameObject("FlashVignette");
            imageGo.transform.SetParent(transform, false);
            _image = imageGo.AddComponent<Image>();
            _image.raycastTarget = false;
            _image.sprite = BuildVignetteSprite();
            _image.color = new Color(1f, 1f, 1f, 0f);

            var rect = _image.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        /// <summary>중앙 투명 → 가장자리 흰색의 방사형 그라데이션 (64×64, 초기화 1회 생성).</summary>
        private Sprite BuildVignetteSprite()
        {
            const int size = 64;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                name = "FlashVignette",
            };

            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = (x + 0.5f) / size - 0.5f;
                    float dy = (y + 0.5f) / size - 0.5f;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy) * 2f;   // 0(중심)~1(가장자리)
                    float a = Mathf.Clamp01((dist - _clearRadius) / Mathf.Max(1f - _clearRadius, 0.01f));
                    pixels[y * size + x] = new Color32(255, 255, 255, (byte)(a * a * 255f));
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        }
    }
}
