using System;
using KidzDev.Unity.Toast;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KidzDev.Unity.Toast.Demo
{
    /// <summary>Wires the demo scene's buttons to <see cref="Toast"/> / a second <see cref="ToastManager"/>.</summary>
    public sealed class ToastDemoController : MonoBehaviour
    {
        [SerializeField] private Button _defaultButton;
        [SerializeField] private Button _shortButton;
        [SerializeField] private Button _iconColorButton;
        [SerializeField] private Button _burstButton;
        [SerializeField] private Button _stickyButton;
        [SerializeField] private Button _topAnchoredButton;
        [SerializeField] private TMP_Text _statusText;

        private IToastService _topManager;
        private ToastHandle _stickyHandle;
        private Sprite _icon;
        private int _burstCounter;

        private void Awake()
        {
            _icon = CreateDiscIcon();
            _topManager = new ToastManager(anchor: ToastAnchor.Top);

            _defaultButton.onClick.AddListener(() => Toast.Default.Show("Item added"));
            _shortButton.onClick.AddListener(() => Toast.Default.Show("Quick message", new ToastOptions { Duration = 1f }));
            _iconColorButton.onClick.AddListener(() => Toast.Default.Show("Coins +100", new ToastOptions
            {
                Icon = _icon,
                BackgroundColor = new Color(0.13f, 0.45f, 0.2f),
                TextColor = Color.white
            }));
            _burstButton.onClick.AddListener(OnBurst);
            _stickyButton.onClick.AddListener(OnToggleSticky);
            _topAnchoredButton.onClick.AddListener(() => _topManager.Show("Shown from the top-anchored manager"));
        }

        private void Update()
        {
            _statusText.text = $"Active toasts (default manager): {Toast.Default.ActiveCount}";
        }

        private void OnDestroy()
        {
            _topManager?.Dispose();
            if (_icon != null) Destroy(_icon.texture);
        }

        private void OnBurst()
        {
            for (int i = 0; i < 5; i++)
            {
                _burstCounter++;
                Toast.Default.Show($"Burst toast #{_burstCounter}");
            }
        }

        private void OnToggleSticky()
        {
            if (_stickyHandle != null && _stickyHandle.IsActive)
            {
                _stickyHandle.Dismiss();
                _stickyHandle = null;
                SetStickyLabel("Sticky: Connection Lost");
                return;
            }

            _stickyHandle = Toast.Default.Show("Connection lost", new ToastOptions { Duration = 0f, TapToDismiss = false });
            SetStickyLabel("Sticky: Reconnect (dismiss)");
        }

        private void SetStickyLabel(string text)
        {
            var label = _stickyButton.GetComponentInChildren<TMP_Text>();
            if (label != null) label.text = text;
        }

        // Built in code (no bundled texture asset) — a small filled circle used as the icon+color toast's icon.
        private static Sprite CreateDiscIcon()
        {
            const int size = 32;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
            var center = new Vector2(size / 2f, size / 2f);
            float radius = size / 2f - 1f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), center);
                    tex.SetPixel(x, y, dist <= radius ? Color.white : new Color(1f, 1f, 1f, 0f));
                }
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        }
    }
}
