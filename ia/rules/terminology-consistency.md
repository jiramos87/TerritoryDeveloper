---
description: Use canonical vocabulary from glossary and specs across code, docs, backlog, and MCP
alwaysApply: true
---

# Terminology and information consistency

When you **add or change** code, `.cursor/specs/`, `.cursor/rules/`, `BACKLOG.md`, `docs/`, how-tos, tutorials, or **territory-ia** MCP tool names and descriptions:

1. **Domain language** — Use the same terms as [`.cursor/specs/glossary.md`](.cursor/specs/glossary.md) and the **linked specs** (especially `isometric-geography-system.md` for roads, water, slopes, sorting). Prefer **glossary table names** (`HeightMap`, `wet run`, `road stroke`, etc.) over ad-hoc synonyms. If the glossary and a spec disagree, **the spec wins** (per glossary header).
2. **C# and assets** — Follow [`.cursor/rules/coding-conventions.mdc`](.cursor/rules/coding-conventions.mdc) for identifiers, XML docs, and new prefab naming.
3. **Backlog** — Issue ids (`BUG-` / `FEAT-` / `TECH-` / …) appear **only** in [`BACKLOG.md`](BACKLOG.md) (open rows) and [`BACKLOG-ARCHIVE.md`](BACKLOG-ARCHIVE.md). **Do not** cite those ids in **glossary**, **reference specs**, **rules** (except this line), **skills**, `docs/` narratives, or code comments—use **glossary** terms and links to specs. In **BACKLOG** **Notes** / **Files** / **Acceptance**, reuse vocabulary from specs/glossary so rows stay consistent with durable IA.
4. **MCP tools** — Tool names are **snake_case** and must match server registration (`tools/mcp-ia-server/src/index.ts`); update [`tools/mcp-ia-server/README.md`](tools/mcp-ia-server/README.md) and [`docs/mcp-ia-server.md`](docs/mcp-ia-server.md) when adding or renaming tools.
5. **New concepts** — If you introduce a **new** domain term or redefine behavior, add or update the **glossary** row and the **authoritative spec** section; do not leave the term only in backlog or chat.
6. **Project specs** (`.cursor/projects/{ISSUE_ID}.md`) — Use glossary terms in **Open Questions**; those questions define **game logic**, not implementation (see [`AGENTS.md`](AGENTS.md) `.cursor/projects/` policy and [`PROJECT-SPEC-STRUCTURE.md`](../projects/PROJECT-SPEC-STRUCTURE.md)).
7. **Reference specs** (`.cursor/specs/*.md`) — Follow [`REFERENCE-SPEC-STRUCTURE.md`](../specs/REFERENCE-SPEC-STRUCTURE.md) for permanent spec authoring; prefer glossary vocabulary and keep **isometric-geography-system.md** authoritative for shared terrain/road/water rules.

Full workflow: [`AGENTS.md`](AGENTS.md) — Terminology and information consistency.
