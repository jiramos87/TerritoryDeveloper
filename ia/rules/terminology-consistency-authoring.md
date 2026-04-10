---
purpose: Authoring guidance for terminology consistency when editing code, MCP tools, project specs, or reference specs
audience: agent
loaded_by: ondemand
slices_via: none
description: On-demand authoring companion to terminology-consistency — C# conventions, MCP tool registration, project-spec / reference-spec authoring checklist
alwaysApply: false
---

# Terminology and information consistency — authoring companion

Runtime stub: [`ia/rules/terminology-consistency.md`](terminology-consistency.md). Fetch this companion via `rule_content terminology-consistency-authoring` when editing C#, MCP tool names, project specs, or reference specs.

When you **add or change** code, `ia/specs/`, `ia/rules/`, `BACKLOG.md`, `docs/`, how-tos, tutorials, or **territory-ia** MCP tool names and descriptions, the following apply in addition to the runtime stub:

1. **C# and assets** — Follow [`ia/rules/coding-conventions.md`](coding-conventions.md) for identifiers, XML docs, and new prefab naming.
2. **MCP tools** — Tool names are **snake_case** and must match server registration (`tools/mcp-ia-server/src/index.ts`); update [`tools/mcp-ia-server/README.md`](../../tools/mcp-ia-server/README.md) and [`docs/mcp-ia-server.md`](../../docs/mcp-ia-server.md) when adding or renaming tools.
3. **New concepts** — If you introduce a **new** domain term or redefine behavior, add or update the **glossary** row and the **authoritative spec** section; do not leave the term only in backlog or chat.
4. **Project specs** (`ia/projects/{ISSUE_ID}.md`) — Use glossary terms in **Open Questions**; those questions define **game logic**, not implementation (see [`AGENTS.md`](../../AGENTS.md) `ia/projects/` policy and [`PROJECT-SPEC-STRUCTURE.md`](../projects/PROJECT-SPEC-STRUCTURE.md)).
5. **Reference specs** (`ia/specs/*.md`) — Follow [`REFERENCE-SPEC-STRUCTURE.md`](../specs/REFERENCE-SPEC-STRUCTURE.md) for permanent spec authoring; prefer glossary vocabulary and keep **isometric-geography-system.md** authoritative for shared terrain/road/water rules.
6. **Backlog Notes / Files / Acceptance** — reuse vocabulary from specs/glossary so rows stay consistent with durable IA.

Full workflow: [`AGENTS.md`](../../AGENTS.md) — Terminology and information consistency.
