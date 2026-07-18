using UnityEngine;

namespace KidzDev.Unity.Toast
{
    /// <summary>Static facade over <see cref="IToastService"/>, mirroring the ecosystem's facade + seam +
    /// default-impl pattern. <see cref="Default"/> lazily creates a <see cref="ToastManager"/>.</summary>
    /// <example>
    /// <code>
    /// Toast.Default.Show("Item added");
    /// var handle = Toast.Default.Show("Connection lost", new ToastOptions { Duration = 0f });
    /// // ...later
    /// handle.Dismiss();
    /// </code>
    /// </example>
    public static class Toast
    {
        private static IToastService _default;

        /// <summary>The ambient toast service. Lazily a default <see cref="ToastManager"/>; settable for injection.</summary>
        public static IToastService Default
        {
            get => _default ??= new ToastManager();
            set => _default = value;
        }

        // With Enter Play Mode Options disabling domain reload, statics survive across play sessions while the
        // scene objects they reference do not — reset so the first access each session builds fresh state.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnReload() => _default = null;
    }
}
