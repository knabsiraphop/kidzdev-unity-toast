using System.Threading;
using Cysharp.Threading.Tasks;

namespace KidzDev.Unity.Toast
{
    /// <summary>The animation seam for toasts. Swap one implementation to restyle every show/hide.</summary>
    /// <remarks>
    /// Contract: implementations animate <see cref="IToastView.Content"/> (the view's inner rect) and/or a
    /// <see cref="UnityEngine.CanvasGroup"/> on <see cref="IToastView.Root"/> — never <c>Root</c>'s own anchored
    /// position, which the stack's <c>VerticalLayoutGroup</c> owns and will reset on the next reflow (e.g. a
    /// sibling toast being added/removed). Must honor <c>ct</c>; on cancellation the exception propagates and
    /// the manager tears the view down immediately.
    /// </remarks>
    public interface IToastTransition
    {
        /// <summary>Animates the toast in. Called after the instance is parented and activated.</summary>
        UniTask PlayEnterAsync(IToastView view, CancellationToken ct);

        /// <summary>Animates the toast out. Called after its hide has been requested (duration elapsed, dismissed, or evicted).</summary>
        UniTask PlayExitAsync(IToastView view, CancellationToken ct);
    }
}
