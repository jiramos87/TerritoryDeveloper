---
name: ship-final
purpose: >-
  Close a master-plan version: assert sections closed → assert all stages done
  (no `partial`) → run `validate:all` on cumulative parent-tag-to-HEAD diff →
  `git tag {slug}-v{N}` → flip `ia_master_plans.closed_at` → journal closeout
  row. Final gate of the ship-protocol cycle. Mechanical — no decisions.
audience: agent
loaded_by: "skill:ship-final"
slices_via: master_plan_locate, master_plan_state, spec_section
description: >-
  Close a master-plan version. Phases: assert all `ia_section_claims` open rows
  for the slug = 0 → assert all `ia_stages.status` ∈ {`done`} (no `pending` /
  `in_progress` / `partial`) → run `validate:all` on cumulative diff
  `git diff {parent_tag}..HEAD` → `git tag {slug}-v{N}` (annotated, local
  only) → flip `ia_master_plans.closed_at = now()` via `master_plan_close` MCP
  (must precede journal_append) → `journal_append(phase=version-close,
  payload_kind=version_close, payload={plan_slug, version, tag, sha,
  validate_all_result, sections_closed[]})`. Triggers: "/ship-final {SLUG}",
  "ship final", "close master plan version".
phases:
  - Parse SLUG + load master_plan_state
  - Assert sections closed (open ia_section_claims = 0)
  - Assert all stages done (no partial / pending / in_progress)
  - Cumulative diff validate:all (git diff {parent_tag}..HEAD)
  - Git tag {slug}-v{N} (annotated, local)
  - Flip closed_at via master_plan_close MCP
  - Journal append (payload_kind=version_close)
triggers:
  - /ship-final {SLUG}
  - ship final
  - close master plan version
argument_hint: "{SLUG}"
model: opus
reasoning_effort: high
tools_role: pair-head
tools_extra:
  - mcp__territory-ia__master_plan_state
  - mcp__territory-ia__master_plan_locate
  - mcp__territory-ia__master_plan_close
  - mcp__territory-ia__journal_append
caveman_exceptions:
  - code
  - commits
  - security/auth
  - verbatim error/tool output
  - structured MCP payloads
  - destructive-op confirmations
hard_boundaries:
  - IF any `ia_section_claims` row open for slug → STOP. Run /section-closeout first.
  - IF any `ia_stages.status` ≠ `done` (incl. `partial`, `pending`, `in_progress`) → STOP. Ship remaining stages first.
  - IF `validate:all` exits non-zero on cumulative diff → STOP. Fix + re-run.
  - IF `closed_at` already set on parent → STOP. Version already closed.
  - Do NOT push tag — local only. User decides remote push.
  - Do NOT create v(N+1) row — that is `master_plan_version_create` (separate MCP).
  - Do NOT skip closed_at flip on green validate — must precede journal_append.
  - Do NOT commit — closeout is a tag + DB flip, no source mutations.
caller_agent: ship-final
---

# Ship-final — master-plan version close

Caveman default — [`agent-output-caveman.md`](../../rules/agent-output-caveman.md). Mechanical tool — minimal prose.

**Recipe:** phases run as recipe [`tools/recipes/ship-final.yaml`](../../../tools/recipes/ship-final.yaml).

**Lifecycle:** Runs LAST per ship-protocol cycle — after every `/ship-stage` + every `/section-closeout` complete. Counterpart to `master_plan_version_create` (PR for new version cycle).

**Model:** `opus` + `reasoning_effort=high` — closure decisions are version-final, no rollback.

---

## Inputs

| Parameter | Source | Notes |
|-----------|--------|-------|
| `SLUG` | Caller | Master-plan slug (matches `ia_master_plans.slug`). Required. |

Auto-derived:

- `version` ← `ia_master_plans.version`
- `parent_tag` ← `{slug}-v{version-1}` when `version > 1`; empty when `version = 1` (full HEAD diff).
- `sections_closed[]` ← all `ia_section_claims.section_id` for slug (read-only enumeration).

---

## Invocation

```bash
npm run recipe:run -- ship-final \
  --input slug={SLUG}
```

Recipe steps (`tools/recipes/ship-final.yaml`):

1. **`load_plan`** — `master_plan_state(slug)` → `{version, closed_at, stages[]}`.
2. **`assert_sections_closed`** — bash: count `ia_section_claims` open rows for slug. STOP on `> 0`.
3. **`assert_stages_done`** — bash: assert every `stages[].status === 'done'`. STOP on any `partial` / `pending` / `in_progress`.
4. **`cumulative_validate`** — bash: `git diff {parent_tag}..HEAD` + `npm run validate:all`. STOP on non-zero exit.
5. **`git_tag`** — bash: `git tag -a {slug}-v{N} -m "Close {slug} v{N}"`. Local only, never push.
6. **`close_plan`** — MCP: `master_plan_close(slug)`. Flips `closed_at = now()`. Must precede journal_append.
7. **`journal_close`** — MCP: `journal_append(phase=version-close, payload_kind=version_close, payload={plan_slug, version, tag, sha, validate_all_result, sections_closed[]})`.

---

## Guards

- Open section claim → recipe stops at `assert_sections_closed` (exit 1). Run `/section-closeout {SLUG} {SECTION_ID}` for each open section, retry.
- Stage not done → recipe stops at `assert_stages_done` (exit 1). Ship remaining stages, retry.
- `validate:all` red → recipe stops at `cumulative_validate` (exit 1). Fix surface + re-run.
- Re-run on partial failure: idempotent at DB level — `master_plan_close` is no-op when `closed_at` already set; recipe stops with `version_already_closed` error from MCP layer.
- Tag exists → `git tag` fails fast with native error. Manual `git tag -d` only when retry needed (destructive-op confirmation).

---

## Guardrails

- IF open section claim → STOP. `/section-closeout` first.
- IF any stage `status ≠ done` → STOP. `/ship-stage` remaining stages first.
- IF `validate:all` red → STOP. Fix + re-run.
- IF `closed_at` already set → STOP with `version_already_closed`.
- Do NOT push tag — local only. Pushing is a human-gated step.
- Do NOT mutate code / specs / schemas — closure is metadata-only (tag + closed_at + journal row).
- Do NOT call `master_plan_version_create` from this skill — separate MCP, separate human gate (start of v2).
- Do NOT skip `closed_at` flip on green validate — flip MUST precede journal_append for audit ordering.

---

## Seed prompt

```markdown
Run ship-final for `{SLUG}` (mechanical version close).

Invoke recipe:
  npm run recipe:run -- ship-final \
    --input slug={SLUG}

Recipe: load_plan → assert_sections_closed → assert_stages_done →
        cumulative_validate → git_tag → close_plan → journal_close.

STOP on open section / non-done stage / validate red / closed_at already set.
Do NOT push tag. Do NOT create v(N+1) row. Do NOT commit source.
```

---

## Changelog

| Date | Change | Trigger |
|------|--------|---------|
| 2026-05-05 | NEW skill — ship-protocol Stage 4 (TECH-12643). 7 steps: load_plan + assert_sections_closed + assert_stages_done + cumulative_validate + git_tag + close_plan + journal_close. | `docs/explorations/ship-protocol-exploration.md` Stage 4 |
