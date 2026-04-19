---
purpose: "TECH-494 — Token-cost telemetry baseline — pre/post lifecycle refactor + Q9 pair-head read-count."
audience: both
loaded_by: ondemand
slices_via: none
parent_plan: "ia/projects/lifecycle-refactor-master-plan.md"
task_key: "T9.4"
---
# TECH-494 — Token-cost telemetry baseline + Q9 pair-head read-count

> **Issue:** [TECH-494](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-19
> **Last updated:** 2026-04-19

## 1. Summary

File Stage 10 precondition tracker. Collect per-Stage token + cache usage across ≥3 post-merge Stages. Feeds Stage 10 T10.1 gate: if pair-head reads < 3/Stage on all sampled Stages, Stage 10 REJECTED (prompt-cache P1 economics not viable).

## 2. Goals and Non-Goals

### 2.1 Goals
1. Collector instruments every agent dispatch; sums tokens + cache hits per Stage.
2. Baseline report covers ≥3 post-merge Stages (any open master plan).
3. Stage 10 T10.1 precondition gate consumes collector output + emits GO/REJECTED.

### 2.2 Non-Goals
1. Implementing the prompt-cache layer itself (Stage 10 scope).
2. Back-filling telemetry for pre-merge Stages (baseline starts post-merge).
3. Per-agent cost attribution UI (raw JSON dump sufficient for gate).

## 3. Scope — required instrumentation

(a) Total prompt tokens per Stage — sum of `usage.input_tokens` + `usage.output_tokens` across every dispatch tied to the Stage.

(b) Pair-head read count per Stage — distinct per-file reads issued by Opus planners (plan-author, opus-auditor, master-plan-new, stage-file-planner, etc.). Each cache-hit read counted separately. Precondition for Stage 10 P1 per `docs/prompt-caching-mechanics.md` §4 R5. Stage 10 gate: ≥3 reads/Stage on ≥3 sampled Stages → GO; otherwise REJECTED.

(c) Cache-write / cache-read / cache-miss token counts — from Anthropic response `usage.cache_creation_input_tokens` + `usage.cache_read_input_tokens`. Missing field → 0 (not crash).

(d) Per-Stage bundle byte + token size — validates F2 sizing gate per lifecycle-refactor rev 4 C1/R2. Bundle = union of spec slices + rules loaded by the Stage's agents.

## 4. Current State

### 4.2 Systems map
- `tools/mcp-ia-server/src/` — possible collector host (MCP tool that aggregates dispatch metadata).
- Anthropic SDK response `usage` object — primary data source.
- `ia/state/token-telemetry/{stage-id}.json` — output sink (new directory).
- `docs/prompt-caching-mechanics.md` §4 R5 — read-count semantics source.
- `ia/projects/lifecycle-refactor-master-plan.md` Stage 10 T10.1 — precondition-gate consumer.

## 5. Implementation notes

- Collector should be passive: never blocks dispatch on telemetry write failure.
- Per-Stage aggregation key = `{master-plan-slug}/{stage-id}`.
- JSON output shape: `{stage_id, total_input_tokens, total_output_tokens, pair_head_reads, cache_creation_tokens, cache_read_tokens, bundle_bytes, bundle_tokens, dispatches: [...]}`.

## Open Questions

- Exact host surface for the collector (MCP tool vs post-hoc log scraper) — defer to implementer.
- Whether to emit per-dispatch trace (useful for debugging, adds storage) — implementer choice.
