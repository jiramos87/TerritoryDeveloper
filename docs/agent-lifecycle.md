# Agent lifecycle — canonical flow

Single canonical map for the `.claude/agents/` + `.claude/commands/` + `ia/skills/` surface. Names one entry point per stage, defines the handoff each stage owes the next, and points at the authoritative rule / policy for every decision.

Thin anchor (always-loaded): [`ia/rules/agent-lifecycle.md`](../ia/rules/agent-lifecycle.md). Verification policy (canonical): [`docs/agent-led-verification-policy.md`](agent-led-verification-policy.md). Project hierarchy: [`ia/rules/project-hierarchy.md`](../ia/rules/project-hierarchy.md). Orchestrator vs project spec: [`ia/rules/orchestrator-vs-spec.md`](../ia/rules/orchestrator-vs-spec.md).

---

## 1. End-to-end flow

Not every issue visits every stage. Small one-shot fixes skip exploration + orchestration and enter at `/project-new`. Larger multi-step programs start at `/design-explore`.

```
exploration                orchestration                 execution                               close
───────────                ─────────────                 ─────────                               ─────
/design-explore            master-plan-new (skill)       /stage-file                             project-stage-close (skill)
  docs/{slug}.md     ───→    ia/projects/          ───→    bulk-file one stage's tasks     ──┐    (per non-final stage of
  + ## Design                {slug}-master-plan.md         → many BACKLOG rows + project    │    multi-stage spec)
  Expansion block            (orchestrator — permanent)    spec stubs                        │
                                                                                             │
                                                         /project-new                        ├──→ /closeout
                                                           single BACKLOG row +              │    umbrella close
                                                           ia/projects/{ISSUE_ID}.md         │    (migrate lessons →
                                                                                             │    delete spec →
                                                         /kickoff  ──→  /implement  ──┐      │    archive row →
                                                           enrich §1–§10   execute     │      │    purge id)
                                                                         phases        │      │
                                                                                       ↓      │
                                                                            /verify-loop  ────┘
                                                                            closed-loop
                                                                            (preflight → compile →
                                                                            validate:all → Path A/B →
                                                                            bounded fix iteration)
```

Ad-hoc lanes (invoked outside the main flow, not ordered):

- `/verify` — lightweight single-pass Verification block (no fix iteration). Use between phases when `/verify-loop` is overkill.
- `/testmode` — standalone test-mode batch / bridge hybrid loop. Called ad-hoc or composed by `/verify-loop`.

---

## 2. Stage → surface matrix

| # | Lifecycle stage | Slash command | Subagent (`.claude/agents/`) | Skill (`ia/skills/`) | Primary output | Hands off to |
|---|-----------------|---------------|------------------------------|----------------------|----------------|--------------|
| 1 | Explore | `/design-explore {DOC_PATH}` | `design-explore.md` | `design-explore/` | `docs/{slug}.md` with `## Design Expansion` persisted | `/master-plan-new` (multi-step) or `/project-new` (single issue) |
| 2 | Orchestrate | *(skill only — invoke via `Skill` tool)* | — | `master-plan-new/` | `ia/projects/{slug}-master-plan.md` orchestrator (permanent, NOT closeable) | `/stage-file {slug}-master-plan.md Stage 1.1` |
| 3 | Bulk-file stage | `/stage-file {PATH} {STAGE}` | `stage-file.md` | `stage-file/` | N BACKLOG rows + N `ia/projects/{ISSUE_ID}.md` stubs (one per `_pending_` task) | `/kickoff {ISSUE_ID}` per filed task |
| 4 | Single issue | `/project-new {intent} [--type ...]` | `project-new.md` | `project-new/` | One BACKLOG row + one `ia/projects/{ISSUE_ID}.md` stub | `/kickoff {ISSUE_ID}` |
| 5 | Refine | `/kickoff {ISSUE_ID}` | `spec-kickoff.md` | `project-spec-kickoff/` | Enriched `ia/projects/{ISSUE_ID}.md` §1–§10 | `/implement {ISSUE_ID}` |
| 6 | Implement | `/implement {ISSUE_ID}` | `spec-implementer.md` | `project-spec-implement/` | Code changes + per-phase spec updates (Decision Log / Issues Found / Lessons) | `/verify-loop {ISSUE_ID}` (or `/verify` between phases) |
| 7 | Verify (closed-loop) | `/verify-loop {ISSUE_ID}` | `verify-loop.md` | `verify-loop/` | JSON Verification block + caveman summary; bounded fix iteration (`MAX_ITERATIONS=2`) | human QA or `/project-stage-close` / `/closeout` |
| 7a | Verify (single-pass) | `/verify` | `verifier.md` | *(composes `project-implementation-validation`, `agent-test-mode-verify`, `ide-bridge-evidence`)* | JSON Verification block (no fix iteration) | same handoff shape as `/verify-loop` |
| 7b | Test-mode ad-hoc | `/testmode {SCENARIO_ID}` | `test-mode-loop.md` | `agent-test-mode-verify/` | `tools/reports/agent-testmode-batch-*.json` | any verify stage |
| 8 | Close stage | *(skill only)* | — | `project-stage-close/` | Stage §7 ticked, §6 / §9 / §10 appended, handoff prompt for next stage's fresh agent | next stage's `/stage-file` or the stage's `/implement` |
| 9 | Close issue (umbrella) | `/closeout {ISSUE_ID}` | `closeout.md` | `project-spec-close/` | Lessons migrated to durable IA → spec deleted → BACKLOG row moved to `BACKLOG-ARCHIVE.md` → id purged | next issue |

Skills without slash commands (`master-plan-new`, `project-stage-close`, plus the verification building blocks `bridge-environment-preflight`, `project-implementation-validation`, `agent-test-mode-verify`, `ide-bridge-evidence`, `close-dev-loop`) are invoked via the `Skill` tool or composed by a higher-level agent (e.g. `/verify-loop`).

---

## 3. Handoff contract

Every stage owes the next one a concrete artifact. Missing artifact = the next stage refuses to start.

| From | Owes | To | Refuses when missing |
|------|------|----|----------------------|
| `/design-explore` | `## Design Expansion` block persisted in `docs/{slug}.md` | `master-plan-new` skill | Skill refuses authoring if expansion block absent |
| `master-plan-new` | `ia/projects/{slug}-master-plan.md` with `_pending_` task seeds + cardinality gate (≥2 tasks/phase) cleared | `/stage-file` | Stage-file refuses when tasks missing or cardinality unjustified |
| `/stage-file` | BACKLOG rows + project spec stubs, orchestrator table rows updated from `_pending_` → issue id | `/kickoff` per filed issue | Kickoff refuses when spec stub missing |
| `/project-new` | One BACKLOG row in correct priority section + one template-seeded `ia/projects/{ISSUE_ID}.md` + `validate:dead-project-specs` green | `/kickoff` | Kickoff refuses bare stub without §1 / §2 context |
| `/kickoff` | §1–§10 enriched (Open Questions resolved or flagged, Implementation Plan concrete) | `/implement` | Implement refuses when Implementation Plan still `_pending_` |
| `/implement` | Phase code committed, compile clean, spec §6 / §9 / §10 appended per phase | `/verify-loop` | Verify-loop refuses when compile gate fails (Step 1) |
| `/verify-loop` | JSON Verification block with `verdict: pass` + `human_ask` filled | `/project-stage-close` or `/closeout` | Close refuses without a `pass` verdict |
| `project-stage-close` | Stage handoff prompt for next stage's fresh agent | `/stage-file` of next stage OR `/implement` of same spec | Next agent refuses without handoff block |
| `/closeout` | Lessons migrated to durable IA, spec deleted, row archived, id purged | — | — (terminal) |

Verification policy contract: [`docs/agent-led-verification-policy.md`](agent-led-verification-policy.md).

---

## 4. Decision tree — which command do I run right now?

```
Question                                                         → Command
────────                                                         ─────────
Fuzzy idea, no doc yet?                                          → none — write docs/{slug}.md yourself first
Exploration doc exists, needs to become a design?                → /design-explore
Design persisted, multi-step work with step > stage > phase?     → master-plan-new (Skill tool)
Design persisted, single issue is enough?                        → /project-new
Orchestrator exists, a stage is ready to materialize?            → /stage-file
Bare BACKLOG row + stub spec, no §1–§10 yet?                     → /kickoff
Spec fully enriched, ready to ship?                              → /implement
Phase just landed, want a quick sanity pass?                     → /verify
Phase / stage / spec done, need full closed-loop + fix iter?     → /verify-loop
Bridge / batch evidence needed in isolation?                     → /testmode
Multi-stage spec, current stage done, next stage pending?        → project-stage-close (Skill tool)
Issue verified pass, ready to migrate lessons + delete spec?     → /closeout
```

---

## 5. Verification split — `/verify` vs `/verify-loop`

| Aspect | `/verify` | `/verify-loop` |
|--------|-----------|----------------|
| Scope | Single pass | Closed-loop (7 steps) |
| Code edits | None (read-only reporter) | Narrow: Step 6 fix iteration only |
| Fix iteration | — | Bounded `MAX_ITERATIONS` (default 2) |
| Output style | `verification-report` (JSON + caveman) | Same shape + `fix_iterations` / `verdict` / `human_ask` fields |
| When | Between phases, pre-PR sanity check | Pre-stage-close / pre-umbrella-close / post-substantive-phase |
| Composes | `validate:all` + compile gate + Path A OR Path B | `bridge-environment-preflight` + `project-implementation-validation` + `agent-test-mode-verify` + `ide-bridge-evidence` + `close-dev-loop` |

Both defer to the single canonical policy [`docs/agent-led-verification-policy.md`](agent-led-verification-policy.md) for timeout escalation, Path A lock release, and Path B preflight. Neither agent restates the policy.

---

## 6. Close split — stage close vs umbrella close

| Aspect | `project-stage-close` (skill) | `/closeout` (umbrella) |
|--------|-------------------------------|------------------------|
| Fires | End of each non-final stage of a multi-stage spec | Issue verified pass, ready to archive |
| Touches | Stage's §7 checklist, §6 / §9 / §10 append, optional Postgres journal, handoff prompt | Full spec lifecycle: lessons migration → spec deletion → BACKLOG row move → id purge |
| Deletes spec? | No — spec lives on for next stage | Yes — spec file deleted after lessons migrated |
| Touches BACKLOG? | No | Yes — row moved to `BACKLOG-ARCHIVE.md` |
| Confirmation gate? | No | No (gate removed per TECH-88) |

Stage close is orchestrator-driven; umbrella close is BACKLOG-id-driven. They never fire against the same target in the same session.

---

## 7. Re-entry and partial completion

Each stage is idempotent against its own output: running `/kickoff` twice on an already-enriched spec reviews instead of rewriting; running `/verify-loop` twice on an already-green branch re-emits the Verification block without fix iteration.

Resume rule: on returning to a paused issue, run `/verify` first to re-establish branch state, then pick up at the stage after the last green handoff artifact.

Never reuse retired ids. The monotonic-per-prefix rule ([`AGENTS.md` §7](../AGENTS.md)) holds across BACKLOG + BACKLOG-ARCHIVE.

---

## 8. Adding a new agent / command / skill

New stage proposal → update:

1. This doc (§1 flow diagram + §2 matrix + §3 handoff row + §4 decision tree row).
2. [`ia/rules/agent-lifecycle.md`](../ia/rules/agent-lifecycle.md) — thin always-loaded pointer.
3. [`AGENTS.md`](../AGENTS.md) §2 — one-line row pointing at this doc.
4. [`ia/skills/README.md`](../ia/skills/README.md) — skill index row if a new skill.
5. [`docs/information-architecture-overview.md`](information-architecture-overview.md) §3 + §7 — if the change affects the Knowledge lifecycle or Skill system tables.

Subagent authoring conventions (Opus vs Sonnet, `reasoning_effort`, caveman directive, forwarded `caveman:caveman` preamble): [`CLAUDE.md`](../CLAUDE.md) §3. Slash command dispatcher shape: mirror an existing `.claude/commands/*.md` (verbatim subagent prompt forwarded via Agent tool with `subagent_type`).

---

## 9. Crosslinks

- [`ia/rules/agent-lifecycle.md`](../ia/rules/agent-lifecycle.md) — always-loaded anchor.
- [`ia/rules/project-hierarchy.md`](../ia/rules/project-hierarchy.md) — step > stage > phase > task semantics.
- [`ia/rules/orchestrator-vs-spec.md`](../ia/rules/orchestrator-vs-spec.md) — permanent vs temporary doc split.
- [`docs/agent-led-verification-policy.md`](agent-led-verification-policy.md) — Verification block canonical policy.
- [`CLAUDE.md`](../CLAUDE.md) — Claude Code host surface (hooks, slash commands, subagents, memory).
- [`AGENTS.md`](../AGENTS.md) — agent workflow + backlog / issue process.
- [`ia/skills/README.md`](../ia/skills/README.md) — skill index + conventions.
