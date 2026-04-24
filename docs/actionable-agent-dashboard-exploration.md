---
purpose: "Exploration stub — future actionable web dashboard for agent implementation lifecycle. Seeded from master-plan foldering refactor E6. Out of scope for current refactor; persisted for later master-plan authoring."
audience: both
loaded_by: ondemand
slices_via: none
---

# Actionable agent dashboard — exploration

> **Status:** Seed stub. Created 2026-04-24 under `docs/master-plan-foldering-refactor-design.md` §4.4.2 (E6 addendum).
> **Scope split from parent refactor:** parent delivers **read-only** dashboard (E6=a). This doc captures the **actionable** (mutation-capable) dashboard idea for a future exploration pass + master plan.

---

## 1. Core idea

Web dashboard becomes a **first-class mutation client** for agent implementation lifecycle — not just a read view over DB state. Operator triggers `/stage-file`, `/ship`, `/author`, `/closeout` from browser; watches progress stream in real time; reviews spec diffs + code-review verdicts inline; approves / denies gates without leaving the UI.

## 2. Motivation

- **Multi-device.** Operator can drive implementation from laptop / tablet / desktop without terminal.
- **Observability.** Live stream of journal entries + compile gates + verify-loop verdicts + commit hashes — less scrolling through CLI logs.
- **Gate polling UX.** `/design-explore` approach selection + `/plan-digest` ambiguity escalation currently block on CLI prompts. Dashboard renders poll as structured form.
- **Cross-session resume.** Running ship-stage from dashboard persists session → can resume on different device.
- **1000+ issue scale.** Bulk status queries, filters, dependency graphs feel natural in UI vs grep-over-markdown.

## 3. Open design axes (seed, not polled)

- **Skill invocation transport.** Web backend spawns `claude-code` subprocess? Spawns subagent via MCP? Posts to headless agent runner? — architecture TBD.
- **Auth.** Single operator (current) vs multi-user later — session management, permissions.
- **Long-running task UX.** Ship-stage runs 30+ minutes. Browser disconnect tolerance? Reconnect pattern? Background worker + push notifications?
- **Spec editing.** Browser spec editor for §Plan Digest / §Acceptance Criteria? — or keep editing in IDE + dashboard read-only for content, actionable for lifecycle ops only?
- **Fix-plan approval flow.** Code-reviewer critical branch emits fix tuples — dashboard renders diff + approve/deny per-tuple? — or kept inline in skill chain?
- **Test-mode + bridge integration.** Dashboard triggers `/verify-loop` + streams test-mode batch output; Play Mode evidence uploads back into dashboard.
- **Offline / degraded.** When DB is up but Unity editor is down — which mutations block, which queue, which allow?

## 4. Prerequisites (inherit from parent refactor)

Before this scope can activate:

- DB-backed state surface (parent §4.2).
- MCP mutation tools complete (parent §4.3).
- Next.js API routes (parent E7).
- `ia_ship_stage_journal` with streaming-capable reads (parent E5).
- Read-only dashboard as baseline (parent E6=a).

## 5. Handoff

When parent refactor ships and read-only dashboard is stable:

1. Run `/design-explore docs/actionable-agent-dashboard-exploration.md` to expand this stub.
2. Persist `## Design Expansion` block with approach comparisons (subprocess vs MCP vs headless runner).
3. Run `/master-plan-new` once approach locked.

## 6. Change log

- **2026-04-24** — Stub seeded from master-plan foldering refactor E6 addendum. Deferred until parent refactor ships.
