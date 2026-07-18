using UnityEngine;

namespace KidzDev.Unity.Toast
{
    /// <summary>Owns the high-sort-order canvas the toast stack container is parented under.</summary>
    public interface IToastLayer
    {
        /// <summary>The transform the stack container is parented under. Creates the canvas lazily on first access.</summary>
        Transform Root { get; }

        /// <summary>Destroys the layer's canvas, if one was ever created.</summary>
        void Teardown();
    }
}
