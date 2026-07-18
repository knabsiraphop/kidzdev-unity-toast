namespace KidzDev.Unity.Toast
{
    /// <summary>Which screen edge a <see cref="ToastManager"/>'s stack grows from.</summary>
    public enum ToastAnchor
    {
        /// <summary>Newest toast nearest the bottom of the screen; older toasts pushed up.</summary>
        Bottom,

        /// <summary>Newest toast nearest the top of the screen; older toasts pushed down.</summary>
        Top
    }
}
