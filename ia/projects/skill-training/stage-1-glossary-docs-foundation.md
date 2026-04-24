### Stage 1 — skill-train Core + Glossary Foundation / Glossary + Docs Foundation

**Status:** Final (4 of 4 done: TECH-367, TECH-368, TECH-369, TECH-370)

**Objectives:** Land 4 canonical glossary terms and the docs surface-map update before any cross-ref is authored in Stage 1.2 or Step 2. Satisfies invariant #12.

**Exit:**

- `ia/specs/glossary.md`: 4 rows added — `skill self-report`, `skill training`, `patch proposal (skill)`, `skill-train`. MCP `glossary_discover "skill self-report"` returns a match.
- `docs/agent-lifecycle.md §Surface map`: `/skill-train` row present (Retrospective, Opus, outside main lifecycle flow).
- `CLAUDE.md §3` + `AGENTS.md`: one-paragraph pointer added to each.
- `npm run validate:all` exits 0.
- Phase 1 — Glossary rows + agent-lifecycle.md surface map row.
- Phase 2 — CLAUDE.md §3 + AGENTS.md one-paragraph pointers.

**Tasks:**

| Task | Name | Issue | Status | Intent |
| --- | --- | --- | --- | --- |
| T1.1 | Glossary rows × 4 | **TECH-367** | Done (archived) | Add 4 rows to `ia/specs/glossary.md` (Documentation category): `skill self-report` — structured JSON emitted by lifecycle skill at handoff when friction detected; `skill training` — retrospective Changelog-driven proposal loop; `patch proposal (skill)` — unified-diff proposal against SKILL.md Phase sequence / Guardrails / Seed prompt, stored as `ia/skills/{name}/train-proposal-{YYYY-MM-DD}.md`; `skill-train` — Opus consumer subagent + slash command for on-demand skill retrospective. Cross-ref between rows where applicable. |
| T1.2 | agent-lifecycle.md surface row | **TECH-368** | Done (archived) | Add `/skill-train` row to `docs/agent-lifecycle.md §Surface map` table — Stage: Retrospective; Slash command: `/skill-train`; Subagent: `skill-train`; Skill: `skill-train`; Model: Opus. Add inline note "retrospective only — outside main lifecycle flow". |
| T1.3 | CLAUDE.md §3 pointer | **TECH-369** | Done (archived) | Add row to `CLAUDE.md §3` key files table: `ia/skills/skill-train/SKILL.md` — on-demand skill retrospective; reads Per-skill Changelog; proposes unified-diff patch against Phase sequence / Guardrails / Seed prompt sections. Caveman prose. |
| T1.4 | AGENTS.md pointer | **TECH-370** | Done (archived) | Add one-paragraph entry to `AGENTS.md` under the skill-lifecycle / retrospective section (create section if absent): explains `skill-train` role — reads accumulated Per-skill Changelog entries, aggregates recurring friction (≥2 occurrences threshold), writes `train-proposal-{DATE}.md` sibling file. Caveman prose. |

---
