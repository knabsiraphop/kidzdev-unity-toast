using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace KidzDev.Unity.Toast.Tests
{
    [TestFixture]
    internal sealed class ToastPlayModeTests
    {
        [UnityTest]
        public IEnumerator SlideFade_Enter_EndsAtRestPosition_Exit_EndsAtOffset() => UniTask.ToCoroutine(async () =>
        {
            var view = FakeToastView.Create();
            view.gameObject.SetActive(true);
            view.gameObject.AddComponent<CanvasGroup>();
            var cg = view.gameObject.GetComponent<CanvasGroup>();
            var offset = new Vector2(0f, -24f);
            var transition = new SlideFadeToastTransition(offset, 0.05f);
            Vector2 rest = view.Content.anchoredPosition;
            try
            {
                await transition.PlayEnterAsync(view, default);
                Assert.AreEqual(1f, cg.alpha, 0.001f, "enter ends fully visible");
                Assert.AreEqual(rest.x, view.Content.anchoredPosition.x, 0.01f);
                Assert.AreEqual(rest.y, view.Content.anchoredPosition.y, 0.01f, "enter ends at Content's rest position");

                await transition.PlayExitAsync(view, default);
                Assert.AreEqual(0f, cg.alpha, 0.001f, "exit ends fully transparent");
                var expected = rest + offset;
                Assert.AreEqual(expected.x, view.Content.anchoredPosition.x, 0.01f);
                Assert.AreEqual(expected.y, view.Content.anchoredPosition.y, 0.01f, "exit ends slid out to the offset");
            }
            finally { UnityEngine.Object.Destroy(view.gameObject); }
        });

        [UnityTest]
        public IEnumerator ShortDurationToast_AutoDismissesWithinExpectedTime() => UniTask.ToCoroutine(async () =>
        {
            var layer = new TestToastLayer();
            var manager = new ToastManager(layer: layer, transition: new InstantToastTransition(), viewFactory: () => FakeToastView.Create());
            try
            {
                manager.Show("hi", new ToastOptions { Duration = 0.2f });
                Assert.AreEqual(1, manager.ActiveCount);

                float elapsed = 0f;
                while (manager.ActiveCount > 0 && elapsed < 1f)
                {
                    await UniTask.NextFrame();
                    elapsed += Time.unscaledDeltaTime;
                }

                Assert.AreEqual(0, manager.ActiveCount, "0.2s toast should have auto-dismissed well within 1s real time");
            }
            finally { manager.Dispose(); layer.Teardown(); }
        });

        [UnityTest]
        public IEnumerator DurationTimer_RunsWhileTimeScaleIsZero() => UniTask.ToCoroutine(async () =>
        {
            var layer = new TestToastLayer();
            var manager = new ToastManager(layer: layer, transition: new InstantToastTransition(), viewFactory: () => FakeToastView.Create());
            float originalTimeScale = Time.timeScale;
            try
            {
                Time.timeScale = 0f;
                manager.Show("paused", new ToastOptions { Duration = 0.2f });
                Assert.AreEqual(1, manager.ActiveCount);

                float elapsed = 0f;
                while (manager.ActiveCount > 0 && elapsed < 1f)
                {
                    await UniTask.NextFrame();
                    elapsed += Time.unscaledDeltaTime;
                }

                Assert.AreEqual(0, manager.ActiveCount, "duration counts down in real (unscaled) time even while the game is paused");
            }
            finally
            {
                Time.timeScale = originalTimeScale;
                manager.Dispose();
                layer.Teardown();
            }
        });

        [UnityTest]
        public IEnumerator Dispose_CancelsInFlightLongTransition() => UniTask.ToCoroutine(async () =>
        {
            var layer = new TestToastLayer();
            var manager = new ToastManager(layer: layer, transition: new SlideFadeToastTransition(duration: 5f), viewFactory: () => FakeToastView.Create());
            try
            {
                manager.Show("hi", new ToastOptions { Duration = 0f });
                await UniTask.NextFrame();
                Assert.AreEqual(1, manager.ActiveCount, "long enter transition in flight");

                manager.Dispose();

                // The transition's cancellation check runs once per loop iteration, so give it a couple of frames.
                for (int i = 0; i < 5 && manager.ActiveCount > 0; i++) await UniTask.NextFrame();

                Assert.AreEqual(0, manager.ActiveCount, "Dispose cancels the in-flight transition and destroys the instance");
            }
            finally { manager.Dispose(); layer.Teardown(); }
        });

        [UnityTest]
        public IEnumerator TapToDismiss_ClickAtToastCenter_DismissesTheToast() => UniTask.ToCoroutine(async () =>
        {
            var esGo = new GameObject("EventSystem", typeof(EventSystem));

            // Build the Canvas root and add its components FIRST, then wrap it in TestToastLayer. Canvas has a
            // [RequireComponent(typeof(RectTransform))]; adding it upgrades the GameObject's plain Transform to a
            // RectTransform, which is a different underlying object. Capturing the Transform reference before
            // that upgrade (the previous version of this test constructed TestToastLayer first) leaves the layer
            // holding a stale reference — SetParent against it silently detaches the container from the live
            // Canvas/GraphicRaycaster hierarchy the test just built, so nothing is ever actually raycastable.
            var canvasGo = new GameObject("[ToastTestCanvas]");
            canvasGo.AddComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGo.AddComponent<CanvasScaler>();
            canvasGo.AddComponent<GraphicRaycaster>();
            var layer = new TestToastLayer(canvasGo.transform);

            IToastView createdView = null;
            var manager = new ToastManager(
                layer: layer,
                transition: new InstantToastTransition(),
                viewFactory: () => createdView = DefaultToastView.Create());
            try
            {
                manager.Show("tap me", new ToastOptions { TapToDismiss = true, Duration = 0f });
                Assert.AreEqual(1, manager.ActiveCount);
                Assert.IsNotNull(createdView);

                // GraphicRaycaster.Raycast skips any Graphic whose depth is still -1 ("hasn't been processed by
                // the canvas" per Unity's own source comment) — depth is only assigned during the native canvas
                // batch build, which runs as part of an actual render. A headless Editor/Test Runner session
                // never repaints the Game View, so EventSystem.RaycastAll would return 0 here forever regardless
                // of position or how many frames elapse. Do GraphicRaycaster's per-graphic checks ourselves,
                // minus that render-dependent depth cull, so the test stays deterministic in both headless and
                // interactive runs. Still a genuine geometric hit-test — it fails for real if layout regresses.
                Graphic hit = null;
                Vector2 screenPoint = default;
                for (int i = 0; i < 10 && hit == null; i++)
                {
                    await UniTask.NextFrame();
                    Canvas.ForceUpdateCanvases();

                    Vector3 worldCenter = createdView.Content.TransformPoint(Vector3.zero);
                    screenPoint = RectTransformUtility.WorldToScreenPoint(null, worldCenter);

                    foreach (var g in canvasGo.GetComponentsInChildren<Graphic>())
                    {
                        if (!g.raycastTarget || !g.isActiveAndEnabled) continue;
                        if (RectTransformUtility.RectangleContainsScreenPoint(g.rectTransform, screenPoint, null)
                            && g.Raycast(screenPoint, null))
                        {
                            hit = g;
                            break;
                        }
                    }
                }
                Assert.IsNotNull(hit, "a raycast-target graphic covers the toast's own center");

                var clickTarget = ExecuteEvents.GetEventHandler<IPointerClickHandler>(hit.gameObject);
                Assert.IsNotNull(clickTarget, "the hit graphic routes clicks to an IPointerClickHandler");
                var pointerData = new PointerEventData(EventSystem.current) { position = screenPoint };
                ExecuteEvents.Execute(clickTarget, pointerData, ExecuteEvents.pointerClickHandler);
                await UniTask.NextFrame();

                Assert.AreEqual(0, manager.ActiveCount, "tapping the toast dismissed it");
            }
            finally
            {
                manager.Dispose();
                layer.Teardown();
                UnityEngine.Object.Destroy(esGo);
            }
        });
    }
}
