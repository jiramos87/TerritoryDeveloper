using System;

namespace Domains.UI.Data
{
    // ── Stage 9.10 — PanelSnapshot DTOs (JsonUtility-friendly, no Newtonsoft) ─

    /// <summary>Top-level panels.json snapshot shape (schema_version 3).</summary>
    [Serializable]
    public class PanelSnapshot
    {
        public string snapshot_id;
        public string kind;
        public int schema_version;
        public PanelSnapshotItem[] items;
    }

    /// <summary>One panel item in panels.json items[].</summary>
    [Serializable]
    public class PanelSnapshotItem
    {
        public string slug;
        public PanelSnapshotFields fields;
        public PanelSnapshotChild[] children;
    }

    /// <summary>Panel fields block — mirrors panel_detail columns projected by exporter.</summary>
    [Serializable]
    public class PanelSnapshotFields
    {
        /// <summary>Catalog display_name — drives header label fallback for themed-label modal-title.</summary>
        public string display_name;
        public string layout_template;
        public string layout;
        public float gap_px;
        public string padding_json;
        public string params_json;
        /// <summary>DB-sourced RectTransform overlay (panel_detail.rect_json).</summary>
        public string rect_json;
    }

    /// <summary>Typed view of panel_detail.rect_json.</summary>
    [Serializable]
    public class PanelRectJson
    {
        public float[] anchor_min;
        public float[] anchor_max;
        public float[] pivot;
        public float[] anchored_position;
        public float[] size_delta;
    }

    /// <summary>Per-child row in panels.json children[].</summary>
    [Serializable]
    public class PanelSnapshotChild
    {
        public int ord;
        public string kind;
        public string params_json;
        public string sprite_ref;
        /// <summary>JSON string of layout routing metadata.</summary>
        public string layout_json;
        /// <summary>Stage 9.15 — per-child semantic slug.</summary>
        public string instance_slug;
    }

    // Imp-2 (bake-fix-2026-05-08) — typed `params_json` / `layout_json` POCOs.

    /// <summary>Inner size sub-object for layout_json.</summary>
    [Serializable]
    public class PanelChildLayoutSize
    {
        public float w;
        public float h;
    }

    /// <summary>Typed view of layout_json on PanelSnapshotChild.</summary>
    [Serializable]
    public class PanelChildLayoutJson
    {
        public string zone;
        public int ord;
        public int col;
        public int row;
        public int sub_col;
        public int rowSpan;
        public PanelChildLayoutSize size;
    }

    /// <summary>Typed view of params_json on PanelSnapshotChild.</summary>
    [Serializable]
    public class PanelChildParamsJson
    {
        public string icon;
        public string kind;
        public string label;
        public string action;
        public string bind;
        public string font;
        public string align;
        public string format;
        public string cadence;
        public string label_bind;
        public string bind_state;
        public string alt_icon;
        public string sub_bind;
        public string sub_format;
        public string shape;
        public string text_static;
        public string size_token;
        public string color_token;
        public string disabled_bind;
        public string visible_bind;
        public string tooltip;
        public string tooltip_override_when_disabled;
        public string action_confirm;
        public string confirm_label;
        public int    confirm_window_ms;
        public string slot_bind;
        public string @default;
        // Stage 10 budget/stats panel
        public string actionId;
        public string bindId;
        public string variant;
        public string quadrant;
        public int min;
        public int max;
        public int step;
        public bool numeric;
        // text-input widget
        public string placeholder;
        // Stage 10 stats/budget panel — tab strips + chart series binding
        public string[] tabs;
        public string[] options;
        public string seriesId;
        public string tabGroup;
        // Stats-panel pilot — corner-anchor overlay
        public string corner;
        public int corner_size;
        public string corner_offset;
        // Bucket C
        public string[] cards;
        public string[] chips;
        public string[] axisLabels;
        public string subtype;
        public string size_tone;
    }

    /// <summary>Typed view of panel-level params_json on PanelSnapshotFields.</summary>
    [Serializable]
    public class PanelFieldsParamsJson
    {
        public string position;
        public int row_columns;
        public int width;
        public int height;
        public string defaultTab;
    }

    /// <summary>Typed view of padding_json on PanelSnapshotFields.</summary>
    [Serializable]
    public class PanelPaddingJson
    {
        public int top;
        public int right;
        public int bottom;
        public int left;
        public float border_width;
        public string border_color_token;
        public float corner_radius;
    }
}
