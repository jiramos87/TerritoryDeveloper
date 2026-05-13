using System;

// Stage 6.1 extract — conformance DTOs extracted from AgentBridgeCommandRunner.Conformance.cs.
// Moved to keep Conformance.cs ≤200 LOC. File-level (no namespace) to match legacy shape.

[Serializable]
public class ConformanceParamsDto
{
    public string ir_path;
    public string theme_so;
    public string prefab_path;
    public string scene_root_path;
}

/// <summary>Stage 1.4 (T1.4.5) — params bag for <c>claude_design_check</c> targeted check command.</summary>
[Serializable]
public class DesignCheckParamsDto
{
    public string check_kind;
    public string prefab_path;
    public string scene_root_path;
    /// <summary>Expected spacing value in px; used by <c>spacing_match</c> check kind.</summary>
    public float expected_spacing;
}

// IR JSON subset needed for conformance scope. JsonUtility cannot deserialize
// the full IR (interactives[].detail is open-ended) — these DTOs intentionally
// drop fields the bridge never reads (motion_curve, illumination, detail).
[Serializable]
public class ConformanceIrRootDto
{
    public ConformanceIrTokensDto tokens;
    public ConformanceIrPanelDto[] panels;
}

[Serializable]
public class ConformanceIrTokensDto
{
    public ConformanceIrPaletteDto[] palette;
    public ConformanceIrFrameStyleDto[] frame_style;
    public ConformanceIrFontFaceDto[] font_face;
}

[Serializable]
public class ConformanceIrPaletteDto
{
    public string slug;
    public string[] ramp;
}

[Serializable]
public class ConformanceIrFrameStyleDto
{
    public string slug;
    public string edge;
    public float innerShadowAlpha;
}

[Serializable]
public class ConformanceIrFontFaceDto
{
    public string slug;
    public string family;
    public int weight;
}

[Serializable]
public class ConformanceIrPanelDto
{
    public string slug;
    public string archetype;
    public string kind;
    public ConformanceIrPanelSlotDto[] slots;
}

[Serializable]
public class ConformanceIrPanelSlotDto
{
    public string name;
    public string[] accepts;
    public string[] children;
    public string[] labels;
}

[Serializable]
public class AgentBridgeConformanceResultDto
{
    public string ir_path;
    public string theme_so;
    public string target_kind; // "prefab" | "scene"
    public string target_path;
    public int row_count;
    public int fail_count;
    public AgentBridgeConformanceRowDto[] rows;
}

[Serializable]
public class AgentBridgeConformanceRowDto
{
    public string node_path;
    public string component;
    public string check_kind;
    public string slug;
    public string expected;
    public string resolved;
    public string actual;
    public string severity; // info | warn | error
    public bool pass;
    public string message;
}
