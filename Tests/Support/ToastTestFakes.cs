using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace KidzDev.Unity.Toast.Tests
{
    // FakeToastView lives in a NON-editor assembly (KidzDev.Unity.Toast.TestSupport) on purpose: it's a
    // MonoBehaviour, and Unity refuses to AddComponent a MonoBehaviour compiled into an editor-only assembly.
    // Both the EditMode and PlayMode test assemblies reference this one, so they share the fakes.

    /// <summary>A bare <see cref="IToastView"/> backed by a real GameObject; records every call it receives.</summary>
    public sealed class FakeToastView : MonoBehaviour, IToastView
    {
        public int SetContentCalls;
        public ToastContent LastContent;

        public GameObject Root => gameObject;
        public RectTransform Content { get; private set; }

        public void SetContent(in ToastContent content)
        {
            SetContentCalls++;
            LastContent = content;
        }

        /// <summary>Builds a fresh, inactive instance (matches how the manager receives a not-yet-parented view).</summary>
        public static FakeToastView Create(string name = "FakeToastView")
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.SetActive(false);

            var contentGo = new GameObject("Content", typeof(RectTransform));
            contentGo.transform.SetParent(go.transform, false);

            var view = go.AddComponent<FakeToastView>();
            view.Content = (RectTransform)contentGo.transform;
            return view;
        }
    }

    /// <summary>An <see cref="IToastLayer"/> over a plain, test-owned transform — no canvas machinery.</summary>
    public sealed class TestToastLayer : IToastLayer
    {
        private readonly Transform _root;

        public TestToastLayer(string name = "[TestToastLayer]")
        {
            _root = new GameObject(name).transform;
        }

        /// <summary>Wraps an existing transform instead of creating one. Use this when the caller needs to add
        /// components (e.g. <see cref="Canvas"/>) to the root <em>before</em> anything captures its Transform —
        /// adding a Canvas upgrades a plain Transform to a RectTransform, which invalidates a reference taken
        /// beforehand (a different underlying object; parenting against the stale reference silently detaches
        /// from the live hierarchy).</summary>
        public TestToastLayer(Transform existingRoot)
        {
            _root = existingRoot;
        }

        public Transform Root => _root;

        public void Teardown()
        {
            if (_root != null) UnityEngine.Object.DestroyImmediate(_root.gameObject);
        }
    }

    /// <summary>
    /// An <see cref="IToastTransition"/> whose completion the test controls. Each call returns a pending task
    /// until <see cref="Open"/> is true; thereafter calls complete synchronously. No frame pumping, so it works
    /// in EditMode <c>[Test]</c> methods.
    /// </summary>
    public sealed class GatedToastTransition : IToastTransition
    {
        public int EnterCalls;
        public int ExitCalls;
        public bool Open = true;
        private readonly List<UniTaskCompletionSource> _pending = new List<UniTaskCompletionSource>();

        public UniTask PlayEnterAsync(IToastView view, CancellationToken ct)
        {
            EnterCalls++;
            return Gate();
        }

        public UniTask PlayExitAsync(IToastView view, CancellationToken ct)
        {
            ExitCalls++;
            return Gate();
        }

        private UniTask Gate()
        {
            if (Open) return UniTask.CompletedTask;
            var tcs = new UniTaskCompletionSource();
            _pending.Add(tcs);
            return tcs.Task;
        }

        /// <summary>Completes every currently-pending enter/exit call (as if the animation finished).</summary>
        public void Release()
        {
            Open = true;
            var copy = _pending.ToArray();
            _pending.Clear();
            foreach (var t in copy) t.TrySetResult();
        }
    }
}
