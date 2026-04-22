# AI Agent Guide — Territory Developer

> **Harness boundary.** This file is the **cross-harness canonical** agent guide (Codex / OpenAI `AGENTS.md` spec + general agent baseline). Claude Code deltas (hooks, slash commands, subagents, `@` imports) live in [`CLAUDE.md`](CLAUDE.md). Cursor deltas (`.mdc` adapters, caller-agent cheatsheet, model gate) live under [`.cursor/rules/`](.cursor/rules). Keep workflow here; keep host-specific surface in the host file.

> **TL;DR.** `territory-ia` MCP first (`backlog_issue` → `router_for_task` → `glossary_*` → `spec_section`). Ship via project-spec lifecycle (create → author → implement → verify → review → audit → close Stage-scoped). Emit **Verification** block per [`docs/agent-led-verification-policy.md`](docs/agent-led-verification-policy.md) at completion. Hard guardrails: [`ia/rules/invariants.md`](ia/rules/invariants.md) (universal + IA) + [`ia/rules/unity-invariants.md`](ia/rules/unity-invariants.md) (Unity runtime; on-demand).

## 1. Before you start

1. Read `/// <summary>` on C# class about to modify.
2. Have issue id → **`backlog_issue`** (territory-ia). Else [`BACKLOG.md`](BACKLOG.md).
3. [`ia/rules/agent-router.md`](ia/rules/agent-router.md) → right specs per task.
4. **MCP first**: in MCP-enabled hosts, `territory-ia` is default retrieval. Order: `backlog_issue` → `router_for_task` → `glossary_discover` / `glossary_lookup` (English only — translate from chat) → `spec_outline` / `spec_section` / `spec_sections` → `invariants_summary` / `list_rules` / `rule_content`. Closing project spec: `project_spec_closeout_digest` after `backlog_issue`. Ref: [`docs/mcp-ia-server.md`](docs/mcp-ia-server.md).
5. **Stale content:** territory-ia MCP caches per-process. After large edits, prefer fresh `read_file` or restart MCP server.

## 2. Agent lifecycle

Full lifecycle flow: [`docs/agent-lifecycle.md`](docs/agent-lifecycle.md) (end-to-end flow + seam → surface matrix + decision tree). Host inventory (hooks, agents, commands): [`CLAUDE.md`](CLAUDE.md) §4 (Claude-native surface) + [`.cursor/rules/`](.cursor/rules) (Cursor adapters); not duplicated here.

### 2a. Skill-lifecycle retrospective (skill-train)

**skill-train** sits outside main lifecycle flow — retrospective meta-surface, never closeable, never auto-applied. On demand (`/skill-train {SKILL_NAME}`), Opus subagent reads target skill's Per-skill Changelog entries since last `source: train-proposed` marker, aggregates recurring friction (≥2 occurrences threshold; configurable via `--threshold N`), writes **patch proposal (skill)** as `ia/skills/{SKILL_NAME}/train-proposal-{YYYY-MM-DD}.md` sibling file — unified-diff against Phase sequence / Guardrails / Seed prompt sections. User-gated review + manual apply only. Sibling producer `release-rollout-skill-bug-log` feeds a separate channel (`source: user-logged`, not self-reported friction) — do NOT merge channels. See glossary terms `skill self-report`, `skill training`, `patch proposal (skill)`, `skill-train`.

## 3. Verification policy (canonical)

Canonicalized at [`docs/agent-led-verification-policy.md`](docs/agent-led-verification-policy.md); rule anchor [`ia/rules/agent-verification-directives.md`](ia/rules/agent-verification-directives.md) (fetch on demand via MCP `rule_content agent-verification-directives`). Policy doc carries **Verification block** format (Node/IA, Unity compile, Path A batch, Path B bridge), bridge timeout (40 s initial, escalation, 120 s ceiling), Path A project-lock release. Do NOT restate here, in skills, or rules.

## 4. Documentation hierarchy

```
docs/information-architecture-overview.md → IA philosophy, layers, lifecycle, extension guide
docs/agent-led-verification-policy.md     → canonical Verification policy
docs/mcp-ia-server.md                     → territory-ia MCP tool catalog + recipes
ia/state/runtime-state.json               → last verify / bridge / queued test scenario (gitignored; read via `runtime_state` MCP or file)
.claude/active-session.json / .cursor/active-session.json → optional active_task_id / active_stage (gitignored; per harness; not in shared runtime-state file)
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
| [`economy-system.md`](ia/specs/economy-system.md) | Zone S channel, budget envelope, treasury floor, bonds, maintenance registry, save v4 economy fields |
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

- Before filing a follow-up **TECH-** (or same-scope) issue: grep `BACKLOG.md` open sections for matching scope — avoid duplicates.
- **Id (per prefix):** run `bash tools/scripts/reserve-id.sh {PREFIX}` (atomic flock on `ia/state/id-counter.json`). Never scan BACKLOG.md or BACKLOG-ARCHIVE.md for max id; never hand-edit the counter. No reuse — archived records keep ids for traceability.
- Write `ia/backlog/{ISSUE_ID}.yaml` with: id, type, title, priority, status: open, section, spec, files, notes, acceptance, depends_on, depends_on_raw, related, created, raw_markdown.
- Run `bash tools/scripts/materialize-backlog.sh` after yaml write to regenerate BACKLOG.md.
- Include: Type, Files, Notes, Depends on (if applicable).
- **Caveman prose** in Notes / Acceptance per [`ia/rules/agent-output-caveman.md`](ia/rules/agent-output-caveman.md). Row structure + bolded glossary terms + id cross-refs + path links verbatim.
- Prefer `ia/backlog/` yaml records + `ia/specs/` for durable rules.

**Priority order:** In progress → High priority → Medium priority → Code Health → Low priority.

**Next-issue prompts.** User asks which is next → respond with it + **ask if they want an AI agent prompt** (analysis + development plan). Format prompt body in fenced ` ```markdown ` block, English unless user requests otherwise.

## 8. Web workspace (`web/`)

Next.js 14+ App Router at `web/`. Full onboarding + dev commands + dashboard diagnostic recipe: [`web/README.md`](web/README.md).

**Surface rules:**
- `web/` is tooling / docs-only surface. Unity runtime invariants (`ia/rules/unity-invariants.md` rules 1–11) are NOT implicated; universal IA invariants (`ia/rules/invariants.md` rules 12–13) still apply.
- **Caveman-exception boundary:** full English for user-facing rendered text in `web/content/**` and page-body JSX strings in `web/app/**/page.tsx`. App shell code, component identifiers, TypeScript comments, commits, IA prose stay caveman. Authority: `ia/rules/agent-output-caveman.md` §exceptions.
- **Validation vs deploy:** `npm run validate:web` runs lint + typecheck + unit tests + **production `next build`** (no Vercel CLI). Catches PostCSS / Tailwind pipeline failures that lint and `tsc` miss. `validate:web:build` is an **alias** of `validate:web`. Deploy is **not** triggered by validation — only **push to `main`** (Vercel Git integration) or **`npm run deploy:web` / `deploy:web:preview`**.
- **`validate:all` and web:** Root `npm run validate:all` invokes `validate:web:conditional`, not unconditional `validate:web`. Full web validation (lint, typecheck, test, `next build`) runs when **unstaged or staged** paths include `web/` (this process’s edits only — not branch-wide diff vs `main`, so parallel agents on one branch do not inherit each other’s web surface). Also when `CI=true`, or `VALIDATE_WEB_FULL=1` / `FORCE_VALIDATE_WEB=1`. Otherwise it runs `npm run progress` only (regenerates `docs/progress.html`). After committing web-only work with a clean tree, run `npm run validate:web` locally or rely on CI.
- **Vercel deploy:** push to `main` triggers a production deploy (framework preset auto-detect; no `vercel.json`). Manual deploys also supported via `npm run deploy:web` (auto-prunes newest 3) and `npm run deploy:web:preview` (unique preview URL). Manual deploy is the path when the master-plan has been merged and you want an instant refresh ahead of the next push.
- **Orchestrator:** `ia/projects/web-platform-master-plan.md` — permanent, never closeable via `/closeout`.
- **Live dashboard freshness:** `/dashboard` fetches `ia/projects/*master-plan*.md` from GitHub raw via Next.js ISR (5-min revalidate). Push to deployed branch → visible within ~5 min without redeploy.

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
- [ ] Changed `CLAUDE.md` `@` imports, `ia/rules/invariants.md`, or `ia/rules/unity-invariants.md` → `npm run validate:claude-imports` + `npm run validate:cache-block-sizing`
- [ ] Substantive implementation → include **Verification** block per [`docs/agent-led-verification-policy.md`](docs/agent-led-verification-policy.md)
