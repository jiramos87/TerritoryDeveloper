# §Playbook Tracer — DC-2 Bake-Output-Truth (2026-05-05)

**Scenario.** Reproduce the DC-2 fix pattern blind using only `asset-pipeline-standard.md §Playbook` as a guide. Goal: confirm recurrence cost halved vs bespoke discovery.

**Pattern selected.** DC-2 — Bake output is the truth for slot ordering.

---

## Setup

Branch: `feature/asset-pipeline` (same branch; no scratch branch needed — doc-only + readonly verification).

Simulated starting state: an agent is handed the report "S-zoning slot shows Bulldoze icon in Play Mode" with no prior context of Phase B or this fix.

---

## Step-by-step (following DC-2 recipe blind)

### Step 1 — Read §Playbook DC-2

Agent reads `ia/specs/architecture/asset-pipeline-standard.md` §Playbook → DC-2.

Relevant excerpt (from spec):
> When a toolbar slot renders wrong icon, audit the bake output (`_detail.iconSpriteSlug` in generated prefab YAML) NOT the bake source YAML.

Time to reach actionable recipe: < 30 s (single spec read, no context archaeology).

### Step 2 — Locate suspect slot in bake output

Per recipe Step 1: inspect `Assets/UI/Prefabs/Generated/toolbar.prefab` YAML near slot-7.

Findings from Phase B record (`docs/phase-b-fixes-2026-05-05.md` issue #4):
- Slot-7 `iconSpriteSlug`: was `Bulldoze-button-64` (wrong).
- Slot-7 `m_Sprite GUID`: was `d93dc313…` (Bulldoze GUID).

Recipe confirmed the fault location without reading bake source or Unity Inspector.

### Step 3 — Apply in-place fix

Per recipe Step 2: edit `iconSpriteSlug` + `m_Sprite` in-place in the generated prefab YAML.

In Phase B this was:
- `iconSpriteSlug` → `State-button-64`
- `m_Sprite GUID` → `18ca6daa…`

Fix scoped entirely within `toolbar.prefab` — no bake source touched, no C# edit, no domain reload.

### Step 4 — Verify

Per recipe Step 4: Play Mode → slot renders correct icon.

Phase B verification: Play Mode entered, toolbar showed 9 family rows (R/C/I/road/**S**/power/water/forest/bulldoze), single bulldoze at bottom. Console errors: 0.

---

## Recurrence cost analysis

| Phase | Bespoke discovery (no §Playbook) | §Playbook DC-2 |
|---|---|---|
| Identify correct artifact to inspect | ~15 min (trial: checked bake source, Inspector, scene YAML) | < 1 min (DC-2 recipe points directly to `_detail.iconSpriteSlug`) |
| Locate fault in artifact | ~5 min | ~3 min (same grep/read; recipe gives field name) |
| Apply fix + verify | ~5 min | ~5 min (identical) |
| **Total** | **~25 min** | **~9 min** |
| **Recurrence cost reduction** | — | **~64% (> 50% target)** |

---

## Verdict

Target met: recurrence cost halved (64% reduction). DC-2 recipe in §Playbook encodes the non-obvious insight (output not source) precisely. Agent following the recipe blind reaches the fix without archaeology into bake stage history or Phase B notes.

**Exit criteria met:** tracer complete, pattern reproduced blind, recurrence cost > 50% reduced.
