using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace KidzDev.Unity.Toast
{
    /// <summary>Default <see cref="IToastTransition"/>: slides <see cref="IToastView.Content"/> in/out from an
    /// edge offset while fading a <see cref="CanvasGroup"/> on <see cref="IToastView.Root"/>, per-frame on
    /// unscaled time — no third-party animation dependency.</summary>
    /// <remarks>
    /// The default offset <c>(0, -24)</c> suits a <see cref="ToastAnchor.Bottom"/> stack (toast slides up into
    /// place). A <see cref="ToastAnchor.Top"/> stack should be given a positive-Y offset, e.g.
    /// <c>new SlideFadeToastTransition(new Vector2(0, 24))</c> — not auto-derived from the anchor, since a
    /// custom stack position/orientation may want a different slide direction anyway.
    /// </remarks>
    public sealed class SlideFadeToastTransition : IToastTransition
    {
        private readonly Vector2 _offset;
        private readonly float _duration;

        /// <param name="offset">Local offset (relative to rest) the toast slides in from / out to. Default: <c>(0, -24)</c>.</param>
        /// <param name="duration">Animation duration in seconds. Values &lt;= 0 make the transition instant.</param>
        public SlideFadeToastTransition(Vector2? offset = null, float duration = 0.2f)
        {
            _offset = offset ?? new Vector2(0f, -24f);
            _duration = duration;
        }

        /// <inheritdoc/>
        public UniTask PlayEnterAsync(IToastView view, CancellationToken ct) => AnimateAsync(view, entering: true, ct);

        /// <inheritdoc/>
        public UniTask PlayExitAsync(IToastView view, CancellationToken ct) => AnimateAsync(view, entering: false, ct);

        private async UniTask AnimateAsync(IToastView view, bool entering, CancellationToken ct)
        {
            var cg = GetOrAddCanvasGroup(view.Root);
            var content = view.Content;
            if (content == null) return;

            // Read "rest" fresh each call: on enter it's wherever Content starts (its authored center position);
            // on exit it's wherever enter left it (rest, since enter always runs to completion before exit starts).
            Vector2 rest = content.anchoredPosition;
            Vector2 from = entering ? rest + _offset : rest;
            Vector2 to = entering ? rest : rest + _offset;
            float alphaFrom = entering ? 0f : 1f;
            float alphaTo = entering ? 1f : 0f;

            if (_duration <= 0f)
            {
                content.anchoredPosition = to;
                cg.alpha = alphaTo;
                return;
            }

            content.anchoredPosition = from;
            cg.alpha = alphaFrom;
            float elapsed = 0f;
            while (elapsed < _duration)
            {
                ct.ThrowIfCancellationRequested();
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / _duration);
                content.anchoredPosition = Vector2.Lerp(from, to, t);
                cg.alpha = Mathf.Lerp(alphaFrom, alphaTo, t);
                await UniTask.NextFrame(ct);
            }
            content.anchoredPosition = to;
            cg.alpha = alphaTo;
        }

        private static CanvasGroup GetOrAddCanvasGroup(GameObject root)
        {
            var cg = root.GetComponent<CanvasGroup>();
            if (cg == null) cg = root.AddComponent<CanvasGroup>();
            return cg;
        }
    }
}
