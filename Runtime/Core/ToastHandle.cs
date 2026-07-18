using Cysharp.Threading.Tasks;

namespace KidzDev.Unity.Toast
{
    /// <summary>The handle every <see cref="IToastService.Show"/> call returns. Fire-and-forget callers can
    /// ignore it; callers of a sticky toast (<see cref="ToastOptions.Duration"/> &lt;= 0) keep it to dismiss
    /// the toast later, e.g. "Connection lost" -> "Reconnected".</summary>
    public sealed class ToastHandle
    {
        private readonly UniTaskCompletionSource _hideSignal;

        internal ToastHandle(UniTaskCompletionSource hideSignal) => _hideSignal = hideSignal;

        /// <summary>Whether the toast is still shown or animating (not yet dismissed/evicted).</summary>
        public bool IsActive => _hideSignal.Task.Status == UniTaskStatus.Pending;

        /// <summary>Requests the toast dismiss now (plays its exit transition). No-op if already dismissed.</summary>
        public void Dismiss() => _hideSignal.TrySetResult();
    }
}
