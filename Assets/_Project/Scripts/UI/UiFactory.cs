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

        /// <summary>
        /// 모달 공통 골격: 전체 딤 + 중앙 박스 + 제목. panelRoot(딤)를 SetActive로 여닫는다.
        /// 반환값은 박스 트랜스폼 — 내용물은 여기에 붙인다.
        /// </summary>
        public static Transform CreateModal(GameObject host, int sortingOrder, string title,
            Vector2 boxSize, out GameObject panelRoot)
        {
            CreateCanvas(host, sortingOrder);

            Image dim = CreatePanel("Dim", host.transform,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero,
                new Color(0f, 0f, 0f, 0.6f));
            dim.raycastTarget = true;
            panelRoot = dim.gameObject;

            Image box = CreatePanel("Box", panelRoot.transform,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero,
                boxSize, new Color(0.08f, 0.09f, 0.11f, 0.98f));
            box.raycastTarget = true;

            CreateText("Title", box.transform,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -50f),
                new Vector2(boxSize.x - 80f, 60f), title, 36, TextAnchor.MiddleCenter, Color.white);

            return box.transform;
        }

        /// <summary>모달 하단 닫기 버튼.</summary>
        public static Button CreateCloseButton(Transform box, UnityEngine.Events.UnityAction onClose)
        {
            Button close = CreateButton("CloseButton", box,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 50f),
                new Vector2(240f, 52f), new Color(0.32f, 0.2f, 0.2f, 1f), out Text label);
            label.text = "닫기";
            label.fontSize = 24;
            close.onClick.AddListener(onClose);
            return close;
        }

        /// <summary>수평 슬라이더 (트랙/채움/핸들 전부 단색 Image — 스프라이트 불필요).</summary>
        public static Slider CreateSlider(string name, Transform parent,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 size,
            Color trackColor, Color fillColor)
        {
            RectTransform root = CreateRect(name, parent, anchorMin, anchorMax, anchoredPosition, size);

            RectTransform track = CreateRect("Track", root,
                new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), Vector2.zero, new Vector2(0f, 8f));
            var trackImage = track.gameObject.AddComponent<Image>();
            trackImage.color = trackColor;
            trackImage.raycastTarget = true;

            RectTransform fillArea = CreateRect("FillArea", root,
                new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), Vector2.zero, new Vector2(-20f, 8f));
            RectTransform fill = CreateRect("Fill", fillArea,
                Vector2.zero, new Vector2(0f, 1f), Vector2.zero, new Vector2(10f, 0f));
            var fillImage = fill.gameObject.AddComponent<Image>();
            fillImage.color = fillColor;
            fillImage.raycastTarget = false;

            RectTransform handleArea = CreateRect("HandleArea", root,
                Vector2.zero, Vector2.one, Vector2.zero, new Vector2(-20f, 0f));
            RectTransform handle = CreateRect("Handle", handleArea,
                Vector2.zero, new Vector2(0f, 1f), Vector2.zero, new Vector2(20f, 0f));
            var handleImage = handle.gameObject.AddComponent<Image>();
            handleImage.color = Color.white;
            handleImage.raycastTarget = true;

            var slider = root.gameObject.AddComponent<Slider>();
            slider.fillRect = fill;
            slider.handleRect = handle;
            slider.targetGraphic = handleImage;
            slider.direction = Slider.Direction.LeftToRight;
            return slider;
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
