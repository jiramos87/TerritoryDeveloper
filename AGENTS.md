# AI Agent Guide — Territory Developer

> **TL;DR.** `territory-ia` MCP first (`backlog_issue` → `router_for_task` → `glossary_*` → `spec_section`). Ship via project-spec lifecycle (create → author → implement → verify → review → audit → close Stage-scoped). Emit **Verification** block per [`docs/agent-led-verification-policy.md`](docs/agent-led-verification-policy.md) at completion. Hard guardrails: [`ia/rules/invariants.md`](ia/rules/invariants.md). Native host surface (Claude Code hooks, slash commands, subagents): [`CLAUDE.md`](CLAUDE.md).

## 1. Before you start

1. Read `/// <summary>` on C# class about to modify.
2. Have issue id → **`backlog_issue`** (territory-ia). Else [`BACKLOG.md`](BACKLOG.md).
3. [`ia/rules/agent-router.md`](ia/rules/agent-router.md) → right specs per task.
4. **MCP first**: in MCP-enabled hosts, `territory-ia` is default retrieval. Order: `backlog_issue` → `router_for_task` → `glossary_discover` / `glossary_lookup` (English only — translate from chat) → `spec_outline` / `spec_section` / `spec_sections` → `invariants_summary` / `list_rules` / `rule_content`. Closing project spec: `project_spec_closeout_digest` after `backlog_issue`. Ref: [`docs/mcp-ia-server.md`](docs/mcp-ia-server.md).
5. **Stale content:** territory-ia MCP caches per-process. After large edits, prefer fresh `read_file` or restart MCP server.

## 2. Agent lifecycle

Canonical flow (exploration → close). Full matrix, handoff contract, decision tree: [`docs/agent-lifecycle.md`](docs/agent-lifecycle.md). Always-loaded anchor: [`ia/rules/agent-lifecycle.md`](ia/rules/agent-lifecycle.md).

```
/design-explore → /master-plan-new → [/stage-decompose (re-decompose only)] → /stage-file (→ /author Stage 1×N) → /plan-review (→ plan-fix-apply when critical) → [per-Task loop: /implement → /verify-loop → /code-review (→ code-fix-apply when critical)] → /audit (Stage 1×N) → /closeout (Stage-scoped: stage-closeout-plan → stage-closeout-apply)
```

Single-task path (N=1): `/project-new (→ project-new-apply) → /author (N=1) → /implement → /verify-loop → /code-review → /audit (N=1) → /closeout (N=1)`.

Stage-end batching: `/author`, `/audit`, `/closeout` all fire ONCE per Stage (bulk Stage 1×N). Per-Task seams = `/implement`, `/verify-loop`, `/code-review`. No `/kickoff`, no `/enrich`, no per-Task close — all absorbed into Stage-scoped bulk pair shape (M6 collapse).

| # | Seam | Slash command | Subagent(s) | Skill | Purpose |
|---|------|---------------|-------------|-------|---------|
| 1 | Explore | [`/design-explore`](.claude/commands/design-explore.md) | `design-explore` | [`design-explore`](ia/skills/design-explore/SKILL.md) | Exploration doc → reviewed design + `## Design Expansion` |
| 2 | Orchestrate | [`/master-plan-new`](.claude/commands/master-plan-new.md) | `master-plan-new` | [`master-plan-new`](ia/skills/master-plan-new/SKILL.md) | Design expansion → `ia/projects/{slug}-master-plan.md` (orchestrator, permanent) |
| 2a | Extend orchestrator | [`/master-plan-extend`](.claude/commands/master-plan-extend.md) | `master-plan-extend` | [`master-plan-extend`](ia/skills/master-plan-extend/SKILL.md) | Append new Steps (fully decomposed) to existing orchestrator |
| 2b | Decompose step | [`/stage-decompose`](.claude/commands/stage-decompose.md) | `stage-decompose` | [`stage-decompose`](ia/skills/stage-decompose/SKILL.md) | Re-decompose one Step when scope pivots |
| 3 | Bulk-file stage (pair) | [`/stage-file`](.claude/commands/stage-file.md) | `stage-file-planner` → `stage-file-applier` | [`stage-file-plan`](ia/skills/stage-file-plan/SKILL.md) → [`stage-file-apply`](ia/skills/stage-file-apply/SKILL.md) | Stage → N yaml + spec stubs; auto-chains `/author` |
| 4 | Single issue (pair) | [`/project-new`](.claude/commands/project-new.md) | `project-new-planner` → `project-new-applier` | [`project-new`](ia/skills/project-new/SKILL.md) → [`project-new-apply`](ia/skills/project-new-apply/SKILL.md) | One yaml + one spec stub; auto-chains `/author --task` |
| 5 | Bulk author (Stage 1×N) | [`/author`](.claude/commands/author.md) | `plan-author` | [`plan-author`](ia/skills/plan-author/SKILL.md) | Write ALL N `§Plan Author` sections (audit notes + examples + test blueprint + acceptance) in one Opus pass; canonical-term fold absorbed (no `/enrich`) |
| 6 | Plan review (pair) | [`/plan-review`](.claude/commands/plan-review.md) | `plan-reviewer` → `plan-fix-applier` | [`plan-review`](ia/skills/plan-review/SKILL.md) → [`plan-fix-apply`](ia/skills/plan-fix-apply/SKILL.md) | Review Stage plan quality; fix tuples on critical |
| 7 | Implement | [`/implement`](.claude/commands/implement.md) | `spec-implementer` | [`project-spec-implement`](ia/skills/project-spec-implement/SKILL.md) | Execute Implementation Plan phase by phase (per-Task) |
| 8 | Verify (single-pass) | [`/verify`](.claude/commands/verify.md) | `verifier` | composed | Lightweight Verification block, read-only |
| 8 | Verify (closed-loop) | [`/verify-loop`](.claude/commands/verify-loop.md) | `verify-loop` | [`verify-loop`](ia/skills/verify-loop/SKILL.md) | 7-step closed loop + bounded fix iteration |
| 8 | Test-mode ad-hoc | [`/testmode`](.claude/commands/testmode.md) | `test-mode-loop` | [`agent-test-mode-verify`](ia/skills/agent-test-mode-verify/SKILL.md) | Path A batch / Path B bridge hybrid in isolation |
| 9 | Code review (pair) | [`/code-review`](.claude/commands/code-review.md) | `opus-code-reviewer` → `code-fix-applier` | [`opus-code-review`](ia/skills/opus-code-review/SKILL.md) → [`code-fix-apply`](ia/skills/code-fix-apply/SKILL.md) | Per-Task diff review against spec + invariants; fix tuples on critical |
| 10 | Audit (Stage 1×N) | [`/audit`](.claude/commands/audit.md) | `opus-auditor` | [`opus-audit`](ia/skills/opus-audit/SKILL.md) | Synthesize N per-Task `§Audit` paragraphs in one Opus pass post all per-Task loops; R11 `§Findings` gate |
| 11 | Stage-scoped chain ship | [`/ship-stage`](.claude/commands/ship-stage.md) | `ship-stage` | [`ship-stage`](ia/skills/ship-stage/SKILL.md) | Chain author → implement → verify-loop (`--skip-path-b`) → code-review → audit → closeout per non-Done task in a Stage X.Y; batched Path B at stage end; chain-level stage digest |
| 12 | Close Stage (pair) | [`/closeout`](.claude/commands/closeout.md) | `stage-closeout-planner` → `stage-closeout-applier` | [`stage-closeout-plan`](ia/skills/stage-closeout-plan/SKILL.md) → [`stage-closeout-apply`](ia/skills/stage-closeout-apply/SKILL.md) | Stage-scoped: one invocation closes ALL Task rows of one Stage X.Y in bulk (archive N yaml + delete N specs + flip N rows + unified migration + chain-level digest) |
| 13 | Rollout umbrella | [`/release-rollout`](.claude/commands/release-rollout.md) | `release-rollout` | [`release-rollout`](ia/skills/release-rollout/SKILL.md) (+ `-enumerate`, `-track`, `-skill-bug-log` helpers) | Advance one umbrella rollout-tracker row through 7-column lifecycle (a)–(g) toward (f) ≥1-task-filed |
| — | Progress emit (preamble) | *(none)* | *(all agents, `@`-load)* | [`subagent-progress-emit`](ia/skills/subagent-progress-emit/SKILL.md) | Cross-cutting `⟦PROGRESS⟧` stderr marker shape + `phases:` frontmatter contract |

Retired surfaces (post-M6 — do not reference in new skills / agents / commands): `/kickoff` + `spec-kickoff` + `project-spec-kickoff` (folded into `plan-author`); `project-stage-close` + `project-spec-close` (folded into Stage-scoped `/closeout` pair). Tombstones under `ia/skills/_retired/`, `.claude/agents/_retired/`, `.claude/commands/_retired/`.

Domain-skill (not in main flow): [`ui-hud-row-theme`](ia/skills/ui-hud-row-theme/SKILL.md) for HUD/menu rows with `UiTheme`. Verification building blocks (composed by `/verify-loop`, invokable standalone via `Skill` tool): [`bridge-environment-preflight`](ia/skills/bridge-environment-preflight/SKILL.md), [`project-implementation-validation`](ia/skills/project-implementation-validation/SKILL.md), [`ide-bridge-evidence`](ia/skills/ide-bridge-evidence/SKILL.md), [`close-dev-loop`](ia/skills/close-dev-loop/SKILL.md).

Hard rules (enforced at handoff):

- Orchestrator docs (`*master-plan*`) are permanent — NEVER closeable via `/closeout`. See [`ia/rules/orchestrator-vs-spec.md`](ia/rules/orchestrator-vs-spec.md).
- `/verify` = single pass, read-only. `/verify-loop` = bounded fix iteration (`MAX_ITERATIONS=2`). Both defer to [`docs/agent-led-verification-policy.md`](docs/agent-led-verification-policy.md); never restate the policy.
- `/closeout` is Stage-scoped. One invocation closes ALL Task rows of one Stage X.Y in bulk (planner → applier pair). Per-Task closeout surface retired.
- Pair contract. All Plan-Apply pair seams (`stage-file`, `project-new`, `plan-review`, `code-review`, `closeout`) obey [`ia/rules/plan-apply-pair-contract.md`](ia/rules/plan-apply-pair-contract.md): Opus pair-head writes `{operation, target_path, target_anchor, payload}` tuples; Sonnet pair-tail reads verbatim and applies.
- Missing handoff artifact → next stage refuses to start. Full contract: [`docs/agent-lifecycle.md`](docs/agent-lifecycle.md) §3.

Skill index + conventions: [`ia/skills/README.md`](ia/skills/README.md). Claude Code host surface (hooks, agent bodies, command dispatchers): [`CLAUDE.md`](CLAUDE.md) §3.

### 2a. Skill-lifecycle retrospective (skill-train)

**skill-train** sits outside main lifecycle flow — retrospective meta-surface, never closeable, never auto-applied. On demand (`/skill-train {SKILL_NAME}`), Opus subagent reads target skill's Per-skill Changelog entries since last `source: train-proposed` marker, aggregates recurring friction (≥2 occurrences threshold; configurable via `--threshold N`), writes **patch proposal (skill)** as `ia/skills/{SKILL_NAME}/train-proposal-{YYYY-MM-DD}.md` sibling file — unified-diff against Phase sequence / Guardrails / Seed prompt sections. User-gated review + manual apply only. Sibling producer `release-rollout-skill-bug-log` feeds a separate channel (`source: user-logged`, not self-reported friction) — do NOT merge channels. See glossary terms `skill self-report`, `skill training`, `patch proposal (skill)`, `skill-train`.

## 3. Verification policy (canonical)

Canonicalized at [`docs/agent-led-verification-policy.md`](docs/agent-led-verification-policy.md); surfaced as always-on rule [`ia/rules/agent-verification-directives.md`](ia/rules/agent-verification-directives.md). Policy doc carries **Verification block** format (Node/IA, Unity compile, Path A batch, Path B bridge), bridge timeout (40 s initial, escalation, 120 s ceiling), Path A project-lock release. Do NOT restate here, in skills, or rules.

## 4. Documentation hierarchy

```
docs/information-architecture-overview.md → IA philosophy, layers, lifecycle, extension guide
docs/agent-led-verification-policy.md     → canonical Verification policy
docs/mcp-ia-server.md                     → territory-ia MCP tool catalog + recipes
ia/rules/                                 → guardrails (always-loaded; light)
ia/skills/                                → workflow recipes (orchestration, not facts)
ia/specs/                                 → deep reference (read on demand per task)
ia/projects/                              → temporary project specs (deleted on close); orchestrator docs (permanent)
ia/rules/project-hierarchy.md             → step/stage/phase/task execution hierarchy
ia/rules/orchestrator-vs-spec.md          → orchestrator vs project spec distinction
ia/templates/                             → project spec template + frontmatter schema
ARCHITECTURE.md                           → runtime layers, dependency map, Local verification
BACKLOG.md / BACKLOG-ARCHIVE.md           → issue tracking (root)
CLAUDE.md / AGENTS.md                     → host entry points (root)
MEMORY.md                                 → project-level architectural memory (root)
```

### `ia/specs/` inventory

| File | Scope |
|------|-------|
| [`isometric-geography-system.md`](ia/specs/isometric-geography-system.md) | Canonical: terrain, water, cliffs, shores, sorting, terraform, roads, rivers, pathfinding |
| [`ui-design-system.md`](ia/specs/ui-design-system.md) | UI foundations, components, patterns |
| [`roads-system.md`](ia/specs/roads-system.md) | Road placement pipeline, validation, resolver, bridge rules, land slope stroke policy |
| [`simulation-system.md`](ia/specs/simulation-system.md) | Simulation tick order, AUTO pipeline, growth |
| [`persistence-system.md`](ia/specs/persistence-system.md) | Save/load pipeline, visual restore |
| [`water-terrain-system.md`](ia/specs/water-terrain-system.md) | Height model, water bodies, cliffs, shores, cascades |
| [`managers-reference.md`](ia/specs/managers-reference.md) | Managers + helper services: responsibilities, deps |
| [`glossary.md`](ia/specs/glossary.md) | Domain term definitions (English only) |
| [`unity-development-context.md`](ia/specs/unity-development-context.md) | Unity patterns: MonoBehaviour lifecycle, Inspector / `SerializeField`, `FindObjectOfType`, Script Execution Order |
| [`audio-blip.md`](ia/specs/audio-blip.md) | Blip procedural SFX subsystem: DSP kernel, authoring, runtime architecture, fixtures, invariants |
| [`REFERENCE-SPEC-STRUCTURE.md`](ia/specs/REFERENCE-SPEC-STRUCTURE.md) | Meta: author + extend reference specs |

Do NOT add bug write-ups, agent prompts, one-off specs under `ia/specs/`. Use `BACKLOG.md` while open; delete temp markdown after completion.

## 5. `ia/projects/` policy

Project-specific specs for features or complex bugs **in active development** → `ia/projects/`. **Temporary** — deleted after verified completion.

| Aspect | Rule |
|--------|------|
| Template | [`ia/templates/project-spec-template.md`](ia/templates/project-spec-template.md) |
| Structure | [`ia/projects/PROJECT-SPEC-STRUCTURE.md`](ia/projects/PROJECT-SPEC-STRUCTURE.md) |
| Naming | `{ISSUE_ID}-{description}.md` (e.g. `BUG-37-zone-cleanup.md`); legacy bare `{ISSUE_ID}.md` still accepted |
| Lifecycle | Create → author → implement → verify → code-review → audit → close (Stage-scoped) |
| On completion | Lessons-learned migration + spec deletion handled by Stage-scoped `/closeout` pair (`stage-closeout-plan` → `stage-closeout-apply`) |
| Dead-path check | `npm run validate:dead-project-specs` (advisory: `--advisory` or `CI_DEAD_SPEC_ADVISORY=1`) |
| Frontmatter | 4-field IA header (`purpose`, `audience`, `loaded_by`, `slices_via`); validator: `npm run validate:frontmatter` |

**Requirements vs implementation.** Separate **product / game-logic** content (player + simulation rules — [`ia/specs/glossary.md`](ia/specs/glossary.md) terms) from **implementation** (files, classes, algorithms). Implementing agent picks code-level solutions **unless** it changes spec-defined game behavior; then record conflict in **Decision Log** or ask product owner.

**Open Questions.** Every collaborative project spec SHOULD include `## Open Questions (resolve before / during implementation)`. Phrase in canonical domain vocabulary; target definitions + intended game logic only — implementation choices → **Implementation Plan** / **Implementation investigation notes**.

**Multi-stage specs.** Large rewrites declare top-level **stages** with internal **phases**, executed by one fresh agent per stage; Stage-scoped `/closeout` pair (`stage-closeout-plan` → `stage-closeout-apply`) closes ALL Task rows of one Stage X.Y in bulk (archive N yaml + delete N specs + flip N rows + chain-level digest in one pass).

### Project docs outside `ia/specs/`

Charters + discovery for cross-cutting programs → `docs/`. Umbrella programs:

- **JSON interchange program** — glossary row + [`docs/postgres-interchange-patterns.md`](docs/postgres-interchange-patterns.md), [`docs/mcp-ia-server.md`](docs/mcp-ia-server.md), [`projects/json-use-cases-brainstorm.md`](projects/json-use-cases-brainstorm.md); charter trace in [`BACKLOG-ARCHIVE.md`](BACKLOG-ARCHIVE.md).
- **Compute-lib program** — glossary **Compute-lib program**; charter in [`BACKLOG-ARCHIVE.md`](BACKLOG-ARCHIVE.md); ongoing work in [`BACKLOG.md`](BACKLOG.md) **§ Compute-lib program**.

Durable IA does NOT embed backlog issue ids — see [`ia/rules/terminology-consistency.md`](ia/rules/terminology-consistency.md).

## 6. Terminology and information consistency

| Source | Use for |
|--------|---------|
| [`ia/specs/glossary.md`](ia/specs/glossary.md) | Canonical domain terms; check before naming features, bugs, user-facing copy |
| Linked specs | Definitions trump glossary on conflict (glossary defers to spec) |
| [`ia/rules/coding-conventions.md`](ia/rules/coding-conventions.md) | C# identifiers, XML docs, prefab naming for new assets |
| [`BACKLOG.md`](BACKLOG.md) | Issue id prefixes; **Files** / **Notes** / **Acceptance** reuse spec/glossary vocabulary |
| [`tools/mcp-ia-server/`](tools/mcp-ia-server/) | Tool names (`snake_case`) match `registerTool` in code; keep [`docs/mcp-ia-server.md`](docs/mcp-ia-server.md) + package README in sync |

New/changed concepts → update glossary **and** relevant spec section. No terminology-only-in-backlog-or-chat. Always-loaded rule body: [`ia/rules/terminology-consistency.md`](ia/rules/terminology-consistency.md).

## 7. Backlog workflow

**Backlog record** = per-issue yaml under `ia/backlog/{id}.yaml` (open) or `ia/backlog-archive/{id}.yaml` (closed). Source of truth for MCP + mutator skills. **Backlog view** = generated `BACKLOG.md` + `BACKLOG-ARCHIVE.md` — materialized from yaml by `bash tools/scripts/materialize-backlog.sh`. Read-only for humans + dashboard; never edited directly. Id prefixes: `BUG-`, `FEAT-`, `TECH-`, `ART-`, `AUDIO-`. **`backlog_issue` MCP** loads from yaml records (prefers yaml when `ia/backlog/` exists, falls back to BACKLOG.md).

**Working on issue.**

1. Prefer `backlog_issue` (territory-ia) for id; else `BACKLOG.md` (generated view).
2. Read files in issue **Files** field.
3. Plan mode: analyze + propose plan.
4. Agent mode: implement, move issue → **In progress**.

**After implementing.** Keep issue **In progress** until user confirms verification.

**Closing issue with project spec.** Run Stage-scoped `/closeout {MASTER_PLAN_PATH} {STAGE_ID}` (pair seam: `stage-closeout-planner` Opus → `stage-closeout-applier` Sonnet). Pair writes `§Stage Closeout Plan` tuples in master plan (unified shared migrations + N archive/delete/flip/purge ops), applies once, runs `materialize-backlog.sh` + `validate:dead-project-specs` once at end, emits one chain-level Stage closeout digest. Per-Task closeout surface retired — no per-spec lessons-migration pass.

**Adding issues.**

- **Id (per prefix):** run `bash tools/scripts/reserve-id.sh {PREFIX}` (atomic flock on `ia/state/id-counter.json`). Never scan BACKLOG.md or BACKLOG-ARCHIVE.md for max id; never hand-edit the counter. No reuse — archived records keep ids for traceability.
- Write `ia/backlog/{ISSUE_ID}.yaml` with: id, type, title, priority, status: open, section, spec, files, notes, acceptance, depends_on, depends_on_raw, related, created, raw_markdown.
- Run `bash tools/scripts/materialize-backlog.sh` after yaml write to regenerate BACKLOG.md.
- Include: Type, Files, Notes, Depends on (if applicable).
- **Caveman prose** in Notes / Acceptance per [`ia/rules/agent-output-caveman.md`](ia/rules/agent-output-caveman.md). Row structure + bolded glossary terms + id cross-refs + path links verbatim.
- Prefer `ia/backlog/` yaml records + `ia/specs/` for durable rules.

**Priority order:** In progress → High priority → Medium priority → Code Health → Low priority.

**Next-issue prompts.** User asks which is next → respond with it + **ask if they want an AI agent prompt** (analysis + development plan). Format prompt body in fenced ` ```markdown ` block, English unless user requests otherwise.

## 8. Web workspace (`web/`)

Next.js 14+ App Router at `web/`. Full onboarding: [`web/README.md`](web/README.md).

**Dev commands:**

```bash
cd web && npm run dev        # dev server at http://localhost:4000 (3000 reserved for lims tg-api-v2)
cd web && npm run build      # production build
npm run validate:web         # lint + typecheck + build (repo root)
npm run validate:all         # includes validate:web
```

**Caveman-exception boundary:** full English for user-facing rendered text in `web/content/**` and page-body JSX strings in `web/app/**/page.tsx`. App shell code, component identifiers, TypeScript comments, commits, IA prose stay caveman. Authority: `ia/rules/agent-output-caveman.md` §exceptions.

**Surface rules:**
- `web/` is tooling / docs-only surface. Invariants `#1–#12` (Unity / runtime C#) are NOT implicated.
- Vercel deploy: push to `main` triggers production deploy. `*.vercel.app` URL in `web/README.md` §Deploy once linked.
- Orchestrator: `ia/projects/web-platform-master-plan.md` — permanent, never closeable via `/closeout`.
- No `vercel.json` at MVP — dashboard-linked project uses Next.js framework preset auto-detect.

## 9. Pre-commit checklist

- [ ] Code compiles (Unity Build or `npm run unity:compile-check`)
- [ ] Class-level `/// <summary>` exists + accurate
- [ ] New public methods have XML documentation
- [ ] `Debug.Log` + comments in English
- [ ] `GridManager` touched → verify sorting order w/ different height levels
- [ ] Roads modified → `InvalidateRoadCache()` called where needed
- [ ] New manager → Inspector + `FindObjectOfType` pattern
- [ ] New prefabs follow [`coding-conventions.md`](ia/rules/coding-conventions.md) naming (don't rename existing)
- [ ] Temporary `Debug.Log` removed/gated per [`coding-conventions.md`](ia/rules/coding-conventions.md)
- [ ] Touched-domain wording matches `glossary.md` / linked specs
- [ ] Changed links / `Spec:` lines for `ia/projects/*.md` → `npm run validate:dead-project-specs`
- [ ] Changed `tools/mcp-ia-server`, `docs/schemas`, `ia/specs` bodies, or `glossary.md` → `npm run validate:all` (or `npm run verify:local` when Postgres + Unity bridge apply)
- [ ] Substantive implementation → include **Verification** block per [`docs/agent-led-verification-policy.md`](docs/agent-led-verification-policy.md)
