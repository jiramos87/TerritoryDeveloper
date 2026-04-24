### Stage 2 — Scaffold + design system foundation / Design system foundation


**Status:** Done — tokens + Tailwind wiring task + DataTable/BadgeChip + StatBar/FilterChips + HeatmapCell + AnnotatedMap + `/design` review route + README §Tokens all archived (see BACKLOG-ARCHIVE.md). Stage closed 2026-04-14.

**Objectives:** Land the token layer (NYT dark palette, type scale, spacing) and the six core primitives (`DataTable`, `StatBar`, `BadgeChip`, `FilterChips`, `HeatmapCell`, `AnnotatedMap`) that every downstream public page / wiki / dashboard will consume. Export the palette as JSON under `web/lib/tokens/palette.json` so future Unity UI/UX master plan can consume the same design language (per Q15 cross-cutting note). Ship a `/design` route as a live visual-review surface covering all primitives against fixture data.

**Exit:**

- `web/lib/tokens/` exports `palette.json`, `type-scale.json`, `spacing.json`; Tailwind config (`web/tailwind.config.ts`) imports these as its color / spacing / font-family source of truth.
- `web/components/DataTable.tsx`, `StatBar.tsx`, `BadgeChip.tsx`, `FilterChips.tsx`, `HeatmapCell.tsx`, `AnnotatedMap.tsx` each render against fixture props and have unit-style snapshot smoke (render + assert no throw, if test infra lands at this stage; else manual visual verify at `/design` route).
- `web/app/design/page.tsx` renders every primitive in isolation with 2–3 fixture variants; served under `/design` on dev + deploy; dashboard access gate (Q14 obscure-URL) NOT applied yet since `/design` is internal review only (document that follow-up in Step 3 surfaces).
- `web/README.md` §Tokens documents the JSON export contract (keys, semantic naming, consumption pattern for Unity UI/UX follow-up plan).
- New glossary row candidate: "Web design token set" — canonical name for the palette + type-scale + spacing bundle. Deferred to glossary authoring in Stage 1.2 close (add once tokens stabilize).
- Phase 1 — Token layer (palette + type + spacing JSON + Tailwind wiring).
- Phase 2 — Data-dense primitives (the six components).
- Phase 3 — Review surface (`/design` route + docs).

**Tasks:**

| Task | Issue | Status | Intent |
| --- | --- | --- | --- |
| T2.1 | _(archived)_ | Done (archived) | Tokens + Tailwind wiring — see BACKLOG-ARCHIVE.md 2026-04-14. Authored `web/lib/tokens/{palette,type-scale,spacing}.json` + semantic aliases; wired via Tailwind v4 `@theme` CSS custom properties in `globals.css`. |
| T2.2 | _(archived)_ | Done (archived) | _(merged into T1.2.1 — see archive)_ |
| T2.3 | _(archived)_ | Done (archived) | DataTable + BadgeChip SSR-only primitives — see BACKLOG-ARCHIVE.md 2026-04-14. Authored `web/components/DataTable.tsx` (typed generic `<T,>` + `Column<T>` + `statusCell` slot + `aria-sort`-only sortable indicator) + `BadgeChip.tsx` (4-status enum → `bg-status-*` + `text-status-*-fg` semantic aliases). Phase 1 extended palette JSON + `@theme` w/ new `raw.green` + 8 status aliases before component authoring. |
| T2.4 | _(archived)_ | Done (archived) | StatBar + FilterChips SSR-only primitives — see BACKLOG-ARCHIVE.md 2026-04-14. Authored `web/components/StatBar.tsx` (`TIER_FILL` dispatch → semantic `bg-[var(--color-text-accent-warn\ | critical)]` arbitrary utilities; raw-value tier resolution; `pct` clamp guards `max ≤ 0`) + `FilterChips.tsx` (`chips[]` row, no `onClick`, `active` → `bg-panel`/`text-primary`). No new `bg-accent-*` palette aliases added (deferred until ≥2 bar-style consumers). |
| T2.5 | _(archived)_ | Done (archived) | HeatmapCell + AnnotatedMap SSR-only primitives — see BACKLOG-ARCHIVE.md 2026-04-14. Authored `web/components/HeatmapCell.tsx` (5-bucket `color-mix()` ramp anchored on existing semantic aliases) + `AnnotatedMap.tsx` (SVG wrapper w/ `regions` + `annotations` props; NYT-style spaced-caps geo labels via `letterSpacing: 0.15em` + `textTransform: uppercase`). Both SSR-only. |
| T2.6 | _(archived)_ | Done (archived) | `/design` review route + README §Tokens — see BACKLOG-ARCHIVE.md 2026-04-14. Authored `web/app/design/page.tsx` SSR-only rendering all six primitives w/ 2–3 fixture variants each; inline fixtures at module scope; internal-review banner (caveman prose, internal-facing). `web/README.md` §Tokens documents palette JSON file layout + `{raw.<key>}` indirection via `resolveAlias` in `web/lib/tokens/index.ts` + Unity UI/UX consumption stub. Glossary row "Web design token set" deferred per Exit bullet 5. |

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
