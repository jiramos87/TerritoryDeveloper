# progress-tracker

Static HTML progress tracker for Territory Developer master-plan orchestrator documents.

## Usage

```sh
npm run progress
```

Reads all `ia/projects/*master-plan*.md` files, parses step / stage / phase / task state, and writes `docs/progress.html` — a self-contained single-page dashboard with per-plan progress cards and an overall combined bar.

Open `docs/progress.html` in any browser with no network access required (inline CSS, zero JS, zero external fetches).

## Parsing contract

Targets per master-plan file:

| Target | Pattern |
|--------|---------|
| Plan title | First `# …` heading |
| Overall status | `> **Status:** …` blockquote near document top |
| Step heading | `### Step N — title` |
| Stage heading | `#### Stage N.M — title` |
| Step / stage status | `**Status:** Draft \| In Review \| In Progress — {detail} \| Final` |
| Phase checklist | `- [ ] Phase …` / `- [x] Phase …` bullets within a stage section |
| Task table | Markdown table with columns `Task \| Phase \| Issue \| Status \| Intent`; status enum: `_pending_ \| Draft \| In Review \| In Progress \| Done (archived)` |
| Sibling warnings | Blockquote lines (`> …`) referencing `master-plan`, `Parallel-work rule`, or `Sibling orchestrator` |

## Determinism guarantee

`npm run progress` is fully deterministic: same Markdown input → identical HTML bytes. No wall-clock reads, no `Date.now()`, no git-log queries, no environment-dependent ordering. Run twice without touching master plans → `git diff docs/progress.html` is empty.

## Lifecycle-skill hook contract

The following lifecycle skills invoke `npm run progress` automatically so `docs/progress.html` stays in sync after state-flip events. **No manual regen needed** after these operations:

| Skill | When regen runs | Where in skill body |
|-------|----------------|---------------------|
| `master-plan-new` | After new orchestrator doc written | Phase 8b, before handoff |
| `stage-file` | After orchestrator task table flipped `_pending_` → `Draft` | Post-loop step 1b, before validate:all |
| `project-stage-close` | After stage status flipped to `Final` + sanity-check passes | Post-flip section, before handoff emit |
| `project-spec-close` | After BACKLOG archive row written + spec file deleted | Post-archive section (step 9b) |

After any of these skills completes, a `git status` will show `docs/progress.html` modified (or no change if master-plan state was already up to date with a prior regen).

The regen is a no-op side-effect-free operation when master-plan state has not changed since the last run.

## Module layout

```
tools/progress-tracker/
  index.mjs        CLI entrypoint (glob → parse → render → write)
  parse.mjs        Pure parser: Markdown bytes → PlanData
  render.mjs       Pure renderer: PlanData[] → HTML string
  package.json     ES modules; Node built-ins only; no runtime deps
  README.md        This file
  tests/
    parse.test.mjs  Fixture tests against the three in-flight master plans
    render.test.mjs Snapshot tests on generated HTML structure
```

## Running tests

```sh
node --test tools/progress-tracker/tests/parse.test.mjs
node --test tools/progress-tracker/tests/render.test.mjs
```

Both test suites run against the live master-plan files — no separate fixture copies needed.

## Output

`docs/progress.html` — single committed file, regenerated on every `npm run progress` invocation.
