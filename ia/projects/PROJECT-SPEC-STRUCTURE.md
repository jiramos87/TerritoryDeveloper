---
purpose: "Project spec for Project spec structure — ia/projects/."
audience: both
loaded_by: ondemand
slices_via: none
---
# Project spec structure — `ia/projects/`

Temporary specs for an active **BACKLOG** item live here. **New specs use the descriptive naming convention `{ISSUE_ID}-{description}.md`** (e.g. `BUG-37-zone-cleanup.md`, `FEAT-44-water-junction.md`, `TECH-11-example-migration.md`). The legacy bare `{ISSUE_ID}.md` form (e.g. `BUG-37.md`) is still accepted for back-compat with older specs but should not be used for new files. The descriptive `{description}` suffix carries valuable context for humans and grep alike. Specs are deleted after verified completion; lessons migrate to canonical docs.

> **Naming convention:** `{ISSUE_ID}-{description}.md` is the canonical form for all new project specs. The `{ISSUE_ID}` prefix is one of `BUG-`, `FEAT-`, `TECH-`, `ART-`, `AUDIO-` followed by the issue number and optional letter suffix. The `{description}` is filename-safe (letters, digits, dots, underscores, hyphens), kebab-case, and short enough to scan. Both `project_spec_journal_persist` and `project_spec_closeout_digest` accept either form.

## Which template to use

| Document | Purpose |
|----------|---------|
| [`ia/templates/project-spec-template.md`](../templates/project-spec-template.md) | Copy-paste skeleton when creating a new `{ISSUE_ID}-{description}.md`. |
| This file | Section order, naming rules, and how to split **requirements** vs **implementation**. |

## Umbrella program specs (multi-issue)

Some **BACKLOG** programs use a **parent** project spec plus **child** specs (e.g. **TECH-60** **§ Completed** with **TECH-61**–**TECH-63** — [`BACKLOG-ARCHIVE.md`](../../BACKLOG-ARCHIVE.md) **§ Completed**). Each child links to the parent in its header (**Parent program**). Unless the charter says otherwise, each file follows the **section order** below. **Acceptance** for closing the **umbrella** **project spec** depends on the **BACKLOG** row: some programs require every child **Completed** first; others retire the **umbrella** from open **BACKLOG** while **follow-ups** remain (see [`BACKLOG-ARCHIVE.md`](../../BACKLOG-ARCHIVE.md) vs [`BACKLOG.md`](../../BACKLOG.md) open program sections).

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
   - **Systems map:** short pointer to backlog “Files” / subsystems (no deep API dump required).
   - **Implementation investigation notes** (optional): technical hypotheses for the agent only—must not be mistaken for product requirements.
5. **Proposed Design** — **Target behavior (product)** first; **architecture / code** marked as agent-owned unless the user fixed a design.
6. **Decision Log** — Dated choices; alternatives considered.
7. **Implementation Plan** — Phased checklists (agent executes unless a step would change game logic).
7b. **Test Contracts** (optional; **tooling / verification**) — **Template:** [`ia/templates/project-spec-template.md`](../templates/project-spec-template.md) includes **`## 7b. Test Contracts`** with columns **Acceptance / goal** \| **Check type** \| **Command or artifact** \| **Notes** (e.g. **Node**, **golden** **JSON**, **manual**, future **Unity** **UTF**, advisory **`verify`**). Use **glossary** terms for *what* is verified. **Not** a substitute for **`## Open Questions`**, which remain **game logic** only (see below). **`project_spec_closeout_digest`** does **not** extract **§7b** today — adding a **`test_contracts`** field requires extending `tools/mcp-ia-server/src/parser/project-spec-closeout-parse.ts` in a **separate** **BACKLOG** / **TECH-** follow-up (out of scope for **TECH-63** template + **Skills** ship).
8. **Acceptance Criteria** — Testable conditions mapped to goals / stories.
9. **Issues Found During Development** — Table during implementation.
10. **Lessons Learned** — Fill at closure; migrate to specs / `AGENTS.md` / glossary as needed.
11. **`## Open Questions (resolve before / during implementation)`** — See below (mandatory section for collaborative specs).

## Open Questions: game logic only

- Questions under **`## Open Questions`** MUST use **canonical vocabulary** from [`ia/specs/glossary.md`](../specs/glossary.md) and linked specs.
- They MUST clarify **definitions and intended game behavior** (what the simulation / player rules are), not APIs, class names, or algorithms.
- The **implementing agent** resolves technical approach and code paths **unless** the chosen approach would **change** the game behavior defined in the spec; then update the **Decision Log** or ask the product owner.

## Terminology

- Prefer glossary table names (**road stroke**, **terraform plan**, **Moore neighborhood**, **street**, **interstate**, **RCI**, **zone**, **building**, **wet run**, etc.).
- If the glossary and a spec disagree, **the spec wins** (per glossary header).
- BACKLOG **Files** / **Notes** should use the same words for searchability.

## Lifecycle

1. Create `{ISSUE_ID}-{description}.md` from the template (or the legacy bare `{ISSUE_ID}.md` form for older specs) → refine Open Questions and acceptance → **Final** when stable.
2. Implement → keep **Issues Found** up to date.
3. On user-confirmed completion: migrate durable content to canonical docs; archive issue; **delete** the project spec.

### Closeout checklist (before deleting `{ISSUE_ID}.md`)

After the owner **confirms** verification (**AGENTS.md**):

1. **Migrate** normative content: **Lessons Learned**, **Decision Log** items, and any rules that belong in [reference specs](../specs/) (see [glossary](../specs/glossary.md)), [`ia/rules/`](../rules/), `docs/`, `ARCHITECTURE.md` — not in the deleted file. **Optional:** when **`DATABASE_URL`** is set, also persist **verbose** **Decision Log** + **Lessons learned** bodies to **Postgres** **`ia_project_spec_journal`** per [**ia/skills/project-spec-close/SKILL.md**](../skills/project-spec-close/SKILL.md) checklist **J1** ([`docs/postgres-ia-dev-setup.md`](../../docs/postgres-ia-dev-setup.md) §IA project spec journal).
2. **Cascade links:** Search durable docs (`ia/skills/`, `ia/rules/`, `docs/`, `projects/`, `AGENTS.md`, `ARCHITECTURE.md`, etc.) for markdown links, backticks, or plain paths to `ia/projects/{ISSUE_ID}.md` and replace them with **`BACKLOG.md`** / **`BACKLOG-ARCHIVE.md`** references (and section anchor if useful). Do not leave pointers to a removed path.
3. **Umbrella / sibling specs:** Update remaining `ia/projects/*.md` that depended on this issue (**Depends on**, **Implementation Plan**, **Acceptance**) so they do not describe the closed work as pending.
4. **BACKLOG + archive:** **Remove** the row from **`BACKLOG.md`**. **Append** **`[x]`** to **`BACKLOG-ARCHIVE.md`** with **`Spec:`** → **removed-after-closure** pattern. **Strip** the closed issue id from **glossary**, **reference specs**, **rules**, **skills**, `docs/`, `projects/`, and code comments ([**terminology-consistency**](../rules/terminology-consistency.md), **`project-spec-close`** skill).
5. **Verify:** Run `npm run validate:dead-project-specs` from the repo root (or `node tools/validate-dead-project-spec-paths.mjs`) so CI and agents catch any missed stale path. The **`project-spec-close`** Cursor skill ([`ia/skills/project-spec-close/SKILL.md`](../skills/project-spec-close/SKILL.md)) orchestrates the full sequence. **Closeout helpers:** territory-ia **`project_spec_closeout_digest`**, **`spec_sections`**, and root **`npm run closeout:*`** — see [`docs/mcp-ia-server.md`](../../docs/mcp-ia-server.md) **Project spec workflows**.

### Lessons learned (dead project-spec path scanner, 2026-04-03)

- **`BACKLOG.md`:** The repo scanner checks **open** top-level issue rows and **only** lines where the entire **`Spec:`** value is a single backtick-wrapped `ia/projects/{ISSUE_ID}.md` path. **Notes** prose may mention future or placeholder paths without failing CI.
- **`BACKLOG-ARCHIVE.md`** is **not** scanned — completed history may still mention removed spec paths.
- **Advisory mode:** `node tools/validate-dead-project-spec-paths.mjs --advisory` or `CI_DEAD_SPEC_ADVISORY=1` prints hits but exits 0.
- **Authoring:** Do not put a resolvable `ia/projects/*.md` string in durable markdown unless that file exists, or the validator will flag it.
- **Follow-up:** Optional **territory-ia** MCP wrapper and shared **Node** helpers remain separate **BACKLOG** work; the scanner shipped as script + **CI** + docs only.

### Lessons learned (project-spec-close ordering, 2026-04-03)

- **Ordering:** Closeout must follow **persist IA → delete project spec → `validate:dead-project-specs` → remove BACKLOG row → append archive → id purge** (user-confirmed). The **`project-spec-close`** skill encodes this; skipping **persist** first orphans definitions and breaks agent paths.
- **Scanner scope:** **`project-spec-close`** invokes **`npm run validate:dead-project-specs`** only — no second **Node** scanner in the skill; new stale-reference classes belong on **BACKLOG** or extend the existing validator.
- **MCP:** Composite **closeout_preflight** (or similar) remains **deferred**; **v1** is **territory-ia** **Tool recipe** + file edits only.

### Lessons learned (closeout helpers + digest, 2026-04-03)

- **Shipped helpers:** **`project_spec_closeout_digest`** (structured extract from `ia/projects/{ISSUE_ID}.md`), **`spec_sections`** (batch **`spec_section`**), root **`npm run closeout:worksheet`** / **`closeout:dependents`** / **`closeout:verify`**. Documented in [`docs/mcp-ia-server.md`](../../docs/mcp-ia-server.md) and **`ia/skills/project-spec-close/SKILL.md`** (**Efficiency**).
- **Shared parser:** `tools/mcp-ia-server/src/parser/project-spec-closeout-parse.ts` — intended for reuse when path-based discovery ships; avoid duplicating competing MCP wrappers.
- **Ordering:** Closeout sequence (**persist IA → delete project spec → `validate:dead-project-specs` → archive + id purge**) unchanged from the **`project-spec-close`** skill baseline.
- **Ownership:** The dead project-spec path scanner and **BACKLOG** id validation inside project specs remain separate **BACKLOG** concerns — closeout helpers do not replace them.

### Lessons learned (project-implementation-validation CI parity, 2026-04-03)

- **CI parity:** The **`project-implementation-validation`** manifest mirrors the **IA tools** **Node** job (dead **project spec** paths → **MCP** **`npm test`** → **`validate:fixtures`** → **`generate:ia-indexes --check`**), plus an **advisory** **`npm run verify`** row under **`tools/mcp-ia-server`** — update the skill when **CI** adds required steps.
- **Skip matrix:** Pure **Unity** / **C#** diffs may skip the **Node** manifest; **MCP** / **schema** / **glossary** or **reference spec** bodies that feed indexes should run the full subset.
- **Aggregate script:** Root **`npm run validate:implementation`** (or similar) remains a **separate** **BACKLOG** decision.
- **Glossary:** **project-implementation-validation** is documented next to **project-spec-close** under **Documentation** for agent searchability.
