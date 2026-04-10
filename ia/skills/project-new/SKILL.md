---
name: project-new
description: >
  Use when creating a new BACKLOG.md issue from a user prompt: next BUG-/FEAT-/TECH-/ART-/AUDIO- id,
  row in the correct priority section, bootstrap .cursor/projects/{ISSUE_ID}.md from the template,
  and Depends on / Related with verified ids (territory-ia MCP + optional web_search). Triggers:
  "/project-new", "new backlog issue", "create TECH-xx from prompt", "bootstrap project spec",
  "add issue to backlog from description".
---

# New backlog issue and project spec bootstrap

This skill **does not** call MCP tools itself. In an **Agent** chat with **territory-ia** enabled, follow the **Tool recipe** below **before** editing **`BACKLOG.md`** or creating **`.cursor/projects/{ISSUE_ID}.md`**, so vocabulary and spec slices load as **thin context** (see **`AGENTS.md`** step 3 and **`.cursor/rules/mcp-ia-default.mdc`**).

**Contrast with [`project-spec-kickoff`](../project-spec-kickoff/SKILL.md):** **kickoff** starts from an **existing** project spec file (`backlog_issue` first). **This** skill starts from the **user prompt** to **create** the backlog row and spec stub. After the stub exists, use **kickoff** to refine it, **[`project-spec-implement`](../project-spec-implement/SKILL.md)** to execute the plan, and **[`project-spec-close`](../project-spec-close/SKILL.md)** when closing.

**Related:** Sibling skills — **kickoff** / **implement** / **close** / [**`project-implementation-validation`**](../project-implementation-validation/SKILL.md). Open **MCP** / **Skills** / **project-spec** hygiene rows — [`BACKLOG.md`](../../../BACKLOG.md). Shipped skill trace — [`BACKLOG-ARCHIVE.md`](../../../BACKLOG-ARCHIVE.md). **Conventions:** [`.cursor/skills/README.md`](../README.md).

## Seed prompt (parameterize)

Replace placeholders before sending.

```markdown
Create a new backlog issue and initial project spec from this description:

**Title / intent:** {SHORT_TITLE}
**Issue type:** {BUG-|FEAT-|TECH-|ART-|AUDIO-} (or ask me if unsure)
**User / product prompt:**

{USER_PROMPT}

Follow `.cursor/skills/project-new/SKILL.md`: run the Tool recipe (territory-ia), then add the row to `BACKLOG.md`, create `.cursor/projects/{ISSUE_ID}.md` from `.cursor/templates/project-spec-template.md`, set `Spec:` on the backlog row, and link Depends on / Related with verified ids only. Run `npm run validate:dead-project-specs` before finishing the PR.
```

## When to use `web_search`

Use **`web_search`** only when the prompt depends on **external** facts (vendor APIs, third-party Unity packages, industry standards) **not** defined in this repo.

**Do not** use **web** to override **glossary**, **reference specs**, or **invariants** for **in-repo** game behavior. If research changes backlog wording, cite **URLs** in the new project spec **Decision Log** or backlog **Notes**.

## Tool recipe (territory-ia)

Run **in order** unless the prompt is **pure meta** (e.g. only repo hygiene with zero domain terms — then skip only the steps marked *optional*).

1. **`glossary_discover`** — Pass **`keywords` as a JSON array** of **English** tokens from the user prompt (translate non-English prompts first). Avoid generic-only arrays (`["MCP", "agent"]`).

2. **`glossary_lookup`** — For high-confidence **Term** strings from discover results or known glossary rows.

3. **`router_for_task`** — One call per **1–3** inferred domains. The `domain` argument must match a **Task domain** (or geography quick-reference) substring from **`.cursor/rules/agent-router.mdc`** — see [`.cursor/skills/README.md`](../README.md) (**`router_for_task`** lesson). Ad-hoc phrases often return **`no_matching_domain`**.

4. **`spec_section`** — For each routed spec, fetch **only** the sections the prompt implies; set **`max_chars`**. **Do not** read entire `.cursor/specs/*.md` files unless **`spec_outline`** forces it. If the work mentions **Editor → Reports** or **`tools/reports/`**, include **unity-development-context** **§10**.

5. **`invariants_summary`** — If the **new** issue likely touches **runtime C#** or **game subsystems**. Skip for strict doc/IA-only issues.

6. **`backlog_issue`** — For each related **`ISSUE_ID`** you will cite in **Depends on** / **Related** / **Notes**, to pull **Files**, **Notes**, **`status`**, and **`depends_on_status`**. If **`depends_on_status`** shows unsatisfied hard dependencies for that id, align **Depends on** / **Notes** or wait until prerequisites are met. **`backlog_issue`** searches **`BACKLOG.md`** then [`BACKLOG-ARCHIVE.md`](../../../BACKLOG-ARCHIVE.md) ([`AGENTS.md`](../../../AGENTS.md), [`docs/mcp-ia-server.md`](../../../docs/mcp-ia-server.md)).

7. **`list_specs`** / **`spec_outline`** — **Only** if you do not know the `spec` key for **`spec_section`**.

### Optional: IA project spec journal (Postgres)

**Only when** the user prompt is **ambiguous**, cross-cutting, or the user asked for **exploration** / **design critique** alignment — and **not** for every new issue. Requires **`DATABASE_URL`** (see [`docs/postgres-ia-dev-setup.md`](../../../docs/postgres-ia-dev-setup.md)).

1. **`project_spec_journal_search`** — **English** `query` (short phrase) and/or `raw_text_for_tokens` from the translated prompt; **`max_results`** ≤ **8**. Prefer excerpts; use **`project_spec_journal_get`** only when a hit clearly applies.
2. If the tool returns **`db_unconfigured`**, skip without noise. **Do not** substitute bulk **`spec_section`** reads — keep reference-spec pulls **surgical** per steps 3–4.

### Branching (minimum set)

Mirror **kickoff** branching when classifying the **new** issue:

- **Roads / bridge / wet run** → **roads-system** + **geo** slices via **`router_for_task`** + **`spec_section`**.
- **Water / HeightMap / shore / river** → **water-terrain-system** + **geo**.
- **JSON / schema / Save / interchange** → **persistence-system**; do **not** propose on-disk **Save data** changes unless the user explicitly requires them; see **glossary** **JSON interchange program** and [`BACKLOG-ARCHIVE.md`](../../../BACKLOG-ARCHIVE.md) when applicable.

## File and backlog checklist

1. **Choose prefix** — **`BUG-`**, **`FEAT-`**, **`TECH-`**, **`ART-`**, **`AUDIO-`** per [`AGENTS.md`](../../../AGENTS.md) **Issue ID convention**.

2. **Next id** — Scan **`BACKLOG.md`** and [`BACKLOG-ARCHIVE.md`](../../../BACKLOG-ARCHIVE.md) for the **highest** existing number in that prefix; assign **max + 1**. **Never reuse** a number already present in **either** file (ids are **monotonic** per prefix for the life of the repo). See [`AGENTS.md`](../../../AGENTS.md) **Backlog Workflow** — **Adding new issues**.

3. **Priority section** — Insert the row in the section that matches **severity** and **existing `BACKLOG.md` structure** (e.g. **High priority**, **Code Health**, **Agent ↔ Unity & MCP context lane**). Follow **Priority order** in [`AGENTS.md`](../../../AGENTS.md) when choosing among standard sections.

4. **Backlog row** — Include **Type**, **Files**, **Notes**, **Spec:** **`.cursor/projects/{ISSUE_ID}.md`**, **Depends on** / **Acceptance** as appropriate. Every **`BUG-`/`FEAT-`/`TECH-`-** id you cite must exist in **`BACKLOG.md`** (or be reserved in the **same** edit batch you add it).

5. **Project spec** — Copy [`.cursor/templates/project-spec-template.md`](../../templates/project-spec-template.md) to **`.cursor/projects/{ISSUE_ID}.md`**. Fill header (**Issue** link to **`BACKLOG.md`**), **Summary**, **Goals**, stub **Implementation Plan**, **Open Questions** per [`.cursor/projects/PROJECT-SPEC-STRUCTURE.md`](../../projects/PROJECT-SPEC-STRUCTURE.md) (tooling-only: **None** or point to **Acceptance**).

6. **Validate** — Run **`npm run validate:dead-project-specs`** from the repo root after adding or changing **`Spec:`** paths or links to **`.cursor/projects/*.md`**.

7. **Next step** — Offer **[`project-spec-kickoff`](../project-spec-kickoff/SKILL.md)** to refine the new spec before heavy implementation.

## Follow-up (planned domain skills)

When **implementing** code in roads, terrain/water, or new managers, follow **`project-spec-implement`** branching and any **domain** skills listed on [`BACKLOG.md`](../../../BACKLOG.md).
