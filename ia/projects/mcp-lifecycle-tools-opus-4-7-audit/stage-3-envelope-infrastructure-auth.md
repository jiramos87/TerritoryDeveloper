### Stage 3 — Envelope Foundation (Breaking Cut) / Envelope Infrastructure + Auth


**Status:** Final (2026-04-18)

**Objectives:** Author the `ToolEnvelope<T>` type + `wrapTool()` middleware + `ErrorCode` enum that all 32 handlers will use in Stage 2.2. Author `caller-allowlist.ts` with per-tool map. Both files are the foundation for all remaining stages and steps.

**Exit:**

- `tools/mcp-ia-server/src/envelope.ts` exports `ToolEnvelope<T>`, `EnvelopeMeta`, `ErrorCode`, `wrapTool(handler)`.
- `tools/mcp-ia-server/src/auth/caller-allowlist.ts` exports `checkCaller(tool, caller_agent)` returning `true` or throwing `unauthorized_caller`.
- Unit tests green for both files.
- Phase 1 — Core types + middleware authoring.
- Phase 2 — Unit tests.

**Tasks:**

| Task | Name | Issue | Status | Intent |
| --- | --- | --- | --- | --- |
| T3.1 | Envelope middleware | **TECH-388** | Done (archived) | Author `tools/mcp-ia-server/src/envelope.ts`: `ToolEnvelope<T>` discriminated union (`ok: true, payload: T, meta?` / `ok: false, error: {code, message, hint?, details?}`); `EnvelopeMeta` with `graph_generated_at?`, `graph_stale?`, `partial?: {succeeded, failed}`; `ErrorCode` enum (12 values from §3.1); `wrapTool<I,O>(handler: (input:I)=>Promise<O>)` that catches throws + converts to error envelope. |
| T3.2 | Caller allowlist | **TECH-389** | Done (archived) | Author `tools/mcp-ia-server/src/auth/caller-allowlist.ts`: per-tool allowlist map `Record<string, string[]>` covering all mutation + authorship tools from Steps 3–4 (pre-populate with known callers per §3.8); export `checkCaller(tool: string, caller_agent: string | undefined): void` — throws `{ code: "unauthorized_caller", message, hint }` if caller not in allowlist or allowlist missing `caller_agent`. |
| T3.3 | Envelope unit tests | **TECH-390** | Done (archived) | Tests in `tools/mcp-ia-server/tests/envelope.test.ts`: `wrapTool` happy path (`ok: true, payload`); envelope passthrough (no double-wrap); bare `Error` → `internal_error` (per TECH-388 Decision Log); typed throw `{code: "db_unconfigured", hint?, details?}` preserves code + optional fields; `meta` passthrough. |
| T3.4 | Allowlist unit tests | **TECH-391** | Done (archived) | Tests for `checkCaller`: authorized caller → no throw; unauthorized caller → `unauthorized_caller`; `caller_agent` undefined → `unauthorized_caller`; tool not in map (read-only) → no throw (allowlist only gates mutation/authorship tools; read tools bypass). |

#### §Stage File Plan

> Opus `stage-file-plan` writes structured `{operation, target_path, target_anchor, payload}` tuples here per pending Task. Sonnet `stage-file-apply` reads tuples and materializes BACKLOG rows + spec stubs. Contract: `ia/rules/plan-apply-pair-contract.md`.

_pending — populated by `/stage-file` planner pass._

#### §Plan Fix

> Opus `plan-review` writes targeted fix tuples here when a Stage's Task specs need tightening before first `/implement`. Sonnet `plan-applier` Mode plan-fix reads tuples and applies edits. Contract: `ia/rules/plan-apply-pair-contract.md`.

_pending — populated by `/plan-review` when fixes are needed._

#### §Stage Audit

> Opus `opus-audit` writes one `§Audit` paragraph per Task row here (Stage-scoped bulk, non-pair) once every Task reaches Done post-verify. Feeds `§Stage Closeout Plan` migration tuples downstream. Contract: `ia/rules/plan-apply-pair-contract.md` Stage-scoped non-pair row.

_retroactive-skip — Stage archived prior to 2026-04-24 lifecycle refactor that introduced the canonical `§Stage Audit` subsection (see `docs/MASTER-PLAN-STRUCTURE.md` §3.4 + Changelog entry 2026-04-24). Task-level §Audit prose captured in per-Task specs during Stage-scoped closeout before spec deletion; no retroactive re-run needed._

#### §Stage Closeout Plan

> Opus `stage-closeout-plan` writes unified tuple list here ONCE per Stage when all Task rows reach `Done` post-verify. Sonnet `stage-closeout-apply` reads tuples and applies verbatim. Contract: `ia/rules/plan-apply-pair-contract.md` seam #4.

_pending — populated by `/closeout {{this-doc}} Stage {{N.M}}` planner pass when all Tasks reach `Done`._

---
