---
purpose: "Project spec for Project spec structure — ia/projects/."
audience: both
loaded_by: ondemand
slices_via: none
---
# Project spec structure — `ia/projects/`

Temporary specs for active **BACKLOG** item live here. **New specs use descriptive naming convention `{ISSUE_ID}-{description}.md`** (e.g. `BUG-37-zone-cleanup.md`, `FEAT-44-water-junction.md`, `TECH-11-example-migration.md`). Legacy bare `{ISSUE_ID}.md` form (e.g. `BUG-37.md`) still accepted for back-compat with older specs but must not be used for new files. Descriptive `{description}` suffix carries valuable context for humans + grep alike. Specs deleted after verified completion; lessons migrate to canonical docs.

> **Naming convention:** `{ISSUE_ID}-{description}.md` = canonical form for all new project specs. `{ISSUE_ID}` prefix = one of `BUG-`, `FEAT-`, `TECH-`, `ART-`, `AUDIO-` followed by issue number + optional letter suffix. `{description}` = filename-safe (letters, digits, dots, underscores, hyphens), kebab-case, short enough to scan. Both `project_spec_journal_persist` + `project_spec_closeout_digest` accept either form.

## Which template to use

| Document | Purpose |
|----------|---------|
| [`ia/templates/project-spec-template.md`](../templates/project-spec-template.md) | Copy-paste skeleton when creating new `{ISSUE_ID}-{description}.md`. |
| This file | Section order, naming rules, how to split **requirements** vs **implementation**. |

## Umbrella program specs (multi-issue)

Some **BACKLOG** programs use **parent** project spec plus **child** specs (e.g. **TECH-60** **§ Completed** with **TECH-61**–**TECH-63** — [`BACKLOG-ARCHIVE.md`](../../BACKLOG-ARCHIVE.md) **§ Completed**). Each child links to parent in its header (**Parent program**). Unless charter says otherwise, each file follows **section order** below. **Acceptance** for closing **umbrella** **project spec** depends on **BACKLOG** row: some programs require every child **Completed** first; others retire **umbrella** from open **BACKLOG** while **follow-ups** remain (see [`BACKLOG-ARCHIVE.md`](../../BACKLOG-ARCHIVE.md) vs [`BACKLOG.md`](../../BACKLOG.md) open program sections).

## Required front matter

```markdown
# {ISSUE_ID} — {Title}

> **Issue:** [{ISSUE_ID}](../../BACKLOG.md)
> **Status:** Draft | In Review | Final
> **Created:** YYYY-MM-DD
> **Last updated:** YYYY-MM-DD
```

## Section order (recommended)

1. **Summary** — Problem / outcome in domain language (see glossary).
2. **Goals and Non-Goals** — Measurable product outcomes; explicit out-of-scope.
3. **User / Developer Stories** — Optional table; roles may include Player, QA, Designer.
4. **Current State** — Split when helpful:
   - **Domain:** observed vs expected behavior using canonical terms.
   - **Systems map:** short pointer to backlog "Files" / subsystems (no deep API dump required).
   - **Implementation investigation notes** (optional): technical hypotheses for agent only — must not be mistaken for product requirements.
5. **Proposed Design** — **Target behavior (product)** first; **architecture / code** marked as agent-owned unless user fixed design.
6. **Decision Log** — Dated choices; alternatives considered.
7. **Implementation Plan** — Phased checklists (agent executes unless step would change game logic).
7b. **Test Contracts** (optional; **tooling / verification**) — **Template:** [`ia/templates/project-spec-template.md`](../templates/project-spec-template.md) includes **`## 7b. Test Contracts`** with columns **Acceptance / goal** \| **Check type** \| **Command or artifact** \| **Notes** (e.g. **Node**, **golden** **JSON**, **manual**, future **Unity** **UTF**, advisory **`verify`**). Use **glossary** terms for *what* is verified. **Not** substitute for **`## Open Questions`**, which remain **game logic** only (see below). **`project_spec_closeout_digest`** does **not** extract **§7b** today — adding **`test_contracts`** field requires extending `tools/mcp-ia-server/src/parser/project-spec-closeout-parse.ts` in **separate** **BACKLOG** / **TECH-** follow-up (out of scope for **TECH-63** template + **Skills** ship).
8. **Acceptance Criteria** — Testable conditions mapped to goals / stories.
9. **Issues Found During Development** — Table during implementation.
10. **Lessons Learned** — Fill at closure; migrate to specs / `AGENTS.md` / glossary as needed.
11. **`## Open Questions (resolve before / during implementation)`** — See below (mandatory section for collaborative specs).

## Open Questions: game logic only

- Questions under **`## Open Questions`** MUST use **canonical vocabulary** from [`ia/specs/glossary.md`](../specs/glossary.md) + linked specs.
- MUST clarify **definitions + intended game behavior** (simulation / player rules), not APIs, class names, or algorithms.
- **Implementing agent** resolves technical approach + code paths **unless** chosen approach would **change** game behavior defined in spec; then update **Decision Log** or ask product owner.

## Terminology

- Prefer glossary table names (**road stroke**, **terraform plan**, **Moore neighborhood**, **street**, **interstate**, **RCI**, **zone**, **building**, **wet run**, etc.).
- Glossary + spec disagree → **spec wins** (per glossary header).
- BACKLOG **Files** / **Notes** must use same words for searchability.

## Lifecycle

1. Create `{ISSUE_ID}-{description}.md` from template (or legacy bare `{ISSUE_ID}.md` form for older specs) → refine Open Questions + acceptance → **Final** when stable.
2. Implement → keep **Issues Found** up to date.
3. On user-confirmed completion: migrate durable content to canonical docs; archive issue; **delete** project spec.

### Closeout checklist (before deleting `{ISSUE_ID}.md`)

After owner **confirms** verification (**AGENTS.md**):

1. **Migrate** normative content: **Lessons Learned**, **Decision Log** items, + any rules belonging in [reference specs](../specs/) (see [glossary](../specs/glossary.md)), [`ia/rules/`](../rules/), `docs/`, `ARCHITECTURE.md` — not in deleted file. **Optional:** **`DATABASE_URL`** set → also persist **verbose** **Decision Log** + **Lessons learned** bodies to **Postgres** **`ia_project_spec_journal`** per **`/ship-stage`** Pass B inline closeout via **`stage_closeout_apply`** MCP ([`ia/skills/ship-stage/SKILL.md`](../skills/ship-stage/SKILL.md), [`docs/postgres-ia-dev-setup.md`](../../docs/postgres-ia-dev-setup.md) §IA project spec journal).
2. **Cascade links:** Search durable docs (`ia/skills/`, `ia/rules/`, `docs/`, `projects/`, `AGENTS.md`, `ARCHITECTURE.md`, etc.) for markdown links, backticks, or plain paths to `ia/projects/{ISSUE_ID}.md` + replace with **`BACKLOG.md`** / **`BACKLOG-ARCHIVE.md`** references (+ section anchor if useful). Do not leave pointers to removed path.
3. **Umbrella / sibling specs:** Update remaining `ia/projects/*.md` depending on issue (**Depends on**, **Implementation Plan**, **Acceptance**) so they do not describe closed work as pending.
4. **BACKLOG + archive:** **Remove** row from **`BACKLOG.md`**. **Append** **`[x]`** to **`BACKLOG-ARCHIVE.md`** with **`Spec:`** → **removed-after-closure** pattern. **Strip** closed issue id from **glossary**, **reference specs**, **rules**, **skills**, `docs/`, `projects/`, code comments ([**terminology-consistency**](../rules/terminology-consistency.md), **`/ship-stage`** Pass B inline closeout via **`stage_closeout_apply`** MCP).
5. **Verify:** Run `npm run validate:dead-project-specs` from repo root (or `node tools/validate-dead-project-spec-paths.mjs`) so CI + agents catch any missed stale path. **`/ship-stage`** Pass B inline closeout via **`stage_closeout_apply`** MCP ([`ia/skills/ship-stage/SKILL.md`](../skills/ship-stage/SKILL.md)) orchestrates full sequence in single MCP call. **Closeout helpers:** territory-ia **`stage_closeout_apply`**, **`stage_closeout_digest`**, **`spec_sections`** — see [`docs/mcp-ia-server.md`](../../docs/mcp-ia-server.md) **Project spec workflows**.

### Lessons learned (dead project-spec path scanner, 2026-04-03)

- **`BACKLOG.md`:** Repo scanner checks **open** top-level issue rows + **only** lines where entire **`Spec:`** value = single backtick-wrapped `ia/projects/{ISSUE_ID}.md` path. **Notes** prose may mention future / placeholder paths without failing CI.
- **`BACKLOG-ARCHIVE.md`** **not** scanned — completed history may still mention removed spec paths.
- **Advisory mode:** `node tools/validate-dead-project-spec-paths.mjs --advisory` or `CI_DEAD_SPEC_ADVISORY=1` prints hits but exits 0.
- **Authoring:** Do not put resolvable `ia/projects/*.md` string in durable markdown unless file exists, or validator will flag.
- **Follow-up:** Optional **territory-ia** MCP wrapper + shared **Node** helpers remain separate **BACKLOG** work; scanner shipped as script + **CI** + docs only.

### Lessons learned (closeout ordering, 2026-04-03; absorbed into `/ship-stage` Pass B)

- **Ordering:** Closeout must follow **persist IA → archive task body (`ia_tasks.archived_at`) → `validate:dead-project-specs` → remove BACKLOG row → id purge** (user-confirmed). **`/ship-stage`** Pass B inline closeout via **`stage_closeout_apply`** MCP encodes this in single call; skipping **persist** first orphans definitions + breaks agent paths.
- **Scanner scope:** Pass B closeout invokes **`npm run validate:dead-project-specs`** only — no second **Node** scanner; new stale-reference classes belong on **BACKLOG** or extend existing validator.

### Lessons learned (closeout helpers + digest, 2026-04-03; updated for DB-primary)

- **Shipped helpers:** **`stage_closeout_apply`** (single-call closeout — shared migration tuples + N archive ops + N status flips + N id-purge ops), **`stage_closeout_digest`** (structured extract from DB-backed task body), **`spec_sections`** (batch **`spec_section`**). Documented in [`docs/mcp-ia-server.md`](../../docs/mcp-ia-server.md) + **`ia/skills/ship-stage/SKILL.md`** (Pass B).
- **Ordering:** Closeout sequence (**persist IA → archive (`archived_at`) → `validate:dead-project-specs` → BACKLOG removal + id purge**) — single MCP call applies full sequence atomically.
- **Ownership:** Dead project-spec path scanner + **BACKLOG** id validation inside project specs remain separate **BACKLOG** concerns — closeout helpers do not replace.

### Lessons learned (project-implementation-validation CI parity, 2026-04-03)

- **CI parity:** **`project-implementation-validation`** manifest mirrors **IA tools** **Node** job (dead **project spec** paths → **MCP** **`npm test`** → **`validate:fixtures`** → **`generate:ia-indexes --check`**), plus **advisory** **`npm run verify`** row under **`tools/mcp-ia-server`** — update skill when **CI** adds required steps.
- **Skip matrix:** Pure **Unity** / **C#** diffs may skip **Node** manifest; **MCP** / **schema** / **glossary** or **reference spec** bodies feeding indexes must run full subset.
- **Aggregate script:** Root **`npm run validate:implementation`** (or similar) remains **separate** **BACKLOG** decision.
- **Glossary:** **project-implementation-validation** documented next to **project-spec-close** under **Documentation** for agent searchability.
