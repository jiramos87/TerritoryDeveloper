---
name: unfold
purpose: >-
  Meta-tool. Read composite slash-command invocation, trace subagent + skill chain, emit one
  self-contained decision-tree plan with explicit on_success / on_failure edges. Runtime-only values
  emitted as placeholders. Read-only; NO execution, NO source edits, NO git commits.
audience: agent
loaded_by: "skill:unfold"
slices_via: none
description: >-
  Linearize composite-skill invocation (e.g. `/ship-stage {PLAN} {STAGE}`) into one laid-out markdown
  plan — decision-tree shape, explicit `on_success` / `on_failure` edges, positional args substituted
  literally, runtime-only values as `${placeholder}`. Parses `.claude/commands/{cmd}.md` →
  `.claude/agents/{name}.md` → `ia/skills/{slug}/SKILL.md`, walks phase sequence, inlines direct
  subagents, summarizes nested skills past `--depth`. Emits plan to
  `ia/plans/{cmd-slug}-{arg-slug}-unfold.md`. Use before a risky composite run to preview, after
  editing a skill to diff drift, or to hand a fresh agent a single executable plan without the skill
  runtime. Triggers — "unfold", "/unfold", "flatten skill", "precompile skill", "linearize skill",
  "turn skill into plan", "preview composite skill", "dry-run skill plan".
phases: []
triggers:
  - unfold
  - /unfold
  - flatten skill
  - precompile skill
  - linearize skill
  - turn skill into plan
  - preview composite skill
  - dry-run skill plan
argument_hint: {TARGET_COMMAND} {TARGET_ARGS...} [--out PATH] [--depth N] [--format md|yaml]
model: inherit
tools_role: standalone-pipeline
tools_extra: []
caveman_exceptions:
  - code
  - emitted plan markdown
  - verbatim subagent-prompt quotes
  - verbatim tool output
  - plan-header YAML
  - destructive-op confirmations
hard_boundaries:
  - IF `TARGET_COMMAND` missing / empty → STOP immediately; report input absent.
  - IF `.claude/commands/{CMD}.md` not found → STOP; report command not found.
  - IF `--depth` not integer ≥ 0 → STOP. `--depth` > 3 → clamp to 3 + note in plan header.
  - Do NOT modify source command / subagent / skill files — unfold is strictly read-only.
  - "Do NOT dispatch subagents — pure parse + emit; `tools:` excludes `Agent` by design."
  - Do NOT commit — user decides git state.
  - Do NOT execute the emitted plan — handoff terminal command is a suggestion for the user.
  - Runtime-only values (`{FAILED_ISSUE_ID}`, `{PR_NUMBER}`, `$LAST_COMMIT_SHA`, etc.) → always placeholders. No guessing.
caller_agent: unfold
---

# unfold — skill linearizer (meta-tool)

Caveman default — [`agent-output-caveman.md`](../../rules/agent-output-caveman.md). Exceptions: emitted plan markdown, code fences, verbatim subagent-prompt quotes, handoff JSON, plan-header YAML.

**Lifecycle:** On-demand meta-tool. Sits OUTSIDE main flow — never chained, never auto-applied. Produces auditable plan artifacts under `ia/plans/`. Consumer of other skills' markdown; does NOT modify source skills.

**Dispatch mode:** Canonical = dispatched as `.claude/agents/unfold.md` subagent via `/unfold` command. Inline fallback (SKILL.md-only invocation) available when dispatch unavailable — behavior identical, runs in caller's model context.

**Related:** [`docs/agent-lifecycle.md`](../../../docs/agent-lifecycle.md) §2 Row M · [`.claude/commands/unfold.md`](../../../.claude/commands/unfold.md) · [`.claude/agents/unfold.md`](../../../.claude/agents/unfold.md) · [`skill-train`](../skill-train/SKILL.md) (sibling meta-tool — retrospective friction analysis, not composition preview).

**Scope distinction.** `skill-train` retrospects accumulated friction. `unfold` previews forward composition. No overlap — neither modifies the other's inputs.

---

## Inputs

| Parameter | Source | Notes |
|-----------|--------|-------|
| `TARGET_COMMAND` | First token of `$ARGUMENTS` | Slash-command name; leading `/` optional (e.g. `/ship-stage`, `ship-stage`). Must resolve to `.claude/commands/{name}.md`. Missing → STOP. |
| `TARGET_ARGS` | Tokens 2..N (pre-flags) | Positional args; substituted literally into plan where target skill references them. |
| `--out {PATH}` | Optional flag | Override output path. Default: `ia/plans/{cmd-slug}-{arg-slug}-unfold.md`. |
| `--depth N` | Optional flag | Max inline depth for nested skill references. Default 1 (direct subagent inlined; nested skills summarized by name + path). Cap 3. |
| `--format md\|yaml` | Optional flag | Plan format. Default `md` (decision-tree markdown; yaml emits structured nodes, lower readability). |

---

## Phase sequence

### Phase 0 — Resolve target command

1. Strip leading `/` from `TARGET_COMMAND` → `CMD`.
2. Glob `.claude/commands/{CMD}.md`. Missing → STOP; report `command not found: {CMD}`.
3. Read command file. Parse frontmatter (`description`, `argument-hint`) + body.
4. Extract dispatch target:
   - Search body for `subagent_type: "{NAME}"` — primary resolution.
   - Fallback: `.claude/agents/{NAME}.md` direct reference.
   - None found → mark command inline-only; Phase 1 skipped; command body IS the plan source.
5. Extract `argument-hint` template for arg-name inference (e.g. `{SLUG} {STAGE_ID}`).

### Phase 1 — Load subagent + skill

Skip if Phase 0 marked command inline-only.

1. Read `.claude/agents/{NAME}.md`. Extract: `tools` allowlist, `model`, Mission, Hard boundaries, Exit lines.
2. From subagent body extract skill reference — `ia/skills/{SLUG}/SKILL.md`. Glob to confirm.
3. Read SKILL.md. Parse: Inputs table, `## Phase sequence` (phase headings `### Phase N — {name}`), Guardrails, Exit / Output sections.
4. Missing SKILL.md → fall back to subagent body as plan source; emit `fallback: subagent-body` in plan header.

### Phase 2 — Walk skill phases + substitute args

For each phase in SKILL.md (or subagent body fallback):

1. Extract phase name, numbered step list, conditional markers: `if`, `when`, `on`, `unless`, `STOP`, `STOPPED`, `skip`, `fail`, `→`, `non-populated →`, `missing →`, `absent →`.
2. Identify nested dispatches (sub-skills, sub-commands, inline chains). If `--depth > 0` remaining: recurse Phase 0–2 on each. Else: summarize as `see {skill_path} — not inlined (depth cap)`.
3. Substitute `argument-hint` placeholders (`{SLUG}`, `{STAGE_ID}`, `{ISSUE_ID}`, etc.) with literal `TARGET_ARGS` tokens in positional order.
4. Runtime-only values (e.g. `{FAILED_ISSUE_ID}`, `{PR_NUMBER}`, `$LAST_COMMIT_SHA`, `{ISSUE_ID_LIST}`, `{OUT_PATH}`) → emit as `${var}` placeholder; append one line to placeholder registry with source reference.

### Phase 3 — Emit plan

Write plan to `OUT_PATH`. Create `ia/plans/` if missing. Filename collision → append `-N` suffix.

Plan header (verbatim YAML):

```yaml
---
generator: unfold
generated: {YYYY-MM-DD}
target_command: {TARGET_COMMAND}
target_args: [{TARGET_ARGS joined, quoted}]
command_sha: {git blob SHA of .claude/commands/{CMD}.md at unfold time}
subagent_sha: {git blob SHA of .claude/agents/{NAME}.md or "n/a"}
skill_sha: {git blob SHA of ia/skills/{SLUG}/SKILL.md or "n/a"}
depth: {N}
format: {md|yaml}
fallback: {none | subagent-body | command-body}
---
```

Each step block (md format):

```markdown
## Step {N} — {phase_name} / {step_title}

**Action:** {one-line imperative, caveman}
**Inputs:** {files / args / artifacts — substituted literals}
**Expected:** {success condition — verbatim from skill where present}
**On success:** → Step {N+1} (or terminal label `PASSED` / `STOPPED: {reason}` / `ABORT`)
**On failure:** → Step {N}.fix | abort → handoff `{path}` | terminal label
**Notes:** {invariants, guardrails, runtime-placeholder callouts, nested-skill summary links}
```

Plan footer:

```markdown
## Terminal labels

- `PASSED` — all gates green per skill exit lines.
- `STOPPED: {reason}` — specific gate failure; next step = `{handoff_command}`.
- `ABORT` — unrecoverable; human review required.

## Runtime placeholders

- `${var}` — {one-line source + description, one row per placeholder}

## Nested skills (not inlined)

- `{skill}` — see `{path}`; re-run `/unfold /{skill} ...` for its plan (depth cap hit).
```

yaml format variant: single `plan:` root, `steps:` array of `{id, phase, action, inputs, expected, on_success, on_failure, notes}` objects, plus `terminal_labels`, `placeholders`, `nested_skills` siblings. Reserve for machine consumers; default remains `md`.

### Phase 4 — Validate plan

1. Every `on_success` / `on_failure` edge must target an existing step id OR one of `{PASSED, STOPPED:*, ABORT}`. Dangling → warn in header `warnings:` array.
2. Every SKILL.md `### Phase N` heading must map to ≥1 Step block. Orphan phase → warn.
3. Header SHAs resolve via `git rev-parse HEAD:{path}`. Path outside working tree or git absent → emit `{unresolved}` literal + warn.
4. Warnings do NOT block emission; fatals (missing command / missing SKILL without fallback) already STOP in Phase 0–1.

### Phase 5 — Handoff

Single caveman line:

```
unfold {TARGET_COMMAND} → {OUT_PATH}. {step_count} steps, {branch_count} decision points, {placeholder_count} runtime placeholders. Review, edit, run.
```

Copy-paste terminal command:

```
claude "follow {OUT_PATH}"
```

Exit line: `UNFOLD {TARGET_COMMAND}: WRITTEN {OUT_PATH}` OR `UNFOLD: WARNED — {count} dangling edges, see plan header` OR `UNFOLD: STOPPED — {reason}`.

---

## Guardrails

- IF `TARGET_COMMAND` missing / empty → STOP; report input absent.
- IF `.claude/commands/{CMD}.md` not found → STOP; report command not found.
- IF `--depth` not integer ≥ 0 → STOP; `--depth 0` = no inline (summary-only plan).
- `--depth` hard cap = 3; higher values clamped to 3 + noted in header.
- Do NOT modify source command / subagent / skill files — unfold is strictly read-only.
- Do NOT commit the emitted plan — user decides git state.
- Do NOT dispatch subagents — unfold is pure parse + emit. `tools:` frontmatter excludes `Agent` by design.
- Do NOT execute the plan — terminal command in handoff is a suggestion for the user.
- Runtime-only values → always placeholders. Never guess, never infer from training.
- Ambiguous prose (no explicit `if` / `on failure` / `→` markers) → emit step with `On failure: see {skill_path} — prose-extracted, verify manually`.
- Collision on output path → append `-N` suffix; never overwrite without `--out` explicit target.

---

## Output

Phase 0: `CMD resolved → .claude/commands/{CMD}.md`. Missing → STOP line.
Phase 1: `subagent resolved → .claude/agents/{NAME}.md` + `skill resolved → ia/skills/{SLUG}/SKILL.md` (or fallback note).
Phase 2: one-line per phase walked — `phase {N} — {name}: {step_count} steps, {branch_count} branches, {placeholder_count} placeholders`.
Phase 3: `plan written → {OUT_PATH} ({line_count} lines)`.
Phase 4: validation verdict — `ok` OR `warned: {count}` (dangling edges / orphan phases).
Phase 5: handoff line + copy-paste terminal command.

Final exit: `UNFOLD {TARGET_COMMAND}: WRITTEN {OUT_PATH}` | `UNFOLD: WARNED — {count} issues, see plan header` | `UNFOLD: STOPPED — {reason}`.

---

## Known limitations (first cut)

- Control-flow extraction is prose-heuristic. Skills using consistent `→` / `on failure:` markers unfold cleanly; pure-narrative skills emit `verify manually` notes on ambiguous branches.
- Nested skills past `--depth` are summarized, not inlined. Recursive unfold (`/unfold /{nested} ...`) is the workaround.
- yaml format is v0 shape — will stabilize once a machine consumer exists. Default `md` for now.
- SHA header depends on git. Non-git checkouts emit `{unresolved}` + warning.
- No CI drift gate yet. Future: `npm run validate:unfold-drift` — re-runs unfold on tracked skills, fails if plan diff appears without corresponding skill-change commit.
