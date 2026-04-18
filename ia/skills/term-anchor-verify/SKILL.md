---
purpose: "Per English term: call glossary_lookup + router_for_task + spec_section; return {anchored, missing} for each. Codifies the Alignment gate (column g) anchor check."
audience: agent
loaded_by: skill:term-anchor-verify
slices_via: glossary_lookup, router_for_task, spec_section
name: term-anchor-verify
description: >
  Sonnet subskill. Given a list of English domain terms, calls `glossary_lookup` +
  `router_for_task` + `spec_section` for each; returns per-term `{anchored: bool, missing:
  [glossary|router|spec]}`. Codifies the Alignment gate (column g) check from
  `release-rollout` Phase 3 + `release-rollout-track` Phase 1. Centralizes the pass/fail
  classifier so all callers agree on the same anchor contract. Triggers: "term anchor verify",
  "alignment gate check", "verify glossary anchor", "term-anchor-verify subskill",
  "column g anchor check".
---

# Term anchor verify — Alignment gate subskill

Caveman default — [`agent-output-caveman.md`](../../rules/agent-output-caveman.md).

**Purpose:** per-term anchor verification for the **Alignment gate** (rollout tracker column g).
Every NEW domain entity introduced by a master-plan row must pass three anchors before column (e)
can flip: glossary row present, router domain match, spec section anchor. This subskill
centralizes that pass/fail check.

---

## Inputs

| Field | Type | Notes |
|-------|------|-------|
| `terms` | `string[]` | English domain terms to verify. Translate before passing. |
| `context_label` | string | Optional — e.g. `"city-sim-depth row (e) gate"`. For report labelling. |

---

## Output

```json
{
  "context_label": "...",
  "results": [
    {
      "term": "Rollout lifecycle",
      "anchored": true,
      "missing": []
    },
    {
      "term": "CityBudgetAllocation",
      "anchored": false,
      "missing": ["glossary", "spec"]
    }
  ],
  "all_anchored": false,
  "unresolved_terms": ["CityBudgetAllocation"]
}
```

`all_anchored = true` → column (g) `✓`. `all_anchored = false` → column (g) stays `—`; route to glossary + spec authoring before re-fire.

---

## Check contract (per term)

For each term in `terms`:

1. **`glossary_lookup`** — must return a canonical row with a non-empty `definition`. Absent or no match → `missing` += `"glossary"`.
2. **`router_for_task`** — pass `domain: {term}` (or best-fit English token). Must return ≥1 matched domain (not `no_matching_domain`). No match → `missing` += `"router"`.
3. **`spec_section`** — pass the domain + section returned by router. Must return non-empty anchor prose that names or defines the term. Empty or error → `missing` += `"spec"`.

`missing` empty → `anchored: true`. Any entry in `missing` → `anchored: false`.

Set `all_anchored = (all terms have anchored: true)`.

---

## Usage in caller skills

Replace inline "Per entity: glossary_lookup + router_for_task + spec_section" blocks with:

> Run `term-anchor-verify` subskill ([`ia/skills/term-anchor-verify/SKILL.md`](../term-anchor-verify/SKILL.md)).
> Inputs: `terms: [...]` (English, one per new domain entity).
> `all_anchored = true` → gate passes. `false` → route to glossary + spec authoring
> for each term in `unresolved_terms`. Do NOT advance the gated column until re-fire passes.

---

## Guardrails

- Always pass `terms` in English — translate from conversation before calling.
- Do NOT flip gated column on partial pass — all terms must anchor.
- Do NOT create glossary rows or spec sections here — detection only; authoring is caller's job.
- If `router_for_task` returns `no_matching_domain` for a term → treat as `missing: ["router"]`; it is NOT an error of this subskill; caller must author the domain + router entry.

---

## Callers

`release-rollout` Phase 3 (Alignment gate) · `release-rollout-track` Phase 1 (column g/e align verify) · `master-plan-new` / `master-plan-extend` entity anchor validation · `project-new` router match verify.
