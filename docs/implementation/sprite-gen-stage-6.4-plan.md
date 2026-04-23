# Sprite-gen Stage 6.4 — Ground variation (aggregate plan)

> Parent plan: [ia/projects/sprite-gen-master-plan.md](../../ia/projects/sprite-gen-master-plan.md) (Stage 6.4 block)
> Source handoff: `/tmp/sprite-gen-improvement-session.md` §3 Stage 6.4 block (frozen 2026-04-23)
> Locks consumed: **L8** (`ground:` accepts string or object), **L9** (`ground.*` joins `vary:` vocabulary), **L10** (new primitive `iso_ground_noise` + palette accent keys).

## 1. Scope

Stage 6.4 lifts the ground surface from a single-material string into a variable, textured, jitter-aware expression of intent. Loader normalises both forms; composer honours jitter + auto-inserts noise; palette carries accent colours; signatures feed data-driven jitter bounds; variants grammar gains a ground axis; a single test file locks the whole surface; docs close the loop.

## 2. Tasks

| Task key | Issue | Title | Priority | Depends on |
|----------|-------|-------|----------|-----------|
| T6.4.1 | [TECH-715](../../ia/projects/TECH-715.md) | Ground schema — string / object form loader normalization | high | TECH-710..714 |
| T6.4.2 | [TECH-716](../../ia/projects/TECH-716.md) | Palette JSON `accent_dark` / `accent_light` keys | medium | TECH-714 |
| T6.4.3 | [TECH-717](../../ia/projects/TECH-717.md) | `iso_ground_noise` primitive | high | TECH-716 |
| T6.4.4 | [TECH-718](../../ia/projects/TECH-718.md) | Composer ground jitter + texture auto-insert | high | TECH-715, TECH-717 |
| T6.4.5 | [TECH-719](../../ia/projects/TECH-719.md) | Signature extractor `ground.*` extension | medium | TECH-714 |
| T6.4.6 | [TECH-720](../../ia/projects/TECH-720.md) | `vary.ground.*` grammar | high | TECH-710, TECH-715 |
| T6.4.7 | [TECH-721](../../ia/projects/TECH-721.md) | Tests — `test_ground_variation.py` | high | TECH-718, TECH-720 |
| T6.4.8 | [TECH-722](../../ia/projects/TECH-722.md) | DAS §4.1 addendum — accent keys + noise density | medium | TECH-716, TECH-717 |

## 3. Lock → task map

| Lock | Task(s) |
|------|---------|
| **L8** — `ground:` accepts string or object; back-compat by construction | TECH-715 (normaliser), TECH-718 (composer consumes), TECH-721 (legacy byte-identical) |
| **L9** — `ground.*` joins `vary:` vocabulary; signature bounds jitter | TECH-720 (grammar), TECH-719 (signature bounds), TECH-712 extension (bootstrap consumption, Stage 6.3 lineage) |
| **L10** — new primitive `iso_ground_noise`; palette gains `accent_dark` / `accent_light` | TECH-716 (palette keys), TECH-717 (primitive), TECH-718 (auto-insert), TECH-722 (doc) |

## 4. Dependency graph

```
TECH-714 (Stage 6.3 DAS addendum, already filed)
   ├── TECH-715 ──┐
   │             ├── TECH-718 ──┐
   │             │              ├── TECH-721
   ├── TECH-716 ──── TECH-717 ──┘ ┘
   │                              │
   │                              └── TECH-722
   └── TECH-719

TECH-710 (variants loader, Stage 6.3)
   └── TECH-720 ── TECH-721
```

## 5. Exit criteria

- [ ] All 8 tasks merged to `master`.
- [ ] `pytest tools/sprite-gen/tests/ -q` green.
- [ ] Legacy string-form specs render byte-identical to pre-Stage-6.4 baseline.
- [ ] DAS §4.1 documents accent keys + noise density guardrail + signature pointer.

## 6. Out of scope (Stage 6.5+)

- Curation-trained quality gate (Stage 6.5 / L11).
- Preset system (Stage 6.6 / L13).
- Animation schema reservation (Stage 6.7 / L16).

## 7. Cross-stage notes

- TECH-719 extends TECH-704 specifically — same JSON-shape contract.
- TECH-720 rides on TECH-710's range-object validator (reuse helper, don't duplicate).
- TECH-721 consolidates Stage 6.4 regression into one file — easier to reason about than per-lock test sprawl.
