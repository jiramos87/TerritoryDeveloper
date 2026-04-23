# Sprite-gen Stage 7 addendum — Cross-tile passthrough pattern (aggregate plan)

> Parent plan: [ia/projects/sprite-gen-master-plan.md](../../ia/projects/sprite-gen-master-plan.md) (Stage 7 addendum block)
> Source handoff: `/tmp/sprite-gen-improvement-session.md` §3 Stage 7 addendum (frozen 2026-04-23)
> Locks consumed: **L17** — cross-tile passthrough pattern applies to both slope and flat archetypes.
> Filing hint: amend Stage 7 decoration authoring guidance when Stage 7 is filed in full; this block stands in until then.

## 1. Scope

The Stage 7 addendum documents an existing visual-language pattern — the slope-sprite "empty lot / natural-park-walkway" passthrough — and extends it to flat archetypes via a new spec flag. `ground.passthrough: true` tells the composer to skip `iso_ground_noise`, clamp `hue_jitter ≤ 0.01`, and force `value_jitter = 0`, so the tile reads as a seamless neighbor-blending bridge. Four tasks: schema flag (TECH-745), composer branch (TECH-746), regression tests (TECH-747), DAS §3 amendment (TECH-748).

## 2. Tasks

| Task key | Issue | Title | Priority | Depends on |
|----------|-------|-------|----------|-----------|
| T7.10.1 | [TECH-745](../../ia/projects/TECH-745.md) | Spec schema — `ground.passthrough` flag | medium | — |
| T7.10.2 | [TECH-746](../../ia/projects/TECH-746.md) | Composer — inhibit noise + clamp jitter on passthrough tiles | medium | TECH-745 |
| T7.10.3 | [TECH-747](../../ia/projects/TECH-747.md) | Tests — `test_ground_passthrough.py` | medium | TECH-745, TECH-746 |
| T7.10.4 | [TECH-748](../../ia/projects/TECH-748.md) | DAS §3 amendment — cross-tile passthrough pattern | low | TECH-745, TECH-746 |

## 3. Lock → task map

| Lock | Task(s) |
|------|---------|
| **L17** — cross-tile passthrough (slope + flat) | TECH-745 (schema), TECH-746 (composer), TECH-747 (regression), TECH-748 (doc) |

## 4. Dependency graph

```
TECH-745 ── TECH-746 ──┬── TECH-747
                       │
                       └── TECH-748
```

## 5. Exit criteria

- [ ] All 4 tasks merged to `master`.
- [ ] `pytest tools/sprite-gen/tests/test_ground_passthrough.py -q` green.
- [ ] `ground.passthrough: true|false` parses; non-bool raises `SpecError`.
- [ ] Composer skips noise + clamps jitter when `passthrough=true`.
- [ ] `passthrough=false` (default) byte-identical to pre-addendum baseline.
- [ ] DAS §3 documents slope pattern + flat extension + rendering implications.

## 6. Out of scope

- Slope-archetype implementation (pre-existing art pipeline; no code change).
- Curator UI for flagging passthrough tiles (future).
- Multi-tile blending algorithm (passthrough is single-tile-local only).

## 7. Cross-stage notes

- Stage 6.4 ground schema (TECH-715, TECH-718) is the substrate — this addendum adds exactly one sibling key (`passthrough`) without disturbing `material` / `materials` / `hue_jitter` / `value_jitter` / `texture`.
- TECH-747's byte-identical baseline must be captured against the pre-addendum composer, then preserved across Stage 6.5 / 6.6 merges until Stage 7 addendum lands.
- Stage 7 master block (when filed) should fold this addendum into decoration-authoring guidance for the flat-archetype yard/pavement contexts.
