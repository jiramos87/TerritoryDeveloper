# Stage 27 — Lighthouse capture (PR appendix)

**Routes (production shell, `localhost:4000` when running `npm run dev:web` or per `web/README.md`):**

| Route | Path |
| --- | --- |
| Landing | `/` |
| Dashboard | `/dashboard` |
| Releases | `/dashboard/releases` |
| Release progress | `/dashboard/releases/full-game-mvp/progress` |

**Baseline (pre–Stage 27 port or named SHA):** fill after running Lighthouse (DevTools or `npx lighthouse`).

| Route | LCP (s) | CLS | TBT (ms) | Notes |
| --- | --- | --- | --- | --- |
| / |  |  |  |  |
| /dashboard |  |  |  |  |
| /dashboard/releases |  |  |  |  |
| /dashboard/releases/full-game-mvp/progress |  |  |  |  |

**Post-port:** repeat on same machine + network. **Caps:** LCP ≤ 1.1× per-route baseline; CLS &lt; 0.1. If a route fails, call out in PR and consider Surface / motion downgrades on that route (master-plan Exit).

**NB-CD2:** Per-screen CD fixture shape vs server loader output — keep schema-diff notes in PR with heatmap/rollup references.
