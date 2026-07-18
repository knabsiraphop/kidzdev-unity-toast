using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine.EventSystems;

namespace KidzDev.Unity.Toast.Tests
{
    [TestFixture]
    internal sealed class ToastManagerTests
    {
        private TestToastLayer _layer;
        private GatedToastTransition _transition;
        private List<FakeToastView> _createdViews;
        private ToastManager _manager;

        [SetUp]
        public void SetUp()
        {
            _layer = new TestToastLayer();
            _transition = new GatedToastTransition();
            _createdViews = new List<FakeToastView>();
            _manager = new ToastManager(layer: _layer, transition: _transition, viewFactory: CreateTrackedView, maxVisible: 3);
        }

        [TearDown]
        public void TearDown()
        {
            _manager.Dispose();
            _layer.Teardown();
        }

        private FakeToastView CreateTrackedView()
        {
            var view = FakeToastView.Create($"FakeToastView{_createdViews.Count}");
            _createdViews.Add(view);
            return view;
        }

        [Test]
        public void Show_ParentsTheViewUnderTheContainer_AndActivatesIt()
        {
            _manager.Show("hello", new ToastOptions { Duration = 0f });

            var view = _createdViews[0];
            Assert.IsTrue(view.gameObject.activeSelf);
            var container = view.transform.parent;
            Assert.IsNotNull(container, "view is parented under a stack container");
            Assert.AreSame(_layer.Root, container.parent, "container is parented under the layer root");
        }

        [Test]
        public void Show_SetsContentOnTheView()
        {
            _manager.Show("hello world", new ToastOptions { Duration = 0f });

            var view = _createdViews[0];
            Assert.AreEqual(1, view.SetContentCalls);
            Assert.AreEqual("hello world", view.LastContent.Message);
        }

        [Test]
        public void ActiveCount_TracksShowsAndDismisses()
        {
            Assert.AreEqual(0, _manager.ActiveCount);

            var handle = _manager.Show("a", new ToastOptions { Duration = 0f });
            Assert.AreEqual(1, _manager.ActiveCount);

            handle.Dismiss();
            Assert.AreEqual(0, _manager.ActiveCount);
        }

        [Test]
        public void Show_AtCapacity_EvictsOldest_NotNewest()
        {
            _manager.Show("1", new ToastOptions { Duration = 0f });
            _manager.Show("2", new ToastOptions { Duration = 0f });
            _manager.Show("3", new ToastOptions { Duration = 0f });
            Assert.AreEqual(3, _manager.ActiveCount);

            _manager.Show("4", new ToastOptions { Duration = 0f }); // maxVisible = 3

            Assert.AreEqual(3, _manager.ActiveCount, "cap never exceeded");
            Assert.IsTrue(_createdViews[0] == null, "oldest ('1') instance was destroyed");
            Assert.IsFalse(_createdViews[3] == null, "newest ('4') instance is alive");
        }

        [Test]
        public void EvictedEntry_StillExiting_IsSkippedByTheNextEviction()
        {
            _manager.Show("1", new ToastOptions { Duration = 0f });
            _manager.Show("2", new ToastOptions { Duration = 0f });
            _manager.Show("3", new ToastOptions { Duration = 0f });
            Assert.AreEqual(3, _manager.ActiveCount);

            _transition.Open = false; // hold every subsequent enter/exit in flight
            _manager.Show("4", new ToastOptions { Duration = 0f }); // evicts "1"; its exit now parked
            Assert.AreEqual(4, _manager.ActiveCount, "the evicted-but-still-exiting entry is not removed until its exit completes");

            _manager.Show("5", new ToastOptions { Duration = 0f }); // must evict "2" (oldest *live* entry), not re-target "1"
            Assert.AreEqual(5, _manager.ActiveCount);

            _transition.Release(); // let every parked enter/exit finish
            Assert.AreEqual(3, _manager.ActiveCount, "only the genuinely live entries (3, 4, 5) remain");
            Assert.IsTrue(_createdViews[0] == null, "'1' destroyed");
            Assert.IsTrue(_createdViews[1] == null, "'2' destroyed");
            Assert.IsFalse(_createdViews[2] == null, "'3' still alive");
        }

        [Test]
        public void Handle_Dismiss_IsIdempotent()
        {
            var handle = _manager.Show("hi", new ToastOptions { Duration = 0f });
            handle.Dismiss();
            Assert.AreEqual(0, _manager.ActiveCount);

            handle.Dismiss(); // second call must not throw or double-remove
            Assert.AreEqual(0, _manager.ActiveCount);
        }

        [Test]
        public void HideAll_DismissesEveryVisibleToast()
        {
            _manager.Show("1", new ToastOptions { Duration = 0f });
            _manager.Show("2", new ToastOptions { Duration = 0f });
            Assert.AreEqual(2, _manager.ActiveCount);

            _manager.HideAll();

            Assert.AreEqual(0, _manager.ActiveCount);
        }

        [Test]
        public void Dispose_CancelsInFlightShows_AndDestroysInstances()
        {
            _manager.Show("1", new ToastOptions { Duration = 0f });
            var view = _createdViews[0];
            Assert.AreEqual(1, _manager.ActiveCount);

            _manager.Dispose();

            Assert.AreEqual(0, _manager.ActiveCount);
            Assert.IsTrue(view == null, "instance destroyed on manager Dispose");
        }

        [Test]
        public void Show_ViewFactoryReturnsNull_ThrowsDiagnosable()
        {
            using var manager = new ToastManager(layer: new TestToastLayer(), transition: new GatedToastTransition(), viewFactory: () => null);

            var ex = Assert.Throws<System.InvalidOperationException>(() => manager.Show("hi"));
            StringAssert.Contains("viewFactory", ex.Message);
        }

        [Test]
        public void Show_UsesPerCallTransitionOverride_NotTheManagerDefault()
        {
            var perCallTransition = new GatedToastTransition();
            var handle = _manager.Show("hi", new ToastOptions { Transition = perCallTransition, Duration = 0f });

            Assert.AreEqual(1, perCallTransition.EnterCalls);
            Assert.AreEqual(0, _transition.EnterCalls, "the manager's default transition was not used");

            handle.Dismiss();
            Assert.AreEqual(1, perCallTransition.ExitCalls);
        }

        [Test]
        public void BottomAnchor_NewestIsLastSibling()
        {
            _manager.Show("1", new ToastOptions { Duration = 0f });
            _manager.Show("2", new ToastOptions { Duration = 0f });

            var container = _createdViews[0].transform.parent;
            Assert.AreEqual(container.childCount - 1, _createdViews[1].transform.GetSiblingIndex(),
                "Bottom anchor: newest sits nearest the bottom, i.e. last sibling");
        }

        [Test]
        public void TopAnchor_NewestIsFirstSibling()
        {
            var createdTop = new List<FakeToastView>();
            using var topManager = new ToastManager(
                layer: new TestToastLayer(),
                transition: new GatedToastTransition(),
                viewFactory: () =>
                {
                    var v = FakeToastView.Create();
                    createdTop.Add(v);
                    return v;
                },
                anchor: ToastAnchor.Top);

            topManager.Show("1", new ToastOptions { Duration = 0f });
            topManager.Show("2", new ToastOptions { Duration = 0f });

            Assert.AreEqual(0, createdTop[1].transform.GetSiblingIndex(),
                "Top anchor: newest sits nearest the top, i.e. first sibling");
        }

        [Test]
        public void TapToDismiss_WhenEnabled_DismissesOnClick()
        {
            _manager.Show("hi", new ToastOptions { TapToDismiss = true, Duration = 0f });
            Assert.AreEqual(1, _manager.ActiveCount);

            var tap = _createdViews[0].Content.gameObject.GetComponent<ToastTapToDismiss>();
            Assert.IsNotNull(tap, "TapToDismiss adds the click handler to the view's Content node");

            ((IPointerClickHandler)tap).OnPointerClick(null);

            Assert.AreEqual(0, _manager.ActiveCount);
        }

        [Test]
        public void TapToDismiss_WhenDisabled_DoesNotAddHandler()
        {
            _manager.Show("hi", new ToastOptions { TapToDismiss = false, Duration = 0f });

            Assert.IsNull(_createdViews[0].Content.gameObject.GetComponent<ToastTapToDismiss>());
        }

        [Test]
        public void Constructor_ClampsMaxVisibleToAtLeastOne()
        {
            var created = new List<FakeToastView>();
            using var manager = new ToastManager(
                layer: new TestToastLayer(),
                transition: new GatedToastTransition(),
                viewFactory: () =>
                {
                    var v = FakeToastView.Create();
                    created.Add(v);
                    return v;
                },
                maxVisible: 0);

            manager.Show("1", new ToastOptions { Duration = 0f });
            manager.Show("2", new ToastOptions { Duration = 0f });

            Assert.AreEqual(1, manager.ActiveCount, "maxVisible clamped to 1, so the second show evicts the first");
        }
    }
}
