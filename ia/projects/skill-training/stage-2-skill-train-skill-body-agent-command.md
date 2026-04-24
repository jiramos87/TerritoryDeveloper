### Stage 2 — skill-train Core + Glossary Foundation / skill-train Skill Body + Agent + Command

**Status:** Final

**Backlog state (2026-04-18):** 4 tasks filed (TECH-392, TECH-393, TECH-394, TECH-395 archived).

**Objectives:** Author `ia/skills/skill-train/SKILL.md` with full Phase 0–5 sequence, canonical §Schema block, §Emitter stanza template (single source of truth for Step 2), and guardrails. Create matching `.claude/agents/skill-train.md` Opus subagent and `.claude/commands/skill-train.md` dispatcher.

**Exit:**

- `ia/skills/skill-train/SKILL.md`: Phase 0–5 sequence; `skill_self_report` JSON schema block with `schema_version`; `§Emitter stanza template` section (verbatim copy-paste block for 13 skills); Guardrails include "do NOT apply", "do NOT touch other skills", "do NOT commit"; Seed prompt block.
- `.claude/agents/skill-train.md` (Opus): accepts SKILL_NAME (required), `--since {YYYY-MM-DD}`, `--threshold N`, `--all` (with explicit Opus-cost warning); caveman preamble; mirrors `release-rollout-skill-bug-log.md` header shape.
- `.claude/commands/skill-train.md`: thin dispatcher; forwards SKILL_NAME + all optional flags; caveman preamble.
- `npm run validate:all` exits 0.
- Phase 1 — SKILL.md body (Phase 0–5 + §Schema block + §Emitter stanza template).
- Phase 2 — Agent + command dispatcher.

**Tasks:**

| Task | Name | Issue | Status | Intent |
| --- | --- | --- | --- | --- |
| T2.1 | skill-train SKILL.md body | **TECH-392** | Done (archived) | Create `ia/skills/skill-train/SKILL.md`. Phase 0: validate target SKILL.md exists + §Changelog present (inject if absent). Phase 1: read Changelog entries since last `source: train-proposed` entry (or `--since` date). Phase 2: aggregate `friction_types` — recurring = ≥2 occurrences (`--threshold N` overrides). Phase 3: synthesize unified diff targeting Phase sequence / Guardrails / Seed prompt sections of target skill. Phase 4: write `ia/skills/{SKILL_NAME}/train-proposal-{YYYY-MM-DD}.md`; append Changelog pointer entry `source: train-proposed`. Phase 5: handoff — path + friction-count + "review + apply manually". §Schema block defines `skill_self_report` JSON: `{skill, run_date, schema_version, friction_types[], guardrail_hits[], phase_deviations[], missing_inputs[], severity}`. Guardrails: do NOT apply patch; do NOT touch other skills' SKILL.md; do NOT commit. |
| T2.2 | Emitter stanza template section | **TECH-393** | Done (archived) | Add `## Emitter stanza template` section to `skill-train/SKILL.md` — canonical Phase-N-tail block for lifecycle skills to copy verbatim: (1) friction-condition check (`guardrail_hits > 0 OR phase_deviations > 0 OR missing_inputs > 0`); (2) construct `skill_self_report` JSON block; (3) append §Changelog entry `source: self-report` with schema_version date-stamp. Clean run (all conditions false) → no-op, §Changelog untouched. This section is the single source of truth consumed in T2.1.1, T2.1.2, T2.2.1, T2.2.2. |
| T2.3 | skill-train agent | **TECH-394** | Done (archived) | Create `.claude/agents/skill-train.md` (Opus subagent). Mirror `.claude/agents/release-rollout-skill-bug-log.md` header shape: title, model, caveman preamble directive. Inputs: SKILL_NAME (required); `--since {YYYY-MM-DD}` optional; `--threshold N` optional (default 2); `--all` flag carries explicit token-cost warning. Body delegates to `ia/skills/skill-train/SKILL.md` Phase 0–5. No auto-apply; no self-commit. |
| T2.4 | skill-train command | **TECH-395** | Done (archived) | Create `.claude/commands/skill-train.md` — thin dispatcher. Caveman preamble. Forwards `{SKILL_NAME}` (required), `--since`, `--all`, `--threshold` args to `skill-train` subagent via Agent tool call. One-paragraph body. |

---
