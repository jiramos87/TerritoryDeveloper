# Territory Developer — AI Mechanics Audit

> **Origin.** Written 2026-04-19 by an external reviewing agent in `/Users/javier/teselagen/lims-2` (`TEMP-territory-developer-ai-audit-2026-04-19.md`, disposable). Copied into this repo 2026-04-19 as the persistent source-of-record for the remediation exploration [`session-token-latency-audit-exploration.md`](session-token-latency-audit-exploration.md). Original `TEMP-…` file in `lims-2` remains disposable.
>
> **Scope bar.** Focused on everything **beneath** `docs/lifecycle-opus-planner-sonnet-executor-exploration.md`: MCP tooling, non-lifecycle skills, docs used as instruments, rules, auto-loaded AI context, hooks, settings. The planner/executor split itself is **locked** and not challenged.
>
> **Lens.** Token economy and latency of agentic execution. Not code quality, not product correctness.

---

## 0. Executive summary

The repository already does most of the right things — parser cache, YAML-first backlog, MCP slicing over full-spec reads, typed Plan-Apply contracts, two-tier cache architecture in the rev-4 design, subagent-level `tools:` allowlists, a skill subskill (`domain-context-load`) that replaces 8+ inline copies of the same MCP recipe.

But under the hood there are still systemic token-drag and latency-drag points that the locked lifecycle design will **inherit unless fixed first**:

1. **34 MCP tools load into every Claude Code session** via `enabledMcpjsonServers: ["territory-ia"]`. Roughly half (Unity bridge, postgres journal, compute) are never used in IA-authoring sessions but their descriptors still sit in `tools/list`. Estimated always-loaded tool-descriptor overhead ≈ **5–9k tokens/session** before any work happens.
2. **Tool descriptors carry 7–8 "Rejected alias" schema fields** (`spec_section`, `spec_sections`) whose only runtime behaviour is to throw `invalid_input`. These are pure descriptor bloat — every client sees them forever.
3. **Slash-command bodies restate the subagent mission verbatim** (see `/implement` vs `.claude/agents/spec-implementer.md`). Double storage, triple billing when a main session dispatches a subagent: command body (main context) + subagent body (fresh context) + forwarded prompt (fresh context). ~1.5–2× per dispatch.
4. **Caveman directive is redeclared in ~40 surfaces** (every skill preamble, every subagent body, every slash command). The rule is already in the always-loaded anchor (`ia/rules/agent-output-caveman.md`). Each restatement costs ~50–80 tokens; aggregate ≈ **2–3k tokens of redundant reassertion** across the assembled context of any multi-stage run.
5. **Hook-driven shell forks**: `bash-denylist.sh` + `cs-edit-reminder.sh` call `python3` per tool use. Added latency ≈ **30–80 ms × N Bash/Edit calls per session**, non-trivial across hundreds of tool calls a day. Pure-shell JSON extraction exists and is cheap.
6. **Always-loaded ambient is ~7–8k tokens** (CLAUDE.md + the five `@`-imported rules). Much of it duplicates `AGENTS.md` and `docs/agent-lifecycle.md`. Single-source-truth would shrink ambient by ~40–50% with no expressive loss.
7. **`verification-reminder.sh` on Stop** is a one-line echo with zero information; it's pure visual noise and hints at a policy that the agent itself already owns.
8. **Docs drift** already exists: the MCP server README says 29 tools, `src/index.ts` registers 34. Any agent routed to the README will mis-plan tool use.

Biggest leverage: a **progressive-disclosure rewiring of the MCP surface** (Anthropic shipped `defer_loading: true` + Tool Search Tool pattern in 2026; [Medium: Anthropic tool-definition bloat fix](https://medium.com/@DebaA/anthropic-just-shipped-the-fix-for-tool-definition-bloat-77464c8dbec9)) plus a **rule-compression pass** on the always-loaded anchor chain. Together these are low-risk and likely recover **10–20k tokens per session** before any lifecycle refactor lands.

---

## 1. Critical

### C1. Split `territory-ia` into an IA-core MCP and a Unity-bridge MCP; enable per-session

**Observation.** `tools/mcp-ia-server/src/index.ts` registers 34 tools covering four distinct domains:

| Domain | Tools | Session fit |
|---|---|---|
| IA retrieval | `list_specs`, `spec_outline`, `spec_section`, `spec_sections`, `glossary_*`, `router_for_task`, `invariants_*`, `list_rules`, `rule_content`, `rule_section` | ~all lifecycle sessions |
| Backlog / master-plan | `backlog_issue`, `backlog_list`, `backlog_search`, `backlog_record_validate`, `reserve_backlog_ids`, `project_spec_closeout_digest`, `parent_plan_validate`, `master_plan_locate`, `master_plan_next_pending` | lifecycle sessions |
| Unity bridge | `unity_bridge_command`, `unity_bridge_lease`, `unity_bridge_get`, `unity_compile`, `findobjectoftype_scan`, `unity_callers_of`, `unity_subscribers_of`, `csharp_class_summary`, `city_metrics_query` | verify/impl with C# diff only |
| Computational | `isometric_world_to_grid`, `growth_ring_classify`, `grid_distance`, `pathfinding_cost_preview`, `geography_init_params_validate`, `desirability_top_cells` | rare, domain-specific |
| Postgres journal | `project_spec_journal_{persist,search,get,update}` | closeout + closeout-research only |

**Cost.** Descriptors for all 34 load into `tools/list` every session. The Unity bridge tool alone is 35 KB of source with a correspondingly large Zod-derived JSON schema (`timeout_ms.maximum`, enumerated `kind`s, nested parameter objects per kind). Rough estimate: **~6–10k tokens of always-loaded tool surface**, of which 50–70% is unused in any given session.

**Fix.** Three options, in order of effort:

1. **(Easiest)** Create two separate server entries in `.mcp.json`: `territory-ia` (IA + backlog), `territory-unity-bridge` (Unity + compute + journal). Control per-session via `.claude/settings.local.json` → `enabledMcpjsonServers`. Default to IA-only; opt in to the bridge from slash commands or a hook that flips the local setting when verify-loop is about to run.
2. **(Medium)** Keep one server but mark non-IA tools with `defer_loading: true` (Anthropic's new descriptor-bloat fix, see [Medium article](https://medium.com/@DebaA/anthropic-just-shipped-the-fix-for-tool-definition-bloat-77464c8dbec9)). Agent uses the built-in Tool Search Tool to discover and load bridge tools on demand. This mirrors the exact UX seen in this very session — deferred tools surfaced by name, schemas fetched via `ToolSearch`.
3. **(Cleanest)** Do both: server split *and* deferred-loading within each server.

**Impact.** Always-loaded MCP surface shrinks from ~34 tools → ~12 tools for a typical kickoff/implement session. Ballpark saving **5–8k tokens/session**. Cache hit rate improves because the stable prefix no longer mutates when bridge tool definitions change ([F5 cascade in `docs/prompt-caching-mechanics.md`](prompt-caching-mechanics.md) explicitly calls out `tools:` edits cascading down through all cached blocks).

---

### C2. Strip "Rejected alias" descriptor fields from spec_section / spec_sections

**Observation.** `tools/mcp-ia-server/src/tools/spec-section.ts:88–116` exposes **7 optional fields** whose entire purpose is to throw `invalid_input`:

```ts
key: z.string().optional().describe("Rejected alias — use 'spec' instead. Returns invalid_input error."),
document_key: z.string().optional().describe("Rejected alias — use 'spec' instead. Returns invalid_input error."),
doc: z.string().optional().describe("Rejected alias — use 'spec' instead. Returns invalid_input error."),
section_heading: stringOrNumber.optional().describe("Rejected alias — use 'section' instead. Returns invalid_input error."),
section_id: stringOrNumber.optional().describe("Rejected alias — use 'section' instead. Returns invalid_input error."),
heading: stringOrNumber.optional().describe("Rejected alias — use 'section' instead. Returns invalid_input error."),
maxChars: z.number().optional().describe("Rejected alias — use 'max_chars' instead. Returns invalid_input error."),
```

`spec-sections.ts:20–31` repeats the pattern inside the array element schema.

**Cost.** Each rejected field is ~50–60 tokens of descriptor. 7 fields × 2 tools ≈ **700–900 tokens** of always-loaded schema that exists solely to educate the agent via error messages — but the agent sees the descriptor first, so the education is duplicative with the rejection.

**Fix.** Keep the rejection logic in `normalizeSpecSectionInput` (server-side), **remove** the fields from `inputShape`. If a legacy client still passes them, the server still throws `invalid_input` with the same hint. Descriptor shrinks to `{spec, section, max_chars}` — three lines.

**Impact.** ~800 tokens saved at session start. Zero behavioural regression — `normalizeSpecSectionInput` rejects unknown params unconditionally.

---

### C3. Collapse the CLAUDE.md / AGENTS.md / agent-lifecycle doc triangle

**Observation.** The same lifecycle table is restated four times:

| File | Lines | Always loaded? |
|---|---|---|
| `CLAUDE.md` §3 Key files | 82 | yes |
| `AGENTS.md` §2 Agent lifecycle | 199 | no (referenced only) |
| `ia/rules/agent-lifecycle.md` Surface map | 69 | yes |
| `docs/agent-lifecycle.md` full matrix | 198 | no (referenced) |

Each restates: ordered flow → surface map → hard rules. The "Surface map" table in the rule is a near-duplicate of the "Agent lifecycle" table in AGENTS.md.

The always-loaded slice alone (CLAUDE.md + rule) = ~150 lines ≈ **~3k tokens** of lifecycle taxonomy that agents rarely need to consult during a specific stage.

**Fix.** Declare a single authoritative source — `docs/agent-lifecycle.md`. Shrink `ia/rules/agent-lifecycle.md` to a 10-line stub that only names the ordered flow and points to the doc. Shrink CLAUDE.md §3 Key files to 15 lines — only the files that affect *this* session's plumbing (MCP config, hooks, permissions). Move the agent/command/skill inventory tables to AGENTS.md. Drop duplicate "Hard rules" blocks; leave one copy in the rule.

**Impact.** Always-loaded ambient drops ~2–2.5k tokens. Zero information loss — on-demand lookup via `rule_content agent-lifecycle` or reading `docs/agent-lifecycle.md` remains.

---

### C4. De-dupe caveman preambles; stop re-asserting in every surface

**Observation.** `ia/rules/agent-output-caveman.md` is `alwaysApply: true` and `@`-imported into CLAUDE.md. Yet the caveman directive is restated in:

- Every `.claude/agents/*.md` body (first line after frontmatter) — 13 agents
- Every `ia/skills/*/SKILL.md` preamble — ~30 skills
- Every `.claude/commands/*.md` (inside the "Subagent prompt (forward verbatim)" blocks)
- Output styles
- Handoff messages (authored into multiple skills as seed prompts)

Rationale recorded in `ia/rules/agent-output-caveman-authoring.md`: *"Subagents launched via Agent tool run in fresh context and do not inherit the SessionStart hook."* True. But the **rule itself** is auto-loaded because agents inherit `@`-imports from their parent context when they're inline in the skill library — and for those that don't, a single reference line (≤15 tokens) would suffice, not a full re-statement.

**Cost.** Spot-measuring a typical subagent body: caveman preamble + exceptions block ≈ 70–100 tokens. Across all dispatchable agents + skills: ~40 surfaces × 80 tokens ≈ **3.2k tokens of redundant directive**, of which most is re-ingested only when the surface is actually loaded — but the waste compounds because the same block appears inside `/implement` command + `spec-implementer` subagent body + `project-spec-implement` SKILL.md, all of which may feed into a single lifecycle stage.

**Fix.** Two-layer rule:

1. Keep one **canonical** directive line (15 tokens) in each subagent body: `> Follow ia/rules/agent-output-caveman.md (caveman default).`
2. Delete all full-text restatements from skills, commands, and handoff seeds. Reserve full-text only for files that are guaranteed to load **without** the rule — and audit each: after CLAUDE.md imports and the Agent tool's context-merge behaviour, that list is ~zero.

**Impact.** ~2.5–3k tokens recovered across a multi-stage run. Side benefit: the *meaning* of the caveman rule becomes easier to maintain because edits happen in one place.

---

### C5. Replace `verification-reminder.sh` Stop hook with something load-bearing or nothing

**Observation.** `tools/scripts/claude-hooks/verification-reminder.sh` is:

```sh
#!/bin/sh
echo "⚠ Remember to run /verify before merging to main."
```

This fires on **every Stop event** — that is, at the end of every turn where the agent stops. The Verification policy is already codified in `docs/agent-led-verification-policy.md`, loaded via `ia/rules/agent-verification-directives.md`, restated in AGENTS.md §3, and enforced by `/verify`, `/verify-loop`, `/ship`, `/ship-stage`. The agent already knows.

**Cost.** One emoji + 10 words of noise per Stop. Net effect on token economy: ~0. Net effect on signal-to-noise ratio in the conversation transcript: negative. On rare occasions the reminder fires **during** a subagent chain and the parent agent pattern-matches the reminder as a real instruction, causing unnecessary verification re-runs.

**Fix.** Either (a) delete the hook + the `Stop` hook entry in `.claude/settings.json`, or (b) replace it with a **conditional** reminder that fires only when (i) branch is not `main`/`master`, (ii) `.claude/last-verify-exit-code` shows a stale failure or is unset, and (iii) the current HEAD has ≥1 commit ahead of upstream. The conditional version is actually useful; the unconditional one is pure noise.

**Impact.** Small but qualitative. Removes a distractor seen on every turn. Improves the 1h-cache eligibility: `Stop` hook stdout is part of the next turn's input, which can destabilise cached suffixes.

---

## 2. Major

### M1. Flatten `/slash-command → subagent-body → skill-body` triple-statement

**Observation.** `/implement` command body (`.claude/commands/implement.md`) includes a `Subagent prompt (forward verbatim)` block that restates the exact Mission + Phase loop + Hard boundaries that already live in `.claude/agents/spec-implementer.md`. The target subagent receives its own body *and* the forwarded prompt.

Same pattern in `/kickoff`, `/verify-loop`, `/closeout`, `/ship` (Stage 1–4 dispatchers), `/ship-stage`.

**Cost.** Each dispatch carries ~400–600 tokens of restated prompt that the subagent already has from its own body. Over a full `/ship` chain: ~2–3k extra tokens per issue.

**Fix.** Command forwards only what the subagent does *not* already know: `ISSUE_ID`, resolved spec path, resolved master plan path, gate boundary. Trust the subagent body for Mission, Phase loop, Hard boundaries. The `Agent` tool's `prompt` field becomes a short brief ("Run per your body. ISSUE_ID={X}, SPEC={path}, STOP_ON=KICKOFF_DONE.") instead of a full re-statement.

**Impact.** 30–40% reduction in per-dispatch prompt size. On a typical `/ship` run (4 dispatches), 8–12k tokens saved. Requires explicit policy in subagent bodies: "Authoritative mission lives in this file; treat forwarded prompts as parameter-only."

---

### M2. SessionStart hook should warm deterministic MCP prefixes, not print facts

**Observation.** `session-start-prewarm.sh` emits 4 lines of git-status facts. Useful as a human-readable banner but fails the MCP-cache-priming test — it doesn't pre-warm any cached block and its text is variable (branch name, dirty count) which *destabilises* any cache hit attempt on the message prefix.

**Fix.** Two tracks:

1. Keep the banner but route it to stderr (it still shows in the transcript for humans via stderr rendering) so it stays out of the cached message stream. Or surface the same info through a tool call (`git_status_summary`) the agent invokes on demand.
2. Have the hook **emit a deterministic, cacheable preamble** instead — e.g. a fixed block naming the active stage freeze + MCP server version + currently-enabled ruleset. Same block every session for a given branch, cache-friendly, informative.

**Impact.** Enables Tier 1 cache hits on the first real message. The rev-4 design explicitly calls out that `CLAUDE_AUTOCOMPACT_PCT_OVERRIDE=65` shortens the cache-eligible window; every volatile byte at the top of context moves break-even further away.

---

### M3. Convert `bash-denylist.sh` + `cs-edit-reminder.sh` to pure-shell JSON parsing

**Observation.** Both scripts call `python3 -c 'import json…'` per invocation. On macOS the python3 fork + import is ~20–60 ms; aggregated over a session with hundreds of `Bash`/`Edit` calls this adds **seconds of wall-clock latency**.

**Fix.** Pure-shell JSON extraction for the two fields that matter (`tool_input.command` and `tool_input.file_path`). The existing sed fallback is already correct for well-formed input; elevate it to the primary path, keep python3 as a fallback for embedded escapes. Or use `jq` if it's on-path (it often is via Homebrew; auto-detect, same pattern).

**Impact.** ~30–60 ms per Bash/Edit tool call. Compounds significantly. No token saving but real UX latency reduction.

---

### M4. Gate MCP tool visibility per subagent much more aggressively

**Observation.** `spec-implementer.md` already lists 20+ `mcp__territory-ia__*` tools in its frontmatter `tools:` allowlist. Good. But some agents inherit the broader default and expose tools they never call (e.g. `verifier` expects a narrow toolset; check whether its `tools:` line includes only what it actually runs).

**Fix.** Per-agent allowlist audit:

| Agent | Tools it actually invokes | Tools it should ban |
|---|---|---|
| `verifier` | backlog_issue, unity_bridge_command, unity_compile, Bash for `validate:all`/`unity:compile-check` | spec_section, glossary_*, router_for_task (verification is post-read) |
| `closeout` | backlog_issue, project_spec_closeout_digest, project_spec_journal_persist, glossary_lookup | spec_sections batch (unused), unity_bridge_* (N/A) |
| `stage-decompose` | backlog_issue, spec_sections, glossary_discover/lookup, master_plan_locate, master_plan_next_pending | unity_*, findobjectoftype_scan, city_metrics_query |

Narrow allowlists reduce both token count (smaller `tools:` slice on each dispatch) and hallucination risk (agent can't invent an off-script call).

**Impact.** Per-agent ~400–800 tokens saved on dispatch; reduced tool-selection latency; higher cache-hit eligibility (the subagent's tool surface becomes stable).

---

### M5. Move the IA-indexes + YAML-parse into a persistent worker

**Observation.** The MCP server builds its registry at process start (`buildRegistry()` in `src/config.ts`) and relies on per-process parse cache (noted at `README.md` §Troubleshooting → "Slow repeated calls"). Every MCP server restart re-parses all specs, rules, glossary, backlog yamls.

The flow `rev 4 F5 — tools: change invalidates system + messages` means every schema change triggers a full re-warm. Combined with the parse cache being in-memory-only, a restart loses all work.

**Fix.** Two moves:

1. Serialise the parse cache to disk (`tools/mcp-ia-server/.cache/parse-cache.json`) keyed by file-mtime. On start, load + validate. ~10× faster cold start.
2. When the server spawns via `tsx`, pre-compile to `dist/` and invoke the compiled JS. The `npm run build` target exists; `.mcp.json` currently points at `tsx` on source — great for iteration, wasteful for production.

**Impact.** Cold-start latency of MCP ~1500 ms → ~200 ms. Users notice this only on the first call of the session; the win is per-session, not per-call.

---

### M6. Progressive disclosure for `spec_outline` and `list_rules`

**Observation.** `spec_outline` returns the full heading tree for a doc; `list_rules` dumps every rule file's frontmatter. Both are used to help the agent pick a next step.

Full outline of `isometric-geography-system.md` is ~2k tokens alone (the spec is 49 KB of markdown with a deeply nested heading tree).

**Fix.** Default `spec_outline` to top-level sections only (`depth=1`). Expose an optional `depth` parameter that clients pass when they truly need all sub-sections. Same for `list_rules` — default to `alwaysApply: true` rules only, with `include: "all"` opt-in.

**Impact.** `spec_outline` default response ≈ 3× smaller. Progressive-disclosure pattern aligns with 2026 guidance on Tool Search ([buildtolaunch.substack.com](https://buildtolaunch.substack.com/p/claude-code-token-optimization) reports ~15k tokens saved per session from progressive disclosure).

---

### M7. Fix MCP doc drift (`README.md` says 29, code registers 34)

**Observation.** `tools/mcp-ia-server/README.md` states "## Tools (29)". `src/index.ts` counts 34 `register*` calls. The README also lists some descriptions that no longer match current input shapes (e.g. `spec_section` alias story, `backlog_list` not in the README table).

**Cost.** An agent that loads the README as context (via `list_specs`/`list_rules` or direct read) will form a stale mental model of the tool surface. Leads to wasted tool calls probing for non-existent params or missing tools it could have used.

**Fix.** Add a CI check: grep `registerTool(` in `src/tools/*.ts` + count `| **`...`** |` rows in README.md. Fail if counts disagree. Or (better) generate the README table from the code. The `npm run verify` target already exercises all tools — extend it to emit the table.

**Impact.** Eliminates a class of "agent thinks the tool looks like X but it looks like Y" errors. Improves first-call success rate.

---

### M8. Harden the handoff contract with a lint rule on skill seeds

**Observation.** Many skills emit "seed prompts" as ```markdown code blocks (e.g. `project-spec-implement/SKILL.md` §Seed prompt). These are handed verbatim to the next agent. Drift is inevitable — skills and agents edit asynchronously.

**Fix.** Add a validator: every `Seed prompt` block must be self-contained (references only files that exist) and must name its target subagent. `npm run validate:skill-seeds`. Fails if a seed says "spec-kickoff subagent" but `.claude/agents/spec-kickoff.md` is renamed/deleted.

**Impact.** Prevents the silent "agent loads a seed that doesn't match the subagent body it's dispatched to" mode. Debug-time saver more than token saver.

---

## 3. Minor

### m1. Drop the 10 kB `Bury_20260307_131752.json` + `mono_crash.*` blobs from repo root

They aren't loaded by Claude but they pollute glob scans and `ls` outputs that agents do as orientation. `.gitignore` them and delete from tracking; the agent's initial `ls` view shortens by dozens of lines.

### m2. Memory path drift in SessionStart banner

`session-start-prewarm.sh` says `memory: MEMORY.md + ~/.claude-personal/.../memory/`. The actual per-project memory lives under `/Users/javier/.claude/projects/-Users-javier-bacayo-studio-territory-developer/memory/` (the harness standardised on `.claude/projects/`). The banner misguides agents that try to look up memory.

### m3. `CLAUDE_CODE_DISABLE_1M_CONTEXT=1` + autocompact at 65%

The rev-4 doc is written against 1M context assumptions. Setting `CLAUDE_CODE_DISABLE_1M_CONTEXT=1` keeps sessions on the smaller window and triggers autocompact earlier (65% override). This is a valid cost/latency tradeoff today, but should be revisited once the planner/executor split lands — Opus stages that need to read a full master plan + all Task specs benefit from 1M. Decision should be recorded in `prompt-caching-mechanics.md` (which currently assumes 5m/1h TTL but not context size).

### m4. Per-tool cold-start timing is only visible under `DEBUG_MCP_COMPUTE=1`

`instrumentation.ts` emits stderr timing lines. Flip it on by default in `.mcp.json` env — the stderr path is stdio-safe and the visibility pays for itself on the first session-of-the-day slowness.

### m5. `backlog_issue` re-parses `BACKLOG.md` on every call when YAML fallback misses

After the yaml migration, most reads should resolve from `ia/backlog/{id}.yaml`. Check `backlog-parser.ts`: if it still walks `BACKLOG.md` first when yaml exists, flip the order and cache the yaml manifest in memory with mtime invalidation. Cheapest, most frequent tool → biggest cumulative win.

### m6. Tool descriptors should use `.describe()` strings ≤120 chars

Current descriptors on `unity_bridge_command` push 300+ characters of prose per param. MCP client UIs truncate around 160–200; the extra bytes live only in the prompt cost. Set a style rule and a lint.

### m7. `.claude/memory/` is empty

CLAUDE.md §3 implies there is a promotion path from `MEMORY.md` to `.claude/memory/{slug}.md` when entries exceed ~10 lines. Several entries in `MEMORY.md` already exceed that (e.g. umbrella-rollout-tracker-pattern = 4 multi-line sub-bullets). Promote them; shrinks the always-visible `MEMORY.md`.

### m8. Output style `verification-report.md` is 87 lines — almost a skill

Output styles are loaded in full when activated. The Verification-report output style is essentially a re-statement of the agent-led-verification-policy JSON schema. Could be trimmed to ~30 lines by referencing the policy for field semantics and keeping only the "Part 1/Part 2 structure + examples" block.

### m9. Consider `CLAUDE_AUTOCOMPACT_PCT_OVERRIDE=65` instrumentation

There's no telemetry hook that records when autocompact fires. Agents that get compacted mid-chain often lose task context. Add a hook on `PreCompact` (or a Stop hook that inspects compact state) to persist a summary to `.claude/last-compact-summary.md`. Cheap; measurable.

### m10. `ship.md` slash command is 192 lines

192 lines of imperative prompts for a sequential chain that the `ship-stage` subagent already encapsulates. Could collapse to ~60 lines that just dispatch + gate, trusting the subagents' bodies as the authoritative steps. Same pattern as M1 but specific to `/ship`.

---

## 4. Out-of-the-box suggestions

### O1. "Planner context snapshot" as a first-class artifact, not ambient loading

Rev-4 Tier 2 bundle is a one-shot concatenation of Stage context. Today it's assembled at runtime via `domain-context-load` in the planner's context. **Materialise it**: planner skill writes the bundle to `ia/state/{stage-id}-context-bundle.md` before dispatching executors. Executors `@`-include it (single-file, single cache breakpoint). Validator CI-checks the bundle token count against the F2 floor. Side-benefit: bundles become diff-able across Stages, showing which sections the planner considered essential.

### O2. A per-session `tool-usage.log` that subagents read before acting

Common anti-pattern: subagent chains re-fetch the same glossary term 3 times across pair-heads and pair-tails, each time paying ~400 tokens of round-trip. Cheap fix: a tiny SQLite (or jsonl) file written by a PostToolUse hook capturing `{tool_name, args_hash, result_hash, ts}`. Next subagent opens the log before calling `glossary_discover` with the same keywords; if already cached within the Stage window, reuse the local result instead of re-calling MCP.

This is "content-addressable MCP memoisation" at the session boundary — sits beneath the lifecycle, respects its constraints, and directly exploits the F3 observation that concurrent requests don't share cache.

### O3. Replace "caveman" global with compiler-style output-compression directives

Caveman style is effective but verbose to specify (every surface needs the directive + the exceptions list). An alternative: one-line `output-style: caveman` in frontmatter, parsed by a `PreToolUse`-like **PreCompletion** hook that lints the agent's final text and flags non-caveman prose — without the agent having to carry the full rule. Harness-level enforcement, frontmatter expression. Zero token cost per surface.

This is borderline: it requires harness-level support that doesn't currently exist. But worth floating as a direction for the Claude Code team (the dogfooding journal feedback noted in user memory).

### O4. Convert hook marker files into a unified `.claude/runtime-state.json`

Today: `.claude/last-verify-exit-code`, `.claude/last-bridge-preflight-exit-code`, `.queued-test-scenario-id` (+ others). Each is a single-value flat file. A single JSON with a schema lets SessionStart emit a single-block, deterministic preamble (see M2), and lets hooks cheaply append fields without fighting naming.

### O5. Agent-authored "cache breakpoint plan" per stage

Anthropic enforces 4 `cache_control` breakpoints per request ([illusioncloud blog](https://blog.illusioncloud.biz/2026/01/13/prompt-caching-anthropic-cache-breakpoints/)). Skills today don't choose where to place them — the host decides. Expose a tiny MCP tool `cache_breakpoint_recommend(stage_id)` that returns the 4 anchors (Tier 1 prefix end, Tier 2 bundle end, spec end, last executor-mutable block). Skill preambles then annotate the emitted prompts with explicit breakpoint hints. Saves Anthropic having to guess longest-prefix.

Prerequisite: the rev-4 cache-mechanics doc already names Tier 1 + Tier 2. O5 = extend it with a third + fourth anchor recipe.

### O6. Skills index as a navigator MCP tool

`ia/skills/README.md` is ~150 lines describing every skill. The rule "open the matching `SKILL.md` when the task triggers" requires agents to pattern-match themselves. A single MCP tool `skill_for_task(keywords, lifecycle_stage)` that returns the best-matching skill + its URL + (optionally) its first phase's shortened body = a single round-trip instead of 2–3 file reads. Cheap; big UX win.

### O7. Watch the Anthropic "defer_loading" breaking surface

Per the web search above, Anthropic shipped `defer_loading: true` for tool descriptors in 2026. This is the exact antidote to C1 when the two-server split is not viable. Worth a tracking issue.

---

## 5. What NOT to change (positive findings)

- **`domain-context-load` subskill.** Exactly the right pattern — 8 inline copies → 1 canonical recipe. Keep expanding this.
- **YAML-first backlog (`ia/backlog/{id}.yaml`) with `reserve-id.sh` + flock.** Avoids materialising `BACKLOG.md` parse every call. Stay on this track.
- **Per-subagent `tools:` allowlists.** Correct mechanism; just under-used (see M4).
- **Parse cache + fuzzy heading matching in MCP parser.** Right level of help; keep.
- **`plan-apply-pair-contract.md` as ondemand rule.** Not always-loaded; fetched per pair seam. Good discipline.
- **`rev 4` prompt-caching-mechanics doc.** Already documents F1–F6, sizing gate, TTL lock, concurrency constraint. Excellent foundation; make sure it's linked from C1–C3 fixes above so they don't regress it.
- **Caveman rule's exception list.** Covers the real pain points (JSON, error output, security). Don't touch the exception list; do de-dupe the re-statements (C4).

---

## 6. Quantified recap

Rough per-session estimates for a typical `/ship` or `/ship-stage` run. Assumptions: 1–2 dispatches per stage, 4 stages, typical Opus+Sonnet mix.

| Item | Effort | Savings/session | Confidence |
|---|---|---|---|
| C1 Split MCP server / defer-load | 2–3 d | 5–8k tokens | high |
| C2 Drop rejected-alias descriptors | 1 h | ~800 tokens | very high |
| C3 Collapse CLAUDE/AGENTS/lifecycle | 1 d | 2–2.5k tokens | high |
| C4 De-dupe caveman preambles | 2 h | 2.5–3k tokens | high |
| C5 Remove Stop reminder | 5 min | negligible tokens, + signal | very high |
| M1 Flatten triple-statement dispatch | 4 h | 8–12k on /ship | medium-high |
| M2 Deterministic SessionStart preamble | 2 h | cache-hit eligibility | medium |
| M3 Pure-shell JSON in hooks | 1 h | 30–60 ms × N calls | very high |
| M4 Per-agent tool allowlists | 2 h | 0.4–0.8k × N dispatch | high |
| M5 On-disk parse cache + compiled dist | 3 h | MCP cold start 7× | high |
| M6 Progressive disclosure in `spec_outline` | 1 h | 1–2k per outline call | high |
| **Total (C + M)** | **~4–6 d** | **~20–30k tokens/session + ~1 s wall time saved** | — |

That sits comfortably ahead of the rev-4 Tier 1 block itself (5,192 tok measured), so the **proportional gain is 4–6× Tier 1**. Also compatible: every C/M item is implementable without touching the locked planner/executor design; most are under-the-hood refactors.

---

## 7. Priority ordering

If only three things get done this month:

1. **C2** (drop rejected-alias descriptors) — 1 hour, pure win, no risk.
2. **C4** (de-dupe caveman preambles) — half a day, pure win, small risk (have to trust the rule auto-load). Audit + delete.
3. **C1** (MCP server split or `defer_loading`) — the big one; gate the rev-4 Stage 10 cache-optimization work on this landing, otherwise every Stage bundle carries the full bridge tool surface it will never touch.

M1 (flatten triple-statement) is the right fourth target — it's the largest per-dispatch saving.

---

## 8. Files sampled (for audit reproducibility)

Target repo: `/Users/javier/bacayo-studio/territory-developer` (not modified).

Key files read:
- `CLAUDE.md`, `AGENTS.md`, `MEMORY.md`, `.mcp.json`, `.claude/settings.json`
- `ia/rules/{invariants,agent-output-caveman,agent-output-caveman-authoring,agent-lifecycle,agent-router,agent-human-polling,plan-apply-pair-contract,orchestrator-vs-spec,coding-conventions,mcp-ia-default,terminology-consistency}.md`
- `ia/skills/{project-spec-implement,verify-loop,domain-context-load}/SKILL.md` + `ia/skills/README.md`
- `.claude/agents/{spec-implementer,verify-loop}.md`
- `.claude/commands/{implement,ship}.md`
- `.claude/output-styles/verification-report.md`
- `docs/agent-led-verification-policy.md`, `docs/prompt-caching-mechanics.md`
- `tools/mcp-ia-server/README.md`, `src/index.ts`, `src/tools/{backlog-issue,spec-section,spec-sections,router-for-task,invariant-preflight}.ts`
- `tools/scripts/claude-hooks/{bash-denylist,cs-edit-reminder,session-start-prewarm,verification-reminder}.sh`
- Sampled first 180 lines of `docs/lifecycle-opus-planner-sonnet-executor-exploration.md` for context only; **no change proposed** to the locked design.

Context inferred (not fully read): body of `docs/agent-lifecycle.md`, `docs/information-architecture-overview.md`, `docs/mcp-ia-server.md`, non-lifecycle skills under `ia/skills/*` (counted line totals; spot-checked 4).

## 9. Sources

- [Anthropic — Tool Definition Bloat fix (deferred tool loading)](https://medium.com/@DebaA/anthropic-just-shipped-the-fix-for-tool-definition-bloat-77464c8dbec9)
- [Anthropic Prompt Caching 2026: Cost, TTL, Latency Planning](https://aicheckerhub.com/anthropic-prompt-caching-2026-cost-latency-guide)
- [Anthropic Prompt Caching — 4 breakpoint limit + longest-matching-prefix (IllusionCloud)](https://blog.illusioncloud.biz/2026/01/13/prompt-caching-anthropic-cache-breakpoints/)
- [Claude Code Token Optimization: Full System Guide (2026)](https://buildtolaunch.substack.com/p/claude-code-token-optimization)
- [Best Practices for Claude Code — official docs](https://code.claude.com/docs/en/best-practices)
- [Claude API Prompt Caching docs](https://platform.claude.com/docs/en/build-with-claude/prompt-caching)

---

*End of audit.*
