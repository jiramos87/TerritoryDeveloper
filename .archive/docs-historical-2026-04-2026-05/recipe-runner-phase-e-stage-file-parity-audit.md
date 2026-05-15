# stage-file Recipe-Parity Audit — recipe-runner-phase-e Stage 2.1

**Task:** TECH-6965
**Audit date:** 2026-04-30
**Skill:** `ia/skills/stage-file/SKILL.md` (512 lines)
**Recipe:** `tools/recipes/stage-file.yaml` (12 top-level steps)
**Agent body:** `.claude/agents/stage-file.md` (74 lines)

---

## §Path correction

Stage block objective + TECH-6966/6967 task stubs reference `ia/recipes/stage-file.yaml` — **this path does not exist**.
Canonical location: `tools/recipes/stage-file.yaml` (confirmed by `ls`).
Stage 1.3 drift-gate baseline (`ia/state/skill-recipe-baselines/stage-file.json`) also keys off `tools/recipes/`.
No file migration needed — correct the stale references in stage-block prose only.

---

## §Step parity

Maps each recipe step id → SKILL.md phase id. All 12 steps covered.

| recipe_step_id | skill_md_phase_id | notes |
|---|---|---|
| `mode_detect` | Phase 0 — Mode detection | bash script handles 4 modes |
| `lifecycle_ctx` | Phase 1 — Load shared Stage MCP bundle | `lifecycle_stage_context` MCP |
| `stage` | Phase 2.1 — Read Stage block via DB | `stage_render` MCP |
| `plan` | Phase 2.2 — Read plan title via DB | `master_plan_render` MCP |
| `cardinality` | Phase 2.3 — Cardinality gate | bash script |
| `sizing` | Phase 2.4 — Sizing gate | bash script; H1–H6 rules |
| `pending_q` | Phase 5 (pre-filter) — SQL filter pending tasks | drives `file_pass` foreach |
| `manifest_resolve` | Phase 4 — Resolve target manifest section | bash; `target_section` override |
| `file_pass` (foreach) | Phase 5.A + 5.D — Per-task insert + manifest append | `ins` + `append` inner steps |
| `materialize` | Phase 6.1 — Materialize BACKLOG.md | gated on `file_pass.count` |
| `stage_changelog` | Phase 6.4 — R2 Stage Status flip change-log | `master_plan_change_log_append` MCP |
| `progress` | Phase 6.6 — Regenerate dashboard | non-blocking `gate` kind |

**SKILL.md phases NOT directly mapped to a recipe step (handled by subagent or DB):**

| skill_md_phase_id | handled_by |
|---|---|
| Phase 2.2b — Surface-path verify | **orphan** — encode-in-recipe candidate (see §Orphan prose) |
| Phase 3 — Batch Depends-on verification | subagent post-recipe (agent body §Recipe step 4) |
| Phase 5.B — Dep registration | subagent post-recipe (agent body §Recipe step 5) |
| Phase 5.C — raw_markdown patch | subagent post-recipe (agent body §Recipe step 6) |
| Phase 6.3 — Task-table flip | DB auto-handles; no step needed |
| Phase 6.5 — R1 preamble flip | subagent post-recipe (agent body §Recipe step 7) |
| Return to dispatcher | agent body §Output |

---

## §Orphan prose

SKILL.md constraints absent from recipe yaml. 13 orphans total: 1 `encode-in-recipe`, 12 `trim-from-skill`.

| # | constraint | skill_md_line_range | recipe_step_id? | disposition | rationale |
|---|---|---|---|---|---|
| O1 | Phase 0 upstream Stage tail guard (`npm run validate:master-plan-status`) | ~108–110 | — | `trim-from-skill` | advisory heuristic; mode_detect is the active gate; upstream validation is pre-call responsibility |
| O2 | Phase 0 collapsed-flow note (stage_decompose_apply No-op path) | ~110–111 | — | `trim-from-skill` | impl detail; No-op case handled by mode_detect; recipe user doesn't need context |
| O3 | Phase 1 domain-context-load fallback (when lifecycle_stage_context MCP unavailable) | ~127–134 | — | `trim-from-skill` | recipe-dispatch escalates on MCP failure (non-zero exit); no fallback subskill call |
| O4 | Phase 2.2b surface-path verify (warn-only path check after plan render) | ~162–175 | `surface_path_verify` (new) | `encode-in-recipe` | invariant check with real audit value (caught parallel-carcass ghost path); `gate.{validator}` kind; warn-only → non-blocking |
| O5 | Phase 3 batch Depends-on verification algorithm | ~208–218 | — | `trim-from-skill` | agent body §Recipe step 4 covers; SKILL.md prose duplicates subagent logic |
| O6 | Phase 5.A.1 task_insert arg composition YAML table | ~264–283 | — | `trim-from-skill` | recipe `file_pass.ins` encodes exact args; prose table is drift-prone duplicate |
| O7 | Phase 5.A.3 spec stub body compose from project-spec-template.md | ~299–301 | — | `trim-from-skill` | recipe writes `body: ""`; stage-authoring owns stub; compose responsibility removed from stage-file |
| O8 | Phase 5.B dep registration algorithm (cross-iter map + Tarjan) | ~303–310 | — | `trim-from-skill` | agent body §Recipe step 5 covers; detail belongs with subagent |
| O9 | Phase 5.C raw_markdown patch algorithm + format spec | ~312–323 | — | `trim-from-skill` | agent body §Recipe step 6 covers; format spec belongs in task_raw_markdown_write MCP docs |
| O10 | Phase 6.3 task-table flip — "auto-handled by DB" note | ~360–364 | — | `trim-from-skill` | no-op in recipe path; note adds no value |
| O11 | Phase 6.5 R1 master-plan preamble flip via master_plan_preamble_write | ~365–394 | — | `trim-from-skill` | agent body §Recipe step 7 handles post-recipe; not a recipe step (subagent responsibility) |
| O12 | Idempotency section (per-operation guarantees) | ~451–462 | — | `trim-from-skill` | MCP/DB layer guarantees; redundant with MCP docs; not a recipe-runner constraint |
| O13 | Escalation rules table (halt trigger → shape mapping) | ~430–447 | — | `trim-from-skill` | agent body §Escalation shape covers; escalation taxonomy belongs in agent body |

---

## §TECH-6966 handoff

**Baseline line count:** SKILL.md = 512 lines, `.claude/agents/stage-file.md` = 74 lines.
**Target post-trim:** `.claude/agents/stage-file.md` ≤ 50 lines (≥32% drop from 74; `skill:sync:all` regenerates).

**SKILL.md body shape after trim (3 keep sections):**

1. **Preamble cache pointer** — `@ia/skills/_preamble/stable-block.md` (must remain; Tier 1 cache block)
2. **Recipe dispatch invocation** — `npm run recipe:run -- stage-file --inputs <path>` + halt-handler prose (must remain; recipe-shell contract)
3. **Escape-hatch note** — reference to legacy prose-path invocation (`/stage-file --legacy-prose-path`) for fallback if recipe-engine unavailable (must remain; Locked-decision §6)

**Trim targets (13 trim-from-skill orphans → remove from SKILL.md body):**
- O1: lines ~108–110 (upstream tail guard)
- O2: lines ~110–111 (collapsed-flow note)
- O3: lines ~127–134 (domain-context-load fallback)
- O5: lines ~208–218 (Phase 3 batch deps algorithm)
- O6: lines ~264–283 (Phase 5.A.1 arg composition table)
- O7: lines ~299–301 (Phase 5.A.3 spec stub compose)
- O8: lines ~303–310 (Phase 5.B dep registration)
- O9: lines ~312–323 (Phase 5.C raw_markdown patch)
- O10: lines ~360–364 (Phase 6.3 task-table flip note)
- O11: lines ~365–394 (Phase 6.5 R1 preamble flip)
- O12: lines ~451–462 (idempotency section)
- O13: lines ~430–447 (escalation rules table)

**Encode target (1 encode-in-recipe orphan → add step to recipe):**
- O4: add `surface_path_verify` gate step after `plan` step in `tools/recipes/stage-file.yaml`; `gate: surface-path-verify` kind; warn-only (non-blocking); input `paths[]` derived from `stage.relevant_surfaces`.

**Expected post-trim agent body lines (≤50):**
- Frontmatter: ~7 lines
- Preamble anchor + @boot: ~4 lines
- §Mission heading + 1-liner: ~2 lines
- §Recipe dispatch block (steps 1–8): ~25 lines
- §Hard boundaries list: ~10 lines (keep recipe bypass + yaml/DB/commit constraints)
- §Escalation shape: ~3 lines
- §Output shape: ~3 lines
- **Estimated total: ~54 lines** — prune §Hard boundaries to 6 key items → ~50 lines ✓

**Drift-gate baseline regen:** after TECH-6966 trim + recipe step add + `skill:sync:all` → run:
```bash
npm run snapshot-baseline -- stage-file
```
Commit new `ia/state/skill-recipe-baselines/stage-file.json` (hash update only; schema unchanged).
