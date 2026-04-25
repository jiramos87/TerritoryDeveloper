---
name: project-spec-implement
purpose: >-
  Use when executing a ia/projects/{ISSUE_ID}.md Implementation Plan (shipping checklist phases),
  after the spec is ready—not for spec review.
audience: agent
loaded_by: "skill:project-spec-implement"
slices_via: none
description: >-
  Use when executing a ia/projects/{ISSUE_ID}.md Implementation Plan (shipping checklist phases),
  after the spec is ready—not for spec review. Triggers: "implement project spec", "execute project
  spec", "follow Implementation Plan", "ship spec phases", implement BUG-/FEAT-/TECH- project spec.
phases:
  - Parse target
  - Pull backlog issue
  - Orchestrator sync
  - Context load
  - Implement
  - Task exit
triggers:
  - implement project spec
  - execute project spec
  - follow Implementation Plan
  - ship spec phases
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

# Project spec implementation (execution)

Caveman default — [`agent-output-caveman.md`](../../rules/agent-output-caveman.md) (loaded by parent context or agent def).

No MCP calls from skill body. Follow **Tool recipe** below — context as slices, not whole specs.

**Related:** [`project-implementation-validation`](../project-implementation-validation/SKILL.md) (`validate:all`, `verify:local`) · [`ide-bridge-evidence`](../ide-bridge-evidence/SKILL.md) (Play Mode logs/screenshots) · [`close-dev-loop`](../close-dev-loop/SKILL.md) (fix→verify with `debug_context_bundle`) · [`agent-test-mode-verify`](../agent-test-mode-verify/SKILL.md) (batchmode/bridge after Load pipeline work) · Stage-scoped `/closeout` (closeout/IA persist/delete/archive/id purge). Verification: [`docs/agent-led-verification-policy.md`](../../../docs/agent-led-verification-policy.md). Scene wiring: [`ia/rules/unity-scene-wiring.md`](../../rules/unity-scene-wiring.md) — Task-exit gate (step 10).

## When to use

- **Kickoff** → spec needs editorial work, Open Questions, glossary alignment.
- **This skill** → execute `## 7. Implementation Plan` in order, minimal diffs.
- **Close** → after verified: migrate lessons, delete spec, archive row, purge ids.

## Orchestrator navigation

Orchestrator docs (`*master-plan*`, `step-*-*.md`, `stage-*-*.md`): navigate per `ia/rules/project-hierarchy.md`. Orchestrators define skeleton; implementation in child project specs. Do not execute orchestrator exit criteria directly — create child specs first.

Default: spec Status Final or In Review with game-logic Open Questions resolved. Draft/unresolved → state risk, prefer kickoff first.

## Seed prompt (parameterize)

Replace `{SPEC_PATH}` with the project spec path from the backlog **Spec:** line (`ia/projects/{ISSUE_ID}.md`). Use `{ISSUE_ID}` from the spec header `> **Issue:**` line when present.

```markdown
Implement @{SPEC_PATH} following its ## 7. Implementation Plan in order.
Use **territory-ia** in the sequence defined in **project-spec-implement**’s "Tool recipe (territory-ia)" (backlog_issue → domain-context-load once at Stage open → spec_section as needed per task).
Honor **invariants** and **AGENTS.md** **Pre-commit Checklist**. If a task touches **roads**, **water / HeightMap**, or **new managers**, follow the domain handoff to any shipped domain skills on [`BACKLOG.md`](../../../BACKLOG.md).
Update the project spec **Decision Log** / **Issues Found** when you discover gaps; do not change agreed game behavior without spec owner alignment.
```

## Stage MCP bundle contract

Stage opener calls [`domain-context-load`](../domain-context-load/SKILL.md) once; returned payload `{glossary_anchors, router_domains, spec_sections, invariants}` kept in Stage scope. All Sonnet pair-tail invocations within the Stage read from that payload — no re-query of `glossary_discover`, `glossary_lookup`, `router_for_task`, `spec_sections`, or `invariants_summary` inside a Stage. The 5-tool recipe (`glossary_discover → glossary_lookup → router_for_task → spec_sections → invariants_summary`) is encapsulated entirely in `domain-context-load`; callers never inline it.

## Tool recipe (territory-ia)

**Composite-first call (MCP available):**

1. **Parse target** — Load `{SPEC_PATH}`. Extract `ISSUE_ID` from `> **Issue:**`.
2. Call `mcp__territory-ia__issue_context_bundle({ issue_id: "{ISSUE_ID}" })` — first MCP call; returns `{ issue, depends_chain, routed_specs, invariant_guardrails, recent_journal }` in one bundle. Replaces steps 2+2b+3 below. Check `depends_chain` for hard-dep unsatisfied; use `routed_specs` for domain context; use `invariant_guardrails` for impl guardrails.
3. **Orchestrator sync** — from bundle `issue.spec` + `issue.notes`: `Glob ia/projects/*master-plan*.md` + `ia/projects/stage-*.md`; `Grep` for `{ISSUE_ID}` in task table. If row found: flip `In Review → In Progress` (or `Draft → In Progress` if kickoff was skipped). Update top-of-file `> **Status:**` pointer to reflect active task. No orchestrator found → log one-line note; continue.
4. **Task intent** — State which checkboxes in scope; list files/classes from plan + bundle `issue.files`.
5. **Domain routing** — Use `routed_specs` from bundle. If additional ad-hoc lookup needed: `router_for_task` once, then cache; do NOT repeat per task.
6. **`spec_section`** — Only sections the task needs; set `max_chars`. Use bundle `routed_specs` first. No full `ia/specs/*.md` unless `spec_outline` forces it.
7. **Implement** — Minimal diff. Obey invariants/guardrails from bundle `invariant_guardrails`: road preparation family, `InvalidateRoadCache()`, HeightMap↔`Cell.height`, no GridManager bloat.
8. **Optional deep guardrails** — `list_rules` / `rule_content` if bundle `invariant_guardrails` insufficient.
9. **Task exit** — Re-read §8 Acceptance + §7b Test Contracts. Run AGENTS.md Pre-commit Checklist. If §7b lists bridge checks + session has territory-ia + Postgres + Unity on REPO_ROOT → optionally `unity_bridge_command` per [`ide-bridge-evidence`](../ide-bridge-evidence/SKILL.md). If task touched `tools/mcp-ia-server`, `docs/schemas`, glossary, reference spec bodies feeding indexes, or committed index JSON → run [`project-implementation-validation`](../project-implementation-validation/SKILL.md) (`validate:all` / `verify:local`). When spec/user calls for agent-led test mode → optionally [`agent-test-mode-verify`](../agent-test-mode-verify/SKILL.md) after validate:all/compile gates. Record surprises in §9 Issues Found.
10. **Scene-wiring gate (MANDATORY per `ia/rules/unity-scene-wiring.md`)** — if §Plan Digest carries a **Scene Wiring** mechanical step OR the Task scope fires any trigger (new runtime MonoBehaviour, new `[SerializeField]`, new StreamingAssets consumer, new prefab at scene boot, new scene-level `UnityEvent`): execute the wiring step in-band. Prefer `unity_bridge_command` kinds (`open_scene → create_gameobject → set_gameobject_parent → attach_component → assign_serialized_field → save_scene`); fall back to text-edit of `Assets/Scenes/{SCENE}.unity` when bridge unavailable. **Task exit fails** when triggers fired but `git diff --name-only` contains no `Assets/Scenes/*.unity` edit (or no new prefab placement). Emit the evidence block (scene/parent/component/serialized_fields/unity_events/compile_check) in §8 Acceptance before declaring Task complete.

### Bash fallback (MCP unavailable)

Run in order (once per Stage, not per task).

1. **Parse target** — Load `{SPEC_PATH}`. Extract `ISSUE_ID` from `> **Issue:**`.
2. **`backlog_issue`** — Pull Files, Notes, Depends on, Acceptance, `depends_on_status`. Hard dep unsatisfied (`satisfied: false`, `soft_only: false`) → **stop** unless user overrides.
2b. **Orchestrator sync** — `Glob ia/projects/*master-plan*.md` + `ia/projects/stage-*.md`; `Grep` for `{ISSUE_ID}` in task table. If row found: flip `In Review → In Progress` (or `Draft → In Progress` if kickoff was skipped). Update top-of-file `> **Status:**` pointer to reflect active task. No orchestrator found → log one-line note; continue.
3. **`domain-context-load`** — Run [`ia/skills/domain-context-load/SKILL.md`](../domain-context-load/SKILL.md) once at Stage open. Inputs: `keywords` from spec domain + Files; `tooling_only_flag = true` for pure doc/IA stages; `brownfield_flag = false` for runtime C# stages. Use returned payload for all steps below — do NOT re-query within Stage.
4. **Task intent** — State which checkboxes in scope; list files/classes from plan + backlog Files.
5. **Domain routing** — Use `router_domains` from `domain-context-load` payload. If additional ad-hoc lookup needed: `router_for_task` once, then cache; do NOT repeat per task.
6. **`spec_section`** — Only sections the task needs; set `max_chars`. Use `spec_sections` from payload first. No full `ia/specs/*.md` unless `spec_outline` forces it.
7. **Implement** — Minimal diff. Obey invariants/guardrails: road preparation family, `InvalidateRoadCache()`, HeightMap↔`Cell.height`, no GridManager bloat.
8. **Optional deep guardrails** — `list_rules` / `rule_content` if payload `invariants` insufficient.
9. **Task exit** — Re-read §8 Acceptance + §7b Test Contracts. Run AGENTS.md Pre-commit Checklist. If §7b lists bridge checks + session has territory-ia + Postgres + Unity on REPO_ROOT → optionally `unity_bridge_command` per [`ide-bridge-evidence`](../ide-bridge-evidence/SKILL.md). If task touched `tools/mcp-ia-server`, `docs/schemas`, glossary, reference spec bodies feeding indexes, or committed index JSON → run [`project-implementation-validation`](../project-implementation-validation/SKILL.md) (`validate:all` / `verify:local`). When spec/user calls for agent-led test mode → optionally [`agent-test-mode-verify`](../agent-test-mode-verify/SKILL.md) after validate:all/compile gates. Record surprises in §9 Issues Found.
10. **Scene-wiring gate (MANDATORY per `ia/rules/unity-scene-wiring.md`)** — same contract as the MCP path step 10 above: when §Plan Digest carries **Scene Wiring** OR Task scope fires a trigger, execute the wiring (bridge-preferred, text-edit fallback) and emit the evidence block before declaring Task complete. Missing `Assets/Scenes/*.unity` edit under fired triggers = Task exit failure.

### Stage rollback

Verification fails → revert task commits (`git revert`/`git stash`), document in §9, re-run after root cause fix.

### Editor / agent diagnostics

Sorting, grid sampling, Edit vs Play Mode → `unity-development-context` §10 (Territory Developer → Reports → `tools/reports/`). Attach paths in chat; artifacts gitignored.

### Branching (minimum set)

- **Roads/streets/interstate/bridge/wet run** → roads-system + isometric-geography-system via `router_for_task` + `spec_section`.
- **Water/HeightMap/shore/river/lake/water map** → water-terrain-system + relevant geo sections.
- **JSON/schema/DTO/interchange** (Save-adjacent) → persistence-system (Load pipeline, Save data); do not change on-disk Save data unless issue requires; cross-check JSON interchange program notes.

## Domain skill handoff

When work enters these areas, open corresponding skill (when shipped on BACKLOG.md):
- Roads / wet run / bridges
- Terrain / water / shore / HeightMap
- New MonoBehaviour manager / service

## Spec maintenance

- Non-obvious scope/product choices → §6 Decision Log.
- Defects/surprises → §9 Issues Found.
- Code would change agreed game behavior → stop; update spec or ask owner.

## Completion

Map work to §8 Acceptance + backlog Acceptance. Do not archive (remove BACKLOG, append BACKLOG-ARCHIVE, id purge) without explicit user confirmation.

IA-heavy diff (MCP, fixtures, glossary/reference spec sources for indexes) → run/document [`project-implementation-validation`](../project-implementation-validation/SKILL.md) so CI-aligned Node checks not skipped.
