# Exploration — Web Dashboard Lifecycle Controls

**Status:** Draft — initial survey

**Author trigger:** user prompt 2026-04-15  
**Prerequisites before implementation:** Step 5 (portal auth) landed; guardrails designed and enforced (see §Guardrails below).

---

## What this is

Idea: add a control surface to the `/dashboard` web page that lets authenticated users trigger and monitor lifecycle skills (`/stage-file`, `/kickoff`, `/implement`, `/verify-loop`, `/closeout`, etc.) against master plan tasks — from the browser, without needing a terminal or Claude Code CLI session.

In effect: a thin web UI layer on top of the agent pipeline that already exists in the repo.

---

## Problem being solved

Currently, every lifecycle action (filing a stage, kicking off a spec, implementing, verifying, closing) requires:
1. An open Claude Code session
2. Manual slash command invocation
3. Monitoring output in the terminal

This is fine for solo dev workflow but creates friction when:
- Quickly checking status and wanting to advance a single task
- Non-CLI collaborators need to trigger routine lifecycle steps
- Need to schedule or queue work across multiple plans

A dashboard control surface could reduce that friction while keeping the pipeline itself unchanged.

---

## Scope constraints (hard)

- **Auth required first.** No lifecycle controls ship before Step 5 (portal auth) is complete. Unauthenticated users must never reach the control surface.
- **Token protection required.** Every trigger must go through a guardrail layer before invoking any Claude API call. See §Guardrails.
- **Read-only dashboard stays separate.** The existing `/dashboard` read-only view is not modified. Controls live in a separate privileged route (e.g., `/dashboard/controls` or `/admin`).
- **Out of scope:** real-time streaming output (websocket/SSE) at initial tier — polling or fire-and-forget acceptable for MVP.

---

## Approaches considered

### Approach A — Direct API route → Claude API invocation
Each button click hits a Next.js route handler (`POST /api/lifecycle/{skill}`); handler calls Claude API with the skill prompt; response polled or streamed back.

**Pros:** simple wiring; no extra infra.  
**Cons:** long-running (skills can take minutes); Vercel Function default timeout 300s may be hit on complex implements; no durable state; easy to spam tokens accidentally.

### Approach B — Job queue + worker (Vercel Queues or similar)
Button click enqueues a job; durable worker calls Claude API; dashboard polls job status.

**Pros:** durable, retry-safe, observable; no timeout risk; natural rate-limiting via queue.  
**Cons:** more infra; Vercel Queues is public beta; adds complexity before auth is even settled.

### Approach C — Webhook + GitHub Actions dispatch
Button click triggers a `workflow_dispatch` on a GitHub Actions workflow that runs `claude-personal --resume` or a fresh skill invocation.

**Pros:** runs in existing CI infra; no Claude API key in web app; audit trail in GH Actions log; natural timeout/cancellation.  
**Cons:** round-trip latency (GH Actions queue); requires GH PAT in Vercel env vars; output surfacing is awkward (need to read GH API for logs).

### Approach D — Read-only trigger + CLI confirmation gate (hybrid)
Dashboard button stages a "pending trigger" record in DB; user still confirms + runs via CLI, but the dashboard pre-populates the command. Essentially a clipboard/intent surface, not full automation.

**Pros:** zero token risk; no new infra; auth still needed to write the intent.  
**Cons:** doesn't actually close the "open terminal" friction loop; feels incomplete.

---

## Guardrails (non-negotiable before any trigger ships)

These must be designed and enforced before any lifecycle button reaches production:

1. **Auth gate** — only authenticated users with an explicit `lifecycle_trigger` entitlement can POST to trigger endpoints. Session check in middleware + DB entitlement row.

2. **Rate limiting** — per-user per-skill budget (e.g., max 10 lifecycle triggers / hour). Enforced at API route level before any Claude API call. Prevents accidental token burn loops.

3. **Allowlist of triggerable skills** — not all skills are web-triggerable. Initial safe set: `/stage-file` (read-heavy, low cost), `/kickoff` (read + enrich), `/verify` (read-only). High-cost skills (`/implement`, `/verify-loop`) gated behind explicit confirmation modal + separate entitlement flag.

4. **Dry-run mode** — every trigger must support a `?dry=true` param that returns what the skill *would* do (plan read + description) without calling Claude API. UI shows dry-run output before confirmation.

5. **Cost estimate gate** — before executing, estimate token cost from skill type + plan size; show to user; require explicit "I understand this will use ~N tokens" confirm for any skill costing >10k estimated tokens.

6. **Audit log** — every trigger (attempted + completed) written to DB `lifecycle_trigger_log` table: user_id, skill, issue_id, timestamp, estimated_tokens, actual_tokens, outcome.

7. **Kill switch** — `LIFECYCLE_CONTROLS_ENABLED=false` env var disables all trigger endpoints instantly without code deploy. Default: disabled. Must be explicitly enabled in Vercel env.

---

## Implementation points (high-level, pre-design)

- **W-LC1 — Auth + entitlement schema extension:** add `lifecycle_trigger` boolean to `entitlement` table (Step 5 schema); add `lifecycle_trigger_log` table.
- **W-LC2 — Guardrail middleware:** rate limiter + entitlement check + kill switch as composable middleware applied to all `/api/lifecycle/*` routes.
- **W-LC3 — Skill adapter layer:** thin server-side adapter per triggerable skill — takes `{ issueId, planPath, dryRun }` → builds skill prompt → calls Claude API → returns structured result.
- **W-LC4 — Cost estimator:** pre-call token estimator based on skill type + file size heuristics (plan markdown length, task count); surfaced to UI before confirmation.
- **W-LC5 — Trigger UI:** `/dashboard/controls` route (auth-gated); per-task action buttons (per allowlist); dry-run preview panel; confirmation modal with cost estimate; job status polling.
- **W-LC6 — Audit log viewer:** `/dashboard/controls/log` — paginated table of trigger history; filter by user / skill / outcome.

---

## Open questions

1. **Which Claude model for web-triggered skills?** Haiku for low-cost read-only (kickoff, verify); Sonnet for implement/verify-loop? Need cost model before guardrail thresholds are set.
2. **Streaming vs polling?** SSE for real-time output would be better UX but adds complexity. Is polling (5s interval, 10-min timeout) sufficient for MVP?
3. **Multi-user?** If multiple users trigger concurrently against the same plan, race conditions on file writes. Need per-plan locks or serialize via queue. Approach B (job queue) handles this naturally.
4. **Output artifact storage?** Claude API responses are ephemeral. Where does the skill output live? Options: DB `text` column, Vercel Blob, GitHub commit (skill writes back to repo). This is the hardest problem — skill output often *is* a repo write (new files, BACKLOG edits).
5. **Repo write-back mechanism?** If skill output modifies repo files, how does web server commit back? Options: GitHub API commits (no local clone needed), or trigger GH Actions workflow that runs the skill in CI context with write access. Approach C handles this naturally.

---

## Recommended next step (when this exploration opens for design)

Start with **Approach C (GitHub Actions dispatch)** for the write-back problem — it sidesteps the repo-write complexity entirely and reuses existing CI infra. Combine with **Approach A** for read-only / dry-run skills that don't write to repo. Decision to confirm in `/design-explore` expansion.

---

## Deferred / out of scope

- Real-time streaming skill output (SSE / WebSocket)
- Multi-user concurrent trigger locks
- Skill chaining (trigger kickoff → auto-trigger implement on success)
- Mobile-optimized control UI
- Non-authenticated public trigger surface (never)
