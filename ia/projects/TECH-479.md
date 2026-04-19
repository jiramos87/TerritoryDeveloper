---
purpose: "TECH-479 — Subagent-progress-emit skill + phases frontmatter audit + self-report emitter retirement across 15 lifecycle skills (Stage 7 T7.12)."
audience: both
loaded_by: ondemand
slices_via: none
parent_plan: "ia/projects/lifecycle-refactor-master-plan.md"
task_key: "T7.12"
---
# TECH-479 — Subagent-progress-emit skill + phases frontmatter audit + self-report emitter retirement across 15 lifecycle skills (Stage 7 T7.12)

> **Issue:** [TECH-479](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-19
> **Last updated:** 2026-04-19

## 1. Summary

Cross-cutting progress-marker skill + frontmatter audit + self-report emitter retirement. Author `ia/skills/subagent-progress-emit/SKILL.md` defining stderr emission shape `⟦PROGRESS⟧ {skill_name} {phase_index}/{phase_total} — {phase_name}` + frontmatter convention (every lifecycle SKILL.md carries top-level `phases:` YAML array). Update every lifecycle skill to add frontmatter array 1:1 with body `### Phase N —` headings. Update `.claude/agents/*.md` common preamble to `@`-load progress-emit. Add `validate:frontmatter` rule asserting drift-free `phases:` ↔ body headings across all 15 lifecycle skills. **Piggyback**: retire per-skill self-report emitter stanza (~50 lines × 15 skills = ~750 lines boilerplate) — same mechanical pass, zero extra blast radius. Drop outright (preferred — `skill-train` retrospective §Changelog aggregation supersedes) or relocate to single `@`-loaded preamble.

## 2. Goals and Non-Goals

### 2.1 Goals

1. `ia/skills/subagent-progress-emit/SKILL.md` authored w/ emission shape + frontmatter convention + canonical delimiter reserved (`⟦ ⟧`).
2. All 15 lifecycle skills (new + existing) carry top-level `phases:` frontmatter array.
3. `.claude/agents/*.md` common preamble `@`-loads progress-emit.
4. `validate:frontmatter` rule parses `phases:` + asserts matching `### Phase N —` body headings.
5. Surface map in `ia/rules/agent-lifecycle.md` lists `subagent-progress-emit` as cross-cutting row.
6. Per-skill self-report emitter stanza retired across 15 lifecycle skills. Decision during implement: drop outright (skill bodies end cleanly at `## Changelog` heading; `skill-train` aggregates friction via §Changelog scan; clean-run rule already skipped emitter in most runs) OR relocate to `ia/skills/_common/self-report-emitter.md` `@`-loaded preamble. `grep -rn "skill_self_report" ia/skills/ → 0 matches in lifecycle bodies` post-retirement. Update `ia/skills/skill-train/SKILL.md` reader only if relocation chosen.

### 2.2 Non-Goals

1. Non-lifecycle one-shot skills (glossary patchers etc.) — exempt from both audits.
2. Runtime telemetry collection (future Q9 baseline issue — Stage 9 T9.4).
3. `skill-train` retrospective algorithm change (only reader-path update if emitter relocation chosen).

## 4. Current State

### 4.2 Systems map

- All Stage 7 pair + bulk skills (TECH-468..TECH-471, TECH-478, TECH-480, TECH-481) + 5 updated legacy skills (TECH-473) = audit targets (both `phases:` add + self-report strip).
- `.claude/agents/*.md` common preamble = `@`-load anchor point.
- `validate:frontmatter` validator = schema-gate extension target.
- `ia/skills/skill-train/SKILL.md` — §Changelog reader; reader path updated only if self-report relocates (not dropped).

## 7. Implementation Plan

### Phase 1 — Author subagent-progress-emit SKILL.md

### Phase 2 — Add `phases:` frontmatter to 15 lifecycle skills

### Phase 3 — Common-preamble `@`-load + validator extension

### Phase 4 — Surface-map row

### Phase 5 — Retire self-report emitter stanza across 15 lifecycle skills

- Decide drop vs `@`-preamble relocation (lean drop — see Open Q2).
- If drop: delete emitter stanza body (`**Step 1 — Friction-condition check**` through closing fence + `---` separator) from each lifecycle SKILL.md; leave `## Changelog` heading intact.
- If relocate: author `ia/skills/_common/self-report-emitter.md` preamble; replace stanza in each skill body with one-line `@`-load; update `ia/skills/skill-train/SKILL.md` Changelog-reader path.
- Assert `grep -rn "skill_self_report" ia/skills/*/SKILL.md` returns 0 matches in lifecycle bodies (preamble file exempt if relocation path).

### Phase 6 — validate

## 8. Acceptance Criteria

- [ ] progress-emit SKILL.md present.
- [ ] 15 lifecycle skills carry matching `phases:` frontmatter.
- [ ] Common preamble `@`-loads progress-emit (one-line include).
- [ ] `validate:frontmatter` asserts drift-free.
- [ ] Surface map row present.
- [ ] Self-report emitter stanza retired across 15 lifecycle skills (drop OR `@`-preamble).
- [ ] `grep -rn "skill_self_report" ia/skills/*/SKILL.md` returns 0 matches in lifecycle bodies.
- [ ] `skill-train` Changelog-reader path updated if relocation chosen.
- [ ] `npm run validate:all` exit 0.

## Open Questions

1. None on progress-emit scope — tooling only. Unicode bracket delimiter `⟦ ⟧` reserved across active skill body prose (forbid occurrence outside marker emission).
2. Self-report stanza: drop outright vs `@`-preamble relocation? Lean drop — `skill-train` already scans §Changelog; clean-run rule skips stanza in practice; drop removes ~750 lines with zero functional loss. Preamble only needed if future per-skill emission plan emerges. Decide at implement start.
