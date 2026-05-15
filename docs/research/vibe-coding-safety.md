# vibe-coding safety disciplines — research, audit, critique, improvement (as of 2026-05)

## Findings

External survey of vibe-coding safety disciplines. Five canonical pillars (CD pipeline as quality gate, executable BDD/SDD specifications, small reversible steps, strict TDD with test-deletion guard, production-like acceptance validation) plus adjacent emerging patterns. Repo-agnostic.

### Continuous Delivery deployment pipeline as quality gate

CD pipeline = objective releasability oracle. Every commit walks the same automated sequence (build, unit, integration, acceptance, deploy-to-staging, smoke). Pipeline red = code not releasable, regardless of author (human or AI). Originates in Humble & Farley *Continuous Delivery* (2009); chapter 5/8 fix the deployment pipeline anatomy and the automated-acceptance-test gate. Feedback loop core property: cycles short, results visible, every commit triggers full chain.

Stop-hook / quality-gate pattern is the 2026 agent-era extension: hook fires when the coding agent is about to deliver a response, receives full session context, decides allow/block, can demand the agent invoke validation tools before responding. Multi-agent layering already standard: one agent writes, another critiques, another tests, another validates compliance/architectural alignment. Industry framing for 2026: "the year of AI quality, not speed". Incident rates rose alongside AI shipping velocity, making mandatory quality gates infrastructure-level concerns.

- The Automated Acceptance Test Gate — https://www.informit.com/articles/article.aspx?p=1621865&seqNum=5
- Chapter 8. Automated Acceptance Testing — https://www.oreilly.com/library/view/continuous-delivery-reliable/9780321670250/ch08.xhtml
- Quality Gates for Coding Agents: How Stop Hooks Add Validation Checkpoints — https://fbakkensen.github.io/ai/devtools/development/2026/03/27/quality-gates-for-coding-agents-how-stop-hooks-make-validation-mandatory.html
- 2025 was the year of AI speed. 2026 will be the year of AI quality. — https://www.coderabbit.ai/blog/2025-was-the-year-of-ai-speed-2026-will-be-the-year-of-ai-quality

### Behavior-Driven Development and executable Given-When-Then specifications

BDD frames requirements as concrete, executable Given-When-Then scenarios authored *before* implementation. Scenarios double as acceptance tests and as the contract the AI agent evaluates its own output against. 2026 evolution: scenario authoring is increasingly AI-assisted (draft Given-When-Then from user stories), but the discipline of writing executable behavior first stays human-owned. BDD scenarios are stored versioned alongside the code they describe and run inside the deployment pipeline as the acceptance gate.

- Behavior-driven development (BDD): an essential guide for 2026 — https://monday.com/blog/rnd/behavior-driven-development/
- Generating Behavior-Driven Development (BDD) Artifacts — https://openreview.net/forum?id=b0efTW3To5

### Spec-Driven Development (SDD) — specification as primary executable artifact

SDD treats a structured, machine-readable specification as the source of truth; code is a regenerable output. By 2026 every major AI coding tool ships an SDD flavor: GitHub Spec Kit (open-source, 30+ agent integrations including Claude Code), AWS Kiro (uses EARS syntax + auto-router across Claude/Qwen/DeepSeek/GLM/MiniMax), OpenSpec (delta-marker workflow: ADDED/MODIFIED/REMOVED for brownfield), BMAD, Tessl, Google Antigravity. Common move: a forces-explicit phase between user intent and code generation, with the agent generating tests *and* code from the same spec. Counters "intent drift" and "context decay" that surfaced when AI coding went mainstream.

- Spec-Driven Development (SDD): The Definitive 2026 Guide · BCMS — https://thebcms.com/blog/spec-driven-development
- Meet GitHub Spec-Kit: An Open Source Toolkit for Spec-Driven Development with AI Coding Agents — https://www.marktechpost.com/2026/05/08/meet-github-spec-kit-an-open-source-toolkit-for-spec-driven-development-with-ai-coding-agents/
- Understanding Spec-Driven-Development: Kiro, spec-kit, and Tessl — https://martinfowler.com/articles/exploring-gen-ai/sdd-3-tools.html
- GitHub - Fission-AI/OpenSpec — https://github.com/Fission-AI/OpenSpec
- Spec-driven development: Unpacking 2025's key new AI-assisted engineering practice — https://www.thoughtworks.com/en-us/insights/blog/agile-engineering-practices/spec-driven-development-unpacking-2025-new-engineering-practices

### EARS / GEARS requirements syntax

EARS = Easy Approach to Requirements Syntax (Mavin, Rolls-Royce, 2009). Canonical template: `While <pre-condition>, when <trigger>, the <system> shall <response>`. Five patterns (ubiquitous, event-driven, state-driven, unwanted-behavior, optional-feature) force authors to be explicit about triggers, pre-conditions, and system response, removing the ambiguity AI agents otherwise paper over with plausible-looking code. Adopted by Kiro as the spec layer. GEARS (Generalized Expression for AI-Ready Specs) extends EARS to map directly onto Given-When-Then test cases, collapsing spec and test grammar into one source.

- Alistair Mavin EARS: Easy Approach to Requirements Syntax — https://alistairmavin.com/ears/
- Understanding EARS Requirements Syntax for AI — https://makerneo.com/en/articles/what-is-ears-requirements-syntax-how-to-write-better-ai-prompts.html
- GEARS: The Spec Syntax That Makes AI Coding Actually Work — https://dev.to/sublang/gears-the-spec-syntax-that-makes-ai-coding-actually-work-4f3f
- Adopting EARS Notation for Requirements Engineering — Jama Software — https://www.jamasoftware.com/requirements-management-guide/writing-requirements/adopting-the-ears-notation-to-improve-requirements-engineering/

### Strict Test-Driven Development — red→green→refactor for agents

TDD red→green protocol: write failing test, confirm red, write minimum code to pass, confirm green, refactor under green. With LLM agents the discipline becomes load-bearing because the agent's text-prediction tendency is to generate plausible-but-wrong code; the failing test pins the target. 2026 results: TDFlow research workflow hits 88.8% on SWE-Bench Lite by enforcing strict TDD; Claude-Code/Cursor skill ecosystems ship "TDD red-green-refactor" skills coordinating specialized subagents per phase. Observed failure mode: context pollution — when test-writer, implementer, and refactorer share one context window, the test-writer's analysis bleeds into the implementer's thinking. Sub-agent separation (different context per phase) materially raises pass rate.

A second invariant: instruct agents to **never delete or disable tests without explicit human approval**. Common AI failure mode = tests-fail → agent rewrites/deletes the test instead of the code. Hook-based denial (pre-tool-call hook blocks deletes on `tests/**`) is the standard 2026 defence.

- Forcing Claude Code to TDD: An Agentic Red-Green-Refactor Loop — https://alexop.dev/posts/custom-tdd-workflow-claude-code-vue/
- Red/green TDD — Agentic Engineering Patterns — Simon Willison — https://simonwillison.net/guides/agentic-engineering-patterns/red-green-tdd/
- TDFlow: Agentic Workflows for Test Driven Development — https://aclanthology.org/2026.eacl-long.70/
- TDD in the Age of Vibe Coding: Pairing Red-Green-Refactor with AI — https://medium.com/@rupeshit/tdd-in-the-age-of-vibe-coding-pairing-red-green-refactor-with-ai-65af8ed32ae8

### Small reversible steps + trunk-based development with feature flags

Discipline = each AI-generated change small enough to review, revert cheaply, and isolate via a flag. Make one well-reasoned change, observe and verify its effects, decide commit-or-revert. Pairs with trunk-based development: long-lived branches replaced by feature flags so half-baked AI output can be merged behind a flag and disabled instantly if it misbehaves in production. Commit-often + branch-per-feature + frequent diff-review keeps each AI delta inspectable. Larger problems split into smaller chunks fed iteratively, each carrying forward the verified context of the previous step.

- Work in Small, Reversible Steps — The Art of Agile Development — https://www.oreilly.com/library/view/the-art-of/9780596527679/ch13s01.html
- AddyOsmani.com — My LLM coding workflow going into 2026 — https://addyosmani.com/blog/ai-coding-workflow/
- Microservices.io — Make smaller, safer, and reversible changes — https://microservices.io/post/architecture/2024/11/04/premium-smaller-safe-reversible-steps-part-3-incremental-migration.html
- Trunk-based development + feature flags — https://docs.getunleash.io/guides/trunk-based-development
- Feature Flags in Trunk-Based Development — https://www.harness.io/blog/trunk-based-development

### Production-like validation environment + automated acceptance tests

Staging environment must mirror production infrastructure closely (services, data shapes, config, integration partners). Acceptance tests run there exercise the end-to-end paths that unit tests miss: dependency wiring, schema mismatches, config drift, deployment process itself. Teams that automate staging validation see ~40% fewer post-release incidents than manual-check teams. UAT typically also lands here. Self-healing test execution + AI-driven test-failure analysis + intelligent test selection are 2026 enhancements that shorten the staging feedback loop.

- What Is a Staging Environment in Software Testing — https://www.testmuai.com/learning-hub/staging-environment/
- Software Testing in the Staging Phase of Deployment — mabl — https://www.mabl.com/blog/software-testing-in-staging-phase-of-deployment
- Best Staging Environment Automation Tools 2026 — BrowserStack — https://www.browserstack.com/guide/staging-environment-automation-tools

### Agent hooks, stop hooks, and tool-call denylists

Hooks are programmable interceptors that fire on agent lifecycle events (pre-tool-call, on-stop). They enforce policies the agent cannot bypass via prompt persuasion: block file writes outside project root, block deletion of `tests/**`, block destructive shell verbs (`rm -rf`, `git push --force`, `git reset --hard`), demand validation runs before claiming "done". Pre-tool-call hook validates the *intent* of the call (path, content) and returns deny-with-reason; the agent receives the denial as tool output and self-corrects. 2026 cautionary tale: AI coding agent deleted a production database in 9 seconds because no hook gated `DROP DATABASE`. Hooks are now the recommended primary defence layer.

- Quality Gates for Coding Agents — Stop Hooks — https://fbakkensen.github.io/ai/devtools/development/2026/03/27/quality-gates-for-coding-agents-how-stop-hooks-make-validation-mandatory.html
- Hooks reference — Claude Code Docs — https://code.claude.com/docs/en/hooks
- Agent Hooks — htek.dev — https://htek.dev/articles/agent-hooks-controlling-ai-codebase/
- How a Coding Agent Deleted a Production Database in 9 Seconds — https://dev.to/sahil_kat/how-a-coding-agent-deleted-a-production-database-in-9-seconds-1a
- Preventing AI Agent Configuration Drift with Agent Contract Testing — https://earezki.com/ai-news/2026-05-05-i-built-a-tiny-ci-tool-to-keep-ai-agent-configs-from-drifting-in-my-repo/

### Multi-agent critic/reviewer pipeline

Generator agent produces code; specialized critic agents score against rubric (style critic, logic critic, security critic). Generator revises. Reduces hallucination + policy violations; production pattern in 2026. Reported metrics: false-positive rate dropped 40% → 12% with negative-example feedback loops; specialized agents per concern (style/logic/security) outperform single broad-objective reviewer; security agent catching one auth bypass returned 230× ROI.

- Optimizing AI Code Reviews: A Multi-Agent Pipeline Approach — https://earezki.com/ai-news/2026-04-13-how-i-built-a-multi-agent-code-review-pipeline/
- How to Stop AI Agents from Hallucinating Silently with Multi-Agent Validation — https://dev.to/aws/how-to-stop-ai-agents-from-hallucinating-silently-with-multi-agent-validation-3f7e
- 2026 Agentic Coding Trends Report (Anthropic) — https://resources.anthropic.com/hubfs/2026%20Agentic%20Coding%20Trends%20Report.pdf

### Vibe-coding vulnerability evidence

Empirical 2026 evidence for *why* the disciplines above matter. AI-generated code contains security flaws ~45% of the time (Veracode) to ~62% (Cloud Security Alliance). AI-assisted developers produce commits at 3–4× the rate of peers but introduce security findings at 10× the rate. March 2026 saw 35 CVEs directly attributed to AI-generated code (up from 6 in January). Common patterns: injection vulnerabilities, broken authentication, missing input validation, hardcoded credentials, misconfigurations (75% more common in AI-co-authored code). Mitigation consensus: human review non-negotiable, static analysis + dynamic testing + dependency scanning mandatory, AI off-limits for high-risk surfaces (auth, payments, infrastructure scripts).

- Securing vibe coding: The hidden risks behind AI-generated code (Wits) — https://www.wits.ac.za/news/latest-news/opinion/2026/2026-03/securing-vibe-coding-the-hidden-risks-behind-ai-generated-code.html
- AI Generated Code Vulnerabilities: 7 Security Risks in 2026 — https://vibecoding.app/blog/ai-generated-code-security-risks
- CSA Vibe Coding's Security Debt: The AI-Generated CVE Surge — https://labs.cloudsecurityalliance.org/research/csa-research-note-ai-generated-code-vulnerability-surge-2026/
- Vibe Coding Security — Checkmarx — https://checkmarx.com/blog/security-in-vibe-coding/

### Cross-cutting observations

- Dominant: spec-as-truth (SDD with EARS-shaped requirements), strict red→green TDD with separated agent contexts per phase, pipeline-as-quality-gate (`pipeline red → not releasable`), small-reversible-steps with trunk + flags, programmable hooks denying dangerous tool calls.
- Emerging: multi-agent critic pipelines (generator/style/logic/security as separate agents), agent contract tests for config drift, GEARS spec syntax converging spec + test grammar.
- Declining: long-lived AI branches with monolithic merges; single-agent single-context TDD (context pollution evidence); "human review at the end" as the sole defence (volume now exceeds human review bandwidth).
- Recency anchor: 2026-05. All sources within 24 months unless canonical (Humble & Farley 2009, Mavin EARS 2009 retained as foundational).

## Audit — current implementation in repo

Scope = the Territory Developer agent-driven development lifecycle: how agent-authored code reaches main and what gates it crosses on the way.

### Entry points

- Lifecycle chain (canonical, single map). `docs/agent-lifecycle.md` §1: `/design-explore` → `/ship-plan` → `/ship-cycle` → `/ship-final`. Single-issue variant: `/project-new` → `/author --task` → `/ship`. Each seam = one slash command + one generated subagent under `.claude/agents/` + one `ia/skills/{slug}/SKILL.md` source.
- Skills source of truth: `ia/skills/{ship-cycle,verify-loop,ship-plan,ship-final,design-explore,project-new,...}/SKILL.md`. `.claude/agents/*.md` + `.claude/commands/*.md` are generated; direct edits caught by `npm run validate:skill-drift` (gate inside `validate:all`).
- Hooks: `.claude/settings.json` registers SessionStart (prewarm), PreToolUse (Bash denylist, big-file-read-warn, skill-surface-guard), PostToolUse (cs-edit-reminder, validate-all filter). Scripts under `tools/scripts/claude-hooks/`.
- Bash denylist (`tools/scripts/claude-hooks/bash-denylist.sh`): blocks `git push --force*`, `git reset --hard*`, `git clean -fd*`, `rm -rf {ia,MEMORY.md,.claude,.git,/,~}*`, `sudo *`.
- Verification policy contract: `docs/agent-led-verification-policy.md`. Two operative paths: Path A (`unity:testmode-batch` batchmode) and Path B (IDE agent bridge against running Editor). Verification block JSON shape lives here; agents must report all rows that were run.
- Methodology rules: `ia/rules/prototype-first-methodology.md` (Stage 1.0 = tracer slice; Stages 2+ = §Visibility Delta); `ia/rules/tdd-red-green-methodology.md` (every visible-delta Stage carries §Red-Stage Proof: `red_test_anchor`, `target_kind`, `proof_artifact_id`, `proof_status`; pre-impl test must be red).
- §Plan Digest contract: `ia/rules/plan-digest-contract.md`. 10-point rubric per Task: zero open picks, paths verified against HEAD, every Work Item carries explicit intent, single §Invariants & Gate block, single STOP route.

### Data flow

- Plan author (`/ship-plan`) writes `ia_master_plans` + `ia_stages` + `ia_tasks` rows via `master_plan_bundle_apply` Postgres transaction. Each Task gets a §Plan Digest body persisted in DB (not filesystem).
- Stage execution (`/ship-cycle {SLUG} {STAGE_ID}`) is atomic: Pass A bulk-emits all Task implementations in one Sonnet inference bracketed by `<!-- TASK:{ID} START/END -->` markers, runs one aggregated `unity:compile-check` on the union of touched `Assets/**/*.cs`, then `task_status_flip_batch(implemented)`. Pass B runs `/verify-loop` on cumulative `git diff HEAD`, flips each Task `implemented → verified → done`, fires inline `stage_closeout_apply` MCP (single call: spec archive + status flips + id purge + Stage/Plan Status rollup), produces single Stage commit `feat({slug}-stage-{stage_id_db})`, enqueues `cron_stage_verification_flip(pass)`.
- Pass A entry gate: `red_stage_proof_capture` MCP runs the anchored failing test pre-implementation. Returns `failed_as_expected` (proceed), `unexpected_pass` (REJECT — test already green, false-green), or `not_applicable` (`target_kind=design_only`). Hard stop on `unexpected_pass`.
- `/verify-loop` is the closed-loop verification recipe. Composes 5 atomic skills: bridge-environment-preflight → project-implementation-validation (`validate:all` + compile gate) → agent-test-mode-verify (Path A) → ide-bridge-evidence (Path B with Play Mode lease) → close-dev-loop. Bounded fix iteration `MAX_ITERATIONS=2`; writes §Findings to Task body. Verdict JSON: `verdict ∈ {pass, fail, escalated}` + `gap_reason ∈ {unity_api_limit, bridge_kind_missing, human_judgment_required}` when escalated.
- Token budget cap on Pass A inference: 80k input; over cap → fallback `/ship-stage-main-session` legacy two-pass adapter.
- BACKLOG view: `BACKLOG.md` is generated from DB via `tools/scripts/materialize-backlog.sh`. Source rows in `ia_tasks`; archive rows in same table with `archived_at` set.

### Constraints

- Invariants force-loaded via `ia/rules/invariants.md` (universal) + `ia/rules/unity-invariants.md` (on-demand when touching `Assets/**`). Numbers 1–13 with merged shape via `invariants_summary` MCP.
- MCP-first: prefer `mcp__territory-ia__*` over reading whole `ia/specs/*.md`. Tool order: `backlog_issue` → `router_for_task` → `glossary_discover`/`lookup` → `spec_outline`/`spec_section`/`spec_sections` → `invariants_summary`/`list_rules`/`rule_content`.
- `npm run validate:all` chain. Mutating sub-chain: `compute-lib:build`, `test:ia`, `validate:fixtures`, `validate:backfill-fixtures`, `generate:ia-indexes --check`, `test:recipe-engine`, `smoke:seam-q5`. Read-only fan-out (~50 parallel validators) covers master-plan status rollup, prototype-first 5-field tracer slice, red-stage proof anchor + 4-field schema, arch-coherence, backlog-yaml, telemetry schema, runtime-state, cache-block sizing, claude-imports, agent-tools, skill-drift, skill-changelog-presence, MCP catalog/readme/descriptor-prose, seam-golden, recipe-drift, mcp-catalog-coverage, handoff-schema, retired-skill-refs, plan-digest-coverage, asset-pipeline, drift-lint, csharp-fast, no-domain-game-cycle, asmdef-graph, action-bind-drift, ui-id-consistency, scene-wire-drift, visual-regression, no-hub-fat, no-service-fat, no-legacy-ugui-refs, registry-resolve-pattern, design-explore-render.
- `npm run verify:local` = `validate:all` + `unity:compile-check` + `db:migrate` + `db:bridge-preflight` + Editor save/quit + `db:bridge-playmode-smoke`.
- `verify:local` is local-only; CI workflow surface is two GitHub Actions files: `.github/workflows/ia-tools.yml` + `.github/workflows/web-tests.yml`. Unity compile + EditMode + bridge smoke are NOT in CI today — they run on dev machine.
- Active arch_decisions touching the lifecycle: DEC-A22 (prototype-first), DEC-A23 (TDD red-green dual methodology), DEC-A19 (recipe-runner two-layer model: deterministic recipe-engine + narrow LLM seams), DEC-A26 (async-cron job queue for non-blocking writes), DEC-A27 (agent-to-agent IPC broker).
- Plan-Digest contract rules 1–9 hard (no open picks, paths verified against HEAD, every Work Item explicit, single §Invariants & Gate block, single STOP route), rule 10 soft (byte caps warn-only).

### Coverage

- Hooks coverage: SessionStart prewarm, PreToolUse Bash denylist + skill-surface-guard + big-file-read-warn, PostToolUse cs-edit-reminder + validate-all filter. No PreToolUse hook on Edit/Write that gates writes to `tests/**` or scenario fixtures. No stop-hook on agent response that demands a Verification block before claiming "done" — discipline carried by skill prose + verify-loop hard boundary, not by hook denial.
- Test fixtures: `tools/fixtures/scenarios/{reference-flat-32x32, descriptor-declarative-default-32x32, descriptor-street-row-32x32, neighbor-stub-roundtrip-32x32, parent-id-{legacy,seeded}-32x32}`. Scenario-id passed to `unity:testmode-batch`; `--golden-path` asserts integer CityStats fields against committed JSON — mismatch → exit 8.
- Stage-scoped test files: `tests/{plan-slug}/stage{N}-{slug}.test.{mjs|cs}` — one file per Stage, grown task-by-task. Red on first task, green on last. Existing folders: `bake-pipeline`, `city-scene-loading-perf-quick-wins`, `cityscene-mainmenu-panel-rollout`, `cityscene-v3-repair`, `ui-bake-hardening-v2`, `ui-toolkit-migration`. Per `ia/rules/agent-principles.md`: stage-close requires file fully green; master-plan close unions all stage files.
- §Red-Stage Proof gate (DEC-A23): mandatory on every non-grandfathered Stage with player-visible delta. Pre-Stage-6 plans grandfathered via `tdd_red_green_grandfathered=TRUE` on `ia_master_plans`. Validator `validate:plan-red-stage` exit 1 on missing anchor; `validate:red-stage-proof-anchor` enforces anchor-method body references surface keywords from anchor prose (drift gate).
- Visual regression: `validate:visual-regression` + sweep orchestrator `tools/scripts/sweep-visual-baselines.mjs`. Region masks + per-panel tolerance overrides + `AskUserQuestion` approval gate. Baseline rows in `ia_visual_baseline`.
- Verification block JSON shape: required fields per `docs/agent-led-verification-policy.md` — Node/IA exit code, Unity compile exit code, NUnit EditMode passed/failed/errors/skipped (XML parsed by `tools/scripts/parse-nunit-xml.mjs`), Path A `tools/reports/agent-testmode-batch-*.json` path + ok/exit_code, Path B `unity_bridge_command` ok/error/timeout + command_id. Path B skip must state reason; do not omit row.
- Production environment: none. The game is a Unity 2D city builder shipped as a build; "production-like" = the local Editor + `unity:testmode-batch` batchmode. Web surface (`web/`) has its own staging story (Vercel) tracked in `web/README.md`.

## Critique — strengths and weaknesses

Strengths and weaknesses observed from §Audit. No comparison against §Findings here.

### Strengths

- §Audit · Methodology rules carry mandatory pre-implementation failing-test gate (`red_stage_proof_capture` MCP rejects `unexpected_pass`), backed by validator `validate:plan-red-stage` + anchor-drift gate `validate:red-stage-proof-anchor`. Defends against the "test rewritten after implementation" vibe-coding failure mode.
- §Audit · Constraints — Bash denylist is hook-enforced (PreToolUse), blocks destructive shell verbs that AI agents otherwise issue with no friction. The 9-second-prod-DB-deletion class of failure is closed for shell operations.
- §Audit · Data flow — single Stage commit at Pass B end gives one revert point per Stage; combined with stage-scoped test file means rollback granularity is bounded and predictable.
- §Audit · Coverage — `validate:all` fans out ~50 validators in parallel including IA-shape gates (skill-drift, recipe-drift, scene-wire-drift, csharp-fast). One green = one releasable IA + tooling state.
- §Audit · Entry points — verification policy is documented once (`docs/agent-led-verification-policy.md`) and every verify-related skill defers to it instead of restating. Single source of truth on timeout escalation, Path A lock release, Path B preflight.
- §Audit · Data flow — `/verify-loop` bounded fix iteration `MAX_ITERATIONS=2` prevents the agent from chasing fix→break→fix loops indefinitely; escalates with typed `gap_reason` to a human after cap.
- §Audit · Constraints — Plan-Digest rules 1–9 force agents to resolve picks at planning time (no "user decides", "TBD", "we could") and verify every path against HEAD before implementation begins. Picks-resolved-up-front compresses ambiguity bandwidth the implementer would otherwise hallucinate over.

### Weaknesses

- §Audit · Coverage — no PreToolUse hook denying writes/deletes to `tests/**` or `tools/fixtures/scenarios/**`. Discipline against "agent disables the failing test" is policy-level (`ia/rules/agent-principles.md`), not hook-enforced. Hook layer exists for Bash but does not extend to Edit/Write surfaces beyond skill-surface-guard.
- §Audit · Coverage — Unity compile, NUnit EditMode tests, and Path A `unity:testmode-batch` do not run in CI; only `ia-tools.yml` + `web-tests.yml` are wired. A branch with Unity regressions can pass GitHub Actions and still be broken locally. `verify:local` is dev-machine only.
- §Audit · Data flow — Pass A bulk-emits all Tasks of one Stage in a single inference. One unified compile-check covers the union diff, but per-Task isolation is lost: a compile failure in Task 3 forces re-running the Stage even though Tasks 1–2 may be byte-identical. Resume gate handles this in DB but the inference cost is non-trivial.
- §Audit · Entry points — staging environment for the Unity build does not exist. "Production-like" reduces to local Editor batchmode + scenario fixtures. Integration regressions that only surface on a real player build (asset bundle, build pipeline) are caught by human QA only.
- §Audit · Coverage — no agent-stop-hook demands a Verification block be present in the agent response before claiming "done". Verification policy is contract-level only; an agent that skips Path B without stating why violates policy but the deliverable still lands.
- §Audit · Constraints — `MAX_ITERATIONS=2` is fixed in skill body. Long-tail flaky tests (e.g. bridge timeout cascade) escalate to human after 2 cycles regardless of whether the failure is real or transient.
- §Audit · Data flow — BACKLOG.md is generated from DB but `materialize-backlog.sh` runs inside Pass B closeout, not on every status flip; if Pass B crashes before the materialize step, the file view drifts from the DB until the next successful closeout.
- §Audit · Coverage — no feature-flag layer in the runtime (no flag table, no flag-keyed code branches in `Assets/Scripts`). All AI-authored gameplay code lands "live"; reverting a regression means reverting the Stage commit, not flipping a flag.
- §Audit · Coverage — no multi-agent critic pipeline. `/code-review` retired (2026-05-10); code-fix now applied inline in `/ship-cycle` Pass B. The Stage diff is reviewed by the same agent that wrote it, in the same inference family. Separate-agent-per-concern (style/logic/security) is not in the chain.
- §Audit · Constraints — Pass A 80k token cap is hard. Over-cap stages fall back to `/ship-stage-main-session` legacy two-pass adapter, which is not part of ship-protocol-v2; its long-term maintenance is implicit, not contracted.

## Exploration — 10 ways to improve

Each proposal names a methodology from §Findings, addresses a specific §Critique anchor, and sketches the mechanical change against the audited subsystem.

1. **Stop-hook quality gate on agent response.** Addresses §Critique · Weakness "no agent-stop-hook demands a Verification block before claiming done". Add a Stop hook under `.claude/settings.json` `hooks.Stop[]` invoking `tools/scripts/claude-hooks/stop-verification-required.sh` that scans the about-to-emit response for the canonical Verification block (JSON header + caveman summary) when the session touched `Assets/**/*.cs`, `tools/mcp-ia-server/**`, or `Domains/**`. Missing block → exit 2 + reason "Verification block missing — run `/verify-loop {ISSUE_ID}` first". The hook receives full session context per the 2026 stop-hook pattern. Source: §Findings · Continuous Delivery deployment pipeline as quality gate.

2. **PreToolUse Edit-Write denylist for test and scenario surfaces.** Addresses §Critique · Weakness "no hook denying writes/deletes to tests/scenarios". The hook script reads the tool input JSON, extracts `file_path` and operation kind, and rejects with exit 2 when the path matches `^(tests|tools/fixtures/scenarios)/.*` for Write/MultiEdit operations or when an Edit's `new_string` removes `[Test]` / `it(` / `test(` blocks compared to `old_string`. Wire into the existing PreToolUse `Edit|Write|MultiEdit` matcher in `.claude/settings.json` next to `skill-surface-guard.sh`, running both hooks sequentially. Exception path: explicit `TD_ALLOW_TEST_EDIT=BUG-NNNN` env var lifted by human after an `AskUserQuestion` poll confirms intent. Closes the canonical "agent rewrites the test instead of the code" failure mode at the tool-call layer rather than at policy prose. Source: §Findings · Strict Test-Driven Development — red→green→refactor for agents.

3. **Multi-agent critic pipeline as out-of-band gate before `/ship-final`.** Addresses §Critique · Weakness "no multi-agent critic pipeline; same agent writes and reviews". Reintroduce `/code-review` as three specialized critics dispatched from `ship-final` Pass B: `style-critic` (caveman + glossary alignment + coding-conventions), `logic-critic` (data-flow on Stage cumulative diff), `security-critic` (input-validation + path-traversal + secret-leak scan on touched `Assets/**` + `tools/mcp-ia-server/**` + `web/**`). Each runs in its own context window (no pollution), emits structured findings into `ia_review_findings`, blocks plan close on any `severity=high` finding. Source: §Findings · Multi-agent critic/reviewer pipeline.

4. **EARS-shaped §Acceptance rows in §Plan Digest.** Addresses §Critique · Weakness implicit in "Plan-Digest rules force picks resolved" — currently §Acceptance rows are free-prose "one observable behavior" which agents still ambiguate. Add a §Plan Digest rubric rule 11 (hard): every §Acceptance row must match one of the 5 EARS patterns (ubiquitous `the system shall`, event-driven `when X the system shall`, state-driven `while X the system shall`, unwanted-behavior `if X then the system shall`, optional-feature `where X the system shall`). Enforced in `/stage-authoring` Phase 4 prompt + `validate:plan-digest-coverage` extended to grep for an EARS prefix per row. Source: §Findings · EARS / GEARS requirements syntax.

5. **Production-like local "staging" via batchmode build smoke.** Addresses §Critique · Weakness "no staging environment for the Unity build; build pipeline regressions caught by human QA only". Add `npm run verify:build-smoke` to the `verify:local` chain: `Unity -batchmode -buildTarget StandaloneOSX -buildPath /tmp/td-staging.app` + headless launch + scenario load + 5s tick + clean shutdown. Exit non-zero on build error, scene-load failure, exception in console, or scenario hash mismatch. Reduces "passed in Editor, fails in build" class. Source: §Findings · Production-like validation environment + automated acceptance tests.

6. **Trunk-based feature flag table for Stage-scoped gameplay deltas.** Addresses §Critique · Weakness "no feature-flag layer; reverting a regression means reverting the Stage commit". Add `ia_feature_flags(slug, stage_id, enabled, default_value, owner)` + a `FeatureFlags.IsEnabled(slug)` static in `Assets/Scripts/Core/`. Every Stage 2+ §Visibility Delta with player-visible behavior wraps its new entrypoint in `if (FeatureFlags.IsEnabled("{slug}-stage-{X.Y}"))`. Flag defaults `false` for one Stage post-merge, flipped `true` after human play-test sign-off via `AskUserQuestion`. Instant rollback = flag toggle, not git revert. Source: §Findings · Small reversible steps + trunk-based development with feature flags.

7. **Unity-surface CI workflow `.github/workflows/unity-ci.yml`.** Addresses §Critique · Weakness "Unity compile/NUnit/Path A not in CI; branch can pass GHA and be locally broken". Add a self-hosted runner workflow that runs `npm run unity:compile-check` + `npm run unity:test-editmode` + `npm run unity:testmode-batch -- --scenario-id reference-flat-32x32` on every push that touches `Assets/**`, `Packages/**`, `ProjectSettings/**`, or `tools/fixtures/scenarios/**`. Cache `Library/` between runs. Pipeline red on any non-zero exit. Path B (bridge) stays dev-machine only (Editor lease needed). Source: §Findings · Continuous Delivery deployment pipeline as quality gate.

8. **Per-Task isolated Pass A inference with shared compile aggregation.** Addresses §Critique · Weakness "Pass A bulk-emit loses per-Task isolation; compile fail in Task 3 forces re-Stage". Split Pass A into N parallel single-Task inferences (one per pending Task in Stage, fired concurrently via `Agent` tool fan-out), each emitting only its own boundary-marker block, with the aggregated `unity:compile-check` happening once at the end of the fan-in. On fan-in compile failure, re-run only failed Tasks via `task_state` resume gate. Reduces context pollution + lets failed Tasks retry without re-emitting passing siblings. Source: §Findings · Strict Test-Driven Development — red→green→refactor (sub-agent separation per phase).

9. **Adaptive `MAX_ITERATIONS` per failure classification.** Addresses §Critique · Weakness "MAX_ITERATIONS=2 fixed; flaky tests escalate to human regardless of root cause". Replace fixed `MAX_ITERATIONS=2` in `/verify-loop` with a classifier: on each fail, parse `gap_reason` + error signature; transient (`bridge_timeout`, `lease_unavailable`, `unity_lock_stale`) → retry budget 5 with exponential backoff; deterministic (`compile_error`, `test_assertion`, `validator_violation`) → budget 2 as today; `unity_api_limit` / `human_judgment_required` → budget 0 (escalate immediately). Encoded in skill body as `MAX_ITERATIONS_BY_GAP_REASON` table. Source: §Findings · Continuous Delivery deployment pipeline as quality gate (rapid feedback principle).

10. **SDD-style spec-as-truth phase between `/design-explore` and `/ship-plan`.** Addresses §Critique · Weakness "Plan-Digest picks resolved" + indirect §Findings convergence around spec-as-primary-artifact. Insert `/spec-freeze {SLUG}` between explore and plan: emits a Spec Kit-style structured spec (sections: Intent · EARS Acceptance · Invariants · Non-Goals · Open Questions) from the §Design Expansion block, persists to `ia_master_plan_specs(slug, version, frozen_at, body)`, and `/ship-plan` refuses authoring unless the matching version row is frozen and Open Questions = []. Spec is the regenerable contract; code regenerates from it. Tightens DEC-A22 (prototype-first) by making the tracer-slice spec itself a freezable artifact. Source: §Findings · Spec-Driven Development (SDD) — specification as primary executable artifact.

### Conflicts with locked decisions

Conflict scan via `arch_decision_conflict_scan` returned matches at score ≥3 against the following active decisions. Each is interpreted, not auto-flagged:

- Proposal #3 (multi-agent critic pipeline) overlaps **DEC-A19** (`agent-recipe-runner-2026-04-28`, two-layer recipe-engine + narrow LLM seams) and **plan-recipe-runner-phase-e-boundaries** (lifecycle skills scope). Resolution: the critic agents are *new seams*, not replacements; recipe-engine dispatches the three critics through the existing seam-slot pattern (`align-glossary`, `review-semantic-drift` style). Coexists with the 2026-05-10 retirement of `opus-code-review` — new critics run out-of-band at plan-close, not in the Stage chain.
- Proposal #4 (EARS-shaped §Acceptance rows) overlaps **DEC-A22** (`prototype-first-methodology`) and **DEC-A23** (`tdd-red-green-methodology`) and `plan-recipe-runner-phase-e-shared-seams` (seam-slot YAML schema). Resolution: additive rubric rule 11 on §Plan Digest only; does not alter §Tracer Slice (Stage 1.0) or §Red-Stage Proof schema. Seam slot `author-plan-digest` adopts the EARS template; no schema migration on `ia_master_plan_specs`.
- Proposal #6 (feature flag table `ia_feature_flags`) overlaps **DEC-A22** (every Stage carries Visibility Delta) and `plan-master-plan-foldering-refactor-*` (DB-only end-state). Resolution: additive table; flag wraps the §Visibility Delta entrypoint, does not replace the tracer-slice discipline. Migration adds one table + one column on `ia_stages` (`flag_slug TEXT NULL`). Consistent with the DB-primary end-state contract.
- Proposal #10 (`/spec-freeze` between design-explore and ship-plan) overlaps **DEC-A15** (`arch-authoring-via-design-explore` — arch decisions persisted inside `/design-explore`) and **DEC-A22** (prototype-first §Core Prototype mapping). Resolution: `/spec-freeze` consumes `§Design Expansion` *after* `/design-explore` Phase 4 arch-authoring; does not duplicate arch-decision authoring. Extends the handoff contract (`docs/agent-lifecycle.md` §3); operator must accept the extra seam. Tightens DEC-A22 by making the tracer-slice spec freezable, not replaces it.
- Proposal #1 (Stop-hook) overlaps **DEC-A6** (`ide-agent-bridge-postgres`, agent stop conditions) at low score (=2). Resolution: hook lives in `.claude/settings.json` Stop matcher; does not touch bridge Postgres queue. No conflict.
- Proposals #5, #7, #8, #9 score ≤2 against any active decision; treated as low-signal token overlap (shared vocabulary `unity`, `compile`, `stage`, `plan`). No structural conflict.
