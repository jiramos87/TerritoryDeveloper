# Project spec structure — `.cursor/projects/`

Temporary specs for an active **BACKLOG** item live here as `{ISSUE_ID}.md` (e.g. `BUG-37.md`, `FEAT-44.md`). They are deleted after verified completion; lessons migrate to canonical docs.

## Which template to use

| Document | Purpose |
|----------|---------|
| [`.cursor/templates/project-spec-template.md`](../templates/project-spec-template.md) | Copy-paste skeleton when creating a new `{ISSUE_ID}.md`. |
| This file | Section order, naming rules, and how to split **requirements** vs **implementation**. |

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
8. **Acceptance Criteria** — Testable conditions mapped to goals / stories.
9. **Issues Found During Development** — Table during implementation.
10. **Lessons Learned** — Fill at closure; migrate to specs / `AGENTS.md` / glossary as needed.
11. **`## Open Questions (resolve before / during implementation)`** — See below (mandatory section for collaborative specs).

## Open Questions: game logic only

- Questions under **`## Open Questions`** MUST use **canonical vocabulary** from [`.cursor/specs/glossary.md`](../specs/glossary.md) and linked specs.
- They MUST clarify **definitions and intended game behavior** (what the simulation / player rules are), not APIs, class names, or algorithms.
- The **implementing agent** resolves technical approach and code paths **unless** the chosen approach would **change** the game behavior defined in the spec; then update the **Decision Log** or ask the product owner.

## Terminology

- Prefer glossary table names (**road stroke**, **terraform plan**, **Moore neighborhood**, **street**, **interstate**, **RCI**, **zone**, **building**, **wet run**, etc.).
- If the glossary and a spec disagree, **the spec wins** (per glossary header).
- BACKLOG **Files** / **Notes** should use the same words for searchability.

## Lifecycle

1. Create `{ISSUE_ID}.md` from the template → refine Open Questions and acceptance → **Final** when stable.
2. Implement → keep **Issues Found** up to date.
3. On user-confirmed completion: migrate durable content to canonical docs; archive issue; **delete** the project spec.

### Closeout checklist (before deleting `{ISSUE_ID}.md`)

After the owner **confirms** verification (**AGENTS.md** — do not mark **Completed** in **BACKLOG** until then):

1. **Migrate** normative content: **Lessons Learned**, **Decision Log** items, and any rules that belong in [reference specs](../specs/) (see [glossary](../specs/glossary.md)), [`.cursor/rules/`](../rules/), `docs/`, `ARCHITECTURE.md` — not in the deleted file.
2. **Cascade links:** Search durable docs (`.cursor/skills/`, `.cursor/rules/`, `docs/`, `projects/`, `AGENTS.md`, `ARCHITECTURE.md`, etc.) for markdown links, backticks, or plain paths to `.cursor/projects/{ISSUE_ID}.md` and replace them with **`BACKLOG.md`** / **`BACKLOG-ARCHIVE.md`** references by **issue id** (and section anchor if useful). Do not leave pointers to a removed path.
3. **Umbrella / sibling specs:** Update remaining `.cursor/projects/*.md` that depended on this issue (**Depends on**, **Implementation Plan**, **Acceptance**) so they do not describe the closed work as pending.
4. **BACKLOG row:** When moving the issue to **Completed**, adjust the **`Spec:`** line to a **removed-after-closure** pattern (see completed rows in **`BACKLOG.md`**) instead of a live `.cursor/projects/…` path.
5. **Verify:** Run `npm run validate:dead-project-specs` from the repo root (or `node tools/validate-dead-project-spec-paths.mjs`) so CI and agents catch any missed stale path. The **`project-spec-close`** Cursor skill ([`.cursor/skills/project-spec-close/SKILL.md`](../skills/project-spec-close/SKILL.md)) orchestrates the full closeout sequence (**TECH-51** completed — see [`BACKLOG.md`](../../BACKLOG.md) **§ Completed**). **Closeout helpers (**TECH-58** **§ Completed**):** territory-ia **`project_spec_closeout_digest`**, **`spec_sections`**, and root **`npm run closeout:*`** — see [`docs/mcp-ia-server.md`](../../docs/mcp-ia-server.md) **Project spec workflows**.

### Lessons learned (**TECH-50** closure, 2026-04-03)

- **`BACKLOG.md`:** The repo scanner checks **open** top-level issue rows and **only** lines where the entire **`Spec:`** value is a single backtick-wrapped `.cursor/projects/{ISSUE_ID}.md` path. **Notes** prose may mention future or placeholder paths without failing CI.
- **`BACKLOG-ARCHIVE.md`** is **not** scanned — completed history may still mention removed spec paths.
- **Advisory mode:** `node tools/validate-dead-project-spec-paths.mjs --advisory` or `CI_DEAD_SPEC_ADVISORY=1` prints hits but exits 0.
- **Authoring:** Do not put a resolvable `.cursor/projects/*.md` string in durable markdown unless that file exists, or the validator will flag it.
- **Follow-up:** Optional **territory-ia** MCP wrapper and shared **Node** helpers with **TECH-30** remain separate backlog / implementation work (**TECH-50** shipped script + CI + docs only).

### Lessons learned (**TECH-51** closure, 2026-04-03)

- **Ordering:** Closeout must follow **persist IA → delete project spec → `validate:dead-project-specs` → BACKLOG Completed** (user-confirmed). The **`project-spec-close`** skill encodes this; skipping **persist** first orphans definitions and breaks agent paths.
- **Scanner scope:** **`project-spec-close`** invokes **`npm run validate:dead-project-specs`** only — no second **Node** scanner in the skill; new stale-reference classes belong in **TECH-50** / **TECH-30** or a new **BACKLOG** row.
- **MCP:** Composite **closeout_preflight** (or similar) remains **deferred** — **TECH-48** / follow-up may subsume; **v1** is **territory-ia** **Tool recipe** + file edits only.

### Lessons learned (**TECH-58** closure, 2026-04-03)

- **Shipped helpers:** **`project_spec_closeout_digest`** (structured extract from `.cursor/projects/{ISSUE_ID}.md`), **`spec_sections`** (batch **`spec_section`**), root **`npm run closeout:worksheet`** / **`closeout:dependents`** / **`closeout:verify`**. Documented in [`docs/mcp-ia-server.md`](../../docs/mcp-ia-server.md) and **`.cursor/skills/project-spec-close/SKILL.md`** (**Efficiency**).
- **Shared parser:** `tools/mcp-ia-server/src/parser/project-spec-closeout-parse.ts` — intended for reuse when **TECH-48** ships path-based discovery; avoid duplicating competing MCP wrappers.
- **Ordering:** Closeout sequence (**persist IA → delete project spec → `validate:dead-project-specs` → BACKLOG Completed**) unchanged from **TECH-51**.
- **Ownership:** **TECH-50** remains the **dead project-spec path** scanner; **TECH-30** remains **BACKLOG** id validation inside project specs — **TECH-58** does not replace them.

### Lessons learned (**TECH-52** closure, 2026-04-03)

- **CI parity:** The **`project-implementation-validation`** manifest mirrors the **IA tools** **Node** job (dead **project spec** paths → **MCP** **`npm test`** → **`validate:fixtures`** → **`generate:ia-indexes --check`**), plus an **advisory** **`npm run verify`** row under **`tools/mcp-ia-server`** — update the skill when **CI** adds required steps.
- **Skip matrix:** Pure **Unity** / **C#** diffs may skip the **Node** manifest; **MCP** / **schema** / **glossary** or **reference spec** bodies that feed indexes should run the full subset.
- **Aggregate script:** Root **`npm run validate:implementation`** (or similar) remains a **separate** **BACKLOG** decision — not shipped with **TECH-52**.
- **Glossary:** **project-implementation-validation** is documented next to **project-spec-close** under **Documentation** for agent searchability.
