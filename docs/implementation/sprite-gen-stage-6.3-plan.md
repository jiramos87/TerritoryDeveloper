# sprite-gen — Stage 6.3 Plan Digest

Compiled 2026-04-23 from 6 task spec(s): **TECH-709** .. **TECH-714**.

**Master plan:** `ia/projects/sprite-gen-master-plan.md` — Stage 6.3 — Placement + variant randomness + split seeds.

**Status:** Draft. Specs: `ia/projects/TECH-709.md` .. `ia/projects/TECH-714.md`; yaml: `ia/backlog/TECH-709.yaml` .. `ia/backlog/TECH-714.yaml`.

---

## Stage exit criteria (orchestrator)

- Spec schema accepts `building.footprint_px`, `padding`, `align`; `variants: {count, vary, seed_scope}` block (with legacy scalar back-compat); top-level `palette_seed` + `geometry_seed` (with legacy `seed` fan-out).
- Composer honours placement + variants via pure `resolve_building_box` helper + split-seed variant loop; legacy specs render byte-identical.
- CLI `python3 -m src bootstrap-variants <stem> --from-signature` seeds `vary:` defaults from class signature; opt-in only.
- Three new test files lock placement combinatorics, variant determinism, split-seed independence.
- DAS R11 addendum documents new surface.
- `pytest tools/sprite-gen/tests/` exits 0.

---

## §Plan Digest — TECH-709 (excerpt)

### §Goal

Add pixel-exact placement fields (`building.footprint_px`, `padding`, `align`) to the spec loader with sensible defaults + enum validation, preserving byte-identical render output for existing specs. Consumes L5.

### §Mechanical Steps (summary)

1. `_normalize_building_placement` helper in `spec.py`.
2. Conflict warning on `footprint_px` + `footprint_ratio` both present.
3. Unit tests for defaults, explicit values, enum validation.

---

## §Plan Digest — TECH-710 (excerpt)

### §Goal

Extend `spec.py` to accept `variants: {count, vary, seed_scope}` with scalar `variants: N` legacy back-compat and `palette_seed` + `geometry_seed` with scalar `seed: N` fan-out. `seed_scope` default `palette` preserves legacy behaviour. Consumes L6, L14.

### §Mechanical Steps (summary)

1. `_normalize_variants` (int or dict → object form).
2. `_normalize_seeds` (legacy scalar → split seeds).
3. Enum validation on `seed_scope`.

---

## §Plan Digest — TECH-711 (excerpt)

### §Goal

Ship pure `resolve_building_box(spec) -> (bx, by, offset_x, offset_y)` + wire composer variant loop to use split seeds (`palette_seed + i`, `geometry_seed + i`). Legacy specs render byte-identical.

### §Mechanical Steps (summary)

1. Author helper + unit tests.
2. Wire composer to use helper (replace hand-rolled centering).
3. Variant loop with split-seed samplers.
4. Full-suite byte-identical regression.

---

## §Plan Digest — TECH-712 (excerpt)

### §Goal

Ship `python3 -m src bootstrap-variants <stem> --from-signature` — reads class signature, derives sensible `vary:` defaults, merges into spec (author keys win). Opt-in; never runs during render. Consumes L7.

### §Mechanical Steps (summary)

1. Wire subparser + argparse.
2. Derivation rules (signature → `vary.*` axes).
3. Deep merge preserving author keys.
4. Missing-signature error path.

---

## §Plan Digest — TECH-713 (excerpt)

### §Goal

Three new test files lock placement combinatorics, variant determinism, split-seed independence so Stage 6.3 surface changes can't drift.

### §Mechanical Steps (summary)

1. `test_building_placement.py` — 4 aligns × 3 padding profiles matrix.
2. `test_variants_geometric.py` — 4 pairwise-distinct + reproducible.
3. `test_split_seeds.py` — palette-freeze + geometry-freeze independence.

---

## §Plan Digest — TECH-714 (excerpt)

### §Goal

Append DAS §R11.1 — documents Stage 6.3 surface additions (placement fields, split seeds with legacy fan-out, `vary:` grammar).

### §Mechanical Steps (summary)

1. Locate R11 in `docs/sprite-gen-art-design-system.md`.
2. Append §R11.1 with placement + split seeds + vary grammar.
3. Grep checks pass.

---

## Dependency graph

- TECH-709 — depends on Stage 6.2 (TECH-704..708).
- TECH-710 — depends on Stage 6.2 (TECH-704..708).
- TECH-711 — depends on TECH-709 + TECH-710 (schema must exist).
- TECH-712 — depends on TECH-710 (variants loader must exist); reads signatures from TECH-705.
- TECH-713 — depends on TECH-711 + TECH-712 (helper + CLI must exist).
- TECH-714 — depends on TECH-709 + TECH-710 (doc references fields).

## Locks consumed

- **L5** — Spec gains `building.footprint_px`, `building.padding`, `building.align`. **Consumed by:** TECH-709.
- **L6** — `variants:` becomes block `{count, vary, seed_scope}`; legacy scalar back-compat. **Consumed by:** TECH-710.
- **L7** — `bootstrap-variants --from-signature` CLI; never auto-rewrites. **Consumed by:** TECH-712.
- **L14** — Split seeds `palette_seed` + `geometry_seed`. **Consumed by:** TECH-710 (loader) + TECH-711 (composer).
