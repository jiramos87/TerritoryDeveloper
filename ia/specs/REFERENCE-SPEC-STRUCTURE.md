# Reference spec structure — `.cursor/specs/`

Permanent **reference specs** live here as `*.md` files. They are the long-lived source of truth for domain rules and vocabulary (not temporary write-ups for a single backlog item). See [`AGENTS.md`](../../AGENTS.md) for the canonical inventory.

## Which guide to use

| Document | Purpose |
|----------|---------|
| This file | Section conventions, terminology rules, lifecycle, and checklist for **new** reference specs. |
| [`.cursor/specs/glossary.md`](glossary.md) | Canonical **domain** term tables; reference specs should prefer these names. |
| [`.cursor/projects/PROJECT-SPEC-STRUCTURE.md`](../projects/PROJECT-SPEC-STRUCTURE.md) | Temporary **project specs** (`{ISSUE_ID}.md`) — product vs implementation split, Open Questions, closure. |

## Reference spec vs project spec

| Aspect | Reference spec (`.cursor/specs/`) | Project spec (`.cursor/projects/`) |
|--------|-----------------------------------|--------------------------------------|
| Lifetime | Permanent until deliberately superseded | Temporary — delete after verified backlog completion |
| Audience | All contributors; MCP slices; router | Active issue owners and implementing agents |
| Content | Normative game/system behavior, stable anchors | Issue-scoped goals, acceptance, decision log |
| Bug narratives | Do not use reference specs for bug write-ups — use `BACKLOG.md` + project spec | Bug/feature discovery and implementation notes |
| Vocabulary | Align with glossary; spec section wins over glossary if definitions differ | Open Questions must use glossary terms for **game logic** only |

## Conventions

1. **Stable headings** — Use numbered sections (e.g. `## 14.5 …`) where the geography spec and MCP `spec_section` rely on them. Avoid renaming anchors without updating cross-links and agent-router references.
2. **Canonical geography** — For grid math, water, cliffs, shores, roads, rivers, and pathfinding, [`isometric-geography-system.md`](isometric-geography-system.md) is the single source of truth. Other reference specs must not contradict it; defer with links.
3. **Cross-links** — Link to glossary rows and spec sections by path and section id; keep spec abbreviations (`geo`, `roads`, …) consistent with the glossary header.
4. **territory-ia MCP** — All `*.md` files in this directory are registered automatically. Optional short aliases live in `tools/mcp-ia-server/src/config.ts` (`SPEC_KEY_ALIASES`). After adding a file, confirm `list_specs` and, if useful, add an alias.
5. **Reusable IA pattern** — Domain-agnostic guide to file-backed specs + MCP tools: [`docs/mcp-markdown-ia-pattern.md`](../../docs/mcp-markdown-ia-pattern.md).
6. **agent-router task domains** — When adding rows to [`.cursor/rules/agent-router.mdc`](../rules/agent-router.mdc) (**Task → Spec routing**), phrase **Task domain** cells so MCP **`router_for_task`** does not mis-route: matching uses substring overlap and tokens (length ≥ 3). For example, wording such as “not isometric **math**” can match a user query **“grid math”** (token `math`) and return the wrong spec before the geography quick-reference table. Prefer distinct wording (e.g. “stacking rules” when contrasting with isometric **Sorting order**).
7. **IA index manifests (I1 / I2)** — After material edits to `.cursor/specs/*.md` or `glossary.md`, run `npm run generate:ia-indexes` from the repository root and commit `tools/mcp-ia-server/data/spec-index.json` and `glossary-index.json` so **CI** stays green (`generate:ia-indexes -- --check`). See [`docs/mcp-ia-server.md`](../../docs/mcp-ia-server.md) and glossary **IA index manifest**.

## Terminology

- Prefer **glossary table names** (**road stroke**, **terraform plan**, **street**, **interstate**, **wet run**, **urban centroid**, **AUTO systems**, etc.) over informal synonyms.
- If the glossary lacks a term you need: add a glossary row **and** define or cite the authoritative behavior in the relevant reference spec (do not leave the term only in backlog or chat).
- If glossary and a reference spec disagree on meaning: **the spec wins** (per glossary header); update the glossary to match or to point at the spec section.

## New reference spec checklist

1. Add a row to the `.cursor/specs/` inventory table in [`AGENTS.md`](../../AGENTS.md) (scope column).
2. Use the **minimal template** below as a starting point.
3. If agents should call it by a short MCP key, add `SPEC_KEY_ALIASES` entries in [`tools/mcp-ia-server/src/config.ts`](../../tools/mcp-ia-server/src/config.ts) and update [`docs/mcp-ia-server.md`](../../docs/mcp-ia-server.md) / [`tools/mcp-ia-server/README.md`](../../tools/mcp-ia-server/README.md) if those docs list aliases.
4. Do not place active issue narratives here — use [`BACKLOG.md`](../../BACKLOG.md) and `.cursor/projects/{ISSUE_ID}.md`.

## Minimal template (copy for a new `*.md`)

```markdown
# {Title} — Territory Developer

> One-line scope: what systems or UI this reference spec covers.

## 1. …

(Normative sections with stable `##` headings / numbering as needed.)

## Glossary alignment

- Key terms used in this spec: … (or: see [glossary.md](glossary.md) sections …)
```

## Deprecated prose → canonical terms (authoring)

When editing reference specs, prefer **`glossary_discover`** / **`glossary_lookup`** (territory-ia MCP) with English keywords to surface the table **Term** before global search-and-replace.

| Avoid in specs (unless defining) | Prefer |
|----------------------------------|--------|
| map edge (play-area boundary) | **Map border** |
| grid edge (when meaning outer playable boundary) | **Map border** |
| generic “road” for committed network tiles | **Street (ordinary road)** or **interstate**, or umbrella **street or interstate** when the rule applies to both |
| informal “validation” / “placement gate” for commit path | **Road validation pipeline** (plus **`PathTerraformPlan`**, Phase-1, `Apply` as needed) |
| “road only” (walkability) | grass + **street**/**interstate** cells (or cite **road stroke** when speaking of the drag path) |

**Local geometry:** Use **cell** edge, **Moore**/**cardinal neighbor**, or **shared cardinal edge** — not **map border** — unless the cell lies on the outer grid boundary.
