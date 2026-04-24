### Stage 1.4 — MCP `catalog_*` tools + allowlist

**Status:** Final

**Objectives:** Expose catalog operations as **typed MCP tools**; update **`caller-allowlist.ts`** for mutation classes per repo policy.

**Exit:**

- MCP server lists new tools; package tests cover happy + error paths.
- Docs snippet in `docs/mcp-ia-server.md` updated if required by validators.

**Tasks:**

| Task | Name | Issue | Status | Intent |
|---|---|---|---|---|
| T1.4.1 | catalog_list + catalog_get | **TECH-650** | Done | Thin wrappers over HTTP or shared DB layer; enforce **published** default for agents unless flag set. |
| T1.4.2 | catalog_upsert + pool tools | **TECH-651** | Done | Implement **`catalog_upsert`** + minimal **`catalog_pool_*`** per §8.3; validate payloads server-side. |
| T1.4.3 | MCP unit tests | **TECH-652** | Done | Extend `tools/mcp-ia-server` tests with fixture DB or mocked fetch; cover dry-run flags if exposed here. |
| T1.4.4 | caller-allowlist updates | **TECH-653** | Done | Edit `caller-allowlist.ts` — classify create/update vs delete guarded; follow existing TECH-506 patterns. |
| T1.4.5 | Doc touch + validate:all | **TECH-654** | Done | Update human MCP catalog if CI requires; run **`npm run validate:all`** green. |

#### §Stage File Plan

_retroactive-skip — Stage archived prior to 2026-04-24 lifecycle refactor that introduced the canonical `§Stage File Plan` pair seam (see `ia/rules/plan-apply-pair-contract.md`). Tasks TECH-650–TECH-654 filed inline under pre-refactor flow; see archived yaml in `ia/backlog-archive/`._

#### §Plan Fix

_retroactive-skip — pre-refactor Stage; no `plan-review` sweep persisted inline._

#### §Stage Audit

_retroactive-skip — Stage archived prior to 2026-04-24 lifecycle refactor that introduced the canonical `§Stage Audit` subsection (see `ia/projects/MASTER-PLAN-STRUCTURE.md` §3.4 + Changelog entry 2026-04-24)._

#### §Stage Closeout Plan

_retroactive-skip — Stage closed inline under pre-refactor per-Task closeout flow; task rows flipped to Done, specs deleted, archive yaml in `ia/backlog-archive/TECH-650`–`TECH-654`._
