# UI Element Grill — Convergence Rubric

> **Role.** Documents the scoring algorithm used to measure agent-grilled panel definitions against independently-authored human references. Threshold ≥0.85 = agent-autonomous grilling declared. Authored in Stage 6.0 (TECH-24416). Test file: `tools/scripts/__tests__/ui-grill-convergence-rubric.test.mjs` (deferred to future Region UI master plan).

---

## Algorithm

**Score formula (v1):**

```
score = (w_structure * structure_match) + (w_token * token_coverage) + (w_action * action_match)
```

All weights equal in v1: `w_structure = w_token = w_action = 1/3`.

Range: `[0.0, 1.0]`. Threshold: `0.85`.

---

## Sub-scores

### 1. structure_match

Measures geometric + layout fidelity between agent draft and human reference.

**Inputs:** `rect_json` (px bounding box) from both definitions; child component list.

**Computation:**

```
# Bounding-box overlap ratio (intersection / union)
ref_area   = ref.rect.width * ref.rect.height
draft_area = draft.rect.width * draft.rect.height
intersect  = max(0, min(ref.x+ref.w, draft.x+draft.w) - max(ref.x, draft.x))
           * max(0, min(ref.y+ref.h, draft.y+draft.h) - max(ref.y, draft.y))
union      = ref_area + draft_area - intersect
bbox_score = intersect / union   # IoU

# Child count ratio
child_score = min(len(draft.children), len(ref.children)) / max(len(draft.children), len(ref.children))
              # = 1.0 if both empty

structure_match = 0.5 * bbox_score + 0.5 * child_score
```

### 2. token_coverage

Measures how many of the reference's design-system token consumers the agent draft also references.

**Inputs:** token consumer lists from both definitions (tokens from `ia/specs/ui-design-system.md §Tokens`).

**Computation:**

```
ref_tokens   = set(ref.token_consumers)
draft_tokens = set(draft.token_consumers)

token_coverage = len(ref_tokens & draft_tokens) / len(ref_tokens)
                 # = 1.0 if ref_tokens is empty
```

### 3. action_match

Measures how many of the reference's interaction states the agent draft also captures.

**Inputs:** interaction state lists (e.g. hover, click, dismiss, focus, disabled) from both definitions.

**Computation:**

```
ref_states   = set(ref.interaction_states)
draft_states = set(draft.interaction_states)

action_match = len(ref_states & draft_states) / len(ref_states)
              # = 1.0 if ref_states is empty
```

---

## Threshold + iteration policy

| Score | Decision |
|---|---|
| `>= 0.85` | Pass — agent grilling declared autonomous for this surface class. |
| `< 0.85` | Fail — emit sub-score breakdown to stdout. Iterate: corpus rows → skill body → re-run grill → re-score. Loop until pass OR escalate to human-in-loop. |

---

## Weights review marker

> **v1 weights are equal (1/3 each). Review at Region UI master-plan kickoff.** If structure-match systematically scores lower than action-match on real panels, consider bumping `w_structure`. Defer parameterization until ≥3 novel panels scored.

---

## Cross-links

- Test file (deferred): `tools/scripts/__tests__/ui-grill-convergence-rubric.test.mjs` (future Region UI master plan)
- Grill skill: `ia/skills/ui-element-grill/SKILL.md`
- Definitions doc: `docs/ui-element-definitions.md`
- Rollout plan: `docs/ui-bake-pipeline-rollout-plan.md §Track E`

---

## Changelog

| Date | Change |
|---|---|
| 2026-05-08 | Doc created. Algorithm + three sub-scores + equal weights v1 + threshold 0.85 authored (TECH-24416 Stage 6.0). |
