using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace KidzDev.Unity.Toast
{
    /// <summary>Added to <see cref="IToastView.Content"/> (the raycastable node) when <see cref="ToastOptions.TapToDismiss"/>
    /// is set. Only forwards the click; <see cref="ToastManager"/> owns what "dismiss" actually does.</summary>
    internal sealed class ToastTapToDismiss : MonoBehaviour, IPointerClickHandler
    {
        private Action _onClicked;

        public void Bind(Action onClicked) => _onClicked = onClicked;

        void IPointerClickHandler.OnPointerClick(PointerEventData eventData) => _onClicked?.Invoke();
    }
}
