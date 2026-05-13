using System;
using System.Collections.Generic;
using System.IO;
using Domains.Bridge.Services;
using Territory.UI;
using Territory.UI.Themed;
using UnityEditor;
using UnityEngine;

// Stage 6.1 thin — conformance dispatch delegated to BridgeConformanceService POCO.
// RunClaudeDesignConformance + RunClaudeDesignCheck remain here; all check methods
// + IR index builders + color helpers live in BridgeConformanceService.
// ExtractParamsJsonBlockInspect kept here for Inspect sibling partial compatibility.

public static partial class AgentBridgeCommandRunner
{
    static void RunClaudeDesignConformance(string repoRoot, string commandId, string requestJson)
    {
        if (!TryParseConformanceParams(requestJson, out var dto, out string parseErr)) { TryFinalizeFailed(repoRoot, commandId, parseErr); return; }
        if (string.IsNullOrWhiteSpace(dto.ir_path)) { TryFinalizeFailed(repoRoot, commandId, "params_invalid:ir_path"); return; }
        if (string.IsNullOrWhiteSpace(dto.theme_so)) { TryFinalizeFailed(repoRoot, commandId, "params_invalid:theme_so"); return; }
        bool hasPrefab = !string.IsNullOrWhiteSpace(dto.prefab_path);
        bool hasScene  = !string.IsNullOrWhiteSpace(dto.scene_root_path);
        if (hasPrefab == hasScene) { TryFinalizeFailed(repoRoot, commandId, "params_invalid:exactly_one_of_prefab_path_or_scene_root_path"); return; }

        ConformanceIrRootDto ir;
        try
        {
            string absIr = Path.IsPathRooted(dto.ir_path) ? dto.ir_path : Path.Combine(repoRoot, dto.ir_path);
            ir = JsonUtility.FromJson<ConformanceIrRootDto>(File.ReadAllText(absIr));
        }
        catch (Exception ex) { TryFinalizeFailed(repoRoot, commandId, $"ir_read_failed:{ex.GetType().Name}:{ex.Message}"); return; }
        if (ir == null) { TryFinalizeFailed(repoRoot, commandId, "ir_parse_returned_null"); return; }

        var theme = AssetDatabase.LoadAssetAtPath<UiTheme>(dto.theme_so);
        if (theme == null) { TryFinalizeFailed(repoRoot, commandId, $"theme_so_not_found:{dto.theme_so}"); return; }

        Transform root; GameObject loadedPrefabContents = null;
        string targetKind, targetPath;
        try
        {
            if (hasPrefab)
            {
                loadedPrefabContents = PrefabUtility.LoadPrefabContents(dto.prefab_path);
                if (loadedPrefabContents == null) { TryFinalizeFailed(repoRoot, commandId, $"prefab_load_failed:{dto.prefab_path}"); return; }
                root = loadedPrefabContents.transform; targetKind = "prefab"; targetPath = dto.prefab_path;
            }
            else
            {
                var sceneRoot = GameObject.Find(dto.scene_root_path);
                if (sceneRoot == null) { TryFinalizeFailed(repoRoot, commandId, $"scene_root_not_found:{dto.scene_root_path}"); return; }
                root = sceneRoot.transform; targetKind = "scene"; targetPath = dto.scene_root_path;
            }

            var irPanels = BridgeConformanceService.BuildIrPanelIndex(ir);
            var irLabelIndex = BridgeConformanceService.BuildIrLabelIndex(ir);
            BridgeConformanceService.ApplyThemePrePass(root, theme);

            var rows = new List<AgentBridgeConformanceRowDto>();
            BridgeConformanceService.WalkConformanceNode(root, root, theme, ir, irPanels, irLabelIndex, rows);

            int failCount = 0;
            for (int i = 0; i < rows.Count; i++) if (!rows[i].pass) failCount++;

            var resultDto = new AgentBridgeConformanceResultDto { ir_path = dto.ir_path, theme_so = dto.theme_so, target_kind = targetKind, target_path = targetPath, row_count = rows.Count, fail_count = failCount, rows = rows.ToArray() };
            var resp = AgentBridgeResponseFileDto.CreateOk(commandId, "claude_design_conformance");
            resp.claude_design_conformance_result = resultDto;
            CompleteOrFail(repoRoot, commandId, JsonUtility.ToJson(resp, true));
        }
        catch (Exception ex) { TryFinalizeFailed(repoRoot, commandId, $"conformance_threw:{ex.GetType().Name}:{ex.Message}"); }
        finally { if (loadedPrefabContents != null) PrefabUtility.UnloadPrefabContents(loadedPrefabContents); }
    }

    static void RunClaudeDesignCheck(string repoRoot, string commandId, string requestJson)
    {
        if (!TryParseDesignCheckParams(requestJson, out var dto, out string parseErr)) { TryFinalizeFailed(repoRoot, commandId, parseErr); return; }
        if (string.IsNullOrEmpty(dto.check_kind)) { TryFinalizeFailed(repoRoot, commandId, "params_invalid:check_kind"); return; }

        GameObject loadedPrefabContents = null;
        try
        {
            Transform root = null;
            if (!string.IsNullOrEmpty(dto.prefab_path))
            {
                loadedPrefabContents = PrefabUtility.LoadPrefabContents(dto.prefab_path);
                if (loadedPrefabContents == null) { TryFinalizeFailed(repoRoot, commandId, $"prefab_load_failed:{dto.prefab_path}"); return; }
                root = loadedPrefabContents.transform;
            }
            else if (!string.IsNullOrEmpty(dto.scene_root_path))
            {
                var sceneRoot = GameObject.Find(dto.scene_root_path);
                if (sceneRoot == null) { TryFinalizeFailed(repoRoot, commandId, $"scene_root_not_found:{dto.scene_root_path}"); return; }
                root = sceneRoot.transform;
            }
            else { TryFinalizeFailed(repoRoot, commandId, "params_invalid:require_prefab_path_or_scene_root_path"); return; }

            bool pass; string detail;
            switch (dto.check_kind)
            {
                case "frame_visual_present":
                { var img = root.GetComponent<UnityEngine.UI.Image>(); pass = img != null && img.sprite != null; detail = pass ? "Image.sprite is non-null" : "Image.sprite is null or Image component missing"; break; }
                case "font_asset_bound":
                { var texts = root.GetComponentsInChildren<TMPro.TMP_Text>(true); if (texts == null || texts.Length == 0) { pass = false; detail = "no TMP_Text components found in scope"; break; } int unbound = 0; for (int i = 0; i < texts.Length; i++) if (texts[i].font == null) unbound++; pass = unbound == 0; detail = pass ? $"all {texts.Length} TMP_Text(s) have a bound font asset" : $"{unbound}/{texts.Length} TMP_Text(s) have unbound font asset"; break; }
                case "spacing_match":
                { const float SpacingTolerance = 1f; var lg = root.GetComponent<UnityEngine.UI.LayoutGroup>(); if (lg == null) { pass = false; detail = "no LayoutGroup on root"; break; } float actualSpacing = lg is UnityEngine.UI.HorizontalOrVerticalLayoutGroup hvlg ? hvlg.spacing : 0f; float delta = Mathf.Abs(actualSpacing - dto.expected_spacing); pass = delta <= SpacingTolerance; detail = pass ? $"spacing={actualSpacing:F1} matches expected={dto.expected_spacing:F1} within ±{SpacingTolerance} px" : $"spacing={actualSpacing:F1} differs from expected={dto.expected_spacing:F1} (delta={delta:F1}, tolerance=±{SpacingTolerance} px)"; break; }
                case "button_states_wired":
                { var sel = root.GetComponent<UnityEngine.UI.Selectable>(); if (sel == null) { pass = false; detail = "no Selectable component on root"; break; } pass = sel.transition != UnityEngine.UI.Selectable.Transition.None || sel.colors.normalColor != Color.white; detail = pass ? $"transition={sel.transition} normalColor={sel.colors.normalColor}" : "Selectable has default transition=None and normalColor=white — not wired"; break; }
                case "illumination_layer_present":
                { bool found = false; for (int i = 0; i < root.childCount; i++) { if (root.GetChild(i).GetComponent<ThemedIlluminationLayer>() != null) { found = true; break; } } pass = found; detail = pass ? "ThemedIlluminationLayer found as direct child" : "no ThemedIlluminationLayer direct child found"; break; }
                default: TryFinalizeFailed(repoRoot, commandId, $"check_kind_unknown:{dto.check_kind}"); return;
            }

            string resultJson = $"{{\"check_kind\":\"{dto.check_kind}\",\"pass\":{(pass ? "true" : "false")},\"detail\":\"{EscapeJsonString(detail)}\"}}";
            var resp = AgentBridgeResponseFileDto.CreateOk(commandId, "claude_design_check");
            resp.result_json = resultJson;
            CompleteOrFail(repoRoot, commandId, JsonUtility.ToJson(resp, true));
        }
        catch (Exception ex) { TryFinalizeFailed(repoRoot, commandId, $"design_check_threw:{ex.GetType().Name}:{ex.Message}"); }
        finally { if (loadedPrefabContents != null) PrefabUtility.UnloadPrefabContents(loadedPrefabContents); }
    }

    static bool TryParseConformanceParams(string requestJson, out ConformanceParamsDto dto, out string error)
    {
        dto = null; error = null;
        if (string.IsNullOrWhiteSpace(requestJson)) { error = "params_invalid: empty request_json"; return false; }
        string paramsJson = ExtractParamsJsonBlockInspect(requestJson);
        if (string.IsNullOrWhiteSpace(paramsJson)) { dto = new ConformanceParamsDto(); return true; }
        try { dto = JsonUtility.FromJson<ConformanceParamsDto>(paramsJson); }
        catch (Exception ex) { error = $"params_invalid: {ex.Message}"; return false; }
        if (dto == null) dto = new ConformanceParamsDto();
        return true;
    }

    static bool TryParseDesignCheckParams(string requestJson, out DesignCheckParamsDto dto, out string error)
    {
        dto = null; error = null;
        if (string.IsNullOrWhiteSpace(requestJson)) { error = "params_invalid: empty request_json"; return false; }
        string paramsJson = ExtractParamsJsonBlockInspect(requestJson);
        if (string.IsNullOrWhiteSpace(paramsJson)) { dto = new DesignCheckParamsDto(); return true; }
        try { dto = JsonUtility.FromJson<DesignCheckParamsDto>(paramsJson); }
        catch (Exception ex) { error = $"params_invalid: {ex.Message}"; return false; }
        if (dto == null) dto = new DesignCheckParamsDto();
        return true;
    }

    // Mirror of ExtractParamsJsonBlock (from Mutations.cs) — used by Inspect + Conformance.
    static string ExtractParamsJsonBlockInspect(string requestJson) => ExtractParamsJsonBlock(requestJson);
}
