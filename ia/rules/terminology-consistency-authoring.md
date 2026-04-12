---
purpose: Authoring guidance for terminology consistency when editing code, MCP tools, project specs, or reference specs
audience: agent
loaded_by: ondemand
slices_via: none
description: On-demand authoring companion to terminology-consistency — C# conventions, MCP tool registration, project-spec / reference-spec authoring checklist
alwaysApply: false
---

# Terminology and information consistency — authoring companion

Runtime stub: [`ia/rules/terminology-consistency.md`](terminology-consistency.md). Fetch via `rule_content terminology-consistency-authoring` when editing C#, MCP tool names, project specs, reference specs.

Add/change code, `ia/specs/`, `ia/rules/`, `BACKLOG.md`, `docs/`, how-tos, tutorials, or **territory-ia** MCP tool names/descriptions → apply in addition to runtime stub:

1. **C# / assets** — [`coding-conventions.md`](coding-conventions.md) for identifiers, XML docs, new prefab naming.
2. **MCP tools** — snake_case; match server registration (`tools/mcp-ia-server/src/index.ts`); update [`tools/mcp-ia-server/README.md`](../../tools/mcp-ia-server/README.md) + [`docs/mcp-ia-server.md`](../../docs/mcp-ia-server.md) on add/rename.
3. **New concepts** — new domain term or redefined behavior → add/update glossary row + authoritative spec section. No backlog-only / chat-only terms.
4. **Project specs** (`ia/projects/{ISSUE_ID}.md`) — glossary terms in **Open Questions**; questions define **game logic**, not implementation ([`AGENTS.md`](../../AGENTS.md) `ia/projects/` policy + [`PROJECT-SPEC-STRUCTURE.md`](../projects/PROJECT-SPEC-STRUCTURE.md)).
5. **Reference specs** (`ia/specs/*.md`) — [`REFERENCE-SPEC-STRUCTURE.md`](../specs/REFERENCE-SPEC-STRUCTURE.md); prefer glossary vocabulary; **isometric-geography-system.md** authoritative for shared terrain/road/water rules.
6. **Backlog Notes / Files / Acceptance** — reuse spec/glossary vocabulary.

Full workflow: [`AGENTS.md`](../../AGENTS.md) — Terminology and information consistency.
