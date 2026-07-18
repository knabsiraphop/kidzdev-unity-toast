using UnityEngine;

namespace KidzDev.Unity.Toast
{
    /// <summary>The data a view renders for one toast. Kept separate from <see cref="ToastOptions"/> (which
    /// also carries behavior like <see cref="ToastOptions.Duration"/>/<see cref="ToastOptions.TapToDismiss"/>) —
    /// this is only the visual payload.</summary>
    public readonly struct ToastContent
    {
        public readonly string Message;
        public readonly Sprite Icon;
        public readonly Color? BackgroundColor;
        public readonly Color? TextColor;

        public ToastContent(string message, Sprite icon, Color? backgroundColor, Color? textColor)
        {
            Message = message;
            Icon = icon;
            BackgroundColor = backgroundColor;
            TextColor = textColor;
        }
    }
}
