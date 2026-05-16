using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Domains.UI.Data;
using UnityEngine;

namespace Domains.UI.Services
{
    /// <summary>Static token-resolution helpers extracted from UiBakeHandler (TECH-31975).
    /// Runtime-safe — no UnityEditor/AssetDatabase refs.</summary>
    public static class TokenResolver
    {
        static Dictionary<string, int> s_SpacingInt;
        static Dictionary<string, int[]> s_SpacingArr;
        static Dictionary<string, string> s_ColorHex;
        static Dictionary<string, TokenTypeScaleValueDto> s_TypeScale;

        static readonly Regex s_SpacingKeyRegex = new Regex(
            "\"(top|left|right|bottom|border_width|corner_radius|gap|padding)\"\\s*:\\s*\"([a-z][a-z0-9-]+)\"",
            RegexOptions.Compiled);

        /// <summary>Load tokens.json sibling of panelsPath into static caches. Refreshes on every bake invocation.</summary>
        public static void LoadTokenSnapshot(string panelsPath)
        {
            s_SpacingInt  = new Dictionary<string, int>(StringComparer.Ordinal);
            s_SpacingArr  = new Dictionary<string, int[]>(StringComparer.Ordinal);
            s_ColorHex    = new Dictionary<string, string>(StringComparer.Ordinal);
            s_TypeScale   = new Dictionary<string, TokenTypeScaleValueDto>(StringComparer.Ordinal);
            if (string.IsNullOrEmpty(panelsPath)) return;
            string dir = System.IO.Path.GetDirectoryName(panelsPath);
            if (string.IsNullOrEmpty(dir)) return;
            string tokensPath = System.IO.Path.Combine(dir, "tokens.json");
            if (!System.IO.File.Exists(tokensPath)) return;
            string raw;
            try { raw = System.IO.File.ReadAllText(tokensPath); } catch { return; }
            TokenSnapshot snap;
            try { snap = JsonUtility.FromJson<TokenSnapshot>(raw); } catch { return; }
            if (snap?.items == null) return;
            foreach (var it in snap.items)
            {
                if (it == null || string.IsNullOrEmpty(it.slug) || string.IsNullOrEmpty(it.value_json)) continue;
                switch (it.token_kind)
                {
                    case "spacing":
                        var arrMatch = Regex.Match(it.value_json, "\"value\"\\s*:\\s*\\[([^\\]]*)\\]");
                        if (arrMatch.Success)
                        {
                            var parts = arrMatch.Groups[1].Value.Split(',');
                            var arr = new int[parts.Length];
                            bool ok = true;
                            for (int i = 0; i < parts.Length; i++)
                            {
                                if (!int.TryParse(parts[i].Trim(), out arr[i])) { ok = false; break; }
                            }
                            if (ok) s_SpacingArr[it.slug] = arr;
                        }
                        else
                        {
                            try
                            {
                                var sp = JsonUtility.FromJson<TokenSpacingValueDto>(it.value_json);
                                if (sp != null) s_SpacingInt[it.slug] = sp.value;
                            }
                            catch { }
                        }
                        break;
                    case "color":
                        try
                        {
                            var col = JsonUtility.FromJson<TokenColorValueDto>(it.value_json);
                            string hex = !string.IsNullOrEmpty(col?.hex) ? col.hex : col?.value;
                            if (!string.IsNullOrEmpty(hex)) s_ColorHex[it.slug] = hex;
                        }
                        catch { }
                        break;
                    case "type-scale":
                        try
                        {
                            var ts = JsonUtility.FromJson<TokenTypeScaleValueDto>(it.value_json);
                            if (ts != null) s_TypeScale[it.slug] = ts;
                        }
                        catch { }
                        break;
                }
            }
        }

        /// <summary>Substitute spacing-token slug refs inside a padding_json blob. Idempotent.</summary>
        public static string SubstituteSpacingTokensInJson(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return raw;
            if (s_SpacingInt == null || s_SpacingInt.Count == 0) return raw;
            return s_SpacingKeyRegex.Replace(raw, m =>
            {
                string key = m.Groups[1].Value;
                string slug = m.Groups[2].Value;
                if (s_SpacingInt.TryGetValue(slug, out var v)) return "\"" + key + "\": " + v.ToString(System.Globalization.CultureInfo.InvariantCulture);
                return m.Value;
            });
        }

        /// <summary>Resolve type-scale font size by slug; fallback when missing.</summary>
        public static float ResolveTypeScaleFontSize(string slug, float fallback)
        {
            if (!string.IsNullOrEmpty(slug) && s_TypeScale != null
                && s_TypeScale.TryGetValue(slug, out var ts) && ts != null && ts.pt > 0)
                return ts.pt;
            return fallback;
        }

        /// <summary>Resolve type-scale weight by slug; fallback when missing.</summary>
        public static string ResolveTypeScaleWeight(string slug, string fallback)
        {
            if (!string.IsNullOrEmpty(slug) && s_TypeScale != null
                && s_TypeScale.TryGetValue(slug, out var ts) && !string.IsNullOrEmpty(ts?.weight))
                return ts.weight;
            return fallback;
        }

        /// <summary>Resolve color token hex by slug; null when missing.</summary>
        public static string ResolveColorTokenHex(string slug)
        {
            if (!string.IsNullOrEmpty(slug) && s_ColorHex != null
                && s_ColorHex.TryGetValue(slug, out var hex))
                return hex;
            return null;
        }
    }
}
