using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace KidzDev.Unity.Toast
{
    /// <summary>Default <see cref="IToastService"/>. Caps how many toasts are visible at once; a new toast
    /// beyond the cap evicts the oldest immediately rather than queueing — toasts are throwaway feedback tied
    /// to the moment they were triggered, not precious content worth waiting to see.</summary>
    /// <remarks>
    /// <para><b>Show flow:</b> evict the oldest visible toast if already at capacity -> build a view via the
    /// factory -> apply content -> parent under the stack container (newest nearest the anchored edge) ->
    /// enter transition -> await a hide request (duration elapsed, <see cref="ToastHandle.Dismiss"/>, a tap, or
    /// eviction) -> exit transition -> destroy instance.</para>
    /// <para>Main-thread only. <see cref="Dispose"/> cancels all in-flight shows and tears down their instances.</para>
    /// </remarks>
    public sealed class ToastManager : IToastService
    {
        private sealed class Entry
        {
            public IToastView View;
            public GameObject Instance;
            public UniTaskCompletionSource HideSignal;
            public bool Dismissing;
        }

        private readonly IToastLayer _layer;
        private readonly IToastTransition _transition;
        private readonly Func<IToastView> _viewFactory;
        private readonly ToastOptions _defaultOptions;
        private readonly int _maxVisible;
        private readonly ToastAnchor _anchor;

        private readonly List<Entry> _entries = new List<Entry>();
        private readonly CancellationTokenSource _lifetimeCts = new CancellationTokenSource();
        private RectTransform _container;
        private bool _disposed;

        /// <param name="layer">Layer the stack container is parented under; defaults to a new <see cref="ToastLayer"/> (sort order 3000).</param>
        /// <param name="transition">Default transition; defaults to <see cref="SlideFadeToastTransition"/>.</param>
        /// <param name="viewFactory">Factory for each toast's view; defaults to <see cref="DefaultToastView.Create"/>.</param>
        /// <param name="defaultOptions">Default per-show options; defaults to <see cref="ToastOptions.Default"/>.</param>
        /// <param name="maxVisible">Cap on concurrently visible toasts; a new <see cref="Show"/> beyond this evicts the oldest. Minimum 1.</param>
        /// <param name="anchor">Which screen edge the stack grows from.</param>
        public ToastManager(
            IToastLayer layer = null,
            IToastTransition transition = null,
            Func<IToastView> viewFactory = null,
            ToastOptions defaultOptions = null,
            int maxVisible = 3,
            ToastAnchor anchor = ToastAnchor.Bottom)
        {
            _layer = layer ?? new ToastLayer();
            _transition = transition ?? new SlideFadeToastTransition();
            _viewFactory = viewFactory ?? DefaultToastView.Create;
            _defaultOptions = defaultOptions ?? ToastOptions.Default;
            _maxVisible = Mathf.Max(1, maxVisible);
            _anchor = anchor;
        }

        /// <inheritdoc/>
        public int ActiveCount => _entries.Count;

        /// <inheritdoc/>
        public ToastHandle Show(string message, ToastOptions options = null)
        {
            ThrowIfDisposed();
            options ??= _defaultOptions;

            EvictOldestIfAtCapacity();

            var view = _viewFactory();
            if (view == null || view.Root == null)
                throw new InvalidOperationException(
                    "The viewFactory passed to ToastManager returned a null IToastView or a view with a null Root.");

            view.SetContent(new ToastContent(message, options.Icon, options.BackgroundColor, options.TextColor));

            var container = EnsureContainer();
            view.Root.transform.SetParent(container, false);
            view.Root.SetActive(true);
            SetSiblingForAnchor(view.Root.transform);

            var hideSignal = new UniTaskCompletionSource();
            var entry = new Entry { View = view, Instance = view.Root, HideSignal = hideSignal };
            _entries.Add(entry);

            if (options.TapToDismiss && view.Content != null)
            {
                var tap = view.Content.gameObject.GetComponent<ToastTapToDismiss>()
                    ?? view.Content.gameObject.AddComponent<ToastTapToDismiss>();
                tap.Bind(() => hideSignal.TrySetResult());
            }

            var transition = options.Transition ?? _transition;
            RunAsync(entry, transition, options.Duration).Forget();

            return new ToastHandle(hideSignal);
        }

        // Only one eviction is ever needed per call: Show() is synchronous and adds exactly one entry, so the
        // live count can never be more than one over capacity at the point this runs.
        private void EvictOldestIfAtCapacity()
        {
            Entry oldest = null;
            int liveCount = 0;
            foreach (var e in _entries) // insertion-ordered, so the first non-dismissing entry is the oldest survivor
            {
                if (e.Dismissing) continue;
                liveCount++;
                oldest ??= e;
            }
            if (liveCount >= _maxVisible && oldest != null)
            {
                oldest.Dismissing = true;
                oldest.HideSignal.TrySetResult();
            }
        }

        private void SetSiblingForAnchor(Transform instance)
        {
            if (_anchor == ToastAnchor.Bottom) instance.SetAsLastSibling();
            else instance.SetAsFirstSibling();
        }

        // Cancellation at ANY stage (manager-wide Dispose()) skips straight to cleanup rather than attempting
        // the next stage — mirrors ui-overlay's LoadingManager.RunAsync.
        private async UniTaskVoid RunAsync(Entry entry, IToastTransition transition, float duration)
        {
            try
            {
                using var linked = CancellationTokenSource.CreateLinkedTokenSource(_lifetimeCts.Token);

                await transition.PlayEnterAsync(entry.View, linked.Token);

                if (duration > 0f)
                    RunAutoDismissAsync(entry, duration, linked.Token).Forget();

                await entry.HideSignal.Task.AttachExternalCancellation(linked.Token);
                await transition.PlayExitAsync(entry.View, linked.Token);
            }
            catch (OperationCanceledException)
            {
                // Manager Dispose() cancelled whichever stage was in flight; finally below performs cleanup.
            }
            finally
            {
                _entries.Remove(entry);
                DestroyObject(entry.Instance);
            }
        }

        // Fires Show()'s duration timer. Routes through the same hide signal a manual Dismiss()/eviction/tap
        // uses, so whichever happens first wins and the others become harmless no-ops (TrySetResult on an
        // already-completed source is a no-op).
        private async UniTaskVoid RunAutoDismissAsync(Entry entry, float seconds, CancellationToken ct)
        {
            try
            {
                await UniTask.Delay(TimeSpan.FromSeconds(seconds), ignoreTimeScale: true, cancellationToken: ct);
            }
            catch (OperationCanceledException)
            {
                return;
            }
            entry.HideSignal.TrySetResult();
        }

        /// <inheritdoc/>
        public void HideAll()
        {
            // Snapshot: each hide signal resumes an awaiting RunAsync that mutates _entries in its finally.
            var snapshot = _entries.ToArray();
            foreach (var entry in snapshot) entry.HideSignal.TrySetResult();
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            // Cancelling the lifetime token resumes every in-flight RunAsync via cancellation, whose finally
            // blocks remove their entries and destroy their instances.
            _lifetimeCts.Cancel();
            _lifetimeCts.Dispose();

            // Anything left (shows parked on the hide-signal await with nothing observing the throw) is
            // cleaned here.
            foreach (var entry in _entries)
                if (entry.Instance != null) DestroyObject(entry.Instance);
            _entries.Clear();

            if (_container != null)
            {
                DestroyObject(_container.gameObject);
                _container = null;
            }
            _layer.Teardown();
        }

        private RectTransform EnsureContainer()
        {
            ThrowIfDisposed();
            if (_container != null) return _container;

            const float margin = 80f;
            const float width = 600f;

            var go = new GameObject("[ToastContainer]", typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(_layer.Root, false);
            rt.sizeDelta = new Vector2(width, 0f);

            if (_anchor == ToastAnchor.Bottom)
            {
                rt.anchorMin = new Vector2(0.5f, 0f);
                rt.anchorMax = new Vector2(0.5f, 0f);
                rt.pivot = new Vector2(0.5f, 0f);
                rt.anchoredPosition = new Vector2(0f, margin);
            }
            else
            {
                rt.anchorMin = new Vector2(0.5f, 1f);
                rt.anchorMax = new Vector2(0.5f, 1f);
                rt.pivot = new Vector2(0.5f, 1f);
                rt.anchoredPosition = new Vector2(0f, -margin);
            }

            var layoutGroup = go.AddComponent<VerticalLayoutGroup>();
            layoutGroup.childAlignment = TextAnchor.MiddleCenter;
            layoutGroup.childControlWidth = true;
            layoutGroup.childControlHeight = true;
            layoutGroup.childForceExpandWidth = false;
            layoutGroup.childForceExpandHeight = false;
            layoutGroup.spacing = 8f;

            var fitter = go.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

            _container = rt;
            return _container;
        }

        // Toasts can be shown outside play mode (EditMode test suite), where Object.Destroy is a deferred
        // no-op that also logs an error. Pick the immediate variant when not playing.
        private static void DestroyObject(UnityEngine.Object obj)
        {
            if (obj == null) return;
            if (Application.isPlaying) UnityEngine.Object.Destroy(obj);
            else UnityEngine.Object.DestroyImmediate(obj);
        }

        private void ThrowIfDisposed()
        {
            if (_disposed) throw new ObjectDisposedException(nameof(ToastManager));
        }
    }
}
