# Sprite-gen Stage 6.5 — Curation-trained quality gate (aggregate plan)

> Parent plan: [ia/projects/sprite-gen-master-plan.md](../../ia/projects/sprite-gen-master-plan.md) (Stage 6.5 block)
> Source handoff: `/tmp/sprite-gen-improvement-session.md` §3 Stage 6.5 block (frozen 2026-04-23)
> Locks consumed: **L11** — curation feeds the signature aggregator, and the composer gates renders against the evolving envelope.

## 1. Scope

Stage 6.5 closes the feedback loop from artist curation back into the generator. `curate.py` gains `log-promote` / `log-reject --reason` subcommands (verb names disambiguate from existing `promote` PNG→Unity ship + `reject` glob-delete — TECH-179); the signature extractor becomes a three-source aggregator (`catalog ∪ promoted − rejected-zones`); the composer adds a score-and-retry gate with an N-retry cap and a `.needs_review` sidecar on exhaustion. Tests and DAS §5 close the contract.

## 2. Tasks

| Task key | Issue | Title | Priority | Depends on |
|----------|-------|-------|----------|-----------|
| T6.5.1 | [TECH-723](../../ia/projects/TECH-723.md) | `curate.py log-promote` → `promoted.jsonl` | high | TECH-704..708 |
| T6.5.2 | [TECH-724](../../ia/projects/TECH-724.md) | `curate.py log-reject --reason` → `rejected.jsonl` | high | TECH-723 |
| T6.5.3 | [TECH-725](../../ia/projects/TECH-725.md) | Signature three-source aggregator | high | TECH-723, TECH-724 |
| T6.5.4 | [TECH-726](../../ia/projects/TECH-726.md) | Composer render-time score-and-retry gate | high | TECH-725 |
| T6.5.5 | [TECH-727](../../ia/projects/TECH-727.md) | `.needs_review` sidecar on floor-miss | medium | TECH-726 |
| T6.5.6 | [TECH-728](../../ia/projects/TECH-728.md) | Tests — `test_curation_loop.py` | high | TECH-726, TECH-727 |
| T6.5.7 | [TECH-729](../../ia/projects/TECH-729.md) | DAS §5 addendum — curation loop + floor + sidecar | medium | TECH-723..727 |

## 3. Lock → task map

| Lock | Task(s) |
|------|---------|
| **L11** — curation → envelope → gate | TECH-723/724 (curate I/O), TECH-725 (aggregator), TECH-726 (gate), TECH-727 (sidecar), TECH-728 (regression), TECH-729 (doc) |

## 4. Dependency graph

```
Stage 6.2 (TECH-704..708, already filed)
   └── TECH-723 ──┐
                  ├── TECH-725 ── TECH-726 ── TECH-727
   └── TECH-724 ──┘                    │           │
                                       └── TECH-728 ┘
                                                    │
   all code tasks ───────────────────────── TECH-729
```

## 5. Exit criteria

- [ ] All 7 tasks merged to `master`.
- [ ] `pytest tools/sprite-gen/tests/ -q` green.
- [ ] Feature-flag off (no envelope) → byte-identical to pre-Stage-6.5 baseline.
- [ ] DAS §5 documents full loop: JSONL schema + reason map + gate + sidecar.

## 6. Out of scope (Stage 6.6+)

- Preset system (Stage 6.6 / L13).
- Animation schema reservation (Stage 6.7 / L16).
- Curator UI / CI integration consuming `.needs_review` sidecars (future).

## 7. Cross-stage notes

- TECH-725 reuses `REJECTION_REASONS` from TECH-724 as the canonical vocab — avoid redeclaring.
- TECH-726 seed advancement formula is non-obvious; locked in TECH-726's Decision Log for future contributors.
- TECH-728 tests live alongside Stage 6.4's `test_ground_variation.py` file — consolidated regression net per stage is the repo convention.
