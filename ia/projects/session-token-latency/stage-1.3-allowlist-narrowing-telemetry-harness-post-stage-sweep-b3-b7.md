### Stage 1.3 — Allowlist narrowing + telemetry harness + post-stage sweep (B3 + B7 + sweep)


**Status:** Final

**Objectives:** Narrow `tools:` frontmatter for the 7 non-pair-seam subagents (B3), wire the PostToolUse session-level telemetry harness (B7-extended), then run the single post-Stage-1 telemetry sweep producing per-theme attribution. Flip `MCP_SPLIT_SERVERS` default to `1` after sweep validates correctness.

> **`MCP_SPLIT_SERVERS` flag-flip timeline (TECH-527):** flag authored + wired in Stage 1.2 (TECH-524 + TECH-525), default `0` through all of Stage 1.2 (backward compat — both buckets on single `territory-ia` server). Stage 1.3 Task T1.3.6 flips default `0` → `1` in `.mcp.json` ONLY AFTER T1.3.5 post-stage sweep confirms per-theme attribution correctness per NB-6 resolution (see `docs/session-token-latency-audit-exploration.md` §Examples §B7-extended). Flip gate: sweep produces `baseline-summary-post-stage1.json` with B1 split attribution row showing expected IA-core-only session token reduction. Do NOT flip the default earlier — telemetry sweep depends on A/B (`0` vs `1`) comparison data during Stage 1.3.
>
> **Status update (2026-04-20):** TECH-538 stubbed in-chain; `baseline-summary-post-stage1.json` = placeholder. TECH-539 sweep report carries per-theme attribution framework but no real rows. Flag flip **DEFERRED** pending real sweep data via follow-up issue. Default `MCP_SPLIT_SERVERS=0` retained in `.mcp.json`.


**Exit:**

- `verifier.md`, `spec-implementer.md`, `stage-decompose.md`, `project-new-planner.md`, `project-new-applier.md`, `design-explore.md`, `test-mode-loop.md` each carry `tools:` frontmatter listing only required tools (no wildcard).
- `npm run validate:agent-tools` CI lint passes: compares declared `tools:` set vs observed tool calls in test fixtures; alerts on wildcard re-introduction.
- `.claude/settings.json` carries `PostToolUse` hook entry running `tools/scripts/agent-telemetry/session-hook.sh`.
- `tools/scripts/agent-telemetry/session-hook.sh` appends per-tool-call JSONL (fields: `ts`, `session_id`, `tool`, `duration_ms`, `agent`, `lifecycle_stage`) to `.claude/telemetry/{session-id}.jsonl`.
- `tools/scripts/agent-telemetry/baseline-summary-post-stage1.json` committed; diff vs `baseline-summary.json` shows per-theme attribution rows for B1/B3/B7.
- `MCP_SPLIT_SERVERS` default flipped to `1` in `.mcp.json` after sweep validates correctness.

**Tasks:**

| Task | Name | Issue | Status | Intent |
|---|---|---|---|---|
| T1.3.1 | Per-agent tools: narrowing | **TECH-534** | Done | Add `tools:` frontmatter to 7 target agents (`.claude/agents/verifier.md`, `spec-implementer.md`, `stage-decompose.md`, `project-new-planner.md`, `project-new-applier.md`, `design-explore.md`, `test-mode-loop.md`). Allowlist: Bash + Read + Grep + Glob + domain-relevant MCP tools only (e.g. verifier: `unity_bridge_command`, `unity_bridge_get`, `invariants_summary`, `backlog_issue`, `spec_section`; design-explore: `glossary_discover`, `glossary_lookup`, `router_for_task`, `spec_sections`, `spec_outline`, `invariants_summary`, `list_rules`, `rule_content`). See exploration §Examples §B3 for verifier before/after shape. |
| T1.3.2 | Agent-tools CI lint | **TECH-535** | Done | Author `npm run validate:agent-tools` in `package.json`: reads all `.claude/agents/*.md` frontmatter; asserts `tools:` present for the 7 narrowed agents; asserts no wildcard entry; alerts on any tool not in the approved MCP server namespace. Add to `validate:all` chain. Carry NB-7 (allowlist drift prevention) as enforced CI gate. |
| T1.3.3 | PostToolUse session hook | **TECH-536** | Done | Add `PostToolUse` entry to `.claude/settings.json` hooks array: `{"matcher": "*", "hooks": [{"type": "command", "command": "tools/scripts/agent-telemetry/session-hook.sh"}]}`. Author `tools/scripts/agent-telemetry/session-hook.sh`: reads tool name + duration from hook env vars; appends JSONL to `.claude/telemetry/{session-id}.jsonl`; exits 0 always (non-blocking). Reuses JSONL schema from T1.1.1. |
| T1.3.4 | Session aggregation helper | **TECH-537** | Done | Author `tools/scripts/agent-telemetry/aggregate-session.sh {session-id}`: reads JSONL for session; outputs per-tool p50/p99 duration + token estimates. Used by post-stage sweep script (T1.3.5). Update `.gitignore` to confirm raw JSONL excluded; `tools/scripts/agent-telemetry/*-summary.json` tracked. |
| T1.3.5 | Post-stage sweep run | **TECH-538** | Done | STUB — `baseline-summary-post-stage1.json` committed as schema-conformant placeholder per user direction (real sweep requires ≥10 human-driven sessions; self-modification hazard precludes the running agent from producing its own telemetry). Follow-up issue required for real sweep. |
| T1.3.6 | Per-theme attribution + flag flip | **TECH-539** | Done | Sweep report `tools/scripts/agent-telemetry/sweep-report-post-stage1.md` authored with per-theme attribution framework (B1/B3/B7). `MCP_SPLIT_SERVERS` flag flip DEFERRED pending real sweep data (stub cannot validate B1 attribution per NB-6 resolution). Default `0` retained in `.mcp.json`. |

#### §Stage File Plan

<!-- stage-file-plan output — do not hand-edit; apply via stage-file-apply -->

```yaml
- reserved_id: "TECH-534"
  task_key: "T1.3.1"
  title: "Per-agent tools: narrowing"
  priority: "high"
  issue_type: "TECH"
  notes: |
    Add tools: frontmatter to 7 target subagents (verifier.md, spec-implementer.md,
    stage-decompose.md, project-new-planner.md, project-new-applier.md, design-explore.md,
    test-mode-loop.md). Allowlist = Bash + Read + Grep + Glob + domain-relevant MCP tools
    only. No wildcard. Source shape: exploration §Examples §B3 (verifier before/after).
  depends_on: []
  related:
    - "TECH-535"
  stub_body:
    summary: |
      Narrow tools: frontmatter for 7 non-pair-seam subagents. Replace implicit
      wildcard with explicit per-agent allowlist. Carries Theme B3 from session-token
      latency audit — reduces cold-start tool schema payload per subagent spawn.
    goals: |
      - 7 target subagent bodies carry explicit tools: frontmatter (no wildcard).
      - Per-agent allowlist matches observed tool calls in recent lifecycle runs.
      - Pair-seam subagents untouched (out of scope Stage 1.3).
      - npm run validate:all green.
    systems_map: |
      Touches: .claude/agents/verifier.md, spec-implementer.md, stage-decompose.md,
      project-new-planner.md, project-new-applier.md, design-explore.md, test-mode-loop.md.
      Reference: docs/session-token-latency-audit-exploration.md §Examples §B3.
      No runtime C# / Unity scene code touched.
    impl_plan_sketch: |
      Phase 1 — Per-agent allowlist authoring.
      1. For each of 7 target agents: enumerate observed tool calls from recent lifecycle runs.
      2. Compose tools: frontmatter entry — Bash + Read + Grep + Glob + domain MCP subset.
      3. Insert frontmatter; re-run targeted smoke (one invocation per agent).
      4. Run npm run validate:all.

- reserved_id: "TECH-535"
  task_key: "T1.3.2"
  title: "Agent-tools CI lint"
  priority: "high"
  issue_type: "TECH"
  notes: |
    Author npm run validate:agent-tools in package.json: parses .claude/agents/*.md
    frontmatter; asserts tools: present for 7 narrowed agents + no wildcard entry;
    alerts on tools outside approved MCP server namespace. Wire into validate:all.
    Carries NB-7 (allowlist drift prevention) as enforced CI gate.
  depends_on:
    - "TECH-534"
  related:
    - "TECH-534"
  stub_body:
    summary: |
      CI lint script blocking wildcard re-introduction + tool drift in narrowed
      subagents. Parses YAML frontmatter; asserts allowlist shape. Wires into
      validate:all chain so future PRs fail fast on regression.
    goals: |
      - npm run validate:agent-tools passes on current main after TECH-534 lands.
      - Lint fails deterministically when wildcard re-added or unapproved tool declared.
      - validate:all composition includes validate:agent-tools.
      - CI job green.
    systems_map: |
      Touches: package.json (scripts entry), tools/scripts/validate-agent-tools.sh (new
      or tools/mcp-ia-server/src/validators/agent-tools.ts). Reads .claude/agents/*.md.
      Hooks into validate:all chain.
    impl_plan_sketch: |
      Phase 1 — Lint authoring.
      1. Author tools/scripts/validate-agent-tools.sh (or TS equivalent).
      2. Parse .claude/agents/*.md frontmatter; assert tools: list for 7 narrowed agents.
      3. Assert no wildcard; assert all entries in approved MCP namespace.
      4. Add to package.json scripts; chain into validate:all.
      5. Run npm run validate:all.

- reserved_id: "TECH-536"
  task_key: "T1.3.3"
  title: "PostToolUse session hook"
  priority: "high"
  issue_type: "TECH"
  notes: |
    Add PostToolUse entry to .claude/settings.json hooks array. Author
    tools/scripts/agent-telemetry/session-hook.sh: reads tool name + duration from
    hook env; appends JSONL to .claude/telemetry/{session-id}.jsonl; exits 0 always
    (non-blocking). Reuses JSONL schema from T1.1.1 (TECH-510).
  depends_on: []
  related:
    - "TECH-537"
  stub_body:
    summary: |
      Wire PostToolUse session telemetry harness. Per-tool-call JSONL append with
      ts, session_id, tool, duration_ms, agent, lifecycle_stage fields. Non-blocking
      exit-0 hook. Feeds aggregation helper (T1.3.4) + post-stage sweep (T1.3.5).
    goals: |
      - .claude/settings.json PostToolUse entry wired, matcher "*", non-blocking.
      - session-hook.sh appends JSONL per tool call; exits 0 always.
      - Raw JSONL excluded via .gitignore (already set by T1.1.1).
      - npm run validate:all green.
    systems_map: |
      Touches: .claude/settings.json (hooks array), tools/scripts/agent-telemetry/session-hook.sh (new),
      tools/scripts/agent-telemetry/ (existing dir from Stage 1.1).
      Schema: reuses T1.1.1 / TECH-510 JSONL shape.
    impl_plan_sketch: |
      Phase 2 — Harness wiring.
      1. Author tools/scripts/agent-telemetry/session-hook.sh: read env vars (CLAUDE_TOOL_NAME,
         CLAUDE_TOOL_DURATION_MS, CLAUDE_SESSION_ID, CLAUDE_AGENT_SLUG); append JSONL.
      2. chmod +x; smoke-test single invocation.
      3. Add PostToolUse entry to .claude/settings.json hooks array.
      4. Run one /verify lifecycle seam to confirm JSONL appends + exit 0.
      5. npm run validate:all.

- reserved_id: "TECH-537"
  task_key: "T1.3.4"
  title: "Session aggregation helper"
  priority: "medium"
  issue_type: "TECH"
  notes: |
    Author tools/scripts/agent-telemetry/aggregate-session.sh {session-id}: reads
    JSONL for session; outputs per-tool p50/p99 duration + token estimates. Consumed
    by post-stage sweep (T1.3.5). Confirm .gitignore excludes raw JSONL;
    *-summary.json tracked.
  depends_on:
    - "TECH-536"
  related:
    - "TECH-536"
    - "TECH-538"
  stub_body:
    summary: |
      Per-session aggregation CLI turning raw JSONL into p50/p99 per-tool duration
      + token estimates. Input to post-stage sweep diff report. Non-interactive;
      reads single session id arg; writes summary JSON to stdout or fixed path.
    goals: |
      - aggregate-session.sh {session-id} returns per-tool p50/p99 + token totals.
      - Output JSON schema compatible with baseline-summary.json diff flow.
      - .gitignore confirmed: raw *.jsonl excluded; *-summary.json tracked.
      - Script tested against sample JSONL from T1.3.3.
    systems_map: |
      Touches: tools/scripts/agent-telemetry/aggregate-session.sh (new).
      Reads: .claude/telemetry/{session-id}.jsonl.
      Reference schema: tools/scripts/agent-telemetry/baseline-summary.json (from TECH-512).
    impl_plan_sketch: |
      Phase 2 — Aggregation tooling.
      1. Author aggregate-session.sh: jq-driven JSONL reader; compute p50/p99 per tool.
      2. Emit JSON shape compatible with baseline-summary.json schema.
      3. Smoke-test on sample session JSONL produced by T1.3.3.
      4. Confirm .gitignore rules.
      5. npm run validate:all.

- reserved_id: "TECH-538"
  task_key: "T1.3.5"
  title: "Post-stage sweep run"
  priority: "high"
  issue_type: "TECH"
  notes: |
    After B1 + B3 + B7 commit boundaries land: run ≥10 representative sessions (pad
    to ≥3 working days per NB-10). Produce tools/scripts/agent-telemetry/baseline-summary-post-stage1.json
    (p50/p95/p99 per metric, same schema as baseline-summary.json). If natural session
    diversity insufficient for per-theme attribution, synthesize A/B sessions (B1 on/off,
    B3 on/off independently) per NB-9.
  depends_on:
    - "TECH-534"
    - "TECH-536"
    - "TECH-537"
  related:
    - "TECH-537"
    - "TECH-539"
  stub_body:
    summary: |
      Execute the single post-Stage-1 telemetry sweep. ≥10 representative lifecycle
      sessions across mixed seams; aggregate to baseline-summary-post-stage1.json.
      A/B synthesize on B1 + B3 independently if natural diversity falls short.
    goals: |
      - baseline-summary-post-stage1.json committed with p50/p95/p99 per metric.
      - Schema matches baseline-summary.json exactly (TECH-511 validator green).
      - Per-theme attribution separation feasible (B1 vs B3 vs B7 distinguishable).
      - npm run validate:telemetry-schema + validate:all green.
    systems_map: |
      Touches: tools/scripts/agent-telemetry/baseline-summary-post-stage1.json (new).
      Consumes: aggregate-session.sh (TECH-537), session-hook.sh (TECH-536).
      Reference: docs/session-token-latency-audit-exploration.md §Provenance, NB-9, NB-10.
    impl_plan_sketch: |
      Phase 3 — Sweep execution.
      1. Confirm TECH-534 + TECH-536 + TECH-537 landed + merged to main.
      2. Run ≥10 representative sessions across /implement, /ship, /stage-file seams.
      3. If diversity insufficient: synthesize A/B pairs (B1 off/on, B3 off/on).
      4. Aggregate via aggregate-session.sh; write baseline-summary-post-stage1.json.
      5. npm run validate:telemetry-schema + validate:all.

- reserved_id: "TECH-539"
  task_key: "T1.3.6"
  title: "Per-theme attribution + flag flip"
  priority: "high"
  issue_type: "TECH"
  notes: |
    Diff baseline-summary-post-stage1.json vs baseline-summary.json; compute
    per-theme attribution rows (B1 server-split, B3 allowlist narrowing, B7 harness
    overhead) per exploration §Examples §B7-extended aggregate shape. Commit sweep
    report. Flip MCP_SPLIT_SERVERS default from 0 to 1 in .mcp.json. Confirm
    npm run validate:all green.
  depends_on:
    - "TECH-538"
  related:
    - "TECH-538"
  stub_body:
    summary: |
      Diff pre/post baseline summaries; produce per-theme attribution rows (B1/B3/B7).
      Commit sweep report artifact. Flip MCP_SPLIT_SERVERS default 0 → 1 in .mcp.json
      after attribution validates B1 split correctness per NB-6 resolution.
    goals: |
      - Sweep report committed with per-theme rows for B1, B3, B7.
      - MCP_SPLIT_SERVERS default flipped 0 → 1 in .mcp.json.
      - B1 split attribution row shows expected IA-core session token reduction.
      - npm run validate:all green.
    systems_map: |
      Touches: .mcp.json (MCP_SPLIT_SERVERS default flip),
      tools/scripts/agent-telemetry/sweep-report-post-stage1.md (new, or similar).
      Consumes: baseline-summary.json + baseline-summary-post-stage1.json.
      Reference: exploration §Examples §B7-extended aggregate shape; NB-6 resolution.
    impl_plan_sketch: |
      Phase 3 — Attribution + flag flip.
      1. Diff baseline-summary.json vs baseline-summary-post-stage1.json per metric.
      2. Attribute deltas to B1, B3, B7 using A/B data (or synthetic pairs per NB-9).
      3. Author sweep report with per-theme rows.
      4. If B1 attribution matches expected reduction: flip MCP_SPLIT_SERVERS default to 1 in .mcp.json.
      5. npm run validate:all.
```

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
