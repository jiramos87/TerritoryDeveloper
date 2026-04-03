# TECH-50 — Doc hygiene: cascade references when project specs close

> **Issue:** [TECH-50](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-03
> **Last updated:** 2026-04-03

## 1. Summary

**Project specs** under `.cursor/projects/{ISSUE_ID}.md` are **temporary**. After verified completion they are **deleted** and lessons migrate to canonical docs ([**PROJECT-SPEC-STRUCTURE**](PROJECT-SPEC-STRUCTURE.md), **AGENTS.md**). **Durable** prose must not keep **markdown links** or “see `.cursor/projects/FOO.md`” pointers to files that no longer exist. The **durable** anchor for closure history is the **issue** row in **`BACKLOG.md`** (or **`BACKLOG-ARCHIVE.md`**).

This issue delivers automation (script, optional **territory-ia** MCP tool) and **Information Architecture** checklist updates so closing a spec triggers **cascade** cleanup of stale paths across the repo.

## 2. Goals and Non-Goals

### 2.1 Goals

1. A **script** (Node preferred for consistency with **TECH-30** / MCP repo) that finds references to `.cursor/projects/*.md` paths that **do not exist** on disk (markdown links, backtick paths, optional **`Spec:`** lines in **BACKLOG** pointing at missing files).
2. Documented **`npm run`** (or equivalent) and optional **CI** integration (fail vs advisory — **Decision Log**).
3. **PROJECT-SPEC-STRUCTURE** + **AGENTS.md** updated with a **closeout** checklist: replace spec paths with **`BACKLOG.md`** / issue id references in durable docs.
4. **Optional:** **territory-ia** MCP tool exposing the same check for agent sessions; documented in **`docs/mcp-ia-server.md`** if shipped.

### 2.2 Non-Goals

1. Removing **issue id** mentions (**TECH-49**, etc.) from skills or docs — those are **intended** for **backlog** traceability.
2. Validating external URLs.
3. Rewriting **game** or **Unity** code.

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Maintainer | I want CI (or a one-command check) to fail when someone links to a deleted project spec. | Script exits non-zero on dead `.cursor/projects/*.md` targets (or advisory mode documented). |
| 2 | Agent | I want to query stale spec paths without memorizing `rg` patterns. | Optional MCP tool returns structured hits (or doc points to `npm run`). |
| 3 | Author | I want a written closeout rule so I know to cite **BACKLOG** instead of deleted specs. | **PROJECT-SPEC-STRUCTURE** + **AGENTS** include the policy. |

## 4. Current State

### 4.1 Domain behavior

N/A — **tooling** only.

### 4.2 Systems map

| Area | Pointer |
|------|---------|
| Project spec lifecycle | [PROJECT-SPEC-STRUCTURE.md](PROJECT-SPEC-STRUCTURE.md), **AGENTS.md** `.cursor/projects/` policy |
| Related backlog | **TECH-30** — issue ids **inside** active `.cursor/projects/*.md` |
| MCP | `tools/mcp-ia-server/`, `docs/mcp-ia-server.md` |
| Priority tasks index | `docs/agent-tooling-verification-priority-tasks.md` (add row if useful) |

### 4.3 Implementation investigation notes

- **Scope:** Scan paths under repo root: `.cursor/`, `docs/`, `AGENTS.md`, `BACKLOG.md`, `projects/`, etc. Exclude `node_modules`, `.git`, optional large trees via allowlist.
- **Merge with TECH-30:** If **TECH-30** ships first, extend it or add `packages/doc-hygiene/` shared by script + MCP instead of a second standalone script.

## 5. Proposed Design

### 5.1 Target behavior (product)

N/A.

### 5.2 Architecture / implementation (agent-owned)

- Regex or markdown-aware scan for `.cursor/projects/[A-Z]+-\d+\.md` (and variants) → `fs.existsSync` relative to repo root.
- Report file + line; group by missing target path.
- Optional MCP: `registerTool` e.g. `doc_dead_project_spec_links` with `REPO_ROOT` (pattern from existing server).

### 5.3 Method / algorithm notes

Optional: also flag **`Spec: .cursor/projects/MISSING.md`** in **BACKLOG** open sections (completed rows may legitimately say “removed after closure”).

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|------------------------|
| 2026-04-03 | New id **TECH-50** | Next free **TECH-** in **BACKLOG** at creation time. | Fold into **TECH-30** only (rejected — scope differs: repo-wide dead paths vs ids in active specs) |

## 7. Implementation Plan

### Phase 1 — Script + docs policy

- [ ] Implement scanner + documented `npm run` (location TBD: `tools/` vs extend **TECH-30**).
- [ ] Update **PROJECT-SPEC-STRUCTURE** (On completion / lifecycle) and **AGENTS.md** (project spec policy) with **closeout** checklist.
- [ ] Cross-link **TECH-30** in implementation notes or shared module.

### Phase 2 — CI and optional MCP

- [ ] Wire CI (or document “run locally before merge”) — severity per **Decision Log**.
- [ ] If MCP tool shipped: `docs/mcp-ia-server.md`, `tools/mcp-ia-server/README.md`, **`npm run verify`**.

## 8. Acceptance Criteria

- [ ] Dead `.cursor/projects/*.md` link detection merged and documented.
- [ ] **PROJECT-SPEC-STRUCTURE** + **AGENTS** state: durable refs → **BACKLOG** / archive, not deleted spec paths.
- [ ] **TECH-30** relationship documented (merge or separate — no silent duplication).
- [ ] Optional MCP + verify only if Phase 2 ships.

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|
| — | — | — | — |

## 10. Lessons Learned

- *(At project close; migrate if conventions change.)*

## Open Questions (resolve before / during implementation)

None — tooling only; see **§8** and **PROJECT-SPEC-STRUCTURE** for tooling-only **Open Questions** policy.
