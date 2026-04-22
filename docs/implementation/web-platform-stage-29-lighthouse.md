# Stage 29 — Lighthouse landing (NB3)

**When:** 2026-04-22. **Target:** `http://127.0.0.1:4000/` (dev server on port 4000). **Tool:** `npx lighthouse` (performance category).

| Metric    | Value | Notes |
| --------- | ----- | ----- |
| LCP (s)   | 6.39  | T27.7 `/` baseline cell empty in `docs/implementation/web-platform-stage-27-lighthouse.md` — LCP cap (baseline × 1.1) not computable until that row is filled |
| CLS       | 0     | Under 0.1 cap |
| TBT (ms)  | 142   | |

**Surface `motion` remediation:** No `Surface` usage under `web/components/landing/**` or `web/app/dashboard/**` (grep 2026-04-22) — no landing/dashboard Surface toggles. Showcase-only `Surface` lives under `web/app/(dev)/design-system/`.
