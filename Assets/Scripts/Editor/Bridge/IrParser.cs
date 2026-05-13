using System;
using System.Collections.Generic;
using Domains.UI.Data;
using UnityEngine;

namespace Domains.UI.Editor.UiBake.Services
{
    /// <summary>Static IR JSON parser. Extracted from UiBakeHandler (TECH-31984).
    /// Covers Parse (IrRoot) and BakeFromPanelSnapshot parse phase.</summary>
    public static class IrParser
    {
        /// <summary>Structured parse result — non-null root on success; populated error on fault.</summary>
        public class ParseResult
        {
            public IrRoot Root;
            public Territory.Editor.Bridge.UiBakeHandler.BakeError Error;
        }

        /// <summary>Parse IR JSON into IrRoot. Returns (root, null) on success; (null, error) on fault.</summary>
        public static ParseResult Parse(string jsonText)
        {
            if (string.IsNullOrWhiteSpace(jsonText))
            {
                return new ParseResult
                {
                    Root = null,
                    Error = new Territory.Editor.Bridge.UiBakeHandler.BakeError
                    {
                        error = "schema_violation",
                        details = "empty_or_whitespace_json",
                        path = "$",
                    },
                };
            }

            IrRoot parsed;
            try
            {
                parsed = JsonUtility.FromJson<IrRoot>(jsonText);
            }
            catch (Exception ex)
            {
                return new ParseResult
                {
                    Root = null,
                    Error = new Territory.Editor.Bridge.UiBakeHandler.BakeError
                    {
                        error = "schema_violation",
                        details = ex.Message,
                        path = "$",
                    },
                };
            }

            if (parsed == null)
            {
                return new ParseResult
                {
                    Root = null,
                    Error = new Territory.Editor.Bridge.UiBakeHandler.BakeError
                    {
                        error = "schema_violation",
                        details = "json_parsed_null",
                        path = "$",
                    },
                };
            }

            if (parsed.tokens == null)
            {
                return new ParseResult
                {
                    Root = null,
                    Error = new Territory.Editor.Bridge.UiBakeHandler.BakeError
                    {
                        error = "schema_violation",
                        details = "tokens_missing",
                        path = "$.tokens",
                    },
                };
            }

            return new ParseResult { Root = parsed, Error = null };
        }

        /// <summary>Parse panels.json snapshot JSON. Returns (snapshot, null) on success; (null, error) on fault.</summary>
        public static (PanelSnapshot snapshot, Territory.Editor.Bridge.UiBakeHandler.BakeError error) ParsePanelSnapshot(string jsonText)
        {
            if (string.IsNullOrWhiteSpace(jsonText))
            {
                return (null, new Territory.Editor.Bridge.UiBakeHandler.BakeError
                {
                    error = "schema_violation",
                    details = "empty_or_whitespace_json",
                    path = "$",
                });
            }

            PanelSnapshot parsed;
            try
            {
                parsed = JsonUtility.FromJson<PanelSnapshot>(jsonText);
            }
            catch (Exception ex)
            {
                return (null, new Territory.Editor.Bridge.UiBakeHandler.BakeError
                {
                    error = "schema_violation",
                    details = ex.Message,
                    path = "$",
                });
            }

            if (parsed == null)
            {
                return (null, new Territory.Editor.Bridge.UiBakeHandler.BakeError
                {
                    error = "schema_violation",
                    details = "json_parsed_null",
                    path = "$",
                });
            }

            if (parsed.items == null || parsed.items.Length == 0)
            {
                return (null, new Territory.Editor.Bridge.UiBakeHandler.BakeError
                {
                    error = "schema_violation",
                    details = "items_missing_or_empty",
                    path = "$.items",
                });
            }

            return (parsed, null);
        }
    }
}
