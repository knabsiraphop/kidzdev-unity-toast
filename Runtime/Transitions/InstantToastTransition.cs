using System.Threading;
using Cysharp.Threading.Tasks;

namespace KidzDev.Unity.Toast
{
    /// <summary>The degenerate transition: no animation. The manager handles activation and teardown, so this
    /// has nothing to do beyond return. Also useful in tests, where deterministic show/hide without a per-frame
    /// wait is easier to assert against.</summary>
    public sealed class InstantToastTransition : IToastTransition
    {
        /// <inheritdoc/>
        public UniTask PlayEnterAsync(IToastView view, CancellationToken ct) => UniTask.CompletedTask;

        /// <inheritdoc/>
        public UniTask PlayExitAsync(IToastView view, CancellationToken ct) => UniTask.CompletedTask;
    }
}
