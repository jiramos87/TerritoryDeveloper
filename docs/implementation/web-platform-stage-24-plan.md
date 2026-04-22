# web-platform — Stage 24 Plan Digest

Compiled 2026-04-22 from 4 task spec(s): TECH-630, TECH-631, TECH-632, TECH-633.

Source orchestrator: `ia/projects/web-platform-master-plan.md` — Stage 24.

---

## §Plan Digest — TECH-630

### §Goal

Add `extract-cd-tokens.ts` so the CD pilot CSS + palette can be machine-read into one canonical JSON map for drift checks and transcription (Stage 24 pipeline entry).

### §Acceptance

- [ ] Script reads `web/design-refs/step-8-console/ds/colors_and_type.css` + `ds/palette.json`.
- [ ] Emits JSON map with `raws`, `semantic`, `motion`, `typeScale`, `spacing` keys per master-plan T24.1.
- [ ] Output to stdout or `--out`; executable via `npx tsx tools/scripts/extract-cd-tokens.ts`.
- [ ] JSDoc references B-CD1; Node built-ins only at runtime.

### §Test Blueprint

| test_name | inputs | expected | harness |
|-----------|--------|----------|---------|
| tsx_smoke | repo | JSON on stdout or file at `--out` | manual / T24.2 tests |

### §Examples

| Input | Notes |
|-------|--------|
| Default paths under `web/design-refs/step-8-console/ds/` | Override only if CLI adds path args in later revision |

### §Mechanical Steps

#### Step 1 — Add extractor module

**Goal:** Create `tools/scripts/extract-cd-tokens.ts` that loads CD paths, parses `:root` vars, builds the five-bucket map, writes JSON.

**Edits:** `tools/scripts/extract-cd-tokens.ts` — new file (parse + `JSON.stringify` + stdout or `--out`).

**Gate:**

```bash
npx tsx tools/scripts/extract-cd-tokens.ts 2>/dev/null | head -c 200
```

**STOP:** If parse throws, fix CSS/palette path or parser; re-run Gate.

**MCP hints:** `backlog_issue` `{"id": "TECH-630"}`

---

## §Plan Digest — TECH-631

### §Goal

Gate CD raws against locked `palette.json` with a check-in drift report and tests so CI blocks silent palette skew before `--ds-*` transcription.

### §Acceptance

- [ ] Drift table emitted at `web/design-refs/step-8-console/.drift-report.md`.
- [ ] Process exits 0 when no raws differ; 1 on any mismatch.
- [ ] `tools/scripts/__tests__/extract-cd-tokens.test.ts` present with clean + mismatch cases.

### §Test Blueprint

| test_name | inputs | expected | harness |
|-----------|--------|----------|---------|
| drift_clean | bundle + palette aligned | exit 0, zero drift rows | spawn tsx or import |
| drift_mismatch | fixture | exit 1, non-empty table | same |

### §Mechanical Steps

#### Step 1 — Drift pass

**Goal:** Add diff + markdown emit + exit code to `extract-cd-tokens.ts`.

**Edits:** extend `tools/scripts/extract-cd-tokens.ts` after canonical map build.

**Gate:**

```bash
npx tsx tools/scripts/extract-cd-tokens.ts; echo $?
```

**STOP:** If exit always 0 on mismatch, fix diff loop; re-gate.

#### Step 2 — Tests

**Goal:** Add `tools/scripts/__tests__/extract-cd-tokens.test.ts`.

**Gate:** project test runner (see root / `web/package.json` for script).

**STOP:** On fail, fix fixture paths.

---

## §Plan Digest — TECH-632

### §Goal

Land the CD → `--ds-*` transcription in `globals.css` + `design-tokens.ts` with drift halt so production tokens only update from clean drift reports.

### §Acceptance

- [ ] Script consumes canonical map via stdin or `--in`.
- [ ] Marker block in `web/app/globals.css` updated; `cdBundle` in `web/lib/design-tokens.ts`.
- [ ] Exits without writing if drift report non-empty.
- [ ] `npm run validate:web` exit 0.

### §Mechanical Steps

#### Step 1 — Add transcriber

**Goal:** Create `tools/scripts/transcribe-cd-tokens.ts` + edit `globals.css` + `design-tokens.ts`.

**Gate:**

```bash
npm run validate:web
```

**STOP:** If TS errors on `design-tokens.ts`, fix exports/types; re-gate.

---

## §Plan Digest — TECH-633

### §Goal

Publish CD `HANDOFF.md` as a durable `design-system.md` §7 appendix so implementers and reviewers share one spec-linked narrative (R1 / R20 path).

### §Acceptance

- [ ] New §7 section; structure mirrors HANDOFF (type, spacing, motion, primitives) per T24.4.
- [ ] Source citations: HANDOFF, extensions `## CD Pilot Bundle — 2026-04-18`, Dribbble + Shopify (NB5).
- [ ] Banner text ties maintenance to `tools/scripts/transcribe-cd-tokens.ts` (or re-run when bundle re-issues).
- [ ] `npm run validate:web` green.

### §Mechanical Steps

#### Step 1 — Author §7

**Goal:** Edit `web/lib/design-system.md` only (no bundle edits).

**Gate:**

```bash
npm run validate:web
```

**STOP:** If MDX/ESLint on docs path fails, fix heading hierarchy; re-gate.
