---
purpose: "TECH-254 — Postgres driver install + web/lib/db/client.ts + Vercel DATABASE_URL wiring."
audience: both
loaded_by: ondemand
slices_via: none
---
# TECH-254 — Postgres driver install + `web/lib/db/client.ts` + Vercel `DATABASE_URL` wiring

> **Issue:** [TECH-254](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-16
> **Last updated:** 2026-04-16

## 1. Summary

Install driver matching TECH-252 provider choice; author connection pool wrapper — lazy-connect, no open at build time; exports typed `db` / `sql` handle for future Stage 5.2 schema consumers. Wire `DATABASE_URL` into Vercel env (dashboard or `vercel env add`). No migrations run at this tier.

## 7. Implementation Plan

- [ ] Install Postgres driver per TECH-252 provider decision.
- [ ] Author `web/lib/db/client.ts` with lazy-connect pool wrapper.
- [ ] Wire `DATABASE_URL` into Vercel env for production + preview + development.
- [ ] `npm run validate:all` exit 0.
