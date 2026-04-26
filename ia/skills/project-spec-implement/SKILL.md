---
name: project-spec-implement
purpose: >-
  Use when executing the §Plan Digest of a Task spec (DB-backed) — minimal-diff implementation
  of authored Mechanical Steps, post `/stage-authoring`.
audience: agent
loaded_by: "skill:project-spec-implement"
slices_via: none
description: >-
  Use when executing the §Plan Digest of a Task spec stored in the DB (`ia_task_specs.body_md`),
  after `/stage-authoring` has populated mechanical steps. Triggers: "implement task spec",
  "execute mechanical steps", "ship task plan digest", implement BUG-/FEAT-/TECH- task.
phases:
  - Parse task id
  - Pull task bundle
  - Read §Plan Digest (detect shape — relaxed §Work Items vs legacy §Mechanical Steps)
  - Context load
  - Execute digest (apply work items / mechanical steps)
  - Compile gate
  - Scene wiring gate
  - Task exit
triggers:
  - implement task spec
  - execute mechanical steps
  - ship task plan digest
  - implement BUG-
  - implement FEAT-
  - implement TECH-
model: inherit
tools_role: custom
tools_extra: []
caveman_exceptions:
  - code
  - commits
  - security/auth
  - verbatim error/tool output
  - structured MCP payloads
hard_boundaries: []
---

# Task spec implementation (execution)

Caveman default — [`agent-output-caveman.md`](../../rules/agent-output-caveman.md).

DB is sole source of truth for task specs (`ia_task_specs.body_md`). Read via `task_bundle` / `task_spec_section`. No filesystem spec read.

**Related:** [`project-implementation-validation`](../project-implementation-validation/SKILL.md) (`validate:all`, `verify:local`) · [`ide-bridge-evidence`](../ide-bridge-evidence/SKILL.md) (Play Mode logs/screenshots) · [`close-dev-loop`](../close-dev-loop/SKILL.md) (fix→verify) · [`agent-test-mode-verify`](../agent-test-mode-verify/SKILL.md) (batchmode/bridge) · `/ship-stage` Pass A (caller in stage chain). Verification: [`docs/agent-led-verification-policy.md`](../../../docs/agent-led-verification-policy.md). Scene wiring: [`ia/rules/unity-scene-wiring.md`](../../rules/unity-scene-wiring.md).

## Inputs

| Param | Source | Notes |
|-------|--------|-------|
| `ISSUE_ID` | 1st arg | Task issue id (e.g. `TECH-471`). Status flips happen in caller (`ship-stage`), not here. |
| `STAGE_MCP_BUNDLE` | optional | Pre-loaded `domain-context-load` payload from caller chain. Avoids re-query when called inside `/ship-stage` chain. |

## When to use

`/ship-stage` Pass A invokes this skill once per non-terminal Task in a Stage. §Plan Digest must already exist in DB (authored by `/stage-authoring`); missing → caller halts at readiness gate.

## Stage MCP bundle contract

Stage opener (`ship-stage` Phase 2) calls [`domain-context-load`](../domain-context-load/SKILL.md) once; returned payload `{glossary_anchors, router_domains, spec_sections, invariants}` kept in Stage scope and passed in as `STAGE_MCP_BUNDLE`. Implementer reads from that payload — no re-query of `glossary_discover`, `glossary_lookup`, `router_for_task`, `spec_sections`, or `invariants_summary` per Task.

## Tool recipe (territory-ia)

1. **Parse target** — Resolve `ISSUE_ID` from input. Caller passes the id directly; do NOT extract from filesystem header.
2. **`task_bundle`** — `mcp__territory-ia__task_bundle({task_id: "{ISSUE_ID}"})` returns `{task, master_plan, stage, depends_chain, recent_journal, invariant_guardrails, files}`. Hard dep unsatisfied (`satisfied: false`, `soft_only: false`) → halt; surface to caller.
3. **Read §Plan Digest** — `mcp__territory-ia__task_spec_section({task_id: "{ISSUE_ID}", section: "§Plan Digest"})` (literal `§` prefix required; see [`plan-digest-contract.md` §Section heading literal](../../rules/plan-digest-contract.md)). **Two shapes valid** — detect by sub-heading presence:
   - **Relaxed shape (new)** — sub-headings: §Goal / §Acceptance / §Pending Decisions / §Implementer Latitude / §Work Items / §Test Blueprint / §Invariants & Gate. §Work Items = flat list of `{path: 1-line intent}` rows. NO verbatim before/after code blocks. Scene Wiring (when needed) appears as a single §Work Items row prefixed `(Scene Wiring)`.
   - **Legacy shape** — sub-heading §Mechanical Steps present. Each step carries literal Edit tuples (`operation`, `target_path`, `before_string`, `after_string`, `invariant_touchpoints`, `validator_gate`) + per-step Gate/STOP. Optional Scene Wiring step.
4. **Domain routing** — Use `STAGE_MCP_BUNDLE.router_domains` when supplied. If absent: `router_for_task` once, cached for the Task. No per-step re-query.
5. **`spec_section` (optional)** — Only when §Plan Digest cites a domain spec ID and the `STAGE_MCP_BUNDLE.spec_sections` payload lacks coverage. Set `max_chars`. No full `ia/specs/*.md` reads.
6. **Execute digest** — Branch on shape detected at step 3:
   - **Relaxed shape (§Work Items present):** for each row, locate anchor in current HEAD (grep / Read), decide operation type (Edit / Write / delete / create), apply minimal diff. Implementer owns micro-edit sequencing, helper extraction, name choices bounded by §Pending Decisions + §Implementer Latitude. After ALL §Work Items applied, run the SINGLE `validator_gate` from §Invariants & Gate (typical: `npm run unity:compile-check && npm run validate:all`). On `STOP-on-anchor-mismatch` (HEAD diverged from §Pending Decisions assumption) / `STOP-on-acceptance-unmet` / `STOP-on-invariant-regression` / `STOP-on-validator-fail` → halt; surface to caller; do NOT silently adapt.
   - **Legacy shape (§Mechanical Steps present):** apply each Edit tuple verbatim, in order. After every Edit, run the inline per-step `validator_gate`. Per-step STOP on failure.
   - **Both shapes:** minimal diff. English comments / logs. Obey `invariant_touchpoints` from `task_bundle.invariant_guardrails`: road preparation family, `InvalidateRoadCache()`, HeightMap↔`Cell.height`, no `GridManager` bloat, no new singletons, no `FindObjectOfType` in per-frame.
7. **Compile gate** — After all Edit tuples applied: `mcp__territory-ia__unity_compile()` (alias `unity_bridge_command get_compilation_status`) when Editor open; else `npm run unity:compile-check`. Errors → fix in-place; do NOT proceed to verification with broken build. Caller (`ship-stage` Pass A) treats compile failure as fast-fail gate.
8. **Optional deep guardrails** — `list_rules` / `rule_content` only if `task_bundle.invariant_guardrails` insufficient.
9. **Scene-wiring gate (MANDATORY per `ia/rules/unity-scene-wiring.md`)** — Trigger fires when §Plan Digest carries a **Scene Wiring** entry (legacy: dedicated mechanical step; relaxed: §Work Items row prefixed `(Scene Wiring)`) OR the diff fires any trigger (new runtime MonoBehaviour, new `[SerializeField]`, new StreamingAssets consumer, new prefab at scene boot, new scene-level `UnityEvent`): execute the wiring step in-band. Prefer `unity_bridge_command` kinds (`open_scene → create_gameobject → set_gameobject_parent → attach_component → assign_serialized_field → save_scene`); fall back to text-edit of `Assets/Scenes/{SCENE}.unity` when bridge unavailable. **Task exit fails** when triggers fired but `git diff --name-only` contains no `Assets/Scenes/*.unity` edit (or no new prefab placement). Emit the evidence block (scene/parent/component/serialized_fields/unity_events/compile_check) for caller to surface.
10. **Task exit** — Re-read §Acceptance + §Test Blueprint from §Plan Digest. Confirm every acceptance row has matching diff or test artifact. When §Test Blueprint lists bridge checks AND session has territory-ia + Postgres + Unity on REPO_ROOT: optionally `unity_bridge_command` per [`ide-bridge-evidence`](../ide-bridge-evidence/SKILL.md). When task touched `tools/mcp-ia-server`, `docs/schemas`, glossary, reference spec bodies feeding indexes, or committed index JSON: run [`project-implementation-validation`](../project-implementation-validation/SKILL.md). Caller (`ship-stage` Pass A) handles `task_status_flip(implemented)` + `journal_append` after Task exit clean — do NOT flip status here.

### Editor / agent diagnostics

Sorting, grid sampling, Edit vs Play Mode → `unity-development-context` §10 (Reports → `tools/reports/`). Attach paths in chat; artifacts gitignored.

### Branching (minimum set)

- **Roads/streets/interstate/bridge/wet run** → roads-system + isometric-geography-system via `router_for_task` + `spec_section`.
- **Water/HeightMap/shore/river/lake/water map** → water-terrain-system + relevant geo sections.
- **JSON/schema/DTO/interchange** (Save-adjacent) → persistence-system; do not change on-disk Save data unless §Plan Digest requires.

## Domain skill handoff

When work enters these areas, open corresponding skill (when shipped on BACKLOG.md):
- Roads / wet run / bridges
- Terrain / water / shore / HeightMap
- New MonoBehaviour manager / service

## Hard boundaries

- Do NOT read task spec body from filesystem — DB only via `task_bundle` / `task_spec_section`.
- Do NOT flip `task_status` here — caller owns flips (`task_status_flip(implemented)`).
- Do NOT re-query `domain-context-load` per Task — use `STAGE_MCP_BUNDLE` payload from caller.
- Do NOT change agreed game behavior without spec owner alignment — surface as anomaly to caller.
- Do NOT skip Scene-wiring gate when triggers fired — `Assets/Scenes/*.unity` edit + evidence block are mandatory.
- Do NOT commit — caller (`ship-stage` Pass B) emits the single stage commit.
- Do NOT delete task specs, archive rows, or purge ids — closeout territory.

## Completion

Emit caveman summary back to caller: `{ISSUE_ID}: §Plan Digest executed (shape={relaxed|legacy}; {n_work_items|n_steps} applied); compile=PASS; scene_wiring={ran|n/a}; ready_for_status_flip.` On anomaly, emit `STOPPED at {row|step} {N}: {reason}` and let caller decide.
