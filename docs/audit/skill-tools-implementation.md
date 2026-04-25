# Skill Tools — Implementation Spec

Pure-mechanical implementation plan for `tools/scripts/skill-tools/` CLI. Single source of truth = `ia/skills/{slug}/SKILL.md` frontmatter; command + agent + cursor mirror are derived. Eliminates 4-surface duplication observed in Phase A/B/C audits.

**Scope:** MVP only — `skill sync` + `skill lint`. Phase 2 (`new`, `rename`, `delete`, `move-shared`) deferred to follow-up spec.

**Non-goals:** Replacing skill body recipe authoring (still hand-written in SKILL.md), refactoring existing 45 SKILL.md files, changing MCP `caller_agent` allowlists.

---

## 1. Canonical Frontmatter Schema

File: `tools/scripts/skill-tools/schema.ts`. Zod schema. All existing SKILL.md frontmatter migrated to this shape; missing fields raise lint warning (not fatal in MVP).

```ts
import { z } from "zod";

export const SkillFrontmatterSchema = z.object({
  // Existing fields (no change)
  name: z.string().regex(/^[a-z0-9-]+$/),
  purpose: z.string().min(20),
  audience: z.enum(["agent", "human", "both"]),
  loaded_by: z.string(),
  slices_via: z.string(),
  description: z.string().min(40),  // Multi-line YAML scalar OK
  phases: z.array(z.string()).min(1),

  // New fields (canonical for sync)
  triggers: z.array(z.string()).min(1),       // ["/ship {ISSUE_ID}", "ship task"]
  argument_hint: z.string().optional(),       // "{ISSUE_ID} (e.g. TECH-42)"
  model: z.enum(["opus", "sonnet", "haiku"]).optional(),
  reasoning_effort: z.enum(["low", "medium", "high"]).optional(),
  tools_role: z.enum([
    "standalone-pipeline",
    "stage-pipeline",
    "pair-head",
    "pair-tail",
    "planner",
    "implementer",
    "validator",
    "lifecycle-helper",
    "custom",
  ]),
  tools_extra: z.array(z.string()).default([]),  // Tools added on top of role baseline
  caveman_exceptions: z.array(z.string()).default([
    "code", "commits", "security/auth", "verbatim error/tool output", "structured MCP payloads",
  ]),
  hard_boundaries: z.array(z.string()).default([]),
  caller_agent: z.string().optional(),  // For cursor mirror MCP caller line
});
```

**Tool role baselines:** Map of `tools_role` → tool list. Source: existing `tools/scripts/validate-agent-tools-uniformity.ts` HEAD/TAIL constants + new entries.

```ts
// tools/scripts/skill-tools/tool-roles.ts
export const TOOL_ROLE_BASELINES: Record<string, readonly string[]> = {
  "standalone-pipeline": [/* full ship-style: Read, Edit, Write, Bash, Grep, Glob + verify + MCP wide */],
  "stage-pipeline":      [/* ship-stage style */],
  "pair-head":           [/* HEAD_BASELINE from validator */],
  "pair-tail":           [/* TAIL_BASELINE from validator */],
  "planner":             [/* router + glossary + spec_section + backlog_issue */],
  "implementer":         [/* spec-implementer style */],
  "validator":           [/* read + grep + lint MCPs */],
  "lifecycle-helper":    [/* minimal: read + edit + glob */],
  "custom":              [],  // Empty — agent must list all in tools_extra
};
```

---

## 2. File Layout

```
tools/scripts/skill-tools/
├── index.ts              # CLI entry: parse argv → dispatch subcommand
├── schema.ts             # Zod schemas
├── tool-roles.ts         # Role → baseline tool list map
├── frontmatter.ts        # Read/parse/validate SKILL.md frontmatter
├── render-command.ts     # Frontmatter → .claude/commands/{slug}.md
├── render-agent.ts       # Frontmatter → .claude/agents/{slug}.md
├── render-cursor.ts      # Frontmatter → .cursor/rules/cursor-skill-{slug}.mdc
├── lint.ts               # Drift + completeness checks
└── __tests__/
    ├── render-command.test.ts
    ├── render-agent.test.ts
    ├── render-cursor.test.ts
    └── lint.test.ts
```

**No new dependencies** beyond what `package.json` already pulls (`zod`, `tsx`, `js-yaml`). Verify before coding via `grep '"zod"' package.json`.

---

## 3. Render Contracts (each generator)

### 3.1 `render-command.ts`

**Input:** `SkillFrontmatter` + path to skill dir.
**Output:** Markdown string for `.claude/commands/{slug}.md`.

**Template (literal string):**

```
---
description: {{ description (collapsed to 1 line, ≤200 chars) }}
argument-hint: "{{ argument_hint || '' }}"
---

# /{{ name }} — {{ purpose-summary (1 line from purpose) }}

Drive `$ARGUMENTS` via the [`{{ name }}`](../agents/{{ name }}.md) subagent. {{ purpose-summary }}

Follow `caveman:caveman` for all output. Standard exceptions: {{ caveman_exceptions joined ", " }}. Anchor: `ia/rules/agent-output-caveman.md`.

## Triggers

{{ triggers as bullet list }}

## Dispatch

Single Agent invocation with `subagent_type: "{{ name }}"` carrying `$ARGUMENTS` verbatim.

## Hard boundaries

See [`ia/skills/{{ name }}/SKILL.md`](../../ia/skills/{{ name }}/SKILL.md) §Hard boundaries.
```

**Hand-written body override:** If `ia/skills/{slug}/command-body.md` exists, append its content in place of the dispatch + hard-boundaries sections. Detected by literal `<!-- skill-tools:body-override -->` marker in command file's existing content during sync (to preserve mid-migration).

### 3.2 `render-agent.ts`

**Input:** `SkillFrontmatter`.
**Output:** Markdown string for `.claude/agents/{slug}.md`.

**Tool list resolution:** `[...TOOL_ROLE_BASELINES[tools_role], ...tools_extra]` deduped, comma-separated.

**Template:**

```
---
name: {{ name }}
description: {{ description (collapsed to 1 line) }}
tools: {{ resolved-tool-list }}
model: {{ model || 'sonnet' }}
{{ #if reasoning_effort }}reasoning_effort: {{ reasoning_effort }}{{ /if }}
---

## Stable prefix (Tier 1 cache)

> `cache_control: {"type":"ephemeral","ttl":"1h"}` — per `docs/prompt-caching-mechanics.md` §3 Tier 1.

@ia/skills/_preamble/stable-block.md

Follow `caveman:caveman` for all responses. Standard exceptions: {{ caveman_exceptions joined ", " }}. Anchor: `ia/rules/agent-output-caveman.md`.

@.claude/agents/_preamble/agent-boot.md

# Mission

Run [`ia/skills/{{ name }}/SKILL.md`](../../ia/skills/{{ name }}/SKILL.md) end-to-end for `$ARGUMENTS`. {{ purpose }}

# Recipe

Follow `ia/skills/{{ name }}/SKILL.md` end-to-end. Phase sequence:

{{ phases as numbered list }}

# Hard boundaries

{{ hard_boundaries as bullet list }}

See [`ia/skills/{{ name }}/SKILL.md`](../../ia/skills/{{ name }}/SKILL.md) §Hard boundaries for full constraints.
```

**Hand-written body override:** Same marker pattern — `<!-- skill-tools:body-override -->`. Per-agent Execution model + Verification + Output sections fall under override block (not all skills need them).

### 3.3 `render-cursor.ts`

**Input:** `SkillFrontmatter`.
**Output:** `.cursor/rules/cursor-skill-{name}.mdc`.

**Template:** identical to existing `tools/scripts/generate-cursor-skill-wrappers.mjs` output. Replace existing `.mjs` with TS port; add `caller_agent` resolution to `frontmatter.caller_agent || existing-map-fallback`.

---

## 4. CLI Surface

`tools/scripts/skill-tools/index.ts`:

```ts
#!/usr/bin/env tsx

const subcommand = process.argv[2];
const slug = process.argv[3];
const flags = parseFlags(process.argv.slice(4));  // --apply, --diff, --json

switch (subcommand) {
  case "sync":
    return await runSync(slug, flags);    // Default: diff-only. --apply: write.
  case "lint":
    return await runLint(slug, flags);    // slug optional; absent = lint all
  default:
    printUsage();
    process.exit(2);
}
```

**`runSync` mechanical contract:**

1. Read `ia/skills/{slug}/SKILL.md` frontmatter.
2. Validate via Zod. Fatal on schema error.
3. Render command + agent + cursor strings.
4. Compare against existing files on disk.
5. If `--diff`: print unified diff per file. Exit 0 if no drift, 1 if drift.
6. If `--apply`: write files. Exit 0 on success.

**`runLint` mechanical contract:**

| Check | Logic | Severity |
|---|---|---|
| 1. Frontmatter parses | Zod success | error |
| 2. `description` parity | Identical normalized text in command + agent + cursor + skill | warning |
| 3. `triggers` parity | All triggers present in agent description + command body | warning |
| 4. Tool list matches role | Agent `tools:` ⊇ `TOOL_ROLE_BASELINES[tools_role]` | error |
| 5. No retired skill mentions | grep agent + command for any name in retired-list | error |
| 6. Cross-ref freshness | All `[/skill-name](path)` links resolve to existing file | warning |
| 7. Phases match | Agent recipe phase count = `frontmatter.phases.length` | warning |

Output: JSON when `--json` flag, table otherwise. Exit 1 on any error severity.

---

## 5. `package.json` Wiring

```json
{
  "scripts": {
    "skill:sync": "tsx tools/scripts/skill-tools/index.ts sync",
    "skill:sync:all": "for f in ia/skills/*/SKILL.md; do tsx tools/scripts/skill-tools/index.ts sync $(dirname $f | xargs basename) --apply; done",
    "skill:lint": "tsx tools/scripts/skill-tools/index.ts lint",
    "validate:skill-drift": "tsx tools/scripts/skill-tools/index.ts lint --json"
  }
}
```

Add `validate:skill-drift` to `validate:all` chain (between `validate:agent-tools` and `validate:mcp-readme`).

Retire `generate-cursor-skill-wrappers.mjs` after migration; replace with `npm run skill:sync:all`.

---

## 6. Migration Plan

### Phase 1 — Tooling (1 PR)

1. Add Zod schema + role baselines + 3 renderers + lint.
2. Wire CLI + npm scripts.
3. Unit tests (render snapshot tests + lint cases).
4. Run `skill:lint` over all 45 skills → expect drift findings (warnings only at this stage).

### Phase 2 — Frontmatter migration (1 PR per N skills, batched)

For each `ia/skills/{slug}/SKILL.md`:

1. Add new frontmatter fields (`triggers`, `tools_role`, `tools_extra`, `caveman_exceptions`, `hard_boundaries`, `caller_agent`).
2. Run `npm run skill:sync {slug} --diff` → review drift.
3. Hand-promote per-agent Execution model + Verification sections into `command-body.md` / `agent-body.md` partials with override marker.
4. Run `npm run skill:sync {slug} --apply`.
5. Verify `validate:all` passes.

**Migration order (safest first):**

1. `ship` (small, recently authored, dogfood)
2. `verify-loop`, `unfold` (single-purpose, low cross-refs)
3. `master-plan-new`, `master-plan-extend`, `stage-decompose` (lifecycle authoring)
4. `stage-authoring`, `ship-stage` (high cross-refs, do last)
5. Pair-seam skills (`opus-code-review`, `plan-applier`, etc.) — touch validator allowlists carefully

### Phase 3 — Lock down (1 PR)

1. Promote drift checks from `warning` → `error` in `lint`.
2. Add pre-commit hook: any direct edit to `.claude/commands/{slug}.md` or `.claude/agents/{slug}.md` without matching SKILL.md frontmatter change → block.
3. Update `CLAUDE.md` §4: command + agent files are GENERATED — edit SKILL.md frontmatter instead.
4. Retire `generate-cursor-skill-wrappers.mjs`.

---

## 7. Test Plan

### Unit (per renderer)

- Snapshot test: known frontmatter → expected output string, byte-for-byte.
- Edge: missing optional fields → defaults applied.
- Edge: empty `tools_extra` → role baseline only.
- Edge: `tools_role: custom` + empty `tools_extra` → fail render with explicit error.

### Integration

- Round-trip: render → parse rendered file → frontmatter equals input (description normalized).
- Lint over fixtures: 1 clean skill (PASS), 1 drift (WARN), 1 missing tool (ERROR), 1 retired-mention (ERROR).

### Migration smoke

- After `ship` migration: `validate:all` passes; `npm run skill:sync ship --diff` reports zero drift; `npm run skill:lint ship` PASS.

---

## 8. Risk + Mitigation

| Risk | Mitigation |
|---|---|
| Per-agent prose loss on sync (e.g. ship's Execution model paragraph) | Body override partial files (`command-body.md`, `agent-body.md`) with marker block |
| Tool baseline drift between validator + skill-tools role map | Single source: import baselines from `validate-agent-tools-uniformity.ts` constants |
| Existing 45 SKILL.md frontmatter incomplete | MVP lint = warning-level only; Phase 3 promotes to error after migration done |
| `validate:all` red on first run after Phase 1 lands | Lint warnings don't fail validate; only Phase 3 lockdown gates CI |

---

## 9. Acceptance Criteria

- `npm run skill:sync ship --apply` regenerates command + agent + cursor mirror identical to current commits.
- `npm run skill:lint` exits 0 on clean repo, exits 1 on injected drift fixture.
- `npm run validate:skill-drift` integrated into `validate:all`.
- One skill (`ship`) fully migrated to canonical frontmatter; rendered files match hand-authored versions byte-for-byte after override partials extracted.
- Unit + integration tests pass under `npm run test:scripts`.
- Phase A/B/C-style cross-cutting refactor (e.g. "extract caveman directive") executable as: edit one shared partial + run `npm run skill:sync:all --apply`. Validated by post-MVP refactor of caveman directive into shared `agent-boot.md`.

---

## 10. Out of Scope (Phase 2+)

- `skill new {slug}` scaffolder
- `skill rename {old} {new}` (cross-ref aware)
- `skill delete {slug}` (orphan-aware)
- `skill move-shared {section}` (extract shared block + sweep)
- Auto-import of role baselines into MCP `caller_agent` allowlists
- Migration of `ia/rules/agent-router.md` index (still hand-maintained)

---

## 11. Effort Estimate

- Phase 1 (tooling): ~6h. Schema + 3 renderers + lint + tests + npm wiring.
- Phase 2 (migration): ~30 min per skill × 45 = ~22h spread across many PRs. Parallelizable.
- Phase 3 (lockdown): ~2h. Pre-commit hook + CLAUDE.md update + lint promotion.

**Total: ~30h spread.** Phase 1 ships first; Phase 2 incremental; Phase 3 once 80%+ migrated.

---

## 12. Next Action

Land Phase 1 in a single PR titled `feat(skill-tools): MVP — sync + lint over canonical SKILL.md frontmatter`. Dogfood by migrating `ship` in same PR. After merge, open follow-up tracking issue for Phase 2 batched migration.
