# TECH-59 — territory-ia MCP: stage Editor export registry payload (issue id + JSON)

> **Issue:** [TECH-59](../../BACKLOG.md)  
> **Status:** Draft  
> **Created:** 2026-04-03  
> **Last updated:** 2026-04-03

**Depends on:** none (soft: **TECH-55** / **TECH-55b** **§ Completed** — [`BACKLOG.md`](../../BACKLOG.md) — for full **Postgres** + **Reports** UX)  
**Related:** **TECH-55** / **TECH-55b** (completed — glossary **Editor export registry**), **TECH-48** (MCP discovery), **TECH-24** (MCP test policy), **TECH-18** (future DB-backed IA)

## 1. Summary

Add a **territory-ia** **MCP** tool that lets an **agent** describe **what JSON** is needed for the **Editor** **Reports** → **Postgres** registry workflow: **normalize** and pass a **`backlog_issue_id`**, attach **one or multiple** **JSON** documents (each tagged by **export kind** or wrapped in a small envelope), and write them to a **repo-local staging file** (**gitignored**). The **human developer** then uses a **Unity** **Editor** menu (**one click**) to **apply** the staged data: set **`EditorPrefs`** (**`TerritoryDeveloper.EditorExportRegistry.BacklogIssueId`**) and optionally kick off registration so **manual typing** in the **Postgres export registry** window is no longer the only agent-friendly path.

**Security:** The MCP tool **must not** accept **`DATABASE_URL`** or passwords; connection config remains **environment** / **EditorPrefs** per **TECH-55b**. **Interchange JSON** and **Save data** boundaries stay per **persistence-system** **Save**.

## 2. Goals and Non-Goals

### 2.1 Goals

1. **MCP tool** (name TBD in **Decision Log**, e.g. `editor_export_registry_stage`) with arguments: at least **`issue_id`** (string, **`normalizeIssueId`** parity with **`backlog-parser.ts`**) and **`documents`** (array of objects, each with **`kind`** + **`json`** or equivalent).
2. **Staging file** under repo (path TBD, **gitignored**), atomic write, optional **TTL** / overwrite policy documented.
3. **Unity** menu: **Apply MCP-staged registry…** (exact label in **Decision Log**) reads staging file, validates schema, applies **EditorPrefs**, shows **Dialog** with summary; optional second button **Register now** if **TECH-55**/**TECH-55b** pipeline supports it without duplicating logic.
4. **English** tool description in **`registerTool`** for **Cursor** agents; update **`docs/mcp-ia-server.md`** and **`tools/mcp-ia-server/README.md`**.
5. **Tests:** **`node:test`** fixture for tool handler (happy path + invalid **`issue_id`** + malformed JSON).

### 2.2 Non-Goals

1. MCP **direct** **Postgres** **INSERT** (keep **Node** / **Unity**-spawned **Node** as today unless a future issue unifies).
2. Replacing **TECH-55b** **DB-first** design — staging composes with either **path-only** or **body** inserts.
3. **Player** runtime or **Save data** changes.

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Agent | I call an MCP tool with **`issue_id`** and one or more **JSON** payloads so the developer does not re-type them. | Tool returns **OK** + staging path; file exists and matches contract. |
| 2 | Developer | I open **Unity** and **apply** staged data with **one** menu action. | **EditorPrefs** **`backlog_issue_id`** updated; **Dialog** confirms. |
| 3 | Maintainer | I want **normalizeIssueId** consistent with **`backlog_issue`** MCP. | Shared helper or duplicated logic with **comment** pointing to **`backlog-parser.ts`**. |

## 4. Current State

### 4.1 Domain behavior

- **`backlog_issue_id`** is **developer-entered** in **Unity** only (**EditorPrefs**); agents have **no** first-class hook (see **Editor export registry** in glossary / **unity-development-context** §10).
- **`register-editor-export.mjs`** expects **`--issue`** from **CLI** or **Unity**-spawned args.

### 4.2 Systems map

| Area | Pointer |
|------|---------|
| **MCP server** | `tools/mcp-ia-server/src/index.ts`, `docs/mcp-ia-server.md` |
| **Parser parity** | `tools/mcp-ia-server/src/parser/backlog-parser.ts` |
| **Unity** | `EditorPostgresExportRegistrar.cs` (**`BacklogIssueIdPrefsKey`**) |
| **Durable trace** | Glossary **Editor export registry**; [`docs/postgres-ia-dev-setup.md`](../../docs/postgres-ia-dev-setup.md); [`BACKLOG.md`](../../BACKLOG.md) **§ Completed** **TECH-55**/**TECH-55b** |

## 5. Proposed Design

### 5.1 Target behavior (product)

**N/A** — tooling only.

### 5.2 Architecture / implementation (agent-owned unless fixed here)

1. **Staging JSON envelope** (illustrative):

```json
{
  "schema_version": 1,
  "artifact": "editor_export_registry_stage",
  "backlog_issue_id": "BUG-37",
  "documents": [
    { "kind": "agent_context", "json": { } },
    { "kind": "terrain_cell_chunk", "json": { "artifact": "terrain_cell_chunk", "schema_version": 1 } }
  ],
  "staged_at_utc": "2026-04-03T12:00:00Z"
}
```

2. **MCP tool** validates: **`issue_id`** non-empty after normalize; **`documents`** length ≥ 1; each **`json`** is object (not string) or parse string — **Decision Log**.
3. **Unity** reads file from **repo root** (same resolution as **`Application.dataPath`** parent); on apply, set **EditorPrefs**; **do not** auto-run **Node** by default (**Editor export registry** is quiet on success — optional **Dialog** “Run registry now?” **Yes/No** per **Decision Log**).

### 5.3 Staging file location (to decide)

- Candidate: **`tools/reports/.mcp-staging/editor-export-registry-pending.json`** (folder **gitignored** alongside **`tools/reports/`** policy) **or** **`.cursor/`** (may be less ideal for Unity). Record final path in **Decision Log** + **`.gitignore`**.

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|------------------------|
| 2026-04-03 | **Issue id TECH-59** | Next free **TECH-** in **Agent** lane; **TECH-55-fix** remains **TECH-55b** umbrella | — |

## 7. Implementation Plan

### Phase 1 — Contract

- [ ] Finalize staging **JSON** shape + **`kind`** enum alignment with **`register-editor-export.mjs`**.
- [ ] Choose staging path + **`.gitignore`** update.

### Phase 2 — MCP

- [ ] Implement tool in **`tools/mcp-ia-server`**; register in **`index.ts`**; **Zod** or manual validation.
- [ ] Unit tests + update **`docs/mcp-ia-server.md`** / **README**.

### Phase 3 — Unity

- [ ] **MenuItem** + reader + **EditorPrefs** apply + user **Dialog**.
- [ ] Optional: invoke existing registrar **Node** script with staged payloads (same **`TryPersistReport`** / **`register-editor-export.mjs`** path as **Editor export registry**).

### Phase 4 — IA

- [ ] **`npm run generate:ia-indexes -- --check`** if tool descriptions feed indexes.
- [ ] Cross-link from **`docs/postgres-ia-dev-setup.md`** (**Editor export registry** section).

## 8. Acceptance Criteria

- [ ] **`backlog_issue`** / **`normalizeIssueId`** rules match staged **`issue_id`** output.
- [ ] MCP tool writes staging file; Unity **Apply** updates **`EditorPostgresExportRegistrar.BacklogIssueIdPrefsKey`**.
- [ ] Staging path **gitignored**; no secrets in tool args.
- [ ] Docs + tests updated; **`npm run test:ia`** green.

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|
| — | … | … | … |

## 10. Lessons Learned

- …

## Open Questions (tooling and agent workflow)

1. **Auto-run Node after apply?** Default **off** vs **on** — noise vs convenience (**TECH-55b** quiet-console goal).
2. **Multiple documents** in one stage: does Unity **register all** sequentially or only set **issue id** + leave exports to menus?
3. **Markdown** (**sorting debug**): stage as UTF-8 string inside JSON vs file path reference only?
4. **Concurrent agents:** last-write-wins vs version field in staging file?

**N/A (game logic)** — no **HeightMap**, **Save data**, or simulation changes.
