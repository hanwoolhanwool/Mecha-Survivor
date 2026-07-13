using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace MechaSurvivor.UI
{
    /// <summary>
    /// 런타임 uGUI 구축 헬퍼. v1 UI는 프리팹 대신 코드로 만든다 —
    /// 씬 YAML 수작업 없이 버전 관리·리뷰가 가능하고, 값 튜닝이 코드 리뷰에 드러난다.
    /// </summary>
    public static class UiFactory
    {
        /// <summary>Screen Space Overlay 캔버스 + 스케일러. sortingOrder로 레이어를 나눈다.</summary>
        public static Canvas CreateCanvas(GameObject host, int sortingOrder)
        {
            var canvas = host.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortingOrder;

            var scaler = host.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            host.AddComponent<GraphicRaycaster>();
            EnsureEventSystem();
            return canvas;
        }

        /// <summary>버튼 클릭에 필요한 EventSystem(신 Input System 모듈)을 보장한다.</summary>
        public static void EnsureEventSystem()
        {
            if (EventSystem.current != null)
            {
                return;
            }

            var go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
            go.AddComponent<InputSystemUIInputModule>();
        }

        public static RectTransform CreateRect(string name, Transform parent,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, worldPositionStays: false);
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;
            return rect;
        }

        /// <summary>단색 사각형 (스프라이트 없는 Image = 흰색 → 색만 입힌다).</summary>
        public static Image CreatePanel(string name, Transform parent,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 size,
            Color color)
        {
            RectTransform rect = CreateRect(name, parent, anchorMin, anchorMax, anchoredPosition, size);
            var image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        /// <summary>수평 게이지 채움용 Image (fillAmount 대신 앵커 스케일 — 스프라이트 불필요).</summary>
        public static Image CreateBarFill(string name, RectTransform background, Color color, float padding = 2f)
        {
            RectTransform rect = CreateRect(name, background, Vector2.zero, Vector2.one,
                Vector2.zero, Vector2.zero);
            rect.offsetMin = new Vector2(padding, padding);
            rect.offsetMax = new Vector2(-padding, -padding);
            rect.pivot = new Vector2(0f, 0.5f);

            var image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        /// <summary>0~1 값을 채움 폭에 반영 (pivot 좌측 고정 스케일).</summary>
        public static void SetBarFill(Image fill, float value01)
        {
            fill.rectTransform.localScale = new Vector3(Mathf.Clamp01(value01), 1f, 1f);
        }

        public static Text CreateText(string name, Transform parent,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 size,
            string content, int fontSize, TextAnchor alignment, Color color)
        {
            RectTransform rect = CreateRect(name, parent, anchorMin, anchorMax, anchoredPosition, size);
            var text = rect.gameObject.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.text = content;
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = color;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            return text;
        }

        public static Button CreateButton(string name, Transform parent,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 size,
            Color background, out Text label)
        {
            RectTransform rect = CreateRect(name, parent, anchorMin, anchorMax, anchoredPosition, size);
            var image = rect.gameObject.AddComponent<Image>();
            image.color = background;

            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;

            ColorBlock colors = button.colors;
            colors.highlightedColor = new Color(1f, 1f, 1f, 1f);
            colors.pressedColor = new Color(0.7f, 0.7f, 0.7f, 1f);
            button.colors = colors;

            label = CreateText("Label", rect, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero,
                string.Empty, 22, TextAnchor.MiddleCenter, Color.white);
            label.raycastTarget = false;
            return button;
        }
    }
}
