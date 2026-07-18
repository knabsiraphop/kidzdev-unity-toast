using System;
using UnityEngine;
using UnityEngine.UI;

namespace KidzDev.Unity.Toast
{
    /// <summary>Default <see cref="IToastLayer"/>. Lazily creates a screen-space overlay canvas above the rest
    /// of the scene's UI — sorted above <c>ui-overlay</c>'s default layer (2000) so "Connection lost" stays
    /// readable over a fullscreen loading dim.</summary>
    public sealed class ToastLayer : IToastLayer
    {
        private readonly int _sortOrder;
        private readonly Func<Transform> _layerFactory;
        private Transform _root;

        /// <param name="sortOrder">Sorting order of the default overlay canvas. Ignored when <paramref name="layerFactory"/> is set.</param>
        /// <param name="layerFactory">Optional factory for the transform toasts are parented under, called lazily on first access.</param>
        public ToastLayer(int sortOrder = 3000, Func<Transform> layerFactory = null)
        {
            _sortOrder = sortOrder;
            _layerFactory = layerFactory;
        }

        /// <inheritdoc/>
        public Transform Root
        {
            get
            {
                if (_root != null) return _root;

                if (_layerFactory != null)
                {
                    _root = _layerFactory();
                    if (_root == null)
                        throw new InvalidOperationException(
                            "The layerFactory returned null; it must return the transform toasts are parented under.");
                    return _root;
                }

                var go = new GameObject("[ToastLayer]");
                if (Application.isPlaying) UnityEngine.Object.DontDestroyOnLoad(go);

                var canvas = go.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = _sortOrder;
                go.AddComponent<CanvasScaler>();
                go.AddComponent<GraphicRaycaster>();

                _root = go.transform;
                return _root;
            }
        }

        /// <inheritdoc/>
        public void Teardown()
        {
            if (_root == null) return;
            DestroyObject(_root.gameObject);
            _root = null;
        }

        private static void DestroyObject(UnityEngine.Object obj)
        {
            if (obj == null) return;
            if (Application.isPlaying) UnityEngine.Object.Destroy(obj);
            else UnityEngine.Object.DestroyImmediate(obj);
        }
    }
}
