using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KidzDev.Unity.Toast
{
    /// <summary>Built-in <see cref="IToastView"/>: a rounded-corner-free capsule with an optional icon and a
    /// single/wrapped-line label, constructed entirely in code (no prefab, no Resources, no Addressables — a
    /// toast isn't addressed by key per call the way a popup is, so there's no loader seam to speak of).</summary>
    /// <remarks>
    /// Structure: <c>Root</c> (the actual child the stack's <c>VerticalLayoutGroup</c> positions — carries
    /// <see cref="LayoutElement"/> + <see cref="CanvasGroup"/>, no visuals of its own) contains <c>Content</c>
    /// (a free, non-layout-controlled child, sized to match <c>Root</c>'s <see cref="LayoutElement"/> and
    /// centered at rest — this is what <see cref="IToastTransition"/> implementations slide). Width is fixed;
    /// height grows with wrapped text, clamped to a sane range.
    /// </remarks>
    internal sealed class DefaultToastView : MonoBehaviour, IToastView
    {
        private const float ContentWidth = 520f;
        private const float HorizontalPadding = 20f;
        private const float VerticalPadding = 16f;
        private const float IconSize = 28f;
        private const float IconTextGap = 12f;
        private const float MinHeight = 56f;
        private const float MaxHeight = 140f;

        private static readonly Color DefaultBackgroundColor = new Color(0.12f, 0.12f, 0.12f, 0.95f);
        private static readonly Color DefaultTextColor = Color.white;

        private RectTransform _content;
        private LayoutElement _layoutElement;
        private Image _background;
        private Image _icon;
        private TMP_Text _label;

        /// <inheritdoc/>
        public GameObject Root => gameObject;

        /// <inheritdoc/>
        public RectTransform Content => _content;

        /// <inheritdoc/>
        public void SetContent(in ToastContent content)
        {
            string message = content.Message ?? string.Empty;
            _background.color = content.BackgroundColor ?? DefaultBackgroundColor;
            _label.color = content.TextColor ?? DefaultTextColor;
            _label.text = message;

            bool hasIcon = content.Icon != null;
            _icon.gameObject.SetActive(hasIcon);
            _icon.sprite = content.Icon;

            float iconSpan = hasIcon ? IconSize + IconTextGap : 0f;
            float textAreaWidth = ContentWidth - HorizontalPadding * 2f - iconSpan;
            Vector2 preferred = _label.GetPreferredValues(message, textAreaWidth, 0f);
            float height = Mathf.Clamp(preferred.y + VerticalPadding * 2f, MinHeight, MaxHeight);

            _layoutElement.preferredWidth = ContentWidth;
            _layoutElement.preferredHeight = height;
            _content.sizeDelta = new Vector2(ContentWidth, height);

            var labelRect = _label.rectTransform;
            labelRect.offsetMin = new Vector2(HorizontalPadding + iconSpan, VerticalPadding);
            labelRect.offsetMax = new Vector2(-HorizontalPadding, -VerticalPadding);
        }

        /// <summary>Builds a fresh instance, detached (not yet parented under any stack container).</summary>
        public static IToastView Create()
        {
            var go = new GameObject("ToastView",
                typeof(RectTransform), typeof(LayoutElement), typeof(CanvasGroup), typeof(DefaultToastView));
            var rootRect = (RectTransform)go.transform;
            rootRect.sizeDelta = new Vector2(ContentWidth, MinHeight);

            var contentGo = new GameObject("Content", typeof(RectTransform), typeof(Image));
            var contentRect = (RectTransform)contentGo.transform;
            contentRect.SetParent(rootRect, false);
            contentRect.anchorMin = new Vector2(0.5f, 0.5f);
            contentRect.anchorMax = new Vector2(0.5f, 0.5f);
            contentRect.pivot = new Vector2(0.5f, 0.5f);
            contentRect.sizeDelta = new Vector2(ContentWidth, MinHeight);
            contentRect.anchoredPosition = Vector2.zero;

            var background = contentGo.GetComponent<Image>();
            background.color = DefaultBackgroundColor;
            background.raycastTarget = true; // toasts are opaque UI; they block clicks to whatever's beneath

            var iconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            var iconRect = (RectTransform)iconGo.transform;
            iconRect.SetParent(contentRect, false);
            iconRect.anchorMin = new Vector2(0f, 0.5f);
            iconRect.anchorMax = new Vector2(0f, 0.5f);
            iconRect.pivot = new Vector2(0f, 0.5f);
            iconRect.sizeDelta = new Vector2(IconSize, IconSize);
            iconRect.anchoredPosition = new Vector2(HorizontalPadding, 0f);
            var icon = iconGo.GetComponent<Image>();
            icon.raycastTarget = false;
            iconGo.SetActive(false);

            var labelGo = new GameObject("Label", typeof(RectTransform));
            var labelRect = (RectTransform)labelGo.transform;
            labelRect.SetParent(contentRect, false);
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;

            var label = labelGo.AddComponent<TextMeshProUGUI>();
            label.fontSize = 26f;
            label.color = DefaultTextColor;
            label.alignment = TextAlignmentOptions.MidlineLeft;
            label.enableWordWrapping = true;
            label.raycastTarget = false;

            var view = go.GetComponent<DefaultToastView>();
            view._content = contentRect;
            view._layoutElement = go.GetComponent<LayoutElement>();
            view._background = background;
            view._icon = icon;
            view._label = label;

            return view;
        }
    }
}
