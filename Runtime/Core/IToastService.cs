using System;

namespace KidzDev.Unity.Toast
{
    /// <summary>Fire-and-forget notification banners. Each <see cref="Show"/> returns immediately; the toast
    /// slides/fades in, waits its duration (or stays sticky), then slides/fades out and destroys itself. Up to
    /// a capped number are visible at once — a new toast beyond the cap evicts the oldest immediately rather
    /// than waiting for a slot, since a toast is throwaway feedback tied to the moment it was triggered.</summary>
    public interface IToastService : IDisposable
    {
        /// <summary>Number of toasts currently shown (including those whose hide is still animating).</summary>
        int ActiveCount { get; }

        /// <summary>Shows a toast with <paramref name="message"/>. Evicts the oldest visible toast first if
        /// already at capacity.</summary>
        ToastHandle Show(string message, ToastOptions options = null);

        /// <summary>Immediately dismisses every currently-shown toast.</summary>
        void HideAll();
    }
}
