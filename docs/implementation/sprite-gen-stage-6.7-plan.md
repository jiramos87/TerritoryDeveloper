# Sprite-gen Stage 6.7 — Animation schema reservation (aggregate plan)

> Parent plan: [ia/projects/sprite-gen-master-plan.md](../../ia/projects/sprite-gen-master-plan.md) (Stage 6.7 block)
> Source handoff: `/tmp/sprite-gen-improvement-session.md` §3 Stage 6.7 block (frozen 2026-04-23)
> Locks consumed: **L16** — reserve animation schema today; implementation deferred.

## 1. Scope

Stage 6.7 reserves the animation surface in the spec grammar without implementing any frame rendering. Spec loader recognises `output.animation:` but permits only `enabled: false`; sibling keys (`frames`, `fps`, `loop`, `phase_offset`, `layers`) are accepted and preserved. A centralised composer guard lets any decoration / building-detail carry `animate: none`; any other value raises `NotImplementedError("Animation deferred; see DAS §12")`. DAS §12 documents the reservation.

## 2. Tasks

| Task key | Issue | Title | Priority | Depends on |
|----------|-------|-------|----------|-----------|
| T6.7.1 | [TECH-737](../../BACKLOG-ARCHIVE.md) | Spec loader — reserved `output.animation` block | medium | — |
| T6.7.2 | [TECH-738](../../BACKLOG-ARCHIVE.md) | Per-primitive `animate:` reservation | medium | TECH-737 |
| T6.7.3 | [TECH-739](../../BACKLOG-ARCHIVE.md) | Tests — `test_animation_reservation.py` | medium | TECH-737, TECH-738 |
| T6.7.4 | [TECH-740](../../BACKLOG-ARCHIVE.md) | DAS §12 stub — Animation (reserved; not yet implemented) | low | TECH-737, TECH-738 |

## 3. Lock → task map

| Lock | Task(s) |
|------|---------|
| **L16** — reserve animation schema, implementation deferred | TECH-737 (spec loader), TECH-738 (composer guard), TECH-739 (regression), TECH-740 (doc) |

## 4. Dependency graph

```
TECH-737 ──┬── TECH-738 ──┬── TECH-739
           │               │
           └───────────────┴── TECH-740
```

## 5. Exit criteria

- [ ] All 4 tasks merged to `master`.
- [ ] `pytest tools/sprite-gen/tests/ -q` green (4 new tests in `test_animation_reservation.py`).
- [ ] `output.animation.enabled: false` + reserved siblings parse clean.
- [ ] `output.animation.enabled: true` raises `SpecError` referencing DAS §12.
- [ ] Primitive `animate: none` renders; other values raise `NotImplementedError` with "DAS §12" in message.
- [ ] DAS §12 exists with reserved-keys table + v1 permitted values + forward pointer.

## 6. Out of scope

- Actual frame rendering.
- Animation timing / compositor integration.
- Curator UI for animated variants.

## 7. Cross-stage notes

- Stage 6.7 is independent — it can ship alongside or ahead of Stage 6.6.
- The `ANIMATION_RESERVED_KEYS` constant (TECH-737) is the authoritative source — TECH-740's doc table must cite the same set.
- Error messages in TECH-737/738 must literally contain "DAS §12" — TECH-739 asserts it and TECH-740 relies on it for grep-based doc-code alignment.
