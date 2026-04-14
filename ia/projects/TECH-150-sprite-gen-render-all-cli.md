---
purpose: "TECH-150 — Sprite-gen render --all batch + --terrain override flag."
audience: both
loaded_by: ondemand
slices_via: none
---
# TECH-150 — Sprite-gen `render --all` + `--terrain` CLI flag

> **Issue:** [TECH-150](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-14
> **Last updated:** 2026-04-14

## 1. Summary

Extend CLI (TECH-149) w/ `render --all` batch mode + `--terrain {slope_id}` override flag. `--all` globs `specs/*.yaml`, iterates, aggregates exit code (0 iff all succeeded, else 1 + prints failed archetypes). `--terrain` overrides spec `terrain:` field per exploration §10. Stage 1.2 validates flag parsing; non-`flat` path lands Stage 1.4 (slope-aware foundation).

## 2. Goals and Non-Goals

### 2.1 Goals

1. `render --all` globs `specs/*.yaml` and runs `render {name}` logic per spec.
2. Aggregate exit: 0 iff all succeeded, else 1 + prints `failed: [name1, name2]` to stderr.
3. `--terrain {slope_id}` overrides spec `terrain:` before compose.
4. `--terrain flat` (Stage 1.2) works end-to-end. `--terrain N` etc. accepted at parse time but compose path handles only `flat` until Stage 1.4 lands foundation primitive.

### 2.2 Non-Goals (Out of Scope)

1. Slope auto-insert — Stage 1.4.
2. Parallel batch (multi-process) — deferred; serial loop sufficient for 15-archetype EA scope.
3. `--palette` override — not in exploration §10; follow-up if needed.

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Sprite-gen dev | As batch operator, I want one-command all-archetype render so that Step 3 EA bulk lands w/o per-archetype shell glue | `python -m sprite_gen render --all` iterates every `specs/*.yaml` + reports aggregate status |

## 4. Current State

### 4.1 Domain behavior

TECH-149 ships single-archetype render only. Batch iteration requires shell `for` loop + manual error aggregation.

### 4.2 Systems map

- `tools/sprite-gen/src/cli.py` (extended from TECH-149).
- `tools/sprite-gen/specs/` — glob source dir.
- `docs/isometric-sprite-generator-exploration.md` §10 CLI interface — ground truth.

## 5. Proposed Design

### 5.1 Target behavior (product)

`render` subparser gains mutually-exclusive group: positional `archetype` xor `--all` flag. When `--all`, globs `specs/*.yaml`, strips `.yaml` extension, iterates w/ try/except per spec; appends failed names to list; final exit reflects aggregate. `--terrain {slope_id}` flag available in both modes; parsed, overrides spec dict before `compose_sprite`.

### 5.2 Architecture / implementation (agent-owned unless fixed by design)

- Validate `slope_id` against enum `{flat, N, S, E, W, NE, NW, SE, SW, NE-up, NW-up, SE-up, SW-up, NE-bay, NW-bay, NW-bay-2, SE-bay, SW-bay}` — matches **Slope variant naming** glossary. Reject unknown w/ exit 1.
- Stage 1.2 compose raises `NotImplementedError` (caught, maps to exit 1) when `terrain != 'flat'` — Stage 1.4 replaces w/ slope-aware path.

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|------------------------|
| 2026-04-14 | Validate slope enum at CLI parse | Fail fast w/ typos | Defer to compose — later failure, worse UX |

## 7. Implementation Plan

### Phase 1 — Batch + terrain flag

- [ ] Extend `render` subparser w/ `--all` + `--terrain`.
- [ ] Glob loop + aggregate exit.
- [ ] `--terrain` override applied pre-compose.
- [ ] Slope enum validation.

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| `--all` writes all archetypes | Python | `pytest tools/sprite-gen/tests/test_cli.py::test_render_all` | Fixture specs dir |
| Aggregate exit 1 on any failure | Python | `pytest tools/sprite-gen/tests/test_cli.py::test_render_all_aggregate` | Inject malformed YAML |
| Invalid slope id rejected | Python | `pytest tools/sprite-gen/tests/test_cli.py::test_terrain_bad_enum` | `--terrain XYZ` → exit 1 |
| IA / validate chain | Node | `npm run validate:all` | |

## 8. Acceptance Criteria

- [ ] `render --all` runs every `specs/*.yaml`; aggregate exit correct.
- [ ] `--terrain flat` override works; `--terrain N` parses but raises NotImplementedError (Stage 1.4 lights up).
- [ ] Invalid slope id exits 1 w/ error message.
- [ ] `npm run validate:all` green.

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|
| 1 | … | … | … |

## 10. Lessons Learned

- …

## Open Questions (resolve before / during implementation)

1. None — tooling only; see §8 Acceptance criteria.
