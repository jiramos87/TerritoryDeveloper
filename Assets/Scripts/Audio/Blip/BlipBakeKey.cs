using System;

/// <summary>
/// Immutable cache key for <see cref="BlipBaker"/>: identifies a baked <see cref="UnityEngine.AudioClip"/>
/// by the authored-time <b>patch hash</b> (see <c>BlipPatch.PatchHash</c> — FNV-1a over serialised
/// fields) and the round-robin <paramref name="variantIndex"/> passed to
/// <see cref="BlipBaker.BakeOrGet"/>.
///
/// <para><b>Patch hash</b> is computed by <c>BlipPatch.PatchHash</c> at SO validation time and passed
/// into <c>BakeOrGet</c> by the caller; <see cref="BlipPatchFlat"/> intentionally omits it
/// (Stage 1.2 defer — <c>BlipPatchFlat.cs</c> line 162).</para>
/// </summary>
public readonly struct BlipBakeKey : IEquatable<BlipBakeKey>
{
    /// <summary>FNV-1a authored-time hash sourced from <c>BlipPatch.PatchHash</c>.</summary>
    public readonly int patchHash;

    /// <summary>Round-robin variant selector (0-based).</summary>
    public readonly int variantIndex;

    /// <summary>
    /// Constructs a <see cref="BlipBakeKey"/> from a pre-computed <paramref name="patchHash"/>
    /// and a <paramref name="variantIndex"/>.
    /// </summary>
    public BlipBakeKey(int patchHash, int variantIndex)
    {
        this.patchHash    = patchHash;
        this.variantIndex = variantIndex;
    }

    /// <inheritdoc/>
    public bool Equals(BlipBakeKey other)
        => patchHash == other.patchHash && variantIndex == other.variantIndex;

    /// <inheritdoc/>
    public override bool Equals(object obj) => obj is BlipBakeKey k && Equals(k);

    /// <summary>
    /// Combines <see cref="patchHash"/> and <see cref="variantIndex"/> with a cheap
    /// multiply-XOR mix (<c>patchHash * 397 ^ variantIndex</c>) that avoids collisions
    /// on small variant counts without the per-call allocation risk of
    /// <c>HashCode.Combine</c> on older runtimes.
    /// </summary>
    public override int GetHashCode() => unchecked((patchHash * 397) ^ variantIndex);
}
