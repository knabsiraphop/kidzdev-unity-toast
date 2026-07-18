using UnityEngine;

namespace KidzDev.Unity.Toast
{
    /// <summary>Per-show configuration for <see cref="IToastService.Show"/>. Pass an instance to override the
    /// manager's defaults for that call only.</summary>
    public sealed class ToastOptions
    {
        /// <summary>Shared defaults used when a <see cref="IToastService.Show"/> call passes no options.</summary>
        public static ToastOptions Default { get; } = new ToastOptions();

        /// <summary>Seconds before the toast auto-dismisses, once shown. <c>&lt;= 0</c> makes it sticky —
        /// it stays until dismissed via the returned <see cref="ToastHandle"/> or <see cref="IToastService.HideAll"/>.
        /// Default: 3.</summary>
        public float Duration { get; set; } = 3f;

        /// <summary>Optional icon shown to the left of the message. <c>null</c> hides the icon slot.</summary>
        public Sprite Icon { get; set; }

        /// <summary>Background tint override; <c>null</c> uses the view's default.</summary>
        public Color? BackgroundColor { get; set; }

        /// <summary>Text color override; <c>null</c> uses the view's default.</summary>
        public Color? TextColor { get; set; }

        /// <summary>When <c>true</c>, tapping the toast dismisses it early. Default: <c>false</c> — off, so a
        /// toast floating over active gameplay UI doesn't silently swallow an unrelated tap as a dismissal.</summary>
        public bool TapToDismiss { get; set; } = false;

        /// <summary>Per-call transition override; <c>null</c> falls back to the manager's default transition.</summary>
        public IToastTransition Transition { get; set; }
    }
}
