using UnityEngine;

namespace KidzDev.Unity.Toast
{
    /// <summary>The view contract a toast prefab/instance implements. <see cref="DefaultToastView"/> is the
    /// built-in, code-constructed implementation; inject a <c>Func&lt;IToastView&gt;</c> factory into
    /// <see cref="ToastManager"/> to supply your own.</summary>
    public interface IToastView
    {
        /// <summary>The layout cell parented under the stack's <c>VerticalLayoutGroup</c> container. Its
        /// position is owned by that layout group — never animate it directly.</summary>
        GameObject Root { get; }

        /// <summary>The inner rect a transition freely animates (slide/fade). Sits inside <see cref="Root"/>,
        /// outside the parent layout group's control.</summary>
        RectTransform Content { get; }

        /// <summary>Applies <paramref name="content"/>'s message/icon/colors and resizes to fit.</summary>
        void SetContent(in ToastContent content);
    }
}
