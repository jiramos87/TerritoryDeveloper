---
purpose: "Reference spec for Reference spec structure — ia/specs/."
audience: agent
loaded_by: ondemand
slices_via: none
---
# Reference spec structure — `ia/specs/`

Permanent **reference specs** live here as `*.md` files. Long-lived source of truth for domain rules + vocabulary (not temporary write-ups for single backlog item). See [`AGENTS.md`](../../AGENTS.md) for canonical inventory.

## Which guide to use

| Document | Purpose |
|----------|---------|
| This file | Section conventions, terminology rules, lifecycle, and checklist for **new** reference specs. |
| [`ia/specs/glossary.md`](glossary.md) | Canonical **domain** term tables; reference specs should prefer these names. |
| [`docs/PROJECT-SPEC-STRUCTURE.md`](../../docs/PROJECT-SPEC-STRUCTURE.md) | Temporary **project specs** (`{ISSUE_ID}.md`) — product vs implementation split, Open Questions, closure. |

## Reference spec vs project spec

| Aspect | Reference spec (`ia/specs/`) | Project spec (`ia/projects/`) |
|--------|-----------------------------------|--------------------------------------|
| Lifetime | Permanent until deliberately superseded | Temporary — delete after verified backlog completion |
| Audience | All contributors; MCP slices; router | Active issue owners and implementing agents |
| Content | Normative game/system behavior, stable anchors | Issue-scoped goals, acceptance, decision log |
| Bug narratives | Do not use reference specs for bug write-ups — use `BACKLOG.md` + project spec | Bug/feature discovery and implementation notes |
| Vocabulary | Align with glossary; spec section wins over glossary if definitions differ | Open Questions must use glossary terms for **game logic** only |

## Conventions

1. **Stable headings** — Use numbered sections (e.g. `## 14.5 …`) where geography spec + MCP `spec_section` rely on them. Avoid renaming anchors without updating cross-links + agent-router references.
2. **Canonical geography** — For grid math, water, cliffs, shores, roads, rivers, pathfinding: [`isometric-geography-system.md`](isometric-geography-system.md) = single source of truth. Other reference specs must not contradict; defer with links.
3. **Cross-links** — Link to glossary rows + spec sections by path + section id; keep spec abbreviations (`geo`, `roads`, …) consistent with glossary header.
4. **territory-ia MCP** — All `*.md` files in directory registered automatically. Optional short aliases in `tools/mcp-ia-server/src/config.ts` (`SPEC_KEY_ALIASES`). After adding file, confirm `list_specs` + add alias if useful.
5. **Reusable IA pattern** — Domain-agnostic guide to file-backed specs + MCP tools: [`docs/mcp-markdown-ia-pattern.md`](../../docs/mcp-markdown-ia-pattern.md).
6. **agent-router task domains** — When adding rows to [`ia/rules/agent-router.md`](../rules/agent-router.md) (**Task → Spec routing**), phrase **Task domain** cells so MCP **`router_for_task`** does not mis-route: matching uses substring overlap + tokens (length ≥ 3). Example: "not isometric **math**" can match query **"grid math"** (token `math`) + return wrong spec before geography quick-reference table. Prefer distinct wording (e.g. "stacking rules" when contrasting with isometric **Sorting order**).
7. **IA index manifests (I1 / I2)** — After material edits to `ia/specs/*.md` or `glossary.md`, run `npm run generate:ia-indexes` from repo root + commit `tools/mcp-ia-server/data/spec-index.json` + `glossary-index.json` so **CI** stays green (`generate:ia-indexes -- --check`). See [`docs/mcp-ia-server.md`](../../docs/mcp-ia-server.md) + glossary **IA index manifest**.
8. **Registry-count gate** — Adding / removing `ia/specs/*.md` flips hardcoded expected entry count in [`tools/mcp-ia-server/tests/tools/build-registry.test.ts`](../../tools/mcp-ia-server/tests/tools/build-registry.test.ts). Bump `assert.equal(r.length, N)` value (+ `rules.length` if rule file moved) in same diff — test = CI gate for registry completeness; `npm run validate:all` fails otherwise.

## Terminology

- Prefer **glossary table names** (**road stroke**, **terraform plan**, **street**, **interstate**, **wet run**, **urban centroid**, **AUTO systems**, etc.) over informal synonyms.
- Glossary lacks needed term → add glossary row **and** define / cite authoritative behavior in relevant reference spec (do not leave term only in backlog or chat).
- Glossary + reference spec disagree on meaning → **spec wins** (per glossary header); update glossary to match or point at spec section.

## New reference spec checklist

1. Add row to `ia/specs/` inventory table in [`AGENTS.md`](../../AGENTS.md) (scope column).
2. Use **minimal template** below as starting point.
3. Agents should call by short MCP key → add `SPEC_KEY_ALIASES` entries in [`tools/mcp-ia-server/src/config.ts`](../../tools/mcp-ia-server/src/config.ts) + update [`docs/mcp-ia-server.md`](../../docs/mcp-ia-server.md) / [`tools/mcp-ia-server/README.md`](../../tools/mcp-ia-server/README.md) if those docs list aliases.
4. Bump expected entry count in [`tools/mcp-ia-server/tests/tools/build-registry.test.ts`](../../tools/mcp-ia-server/tests/tools/build-registry.test.ts) (`assert.equal(r.length, N)`) in same diff — CI registry-completeness gate.
5. Do not place active issue narratives here — use [`BACKLOG.md`](../../BACKLOG.md) + `ia/projects/{ISSUE_ID}.md`.

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

When editing reference specs, prefer **`glossary_discover`** / **`glossary_lookup`** (territory-ia MCP) with English keywords to surface table **Term** before global search-and-replace.

| Avoid in specs (unless defining) | Prefer |
|----------------------------------|--------|
| map edge (play-area boundary) | **Map border** |
| grid edge (when meaning outer playable boundary) | **Map border** |
| generic “road” for committed network tiles | **Street (ordinary road)** or **interstate**, or umbrella **street or interstate** when the rule applies to both |
| informal "validation" / "placement gate" for commit path | **Road validation pipeline** (plus **`PathTerraformPlan`**, Phase-1, `Apply` as needed) |
| "road only" (walkability) | grass + **street**/**interstate** cells (or cite **road stroke** when speaking of drag path) |

**Local geometry:** Use **cell** edge, **Moore**/**cardinal neighbor**, or **shared cardinal edge** — not **map border** — unless cell lies on outer grid boundary.
