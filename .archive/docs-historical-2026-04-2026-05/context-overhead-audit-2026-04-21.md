---
status: draft
last_updated: 2026-04-21
audience: human + agent
purpose: Audit force-loaded context in territory-developer across Claude Code + Cursor harnesses, propose restructure to cut token overhead + hallucination risk
---

# Context-overhead audit (territory-developer) — 2026-04-21

> **TL;DR.** Every session currently force-loads ~4 KB Unity-game invariants + stubs even for web/ / docs/ / MCP tasks. Two of the five `@` imports are redundant anchor stubs. AGENTS.md + CLAUDE.md duplicate the web/ section + the doc-hierarchy table, and disagree on Vercel deploy semantics. `docs/` is correctly *not* force-loaded — but stub rules bias agents to chain-pull 25–42 KB hub docs on turn 1. Proposal: shrink `@` import set from 5 to 3, split Unity invariants out of always-loaded, make CLAUDE.md a thin dispatch layer over AGENTS.md (the cross-harness canonical), delete 2 redundant stubs. Estimated session-start savings: ~50% of the currently-forced tokens, plus a 5–15 k-token chain-load reduction on first turn for non-Unity tasks.

---

## 1. Scope + goals

### 1.1 What this audit covers

- Force-loaded surface for **Claude Code** (CLAUDE.md + `@` imports + SessionStart hook effects).
- Force-loaded surface for **Cursor** (`.cursor/rules/*.mdc` with `alwaysApply: true`).
- First-turn chain-load bias (links from force-loaded stubs to large hub docs).
- Content duplication + drift between CLAUDE.md, AGENTS.md, `ia/rules/`, `docs/`.
- Reach of `ia/specs/` — is it still behind the MCP / glossary / router gate the architecture intends, or does it leak into initial overhead?

### 1.2 Optimization north stars (restated)

1. Minimal initial overhead; context richness arrives **after** first user prompt, routed per task.
2. Prevent hallucinations from bloated / unfocused context windows.
3. `ia/specs/` reached only via **glossary / router / MCP**, never in overhead.
4. **NORTH-1:** reduce token cost of agent-driven development.
5. **NORTH-2:** faster agent execution via precise tool wiring.

---

## 2. Current state — what is actually force-loaded

### 2.1 Claude Code — session start

`CLAUDE.md` (root) uses 5 `@` imports:

```markdown
@ia/rules/invariants.md
@ia/rules/terminology-consistency.md
@ia/rules/mcp-ia-default.md
@ia/rules/agent-output-caveman.md
@ia/rules/agent-lifecycle.md
```

Forced bytes (measured):

| Surface | Lines | Bytes | ~tokens (÷4) |
|---|---:|---:|---:|
| `CLAUDE.md` | 83 | 8,082 | ~2,020 |
| `ia/rules/invariants.md` | 37 | 2,876 | ~719 |
| `ia/rules/terminology-consistency.md` | 16 | 1,451 | ~363 |
| `ia/rules/mcp-ia-default.md` | 12 | 475 | ~119 |
| `ia/rules/agent-output-caveman.md` | 25 | 2,115 | ~529 |
| `ia/rules/agent-lifecycle.md` | 20 | 739 | ~185 |
| **Initial forced total** | **193** | **15,738** | **~3,935** |

**MEMORY.md at root** (81 lines / 21,865 bytes) is **NOT** `@`-imported → **not force-loaded**. Good — contrary to what a casual read of CLAUDE.md §3 might suggest, project MEMORY.md is an on-demand lookup, not session overhead. (User auto-memory at `~/.claude/projects/<slug>/memory/MEMORY.md` is a separate harness feature and *is* force-loaded per-user across all projects — unrelated to this repo.)

### 2.2 Cursor — session start

`.cursor/rules/` holds 30+ `.mdc` rule files. Those with `alwaysApply: true` force-load in every Cursor chat:

- `cursor-caller-agent-cheatsheet.mdc` — MCP mutation `caller_agent` values.
- `cursor-lifecycle-adapters.mdc` — long Cursor-specific lifecycle compat layer.
- `cursor-model-gate.mdc` — model-selection guardrail.
- (Plus any `cursor-skill-*.mdc` with `alwaysApply: true`; majority are `false` and fire on trigger keywords.)

Cursor does **not** read `@`-style markdown imports. So the Claude Code `@` chain has zero effect in Cursor sessions. Cursor picks up domain-level guidance via its `.mdc` `alwaysApply` flag + Cursor's "User Rules" setting (mentioned in `docs/mcp-ia-server.md` §9 but not repo-enforceable).

### 2.3 Chain-loaded on first turn (bias from force-loaded stubs)

These are not strictly forced, but the stub / routing language pulls them reflexively:

| Surface | Pulled because | Lines | Bytes |
|---|---|---:|---:|
| `docs/agent-lifecycle.md` | `ia/rules/agent-lifecycle.md` stub names it as "canonical" | 225 | 25,424 |
| `AGENTS.md` | `CLAUDE.md §1` ("Workflow specifics: AGENTS.md") | 167 | 14,861 |
| `docs/information-architecture-overview.md` | `AGENTS.md §4` + `CLAUDE.md §7` | 314 | 24,515 |
| `docs/mcp-ia-server.md` | `CLAUDE.md §7` + MCP-first directive | 169 | 42,133 |
| `ARCHITECTURE.md` | `CLAUDE.md §5` local-verify link | 176 | 17,521 |
| `docs/agent-led-verification-policy.md` | `AGENTS.md §3` ("canonical") | 86 | ~? |

If an agent walks these on turn 1 "just in case", add **~125 KB ≈ ~30 k tokens** on top of the 4 k forced. The MCP-first directive helps (returns slices), but the `@`-loaded stubs tilt the balance toward whole-file reads.

### 2.4 `ia/specs/` — confirmed gated

None of `ia/specs/*.md` is `@`-imported. None is referenced by force-loaded stubs as "read this first". Access is mediated by:

- `ia/rules/agent-router.md` (NOT force-loaded — 63 lines, reached via `CLAUDE.md §2` fallback clause).
- `mcp__territory-ia__*` tools (MCP slices).
- `docs/mcp-ia-server.md` (router table for MCP tools).

**This goal (#3 above) is already satisfied.** Specs stay out of overhead.

---

## 3. Findings

### F1. Unity invariants live in always-on context

`ia/rules/invariants.md` forces 13 rules + 10 IF→THEN guardrails into every session.

- Rules 1–11 are **Unity / C# / game-runtime** (`HeightMap`, `GridManager`, `InvalidateRoadCache`, cliff faces, road preparation family, `UrbanizationProposal`, shore band, rivers).
- Rules 12–13 are **IA / tooling-general** (specs vs projects split; id-counter authority).
- Guardrails mostly mirror rules 1–11.

`AGENTS.md §8` already acknowledges: *"web/ is tooling / docs-only surface. Invariants #1–#12 (Unity / runtime C#) are NOT implicated."* Yet every web/ session pays ~720 tokens to hold those invariants in context. Same for pure IA / MCP / docs tasks, skill authoring, Cursor tracker flips, backlog triage.

**Cost:** ~720 tokens per session × frequency of non-Unity work (currently high — web dashboard, MCP server extensions, release-rollout sweeps, skill-train proposals).

### F2. Two `@` imports are pure redirect stubs

- **`ia/rules/mcp-ia-default.md`** (12 lines, 475 B) — body reads: *"Prefer `mcp__territory-ia__*` tools over loading full `ia/specs/*.md`. Ordering, fallback, and schema-cache caveat live in `CLAUDE.md` §2 'MCP first'. This rule exists as an `@`-loadable anchor for that directive."*
  - Zero new content. Exists because historically `@` couldn't point at a section of CLAUDE.md itself.
- **`ia/rules/agent-lifecycle.md`** (20 lines, 739 B) — body: *"Full canonical doc: `docs/agent-lifecycle.md`. Ordered flow: §1 of that doc. Surface map: §2 of that doc."*
  - Zero content. Sole purpose: make lifecycle discoverable via `@`. But the link bias pulls 25 KB of `docs/agent-lifecycle.md` on first mention of any lifecycle-ish word.

Both stubs can be **deleted** with zero information loss — provided CLAUDE.md carries a 1-line routing hint for each.

### F3. CLAUDE.md ↔ AGENTS.md duplication + drift

Duplicated content:
- **Web workspace section.** `CLAUDE.md §6` and `AGENTS.md §8` both describe `web/` commands, routes, caveman exceptions, orchestrator pointer.
- **Key files / Doc hierarchy.** `CLAUDE.md §3` and `AGENTS.md §4` overlap (both list `MEMORY.md`, `.claude/settings.json`, `ia/skills/`, `docs/agent-lifecycle.md`, etc.).
- **MCP-first directive.** `CLAUDE.md §2`, `AGENTS.md §1.4`, `ia/rules/mcp-ia-default.md` all say the same thing in different words.

Drift / inconsistencies spotted:
- **Vercel deploy trigger.** `CLAUDE.md §6` → `npm run deploy:web` (manual, "Manual only — closeout / stage-close no longer auto-deploy"). `AGENTS.md §8` → *"push to `main` triggers production deploy"*. These contradict. Likely AGENTS.md is stale (predates the manual-deploy switch mentioned in CLAUDE.md §6).
- **Project spec closeout.** `AGENTS.md §5` table says *"Lessons-learned migration + spec deletion handled by Stage-scoped `/closeout` pair"* ✓ aligned with MEMORY.md M6 entry. But `docs/mcp-ia-server.md §9` still refers to a legacy **`project-spec-close`** skill that MEMORY.md records as retired. That's a second drift site to fix in a cleanup pass.
- **Validation policy.** `AGENTS.md §3` says *"surfaced as always-on rule `ia/rules/agent-verification-directives.md`"* — but that rule file is not in the `@` import chain. It's only "always-on" by convention, not by `@`-loading.

### F4. Stub-bias → 25 KB chain-pull on turn 1

`ia/rules/agent-lifecycle.md` (force-loaded) links to `docs/agent-lifecycle.md` as *"canonical"*. Agents reading the stub then pre-read the 225-line doc "just in case", even when the task is a one-file web bug or a glossary tweak. Same pattern for `docs/information-architecture-overview.md` via AGENTS.md §4.

Root cause: the stubs don't discriminate by task. A better pattern: let CLAUDE.md name the hub **and** the trigger — *"when running `/stage-file`, `/ship-stage`, `/closeout`, or multi-task chains, read `docs/agent-lifecycle.md §1`"* — so agents only follow the link when they match the trigger.

### F5. AGENTS.md harness-ambiguity

Per the OpenAI / Codex `AGENTS.md` spec, `AGENTS.md` at repo root is the **cross-harness canonical agent guide** (Codex CLI reads it; many other tools honor it as baseline). Claude Code reads `CLAUDE.md`; Cursor reads `.cursor/rules/*.mdc`; Codex reads `AGENTS.md`.

Current repo posture: AGENTS.md carries the *workflow content* (backlog, verification, pre-commit), CLAUDE.md carries *Claude-native surface* (hooks, slash commands, subagents). Good split in principle — but the overlap noted in F3 blurs it, and neither file states the boundary plainly.

Recommended posture (clarified):
- **AGENTS.md** → cross-harness canonical workflow + backlog + verification + pre-commit. No host-specific surface.
- **CLAUDE.md** → Claude Code deltas: `@` imports, hooks, slash commands, subagents, MCP schema-cache caveat, any host-only safety. Everything else = "see AGENTS.md".
- **`.cursor/rules/`** → Cursor deltas: `caller_agent` cheatsheet, model gate, lifecycle adapters, per-skill MDC wrappers. Already in good shape.

### F6. `/docs` folder reach — already correct, leaky in one place

`docs/` is **not** force-loaded. Confirmed no `@` imports land in `docs/`. Agents reach `docs/` only through explicit links in CLAUDE.md / AGENTS.md / rules / skills / specs or via MCP `list_specs`. Matches goal #3 intent.

Leak: the force-loaded `ia/rules/agent-lifecycle.md` stub points at `docs/agent-lifecycle.md` without trigger guard → chain-pull bias (F4). Same secondary path: `CLAUDE.md §7` lists `docs/information-architecture-overview.md` + `docs/mcp-ia-server.md` as "where to find more", pulling them even when not needed.

### F7. Skill directive language risks hallucination

`CLAUDE.md §7` says *"MCP tool catalog: `docs/mcp-ia-server.md`"*. But the actual MCP tool schemas are cached by the MCP server at session start; the `.md` file is a *human-readable* catalog that lags the code (`tools/mcp-ia-server/src/index.ts`). Agents reading the 42 KB catalog "for tool names" can hallucinate retired tools (e.g., the `project-spec-close` example above). MCP tool discovery should go through the MCP `list_*` / schema fetch, not the catalog doc.

---

## 4. Proposed restructure

### 4.1 Principles

1. **Force-load only the universally-applicable.** Task-specific rules load on demand via `rule_content` MCP or router reference.
2. **No pure-redirect stubs.** If a rule file exists, it must carry content worth the bytes.
3. **Single source of truth per concern.** CLAUDE.md owns Claude-native; AGENTS.md owns cross-harness workflow; `.cursor/rules/` owns Cursor-native.
4. **Triggered routing.** Every force-loaded link to a hub doc states its trigger ("read when…"), so agents don't reflexively pull.
5. **`ia/specs/` stays behind the MCP / router gate.** Already correct — don't regress.

### 4.2 New `@` import set (Claude Code) — shrink 5 → 3

Keep (universal, small, actionable):
1. `ia/rules/terminology-consistency.md` — unchanged (16 lines).
2. `ia/rules/agent-output-caveman.md` — consider trimming authoring-surface enumeration to a 1-line pointer (`-authoring.md` sibling already holds the full checklist). Could drop ~10 lines.
3. **NEW** `ia/rules/universal-safety.md` — ~20 lines consolidating:
   - MCP-first directive (merges `mcp-ia-default.md`).
   - Id-counter authority (`reserve-id.sh`; never hand-edit `ia/state/id-counter.json`).
   - Bash denylist summary (force-push, reset --hard, etc. — 1 line pointer to `.claude/settings.json`).
   - "Don't invent skill flags / tool names — fetch via MCP schema or skill SKILL.md".

Delete:
- `ia/rules/invariants.md` (move content → §4.3 below).
- `ia/rules/mcp-ia-default.md` (content absorbed into `universal-safety.md`).
- `ia/rules/agent-lifecycle.md` stub (content absorbed into CLAUDE.md §routing with trigger guard).

Projected forced total: ~2.0 KB ≈ ~500 tokens (down from ~4 k).

### 4.3 Split `invariants.md` → 2 files

- **`ia/rules/unity-invariants.md`** — rules 1–11 + Unity guardrails. `loaded_by: on-demand`. Router entry: *"Touching `Assets/Scripts/**/*.cs`, `GridManager`, `HeightMap`, roads, water, cliffs → fetch via `rule_content unity-invariants` or read this file directly."*
- **`ia/rules/ia-invariants.md`** — rules 12–13 (specs-vs-projects split; id-counter authority). Merge into new `universal-safety.md` rather than standalone — these are tiny + universally relevant.

CLAUDE.md §routing table gets 1 line: *"Unity C# work → `ia/rules/unity-invariants.md` via `rule_content`."*

### 4.4 CLAUDE.md — shrink to thin dispatch (≤40 lines)

Target structure:

```
# Claude Code — Territory Developer

@ia/rules/terminology-consistency.md
@ia/rules/agent-output-caveman.md
@ia/rules/universal-safety.md

## What this repo is
<1 paragraph: Unity 2D isometric city builder + Markdown IA + territory-ia MCP.>
<Workflow: AGENTS.md. Architecture: ARCHITECTURE.md.>

## MCP first
<2 sentences: prefer mcp__territory-ia__* over read_file on ia/specs/*.>
<Fallback: ia/rules/agent-router.md.>

## Task routing (trigger → hub)
| Trigger | Read this | Source of truth |
|---|---|---|
| C# / Unity runtime | `ia/rules/unity-invariants.md` | `ia/specs/isometric-geography-system.md` via MCP |
| Lifecycle (`/stage-file`, `/ship-stage`, `/closeout`, chain) | `docs/agent-lifecycle.md §1` | ditto |
| Web/ workspace | `web/README.md` + `ia/rules/web-backend-logic.md` | ditto |
| MCP server code | `tools/mcp-ia-server/src/index.ts` | + MCP `list_*` schemas |
| Verification block format | `docs/agent-led-verification-policy.md` | — |
| Backlog / issues | `backlog_issue` MCP tool | `ia/backlog/*.yaml` |

## Claude-native surface (host-specific)
- Hooks: `.claude/settings.json` + `tools/scripts/claude-hooks/`. Bash denylist enforced pre-tool-use.
- Subagents: `.claude/agents/*.md`. Slash commands: `.claude/commands/*.md`. Output styles: `.claude/output-styles/*.md`.
- SessionStart hook activates `caveman:caveman` skill; global user prefs also default output to caveman.
- Project MEMORY.md (root) is opt-in lookup, not force-loaded.

## Where to find more
- Cross-harness workflow: `AGENTS.md`
- IA architecture narrative: `docs/information-architecture-overview.md`
- MCP tool catalog: fetch via MCP `list_specs` / schemas, not `.md` docs (catalog lags code)
```

Net: ~35 lines, ~3 KB, **~750 tokens** (vs current ~2 k). Web-specific detail (routes, dashboard recipe) → moved to `web/README.md` + a new router row.

### 4.5 AGENTS.md — as cross-harness canonical

- De-duplicate web/ workspace section (move detail to `web/README.md`; AGENTS.md keeps 3 lines + pointer).
- De-duplicate doc-hierarchy table with CLAUDE.md — keep it in AGENTS.md only (cross-harness truth).
- Fix Vercel-deploy-on-push-to-main drift → align with manual-deploy semantics.
- Remove references to retired skills (`project-spec-close`, `spec-kickoff`, etc. per MEMORY.md M6 entry).
- Add explicit line at top: *"CLAUDE.md = Claude Code host surface. `.cursor/rules/` = Cursor host surface. This file = cross-harness workflow canonical."*

Target: ~120 lines (down from 167), ~12 KB.

### 4.6 `docs/mcp-ia-server.md` cleanup (follow-up)

- Replace legacy `project-spec-close` references with Stage-scoped `/closeout` pair (one drift site noted above; grep for others before editing).
- Add a header banner: *"MCP tool schemas canonical source = `tools/mcp-ia-server/src/index.ts` + MCP `list_*`. This doc is a human-readable catalog; it can lag. Do NOT trust tool signatures here over server response."*
- Consider splitting into `docs/mcp-ia-server.md` (stable overview ~40 lines) + `docs/mcp-ia-tool-catalog.md` (generated, regeneratable from code).

### 4.7 `.cursor/rules/` — targeted trims (low priority)

Cursor rules look well-scoped (most `alwaysApply: false`). Two always-on concerns:
- `cursor-lifecycle-adapters.mdc` is long (~100+ lines estimated). Consider keeping only decision-tree top; push per-seam detail to `alwaysApply: false` per-skill files it already uses.
- `cursor-caller-agent-cheatsheet.mdc` — good, small, keep.

No blocker; touch only after the Claude Code side lands.

---

## 5. Migration plan (staged, low-risk)

### Stage A — zero-churn cleanup (single PR)

1. Write `ia/rules/universal-safety.md` (~20 lines).
2. Rename `ia/rules/invariants.md` → `ia/rules/unity-invariants.md`; drop rules 12–13 (migrated into `universal-safety.md`).
3. Replace `CLAUDE.md` `@` block:
   - Remove `@ia/rules/invariants.md`, `@ia/rules/mcp-ia-default.md`, `@ia/rules/agent-lifecycle.md`.
   - Add `@ia/rules/universal-safety.md`.
4. Delete `ia/rules/mcp-ia-default.md`, `ia/rules/agent-lifecycle.md` stub.
5. Grep for references to the deleted filenames + the renamed filename — fix links.
6. `npm run validate:all`.

Expected savings: force-load ~4 k → ~500 tokens.

### Stage B — CLAUDE.md rewrite

1. Rewrite CLAUDE.md per §4.4 skeleton.
2. Move web/ workspace detail → `web/README.md` expansion.
3. Keep CLAUDE.md ≤40 lines.
4. Validate `@` imports resolve; smoke-test one Claude Code session.

### Stage C — AGENTS.md hygiene

1. Fix Vercel-deploy drift (§F3).
2. Strip retired skill names (grep for `project-spec-close`, `project-stage-close`, `spec-kickoff`).
3. De-duplicate web/ + doc-hierarchy with CLAUDE.md.
4. Add harness-boundary preamble.

### Stage D — `docs/mcp-ia-server.md` trust-boundary banner + drift fix

Separable; run when Cursor workflow / MCP-tool work resumes.

### Stage E — optional split of mcp-ia-server catalog

Defer; only if drift keeps recurring.

---

## 6. Risks + open questions

- **R1 — Deleting `@ia/rules/agent-lifecycle.md` stub.** If any non-Claude tool keys off that path, it breaks. Grep confirms only CLAUDE.md `@` references it. Low risk. Still worth a repo-wide grep before delete.
- **R2 — Splitting invariants.** Any skill / agent prompt that string-matches `ia/rules/invariants.md` needs updating. `grep -r "ia/rules/invariants" .` before the rename.
- **R3 — Universal-safety.md content scope.** Risk of creeping back to the old 37-line invariants shape. Cap at ~20 lines; put anything bigger into its own on-demand rule.
- **Q1 — Should `.cursor/rules/` mirror the new split** (always-on Unity-invariant removal)? Probably yes, but batch with Stage C.
- **Q2 — Is there a hook that already lints `@` import drift?** Worth a `validate:claude-imports` script that asserts each `@`-imported file exists + is ≤N lines.
- **Q3 — MEMORY.md at root hits ~22 KB of hand-edited entries.** It is *not* force-loaded, but if agents reflex-read it on turn 1 from CLAUDE.md §3's mention, that's ~5 k tokens. Consider moving its mention out of §3 into task-router rows ("for architecture-decision context: read MEMORY.md").

---

## 7. Estimated savings

Per session, averaged across Unity + web + IA/MCP + skill-authoring tasks:

| Phase | Current | Proposed | Delta |
|---|---:|---:|---:|
| Initial forced | ~4,000 tok | ~1,500 tok | −2,500 |
| Turn-1 chain-pull (non-Unity task) | ~15,000 tok | ~3,000 tok | −12,000 |
| Turn-1 chain-pull (Unity task) | ~15,000 tok | ~8,000 tok | −7,000 |

Ballpark: **10–15 k tokens saved per session** for non-Unity work, **~10 k** for Unity work (since Unity invariants legitimately need loading — but now via MCP slice not whole-file).

At current multi-turn rates, conservative estimate: **1–2 M tokens/month saved** across heavy agent-driven development, with correspondingly fewer "which-rule-applies" hallucinations on non-Unity tasks.

---

## 8. Next step

User review. If approved, file as `TECH-{next}` via `tools/scripts/reserve-id.sh TECH`, draft project spec from `ia/templates/project-spec-template.md`, stage per §5.
