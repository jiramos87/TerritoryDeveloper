# Sprite-gen Stage 6.6 — Preset system (aggregate plan)

> Parent plan: [ia/projects/sprite-gen-master-plan.md](../../ia/projects/sprite-gen-master-plan.md) (Stage 6.6 block)
> Source handoff: `/tmp/sprite-gen-improvement-session.md` §3 Stage 6.6 block (frozen 2026-04-23)
> Locks consumed: **L13** — `preset: <name>` top-level key injects a base spec; author fields override; `vary:` block from preset is preserved (author may extend / override individual axes but not wipe the block).

## 1. Scope

Stage 6.6 adds a preset layer on top of the sprite-gen spec grammar. `tools/sprite-gen/presets/<name>.yaml` holds fully-valid specs (minus `id` / `output.name`). A new `preset: <name>` top-level key resolves the preset as a base, layers author-supplied fields on top (author wins per-field), and applies a strict merge rule to the `vary:` block (preset axes preserved, author may union new axes or override per-axis, whole-block wipe raises). Three presets ship as live consumers: `suburban_house_with_yard`, `strip_mall_with_parking`, `row_houses_3x`. Tests lock the contract end-to-end. DAS §6 catalogues it all.

## 2. Tasks

| Task key | Issue | Title | Priority | Depends on |
|----------|-------|-------|----------|-----------|
| T6.6.1 | [TECH-730](../../ia/projects/TECH-730.md) | Loader — `preset:` inject + author override | high | TECH-709..714 |
| T6.6.2 | [TECH-731](../../ia/projects/TECH-731.md) | `vary:` block merge rule (union + non-wipe) | high | TECH-730 |
| T6.6.3 | [TECH-732](../../ia/projects/TECH-732.md) | Seed preset — `suburban_house_with_yard` | medium | TECH-730, TECH-731, TECH-715, TECH-718 |
| T6.6.4 | [TECH-733](../../ia/projects/TECH-733.md) | Seed preset — `strip_mall_with_parking` | medium | TECH-730, TECH-731, TECH-715, TECH-718 |
| T6.6.5 | [TECH-734](../../ia/projects/TECH-734.md) | Seed preset — `row_houses_3x` | medium | TECH-730, TECH-731, TECH-744 |
| T6.6.6 | [TECH-735](../../ia/projects/TECH-735.md) | Tests — `test_preset_system.py` | high | TECH-730..734 |
| T6.6.7 | [TECH-736](../../ia/projects/TECH-736.md) | DAS §6 addendum — preset contract + catalogue | medium | TECH-730..734 |

## 3. Lock → task map

| Lock | Task(s) |
|------|---------|
| **L13** — preset as base, author overrides, `vary:` preserved | TECH-730 (loader), TECH-731 (vary merge), TECH-732/733/734 (seed presets), TECH-735 (regression), TECH-736 (doc) |

## 4. Dependency graph

```
Stage 6.3 (TECH-709..714) ─┐
                           └── TECH-730 ── TECH-731 ──┬── TECH-732 ──┐
                                                      ├── TECH-733 ──┼── TECH-735 ── TECH-736
                                                      └── TECH-734 ──┘
                                                           │
Stage 9 addendum (TECH-744) ──────────────────────────────┘
Stage 6.4 (TECH-715, TECH-718) ── TECH-732, TECH-733
```

## 5. Exit criteria

- [ ] All 7 tasks merged to `master`.
- [ ] `pytest tools/sprite-gen/tests/ -q` green (5 new tests in `test_preset_system.py`).
- [ ] Three seeded presets render cleanly (`row_houses_3x` gated on Stage 9 addendum / TECH-744).
- [ ] DAS §6 documents preset grammar + merge rule + wipe-guard + catalogue.
- [ ] Grep check: `preset:`, `vary:` merge rule, and all 3 preset names present in DAS.

## 6. Out of scope (Stage 6.7+)

- Animation schema reservation (Stage 6.7 / L16).
- Parametric `tiled-row-N` / `tiled-column-N` slots (Stage 9 addendum / TECH-744).
- Cross-tile passthrough (Stage 7 addendum / L17).
- Curator UI consuming presets (future).

## 7. Cross-stage notes

- TECH-734 (`row_houses_3x`) forward-depends on TECH-744 (Stage 9 parametric slot). The preset file is written now; render activation waits on the Stage 9 addendum. Test file (TECH-735) scaffolds the assertion behind a skip-guard so it's useful pre-Stage-9 merge.
- TECH-732/733 consume TECH-715 (ground object form) and TECH-718 (composer jitter/texture) from Stage 6.4 — both merged before Stage 6.6 opens.
- `vary:` merge rule (TECH-731) must coexist with the Stage 6.5 signature aggregator — presets don't change variant scoring; the aggregator reads the resolved spec, not the preset.
