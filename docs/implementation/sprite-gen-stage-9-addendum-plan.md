# Sprite-gen Stage 9 addendum — Parametric `tiled-row-N` / `tiled-column-N` (aggregate plan)

> Parent plan: [ia/projects/sprite-gen-master-plan.md](../../ia/projects/sprite-gen-master-plan.md) (Stage 9 addendum block)
> Source handoff: `/tmp/sprite-gen-improvement-session.md` §3 Stage 9 addendum (frozen 2026-04-23)
> Issues closed: **I7**
> Filing hint: amend Stage 9 T9.2 when Stage 9 is filed in full; this block stands in until then.

## 1. Scope

The Stage 9 addendum upgrades the slot-name grammar from a hard-coded enum (`tiled-row-3`, `tiled-row-4`, `tiled-column-3`) to a parametric form: `tiled-(row|column)-N` for any `N ≥ 2`. `resolve_slot` distributes N buildings evenly along the named axis with integer-pixel anchors while respecting footprint. Four tasks: parser (TECH-741), resolver (TECH-742), tests (TECH-743), DAS §5 R11 amendment (TECH-744).

Cross-stage note: TECH-734 (`row_houses_3x`, Stage 6.6) was filed with `depends_on: [TECH-744]` — `row_houses_3x` only renders cleanly after the addendum lands.

## 2. Tasks

| Task key | Issue | Title | Priority | Depends on |
|----------|-------|-------|----------|-----------|
| T9.add.1 | [TECH-741](../../ia/backlog-archive/TECH-741.yaml) | Slot name grammar — `tiled-(row\|column)-N` parser | high | — |
| T9.add.2 | [TECH-742](../../ia/backlog-archive/TECH-742.yaml) | `resolve_slot` distribute N evenly across axis | high | TECH-741 |
| T9.add.3 | [TECH-743](../../ia/backlog-archive/TECH-743.yaml) | Tests — `test_parametric_slots.py` | high | TECH-741, TECH-742 |
| T9.add.4 | [TECH-744](../../ia/backlog-archive/TECH-744.yaml) | DAS §5 R11 amendment — parametric slot grammar | medium | TECH-741, TECH-742, TECH-743 |

## 3. Issue → task map

| Issue | Task(s) |
|-------|---------|
| **I7** — parametric slot grammar | TECH-741 (parser), TECH-742 (resolver), TECH-743 (tests), TECH-744 (doc) |

## 4. Dependency graph

```
TECH-741 ── TECH-742 ── TECH-743 ── TECH-744
   │           │            │           │
   └───────────┴────────────┴───────────┘
                 (all feed capstone)

Downstream consumers:
   TECH-734 (row_houses_3x, Stage 6.6) ── TECH-744
   Stage 9 T9.3 (MediumResidentialBuilding-2-128.png 5-house row) — uses tiled-row-5 post-addendum
```

## 5. Exit criteria

- [ ] All 4 tasks merged to `master`.
- [ ] `pytest tools/sprite-gen/tests/test_parametric_slots.py -q` green.
- [ ] `parse_slot("tiled-row-N")` returns `("row", N)` for `N ∈ {2..5}` at minimum.
- [ ] `resolve_slot` anchors are integer pixels, equal-spaced, inside footprint.
- [ ] DAS §5 R11 documents parametric grammar + legacy note + `row_houses_3x` forward pointer.
- [ ] TECH-734's `row_houses_3x` renders cleanly once addendum merges.

## 6. Out of scope

- Stage 9 master block tasks (T9.1, T9.3–T9.6) — land when Stage 9 is filed proper.
- Non-tiled slot shapes (irregular placements, clusters) — future work.
- Cross-tile passthrough — Stage 7 addendum.

## 7. Cross-stage notes

- When Stage 9 is filed as its own block, fold this addendum into T9.2's task description; hard-coded `tiled-row-3/4` references in that master block become aliases through the parametric parser.
- TECH-744 closes the loop on TECH-734's forward-dependency — the `row_houses_3x` preset file exists today but only renders cleanly after the addendum merges.
- `_TILE = 32` (Stage 9 locked) is the canonical tile pixel size; both TECH-742 resolver and TECH-743 tests must import the same constant to avoid drift.
