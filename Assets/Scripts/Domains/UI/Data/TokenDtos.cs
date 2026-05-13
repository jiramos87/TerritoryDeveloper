using System;

namespace Domains.UI.Data
{
    // Bucket F (2026-05-12) — token snapshot DTOs. Used by TokenResolver.

    /// <summary>Top-level tokens.json snapshot shape.</summary>
    [Serializable]
    public class TokenSnapshot
    {
        public string snapshot_id;
        public string kind;
        public int schema_version;
        public TokenSnapshotItem[] items;
    }

    [Serializable]
    public class TokenSnapshotItem
    {
        public string slug;
        public string display_name;
        public string token_kind;
        public string value_json;
    }

    [Serializable]
    public class TokenSpacingValueDto { public int value; }

    [Serializable]
    public class TokenColorValueDto { public string hex; public string value; }

    [Serializable]
    public class TokenTypeScaleValueDto { public int pt; public string weight; public string family; }
}
