---
purpose: "TECH-214 — Dashboard E2E smoke + progress.html deprecation decision log."
audience: both
loaded_by: ondemand
slices_via: none
---
# TECH-214 — Dashboard E2E smoke + `progress.html` deprecation decision log

> **Issue:** [TECH-214](../../BACKLOG.md)
> **Status:** In Review
> **Created:** 2026-04-15
> **Last updated:** 2026-04-15

## 1. Summary

Stage 3.3 Phase 1 task (T3.3.2). Manual end-to-end smoke of `/dashboard` on Vercel deploy (HTTP 200, filter chips, internal banner, `robots.txt` disallow) + append row to existing `## Orchestrator Decision Log` table in `ia/projects/web-platform-master-plan.md` (line ~522, below Orchestration guardrails) documenting deprecation trigger for legacy `docs/progress.html`. Closes Stage 3.3 exit criteria. Tooling + docs only — no runtime C#, no subsystem touch.

## 2. Goals and Non-Goals

### 2.1 Goals

1. Manual smoke on `https://web-nine-wheat-35.vercel.app/dashboard`: HTTP 200; filter chips `?plan=` / `?status=` / `?phase=` modify URL + re-render; "internal" banner visible; `/robots.txt` contains `Disallow: /dashboard`.
2. Append §Decision Log row to `ia/projects/web-platform-master-plan.md` (below Orchestration guardrails or into existing Orchestrator Decision Log table) stating `docs/progress.html` deprecation trigger.
3. Proposed trigger text: "Deprecate `docs/progress.html` ≥2 stable deploy cycles after Step 4 portal-auth gate lands and `/dashboard` moves behind auth middleware."

### 2.2 Non-Goals

1. Do NOT delete `docs/progress.html` now — deprecation is trigger-gated.
2. No Playwright/automated E2E — that surface is Step 5 (TECH-215+ when Stage 5.1 files).
3. No Vercel env var changes.

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Orchestrator agent | Want documented trigger condition before deprecating legacy surface so closure decision is auditable | Decision Log row contains date + decision + rationale + alternatives |
| 2 | Dev | Want live `/dashboard` smoke before Stage 3.3 closes so prod regressions caught early | Smoke checklist ticked inside this spec |

## 4. Current State

### 4.1 Domain behavior

`/dashboard` shipped Stage 3.2 (TECH-205…TECH-208). Vercel deploys on `main` push. No manual smoke record yet; no deprecation plan documented for `docs/progress.html`.

### 4.2 Systems map

- `https://web-nine-wheat-35.vercel.app/dashboard` — target URL.
- `web/app/dashboard/page.tsx` — RSC (do not modify).
- `web/app/robots.ts` — contains `/dashboard` disallow (verify Vercel-served output matches).
- `ia/projects/web-platform-master-plan.md` — Decision Log table target.
- `docs/progress.html` — subject of deprecation trigger (no edit this task; banner landed via archived TECH-213).

## 5. Proposed Design

### 5.1 Target behavior

Smoke results captured in §9 "Issues Found During Development" table of this spec (empty rows if clean pass; otherwise one row per anomaly). Orchestrator Decision Log row persists trigger for future agents deciding when `docs/progress.html` can be deleted.

### 5.2 Architecture / implementation

Manual `curl` + browser checks against Vercel deploy `https://web-nine-wheat-35.vercel.app`. Orchestrator edit — append one row to existing `## Orchestrator Decision Log` table (line ~522 of `ia/projects/web-platform-master-plan.md`, already below `## Orchestration guardrails`). No new section — table already exists.

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|------------------------|

## 7. Implementation Plan

### Phase 1 — Live Vercel smoke

- [x] `curl -I https://web-nine-wheat-35.vercel.app/dashboard` — expect `HTTP/2 200`. Capture status line into §9 if non-200. **RESULT: HTTP/2 404 — anomaly logged in §9.**
- [ ] `curl -s https://web-nine-wheat-35.vercel.app/robots.txt | grep -i '^Disallow: /dashboard'` — expect match. **BLOCKED: deploy prerequisite unmet.**
- [ ] Browser open `/dashboard` — verify: **BLOCKED: deploy prerequisite unmet.**
  - [ ] "internal" banner visible at top (language per archived TECH-213).
  - [ ] Click filter chip `?plan=blip` — URL updates, row set narrows.
  - [ ] Click filter chip `?status=done` — URL updates, row set narrows.
  - [ ] Click filter chip `?phase=1` — URL updates, row set narrows.
  - [ ] Combined filters compose (AND semantics) — rows reflect intersection.

### Phase 2 — Orchestrator Decision Log append

- [x] Edit `ia/projects/web-platform-master-plan.md` — append row to existing `## Orchestrator Decision Log` table (do NOT create new section).
- [ ] Flip T3.3.2 Status `Draft` → `Done` in Stage 3.3 task table (line ~368). **DEFERRED: smoke blocked (HTTP/2 404); status stays `In Review` until deploy + re-smoke.**

### Phase 3 — Validation

- [x] `npm run validate:all` green. Exit 0.
- [x] `grep -c 'Deprecate .docs/progress.html.' ia/projects/web-platform-master-plan.md` — expect ≥1. **Result: 1.**

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| `/dashboard` returns 200 | Manual (curl) | `curl -I https://web-nine-wheat-35.vercel.app/dashboard` | Status line pasted into §9 only if non-200 |
| `robots.txt` disallows `/dashboard` | Manual (curl) | `curl -s .../robots.txt \| grep -i '^Disallow: /dashboard'` | Exit 0 required |
| Filter chips modify URL + narrow rows | Manual (browser) | Click `?plan=` / `?status=` / `?phase=` chips | Record any regression in §9 |
| Internal banner visible | Manual (browser) | Visual check top of `/dashboard` | Language per archived TECH-213 |
| Orchestrator Decision Log row appended | Node (grep) | `grep -c 'Deprecate .docs/progress.html.' ia/projects/web-platform-master-plan.md` | Expect ≥1 |
| IA indexes still green after edit | Node | `npm run validate:all` | Dead-project-specs + frontmatter validators pass |

## 8. Acceptance Criteria

- [ ] `/dashboard` returns HTTP 200 on Vercel deploy.
- [ ] `robots.txt` contains `Disallow: /dashboard` on Vercel deploy.
- [ ] Filter chips (`?plan=` / `?status=` / `?phase=`) modify URL params + narrow rendered rows.
- [ ] Internal banner visible at top of `/dashboard`.
- [ ] `ia/projects/web-platform-master-plan.md` `## Orchestrator Decision Log` table has new row with date + decision + rationale + alternatives for `docs/progress.html` deprecation trigger.
- [ ] Stage 3.3 T3.3.2 status flipped `Draft` → `Done` in orchestrator task table.
- [ ] `npm run validate:all` green.

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|
| 1 | `curl -I https://web-nine-wheat-35.vercel.app/dashboard` → `HTTP/2 404`; `robots.txt` has no `/dashboard` disallow | `web/app/dashboard/` and all Stage 3.2 web files (`plan-loader`, `FilterChips`, `devlog`, etc.) are untracked (`??`) in git — never committed, never deployed to Vercel | Dashboard smoke blocked until Stage 3.2 web files committed + deployed. Browser sub-checks (filter chips, banner) and `robots.txt` disallow check also blocked. Decision Log row appended per spec (trigger is future-gated); T3.3.2 status kept `In Review` until smoke passes. Follow-up: commit web files → Vercel deploy → re-run smoke. |

## 10. Lessons Learned

- …

## Open Questions

None — tooling + docs only; see §8 Acceptance criteria.
