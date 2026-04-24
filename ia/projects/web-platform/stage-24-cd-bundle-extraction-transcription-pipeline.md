### Stage 24 — Visual design layer / CD bundle extraction + transcription pipeline


**Status:** Done — TECH-630…TECH-633 shipped 2026-04-22

**Objectives:** Build the ingestion pipeline for the CD pilot bundle at `web/design-refs/step-8-console/`. Author a token extractor that reads `ds/colors_and_type.css` + bundle `palette.json` and emits a canonical token map with a drift report against the locked `web/lib/tokens/palette.json` raws (halt-on-mismatch). Author a transcriber that renames CD `--raw-*/--text-*/--dur-*` to master-plan `--ds-*` namespace (per D2 prefix + D1 motion naming reconciliation) and emits additions to `web/app/globals.css` `@theme` block + `web/lib/design-tokens.ts` TS const tree. Transcribe bundle `HANDOFF.md` into a `design-system.md` appendix for spec alignment. Pipeline runs as checked-in scripts under `tools/scripts/`; re-runnable when CD pilot re-issues.

**Exit:**

- `tools/scripts/extract-cd-tokens.ts`: reads CD bundle CSS + palette; emits canonical token map JSON; emits drift-report Markdown; halt-on-mismatch exit code if locked raws differ from `web/lib/tokens/palette.json`.
- `tools/scripts/transcribe-cd-tokens.ts`: consumes canonical map; renames per D1 + D2 locks resolved at implementation time; emits `--ds-*` additions to `web/app/globals.css` `@theme` block + TS const additions to `web/lib/design-tokens.ts`; refuses to emit when drift report non-empty.
- `web/design-refs/step-8-console/.drift-report.md`: generated artifact; PR-body ready; zero-row report on clean pass (B-CD1 / Example 1).
- `web/lib/design-system.md` §7 appendix: full transcription of bundle `HANDOFF.md`; cites `web/design-refs/step-8-console/HANDOFF.md` as source; Dribbble + Shopify refs preserved (R1 / R20 path).
- `npm run validate:web` green.
- Phase 1 — Extractor + drift report (`extract-cd-tokens.ts` + drift Markdown emitter + tests).
- Phase 2 — Transcriber + HANDOFF transcription (`transcribe-cd-tokens.ts` + `design-system.md` §7 appendix).

**Tasks:**

| Task | Issue | Status | Intent |
| --- | --- | --- | --- |
| T24.1 | **TECH-630** | Done | Author `tools/scripts/extract-cd-tokens.ts` — reads `web/design-refs/step-8-console/ds/colors_and_type.css` + `web/design-refs/step-8-console/ds/palette.json`; parses CSS custom properties under `:root`; emits canonical token map shape `{ raws: {...}, semantic: {...}, motion: {...}, typeScale: {...}, spacing: {...} }` as JSON to stdout OR to `--out` arg path; tsx-runnable via `npx tsx`; zero runtime deps outside node built-ins; JSDoc cites B-CD1 (drift-on-mutation guard). |
| T24.2 | **TECH-631** | Done | Author drift-report emitter as second pass in `extract-cd-tokens.ts` — diffs canonical map raws against `web/lib/tokens/palette.json` raws; emits `web/design-refs/step-8-console/.drift-report.md` Markdown table (columns: Key, CD value, palette.json value, Match?); exit code 0 on zero drift, exit 1 on any mismatch (CI-friendly). Author `tools/scripts/__tests__/extract-cd-tokens.test.ts` — snapshot test on known-clean bundle + fabricated-mismatch fixture. |
| T24.3 | **TECH-632** | Done | Author `tools/scripts/transcribe-cd-tokens.ts` — consumes canonical map JSON via stdin or `--in` arg; applies D1 motion rename (`--dur-fast` → `--ds-duration-instant` etc. — exact mapping TBV at P0 decision resolution) + D2 prefix rename (`--raw-*` → `--ds-*`, `--text-*` → `--ds-text-*`, `--dur-*` → `--ds-duration-*`); emits two output blocks: (a) CSS fragment appended to `web/app/globals.css` `@theme` inside marker comments `/* CD-BUNDLE-START */` ... `/* CD-BUNDLE-END */` (idempotent re-run replaces block between markers), (b) TS fragment for `web/lib/design-tokens.ts` under `export const cdBundle = { ... } as const`; refuses to write when `.drift-report.md` non-empty. |
| T24.4 | **TECH-633** | Done | Transcribe `web/design-refs/step-8-console/HANDOFF.md` contents into `web/lib/design-system.md` new `## §7 — CD Pilot Bundle appendix` subsection; preserve section structure (type scale notes, spacing scale, motion vocab decisions, primitive list); cite extensions doc `## CD Pilot Bundle — 2026-04-18` as canonical narrative source; cite Dribbble breadcrumb + Shopify dev docs refs from extensions doc §8 (NB5); add banner "Generated from `tools/scripts/transcribe-cd-tokens.ts`; re-run script if bundle re-issues". `npm run validate:web` green. |

#### §Stage File Plan

<!-- stage-file-plan output — do not hand-edit; apply via stage-file-apply -->

```yaml
- reserved_id: "TECH-630"
  title: |
    Author `tools/scripts/extract-cd-tokens.ts` — reads `web/design-refs/step-8-console/ds/colors_and_type.css` + `web/design-refs/step-8-console/ds/palette.json`; parses CSS custom properties under `:root`; emits canonical token map shape `{ raws: {...}, semantic: {...}, motion: {...}, typeScale: {...}, spacing: {...} }` as JSON to stdout OR to `--out` arg path; tsx-runnable via `npx tsx`; zero runtime deps outside node built-ins; JSDoc cites B-CD1 (drift-on-mutation guard).
  priority: medium
  notes: |
    Ingests read-only CD paths; first pipeline stage. Emits only canonical JSON — no drift diff in this task (TECH-631).
  depends_on: []
  related:
    - "TECH-631"
    - "TECH-632"
    - "TECH-633"
  stub_body:
    summary: |
      New tsx entry under `tools/scripts/`; canonical map JSON for downstream drift + transcriber; B-CD1 in JSDoc.
    goals: |
      1. Map shape matches Stage 24 T24.1 Intent.
      2. stdout or `--out`; Node built-ins only.
      3. Runnable via `npx tsx tools/scripts/extract-cd-tokens.ts`.
    systems_map: |
      - `tools/scripts/extract-cd-tokens.ts` (new)
      - `web/design-refs/step-8-console/ds/colors_and_type.css` (read)
      - `web/design-refs/step-8-console/ds/palette.json` (read)
    impl_plan_sketch: |
      ### Phase 1 — Extractor
      - [ ] Author script + JSDoc; smoke run emits JSON.
- reserved_id: "TECH-631"
  title: |
    Author drift-report emitter as second pass in `extract-cd-tokens.ts` — diffs canonical map raws against `web/lib/tokens/palette.json` raws; emits `web/design-refs/step-8-console/.drift-report.md` Markdown table (columns: Key, CD value, palette.json value, Match?); exit code 0 on zero drift, exit 1 on any mismatch (CI-friendly). Author `tools/scripts/__tests__/extract-cd-tokens.test.ts` — snapshot test on known-clean bundle + fabricated-mismatch fixture.
  priority: medium
  notes: |
    CI gate: exit 1 on raws skew. Tests colocated under `tools/scripts/__tests__/`; align with repo Jest/Vitest choice.
  depends_on: []
  related:
    - "TECH-630"
    - "TECH-632"
    - "TECH-633"
  stub_body:
    summary: |
      Drift table + process exit code + unit tests; depends on canonical map from TECH-630.
    goals: |
      1. `.drift-report.md` table format for PRs.
      2. exit 0 / 1 contract.
      3. clean + mismatch test coverage.
    systems_map: |
      - `tools/scripts/extract-cd-tokens.ts` (edit)
      - `web/lib/tokens/palette.json` (read)
      - `web/design-refs/step-8-console/.drift-report.md` (write)
    impl_plan_sketch: |
      ### Phase 1 — Drift + tests
      - [ ] Drift pass + `extract-cd-tokens.test.ts`.
- reserved_id: "TECH-632"
  title: |
    Author `tools/scripts/transcribe-cd-tokens.ts` — consumes canonical map JSON via stdin or `--in` arg; applies D1 motion rename + D2 prefix rename; emits CSS fragment in `web/app/globals.css` `@theme` between `/* CD-BUNDLE-START */` … `/* CD-BUNDLE-END */` and `export const cdBundle` in `web/lib/design-tokens.ts`; refuses to write when `.drift-report.md` non-empty.
  priority: medium
  notes: |
    D1/D2 exact map locked at P0 in spec Decision Log. Halt if drift report has rows.
  depends_on: []
  related:
    - "TECH-630"
    - "TECH-631"
    - "TECH-633"
  stub_body:
    summary: |
      Transcriber: canonical map → `--ds-*` in globals + `cdBundle` TS; idempotent marker block.
    goals: |
      1. globals.css + design-tokens.ts updated without touching CD tree.
      2. No write when drift non-empty.
      3. `validate:web` green.
    systems_map: |
      - `tools/scripts/transcribe-cd-tokens.ts` (new)
      - `web/app/globals.css`
      - `web/lib/design-tokens.ts`
    impl_plan_sketch: |
      ### Phase 1 — Transcriber
      - [ ] Author script; run `npm run validate:web`.
- reserved_id: "TECH-633"
  title: |
    Transcribe `web/design-refs/step-8-console/HANDOFF.md` into `web/lib/design-system.md` new `## §7 — CD Pilot Bundle appendix` subsection; preserve section structure; cite extensions doc + NB5; add regen banner; `npm run validate:web` green.
  priority: medium
  notes: |
    Doc-only; caveman-exception for any user-facing appendix strings. Cites HANDOFF + extensions + Dribbble/Shopify.
  depends_on: []
  related:
    - "TECH-630"
    - "TECH-631"
    - "TECH-632"
  stub_body:
    summary: |
      Durable §7 in `design-system.md` with full HANDOFF transcription + source citations.
    goals: |
      1. §7 structure matches HANDOFF sections.
      2. Extensions `## CD Pilot Bundle — 2026-04-18` + NB5 refs.
      3. validate:web green.
    systems_map: |
      - `web/lib/design-system.md` (edit)
      - `web/design-refs/step-8-console/HANDOFF.md` (read)
    impl_plan_sketch: |
      ### Phase 1 — §7 appendix
      - [ ] Author markdown section + banner; run `npm run validate:web`.
```

#### §Plan Fix — PASS (no drift)

> plan-review exit 0 — all Task specs aligned. No tuples emitted. Downstream pipeline continue.

#### §Stage Audit

> Opus `opus-audit` writes one `§Audit` paragraph per Task row here (Stage-scoped bulk, non-pair) once every Task reaches Done post-verify. Feeds `§Stage Closeout Plan` migration tuples downstream. Contract: `ia/rules/plan-apply-pair-contract.md` Stage-scoped non-pair row.

_retroactive-skip — Stage archived prior to 2026-04-24 lifecycle refactor that introduced the canonical `§Stage Audit` subsection (see `ia/projects/MASTER-PLAN-STRUCTURE.md` §3.4 + Changelog entry 2026-04-24). Task-level §Audit prose captured in per-Task specs during Stage-scoped closeout before spec deletion; no retroactive re-run needed._

#### §Stage Closeout Plan

> Opus `stage-closeout-plan` writes unified tuple list here ONCE per Stage when all Task rows reach `Done` post-verify. Sonnet `stage-closeout-apply` reads tuples and applies verbatim. Contract: `ia/rules/plan-apply-pair-contract.md` seam #4.

_pending — populated by `/closeout {{this-doc}} Stage {{N.M}}` planner pass when all Tasks reach `Done`._

---
