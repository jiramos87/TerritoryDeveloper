### Stage 1 — Geometry MVP / Scaffolding + Primitive Renderer (Layer 1)


**Status:** Final (6 tasks archived as **TECH-123** through **TECH-128**; BACKLOG state: 6 archived / 6)

**Objectives:** Bootstrap `tools/sprite-gen/` folder structure and implement the two core primitives (`iso_cube`, `iso_prism`) with NW-light 3-level shade pass. Canvas sizing + Unity pivot math extracted to `canvas.py`. Unit tests validate pixel-perfect output against canonical canvas examples from the exploration doc.

**Exit:**

- `tools/sprite-gen/` layout matches §9 Folder layout: `src/`, `tests/`, `specs/`, `palettes/`, `out/` (.gitignored), `requirements.txt`, `slopes.yaml` stub
- `iso_cube(w, d, h, material)` renders top rhombus + south parallelogram + east parallelogram with correct 3-level ramp (bright/mid/dark per face)
- `iso_prism(w, d, h, pitch, axis, material)` renders sloped top-faces + triangular end-faces with same shade logic
- `canvas_size(fx, fy, extra_h)` returns `((fx+fy)*32, extra_h)` matching §4 Baseline formula
- `pivot_uv(canvas_h)` returns `(0.5, 16/canvas_h)` matching §4 Unity import defaults
- `pytest tools/sprite-gen/tests/` exits 0 — `test_canvas.py` + `test_primitives.py` pass with no errors. `npm run validate:all` does NOT yet cover Python; pytest stays a manual gate until CI integration lands (candidate fold-in point: Stage 1.3 palette tests, when test surface stabilizes)
- Phase 1 — Project bootstrap + canvas math module.
- Phase 2 — iso_cube + iso_prism primitives with NW-light shade pass.
- Phase 3 — Unit tests for canvas math + primitives.

**Tasks:**


| Task | Name                  | Issue        | Status          | Intent                                                                                                                                                                                                                                                                                                                                 |
| ---- | --------------------- | ------------ | --------------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| T1.1 | Folder scaffold       | **TECH-123** | Done            | Create `tools/sprite-gen/` folder skeleton: `src/__init__.py`, `src/primitives/__init__.py`, `tests/fixtures/` dir, `out/` dir (add to `.gitignore`), `requirements.txt` (pillow, numpy, scipy, pyyaml), `README.md` stub                                                                                                              |
| T1.2 | Canvas math module    | **TECH-124** | Done            | `src/canvas.py` — implement `canvas_size(fx, fy, extra_h=0) → (w, h)` using `(fx+fy)*32` width formula; `pivot_uv(canvas_h) → (0.5, 16/canvas_h)`; docstring cites §4 Canvas math from exploration doc                                                                                                                                 |
| T1.3 | iso_cube primitive    | **TECH-125** | Done            | `src/primitives/iso_cube.py` — `iso_cube(canvas, x0, y0, w, d, h, material)`: draw top rhombus (bright), south parallelogram (mid), east parallelogram (dark) using Pillow polygon fills; NW-light direction hardcoded; pixel coordinates computed from 2:1 isometric projection (tileWidth=1, tileHeight=0.5 per **Tile dimensions**) |
| T1.4 | iso_prism primitive   | **TECH-126** | Done            | `src/primitives/iso_prism.py` — `iso_prism(canvas, x0, y0, w, d, h, pitch, axis, material)`: two sloped top faces + two triangular end-faces; `axis ∈ {'ns','ew'}` selects ridge direction; same bright/mid/dark ramp as iso_cube                                                                                                      |
| T1.5 | Canvas unit tests     | **TECH-127** | Done (archived) | `tests/test_canvas.py` — assert `canvas_size(1,1)=(64,0)`, `canvas_size(1,1,32)=(64,32)`, `canvas_size(3,3,96)=(192,96)`; assert `pivot_uv(64)=(0.5,0.25)`, `pivot_uv(128)=(0.5,0.125)`, `pivot_uv(192)=(0.5, 16/192)` — matches §4 Examples table                                                                                     |
| T1.6 | Primitive smoke tests | **TECH-128** | Done (archived) | `tests/test_primitives.py` — render `iso_cube(w=1,d=1,h=32,material=STUB_RED)` on `canvas_size(1,1,32)=(64,32)` canvas; assert non-zero alpha per face bbox (top/south/east); same smoke for `iso_prism` both axes (pitch=0.5); save fixtures to `tests/fixtures/` tracked in git; re-export `iso_prism` from `primitives/__init__.py` |

#### §Stage File Plan

> Opus `stage-file-plan` writes structured `{operation, target_path, target_anchor, payload}` tuples here per pending Task. Sonnet `stage-file-apply` reads tuples and materializes BACKLOG rows + spec stubs. Contract: `ia/rules/plan-apply-pair-contract.md`.

_pending — populated by `/stage-file` planner pass._

#### §Plan Fix

> Opus `plan-review` writes targeted fix tuples here when a Stage's Task specs need tightening before first `/implement`. Sonnet `plan-applier` Mode plan-fix reads tuples and applies edits. Contract: `ia/rules/plan-apply-pair-contract.md`.

_pending — populated by `/plan-review` when fixes are needed._

#### §Stage Audit

> Opus `opus-audit` writes one `§Audit` paragraph per Task row here (Stage-scoped bulk, non-pair) once every Task reaches Done post-verify. Feeds `§Stage Closeout Plan` migration tuples downstream. Contract: `ia/rules/plan-apply-pair-contract.md` Stage-scoped non-pair row.

_retroactive-skip — Stage archived prior to 2026-04-24 lifecycle refactor that introduced the canonical `§Stage Audit` subsection (see `docs/MASTER-PLAN-STRUCTURE.md` §3.4 + Changelog entry 2026-04-24). Task-level §Audit prose captured in per-Task specs during Stage-scoped closeout before spec deletion; no retroactive re-run needed._

#### §Stage Closeout Plan

> Opus `stage-closeout-plan` writes unified tuple list here ONCE per Stage when all Task rows reach `Done` post-verify. Sonnet `stage-closeout-apply` reads tuples and applies verbatim. Contract: `ia/rules/plan-apply-pair-contract.md` seam #4.

_pending — populated by `/closeout {{this-doc}} Stage {{N.M}}` planner pass when all Tasks reach `Done`._

---
