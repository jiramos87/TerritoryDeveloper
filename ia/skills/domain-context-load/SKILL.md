---
purpose: "Run the shared MCP context recipe (glossary_discover → glossary_lookup → router_for_task → spec_sections → invariants_summary) and return a structured payload including a Tier 2 per-Stage ephemeral cache block. Single source for the recipe shared by 8+ lifecycle skills."
audience: agent
loaded_by: skill:domain-context-load
slices_via: glossary_discover, glossary_lookup, router_for_task, spec_sections, invariants_summary
name: domain-context-load
description: >
  Sonnet subskill. Executes the canonical MCP context recipe
  (glossary_discover → glossary_lookup → router_for_task → spec_sections → invariants_summary)
  and returns a structured payload `{glossary_anchors, router_domains, spec_sections, invariants, cache_block}`.
  Phase N (final concat + cache emit) assembles all MCP output into a single Tier 2 ephemeral
  cache block with `cache_control: {"type":"ephemeral","ttl":"1h"}`. Called once per Stage;
  all Tasks reuse `cache_block` without re-fetching.
  Flags: `brownfield_flag` (skip router + spec_sections + invariants on greenfield);
  `tooling_only_flag` (skip invariants regardless of brownfield). Replaces ~8 copies of the same
  recipe across lifecycle skills; single place to tune keyword strategy or add new MCP steps.
  Triggers: "domain context load", "run MCP context recipe", "glossary router invariants load",
  "domain-context-load subskill", "shared MCP recipe".
model: inherit
---

# Domain context load — shared MCP recipe subskill

Caveman default — [`agent-output-caveman.md`](../../rules/agent-output-caveman.md).

**Purpose:** single canonical execution of the `glossary_discover → glossary_lookup →
router_for_task → spec_sections → invariants_summary` recipe. Replaces inline Tool recipe
blocks in 8+ lifecycle skills. Callers pass domain keywords + flags; subskill returns a
structured context payload ready for use in stage authoring, spec authoring, and alignment gates.

**Tier 2 per-Stage ephemeral bundle:** Phase N (final concat + cache emit) assembles all MCP
output — `glossary_anchors` + `spec_sections` + `invariants` — into a single content block
with `cache_control: {"type":"ephemeral","ttl":"1h"}`. Callers invoke this subskill exactly
once per Stage at Stage-start; all Tasks within the Stage reuse `cache_block` without
re-fetching. See `ia/rules/plan-apply-pair-contract.md` §Tier 2 bundle reuse.

---

## Inputs

| Field | Type | Required | Notes |
|-------|------|----------|-------|
| `keywords` | `string[]` | Yes | English domain tokens. Translate before passing (glossary is English). |
| `brownfield_flag` | bool | No | Default `false`. When `true`, skip `router_for_task` + `spec_sections` + `invariants_summary`. Use for new subsystems with no existing code paths. |
| `tooling_only_flag` | bool | No | Default `false`. When `true`, skip `invariants_summary` regardless of `brownfield_flag`. Use for doc/IA/tooling-only work that touches no runtime C#. |
| `context_label` | string | No | Optional label for the enclosing call site (e.g. `"stage-file Stage 2.1"`). Used in output header only. |

---

## Output

```json
{
  "context_label": "...",
  "glossary_anchors": [
    {"term": "...", "definition": "...", "spec": "..."}
  ],
  "router_domains": ["...", "..."],
  "spec_sections": [
    {"spec": "...", "section": "...", "excerpt": "..."}
  ],
  "invariants": [
    {"number": 3, "text": "..."}
  ],
  "flags": {"brownfield": false, "tooling_only": false},
  "skipped": ["router_for_task", "spec_sections", "invariants_summary"],
  "cache_block": {
    "content": "...",
    "cache_control": {"type": "ephemeral", "ttl": "1h"},
    "token_estimate": 0
  }
}
```

`skipped` lists MCP steps omitted due to flags.
`cache_block.content` = concatenated `glossary_anchors` + `spec_sections` + `invariants` in serialized form.
`cache_block.token_estimate` = rough token count of `cache_block.content` (chars / 4).
`cache_block.cache_control` = `{"type":"ephemeral","ttl":"1h"}` — Tier 2 per-Stage bundle.

---

## Recipe execution (in order)

1. **`glossary_discover`** — pass `keywords` JSON array. Capture high-confidence hits for step 2.
2. **`glossary_lookup`** — for each high-confidence term from step 1. Collect `{term, definition, spec}` into `glossary_anchors`. Hold canonical names — replace ad-hoc synonyms in caller's authoring prose.
3. **`router_for_task`** — skip if `brownfield_flag = true`. Pass 1–3 domain terms from keywords + router table vocabulary. Capture matched domains into `router_domains`.
4. **`spec_sections`** — skip if `brownfield_flag = true`. For each domain in `router_domains`, fetch implied spec sections (`max_chars` small). No full spec reads. Collect into `spec_sections`.
5. **`invariants_summary`** — skip if `brownfield_flag = true` OR `tooling_only_flag = true`. Capture numbered invariants at risk into `invariants`.

Return structured payload to caller.

---

## Phase N — Final concat + cache emit

Runs after steps 1–5. Assembles Tier 2 per-Stage ephemeral bundle.

1. **Concatenate** `glossary_anchors` + `spec_sections` + `invariants` into a single serialized string (JSON or compact prose). Omit skipped fields from concat.
2. **Estimate tokens** — `token_estimate = Math.ceil(content.length / 4)` (rough chars-to-tokens ratio).
3. **Token-floor assert** — compare `token_estimate` against F2 floor (defined in T10.2/TECH-503):
   - Below floor → log warning: `[domain-context-load] cache_block token_estimate {N} below F2 floor {F2}; proceeding (CI gate is authority).`
   - Do NOT hard-stop. Continue to emit. T10.2 CI gate is the hard-fail authority; Phase N assert is advisory safety net only.
4. **Emit `cache_block`** into returned payload:
   ```json
   {
     "content": "<concatenated context string>",
     "cache_control": {"type": "ephemeral", "ttl": "1h"},
     "token_estimate": <N>
   }
   ```
5. Return full payload including `cache_block` to caller.

**Caller contract:** caller invokes this subskill exactly **once per Stage** at Stage-start. Store `cache_block` in shared context block. All Tasks within the Stage consume `cache_block` without re-running this subskill. See `ia/rules/plan-apply-pair-contract.md` §Tier 2 bundle reuse.

---

## Usage in caller skills

Replace any inline "Tool recipe" block with:

> Run `domain-context-load` subskill ([`ia/skills/domain-context-load/SKILL.md`](../domain-context-load/SKILL.md)).
> Inputs: `keywords: [...]`, `brownfield_flag: {true|false}`, `tooling_only_flag: {true|false}`.
> Use returned `glossary_anchors` for prose canonical names; `router_domains` + `spec_sections`
> for Relevant surfaces; `invariants` for guardrail flags.
> Store returned `cache_block` in shared context block for Stage-wide reuse (Tier 2 bundle).
> **Call exactly once per Stage at Stage-start** — do NOT re-run per Task.

---

## Guardrails

- Always pass `keywords` in English — translate from conversation if user used another language.
- `brownfield_flag = true` → only `glossary_discover` + `glossary_lookup` run. Do NOT run router/specs/invariants.
- `tooling_only_flag = true` → skip `invariants_summary` even when `brownfield_flag = false`.
- Do NOT read whole `ia/specs/*.md` files — `spec_sections` slices only.
- Do NOT skip `glossary_discover` + `glossary_lookup` regardless of flags — canonical names always needed.

---

## Callers

`release-rollout` Phase 2 · `release-rollout-enumerate` Phase 1 MCP context ·
`master-plan-new` Phase 2 Tool recipe · `master-plan-extend` Phase 2 Tool recipe ·
`stage-decompose` Phase 1 Tool recipe · `stage-file` Tool recipe ·
`project-new` Tool recipe (steps 1–5) · `design-explore` Phase 5 Tool recipe.
