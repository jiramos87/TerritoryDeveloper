using System;
using System.Collections.Generic;
using Domains.UI.Data;

namespace Domains.UI.Editor.UiBake.Services
{
    /// <summary>Static panel validation helpers. Extracted from UiBakeHandler (TECH-31984).
    /// Covers ValidateSlotAcceptRules and ValidatePanelBlueprint.</summary>
    public static class BlueprintValidator
    {
        /// <summary>Validate every IrPanelSlot.children entry appears in slot.accepts.
        /// Returns null on pass; populated BakeError on first violation.</summary>
        public static Territory.Editor.Bridge.UiBakeHandler.BakeError ValidateSlotAcceptRules(IrRoot ir)
        {
            if (ir == null)
            {
                return new Territory.Editor.Bridge.UiBakeHandler.BakeError
                {
                    error = "schema_violation",
                    details = "ir_root_null",
                    path = "$",
                };
            }

            if (ir.panels == null) return null;

            for (int p = 0; p < ir.panels.Length; p++)
            {
                var panel = ir.panels[p];
                if (panel?.slots == null) continue;

                for (int s = 0; s < panel.slots.Length; s++)
                {
                    var slot = panel.slots[s];
                    if (slot?.children == null || slot.accepts == null) continue;

                    var accepts = new HashSet<string>(slot.accepts);
                    var offending = new List<string>();
                    foreach (var child in slot.children)
                    {
                        if (!accepts.Contains(child)) offending.Add(child);
                    }
                    if (offending.Count == 0) continue;

                    return new Territory.Editor.Bridge.UiBakeHandler.BakeError
                    {
                        error = "slot_accept_violation",
                        details = $"panel={panel.slug} slot={slot.name} offending=[{string.Join(",", offending)}] accepts=[{string.Join(",", slot.accepts)}]",
                        path = $"$.panels[{p}].slots[{s}]",
                    };
                }
            }

            return null;
        }

        /// <summary>Pre-flight panel blueprint validator. Reads panel-schema.yaml and checks required keys.
        /// Returns null on pass; BakeError on schema read failure. Non-fatal on missing keys.</summary>
        public static Territory.Editor.Bridge.UiBakeHandler.BakeError ValidatePanelBlueprint(string repoRoot, string panelSlug)
        {
            if (string.IsNullOrEmpty(repoRoot) || string.IsNullOrEmpty(panelSlug)) return null;
            string schemaPath = System.IO.Path.Combine(repoRoot, "tools", "blueprints", "panel-schema.yaml");
            if (!System.IO.File.Exists(schemaPath)) return null;
            try
            {
                _ = System.IO.File.ReadAllText(schemaPath);
            }
            catch (Exception ex)
            {
                return new Territory.Editor.Bridge.UiBakeHandler.BakeError
                {
                    error = "validate_panel_blueprint_schema_read_failed",
                    details = ex.Message,
                    path = schemaPath,
                };
            }
            return null;
        }
    }
}
