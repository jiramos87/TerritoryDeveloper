---
name: project-implementation-validation
description: >
  Use after substantive implementation when you need repo Node checks aligned with CI: dead project spec
  paths, MCP package tests, JSON fixtures, IA index drift. Triggers: "post-implementation validation",
  "run npm checks after TECH-xx", "validate fixtures", "IA tools parity", "MCP tests", "generate:ia-indexes --check".
---

# Project implementation validation (post-implementation checks)

This skill **does not** call MCP tools itself. It is a **checklist** of **existing** **`npm`** commands — **not** a second copy of **TECH-50** (`tools/validate-dead-project-spec-paths.mjs`) or the **MCP** scripts.

**Related:** **[`project-spec-implement`](../project-spec-implement/SKILL.md)** (phase exit → **Pre-commit Checklist** — **this** skill adds **automated** **Node** steps). **[`project-spec-close`](../project-spec-close/SKILL.md)** (optional: run **after** **IA persistence** when the change touched **MCP**, **schemas**, or **spec/glossary** bodies that feed indexes — **before** mandatory `npm run validate:dead-project-specs` in closeout). **CI parity:** [`.github/workflows/ia-tools.yml`](../../../.github/workflows/ia-tools.yml). **TECH-50** completed — `npm run validate:dead-project-specs`. **Conventions:** [`.cursor/skills/README.md`](../README.md).

## Failure policy

Run steps **in order**. If any step fails, **stop**, report the error output, and do **not** continue to later steps unless the human explicitly overrides.

## When to skip (N/A)

| Situation | Guidance |
|-----------|----------|
| Diff is **only** runtime **C#** / **Unity** assets | You may mark **all** manifest rows **N/A** and follow **[`AGENTS.md`](../../../AGENTS.md)** **Pre-commit Checklist** (Unity build, domain checks) instead. |
| Diff touches **`tools/mcp-ia-server`**, **`docs/schemas`**, **`.cursor/specs`** bodies, **`.cursor/specs/glossary.md`**, or committed **`tools/mcp-ia-server/data/*-index.json`** | Run the full **IA tools**-aligned subset below (at least steps 1–4). |
| Diff only fixes **BACKLOG** prose without **`Spec:`** or **MCP** paths | Step 1 may suffice; use judgment — **CI** still runs the full **Node** job on relevant paths. |

## Validation manifest (v1)

Run from **repository root** unless **Cwd** says otherwise. Script names match root [`package.json`](../../../package.json) and [`tools/mcp-ia-server/package.json`](../../../tools/mcp-ia-server/package.json) at ship time.

| Step | Command | Cwd | Notes |
|------|---------|-----|--------|
| 1 | `npm run validate:dead-project-specs` | repo root | **TECH-50** — open **BACKLOG** **`Spec:`** must point at existing `.cursor/projects/*.md` |
| 2 | `npm run test:ia` | repo root | Delegates to `npm --prefix tools/mcp-ia-server test` — same tests **CI** runs after `npm ci` under **`tools/mcp-ia-server`** |
| 3 | `npm run validate:fixtures` | repo root | Delegates via `--prefix` to **`tools/mcp-ia-server`** |
| 4 | `npm run generate:ia-indexes -- --check` | repo root | Ensures committed **`spec-index.json`** / **`glossary-index.json`** match **markdown** sources |
| 5 | `npm run verify` | `tools/mcp-ia-server` | **Advisory** — **not** in **IA tools** **Node** job today; run when touching **MCP** registration, parsers, or tool handlers (**TECH-24** culture) |

**Equivalent in package folder:** For step 2, `cd tools/mcp-ia-server && npm test` after `npm ci` matches **CI** exactly.

## Future / N/A (placeholders)

Add rows here when **BACKLOG** issues ship new **`npm`** commands (record the addition in a **Decision Log** on the shipping issue’s project spec, or in this **SKILL.md** body with maintainer review):

- **Unity** **Edit Mode** / **batchmode** — **TECH-15** / **TECH-16** / **UTF** (no single **v1** one-liner).
- **`tools/compute-lib`** tests — when the package and **`npm test`** exist.
- **TECH-26** / **TECH-29** static scans — when merged as **`npm run`** targets.

## Optional territory-ia preface

When the session maps to a **BACKLOG** id, call **`backlog_issue`** first for **Files** / **Acceptance**. Call **`invariants_summary`** only if validation is paired with **guardrail** or runtime **C#** doc edits (unusual for pure test runs).

## Manual fallback (no local Node)

Rely on **[`.github/workflows/ia-tools.yml`](../../../.github/workflows/ia-tools.yml)** on push/PR, or run the same commands in CI logs order when a local **Node** install is unavailable.

## Seed prompt (parameterize)

Replace `{CHANGED_AREAS}` with a short note on what shipped (e.g. **MCP** parser, **glossary**, **schema** fixture).

```markdown
Run **project-implementation-validation** after implementing {CHANGED_AREAS}.
Use `.cursor/skills/project-implementation-validation/SKILL.md`: apply **When to skip**, then run the **Validation manifest** steps in order; stop on first failure.
Do not reimplement **TECH-50** — use `npm run validate:dead-project-specs` only.
```
