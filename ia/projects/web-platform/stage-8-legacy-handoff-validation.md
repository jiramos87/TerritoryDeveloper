### Stage 8 — Live dashboard / Legacy handoff + validation


**Status:** Done — TECH-213 closed 2026-04-15 (archived); TECH-214 closed 2026-04-15 (archived). Stage 3.3 exit criteria met; Step 3 closed.

**Objectives:** Wire the `docs/progress.html` "Live dashboard" link to the Vercel deploy URL, run end-to-end smoke confirming the dashboard works in production, and author the Decision Log entry for the `docs/progress.html` deprecation trigger.

**Exit:**

- `docs/progress.html` has a visible "Live dashboard →" banner at top linking to `https://web-nine-wheat-35.vercel.app/dashboard`.
- End-to-end smoke: `/dashboard` returns 200 on Vercel deploy; filter chips modify URL params + re-render; "internal" banner visible; Vercel-served `robots.txt` disallows the route.
- §Decision Log section added to this master plan below Orchestration guardrails, documenting the `docs/progress.html` deprecation trigger (proposed: ≥2 stable deploy cycles after Step 5 portal auth gate lands).
- Phase 1 — Legacy link + E2E smoke + deprecation decision log.

**Tasks:**

| Task | Issue | Status | Intent |
| --- | --- | --- | --- |
| T8.1 | **TECH-213** (archived) | Done | Edit `docs/progress.html` — insert "Live dashboard →" banner `<div>` at top of `<body>` linking to `https://web-nine-wheat-35.vercel.app/dashboard`; minimal inline style consistent with existing page aesthetic (no external CSS added). |
| T8.2 | **TECH-214** (archived) | Done | End-to-end smoke + deprecation decision log — manually confirm Vercel `/dashboard` returns 200, filter chips functional, "internal" banner visible, `robots.txt` disallows route; append §Decision Log section to this master plan below Orchestration guardrails documenting `docs/progress.html` deprecation trigger (proposed: ≥2 stable deploy cycles post Step 5 portal-auth gate). |

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
