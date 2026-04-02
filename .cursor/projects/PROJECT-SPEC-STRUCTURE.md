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
