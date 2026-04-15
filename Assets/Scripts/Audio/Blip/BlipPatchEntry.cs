using System;
using UnityEngine;

namespace Territory.Audio
{
    /// <summary>
    /// Inspector-authored binding of a <see cref="BlipId"/> to its authoring
    /// <see cref="BlipPatch"/> ScriptableObject.
    /// <para>
    /// Used as elements of <c>BlipCatalog.entries</c>.
    /// <c>BlipCatalog.Awake</c> flattens each entry into a parallel
    /// <see cref="BlipPatchFlat"/> array via <see cref="BlipPatchFlat.FromSO"/>.
    /// </para>
    /// </summary>
    [Serializable]
    public struct BlipPatchEntry
    {
        /// <summary>Sound-event identifier bound to <see cref="patch"/>.</summary>
        public BlipId id;

        /// <summary>Authoring ScriptableObject flattened at catalog bootstrap.</summary>
        public BlipPatch patch;
    }
}
