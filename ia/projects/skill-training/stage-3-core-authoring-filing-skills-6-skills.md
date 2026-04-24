### Stage 3 — Phase-N-tail Wiring (13 Lifecycle Skills) / Core Authoring + Filing Skills (6 skills)

**Status:** In Progress — TECH-433 (4 of 4 filed: TECH-430, TECH-431, TECH-432, TECH-433)

**Objectives:** Wire the 6 authoring-and-filing lifecycle skills (`design-explore`, `master-plan-new`, `master-plan-extend`, `stage-decompose`, `stage-file`, `project-new`) with Phase-N-tail stanzas.

**Exit:**

- All 6 SKILL.md files carry Phase-N-tail stanza (verbatim template, `schema_version` stamped) + `## Changelog` section.
- Stanza placed at final handoff phase in each skill's existing Phase sequence.
- `npm run validate:all` exits 0.
- Phase 1 — design-explore, master-plan-new, master-plan-extend + stage-decompose, stage-file, project-new wiring.
- Phase 2 — Cross-read consistency check + validate:all.

**Tasks:**

| Task | Name | Issue | Status | Intent |
| --- | --- | --- | --- | --- |
| T3.1 | Wire authoring-trio Phase-N-tail | **TECH-430** | Done (archived) | Edit `ia/skills/design-explore/SKILL.md`, `master-plan-new/SKILL.md`, `master-plan-extend/SKILL.md`: append Phase-N-tail stanza verbatim from `skill-train/SKILL.md §Emitter stanza template`; inject `## Changelog` section if absent; place stanza at existing handoff Phase N position. Verify `schema_version` date-stamp on all 3. |
| T3.2 | Wire filing-trio Phase-N-tail | **TECH-431** | Done (archived) | Edit `ia/skills/stage-decompose/SKILL.md`, `stage-file/SKILL.md`, `project-new/SKILL.md`: same procedure as T2.1.1. Stanza at final handoff phase; §Changelog injected if absent; schema_version present on all 3. |
| T3.3 | Cross-read stanza consistency | **TECH-432** | Done (archived) | Cross-read all 6 wired SKILL.md files; verify stanza text matches canonical template character-for-character (no paraphrase); `schema_version` stamps identical across all 6; `## Changelog` sections present. Document any deviation found in the relevant skill's §Changelog as `source: wiring-review`. |
| T3.4 | validate:all post Stage 2.1 | **TECH-433** | In Progress | Run `npm run validate:all` from repo root; confirm exit 0. Surface any frontmatter/index failures introduced by skill edits; fix inline before closing stage. |

---
