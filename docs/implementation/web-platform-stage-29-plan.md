# web-platform — Stage 29 Plan Digest

Compiled 2026-04-22 from 4 task spec(s).

---

## §Plan Digest

### §Goal

Add `## Design System` to `web/README.md` so the design spec path, three primitives, dev-only showcase, and `ds-*` / `globals.css` contract are discoverable from the first developer README screen.

### §Acceptance

- [ ] `## Design System` exists with: `web/lib/design-system.md` citation; one line each for `Heading`, `Prose`, `Surface`; `web/app/(dev)/design-system/page.tsx` dev-only note; `ds-*` in `web/app/globals.css` (not `tailwind.config.ts`).
- [ ] Section placed after `## Overview`, before `## Claude Code — Vercel plugin` (or equivalent following heading).

### §Test Blueprint

| test_name | inputs | expected | harness |
|-----------|--------|----------|---------|
| readme_design_system | edited `web/README.md` | new `## Design System` block | manual |
| validate_web | repo after TECH-680+681 | `npm run validate:web` exit 0 | node (TECH-682) |

### §Examples

| Item | Text shape |
|------|------------|
| Spec | `` `web/lib/design-system.md` — authority for type / spacing / motion / aliases `` |
| Primitives | `` `Heading` (level-bound RSC type); `Prose` (MDX body rhythm); `Surface` (tone / padding / motion) `` |
| ds-* | `` `--ds-*` in `web/app/globals.css` — Tailwind v4 `@theme` / CSS vars; do not add tokens to `tailwind.config.ts` `` |

### §Mechanical Steps

#### Step 1 — Insert `## Design System` after Overview

**Goal:** New section in `web/README.md` without disturbing neighbor headings.

**Edits:**

- `web/README.md` — **before**:
  ```
  - **MCP server** (`territory-ia`): stdio dev-only — NOT consumed by this web app.

  ## Claude Code — Vercel plugin
  ```
  **after**:
  ```
  - **MCP server** (`territory-ia`): stdio dev-only — NOT consumed by this web app.

  ## Design System

  - **Spec:** `web/lib/design-system.md` — type scale, spacing, motion, semantic aliases, component bindings.
  - **Primitives:** `Heading` (level-bound RSC typography) · `Prose` (MDX / article vertical rhythm) · `Surface` (tone, padding, motion panel).
  - **Showcase (dev only, unlisted):** `web/app/(dev)/design-system/page.tsx` — local reference for `ds-*` and console chrome; not linked from production marketing routes.
  - **Tokens:** use `ds-*` classes backed by `--ds-*` in `web/app/globals.css` (Tailwind v4). Do not add new theme keys to a legacy `tailwind.config.ts` for these surfaces.

  ## Claude Code — Vercel plugin
  ```

**Gate:**

```bash
cd /Users/javier/bacayo-studio/territory-developer && npm run validate:web
```

**STOP:** If the gate fails on unrelated drift, hand off to **TECH-682** (validate) scope; do not revert README content until failure cause is identified.

**MCP hints:** `backlog_issue` (TECH-680), `plan_digest_resolve_anchor` (confirm single hit on `## Claude Code` anchor).

---
## §Plan Digest

### §Goal

Add **§6** to root `CLAUDE.md`: authoritative row for `web/lib/design-system.md` and its link to `web/lib/design-tokens.ts` + `web/app/globals.css` `ds-*`, plus the page-body English carve-out for `web/app/**/page.tsx`.

### §Acceptance

- [ ] New `## 6. Web design spec` (or equivalent) at end of `CLAUDE.md` contains a table row for `web/lib/design-system.md` and names derivative token files.
- [ ] Carve-out line references `ia/rules/agent-output-caveman.md` §exceptions for `web/app/**/page.tsx`.

### §Test Blueprint

| test_name | inputs | expected | harness |
|-----------|--------|----------|---------|
| claude_imports | edited `CLAUDE.md` | `npm run validate:claude-imports` exit 0 | node |
| claude_cache | edited `CLAUDE.md` | `npm run validate:cache-block-sizing` exit 0 | node |

### §Examples

| File | Role in row |
|------|-------------|
| `web/lib/design-system.md` | Spec: type, spacing, motion, aliases |
| `web/lib/design-tokens.ts` | Generated / shared TS token surface |
| `web/app/globals.css` | `@theme` + `--ds-*` for Tailwind v4 |

### §Mechanical Steps

#### Step 1 — Append `## 6. Web design spec` to `CLAUDE.md`

**Goal:** Single block at end of `CLAUDE.md` without changing `@`-import preamble.

**Edits:**

- `CLAUDE.md` — **before**:
  ```
  Further commands (`validate:frontmatter`, `validate:cache-block-sizing`, `unity:testmode-batch`, `db:bridge-preflight`) live in `docs/agent-led-verification-policy.md` + relevant skill bodies.
  ```
  **after**:
  ```
  Further commands (`validate:frontmatter`, `validate:cache-block-sizing`, `unity:testmode-batch`, `db:bridge-preflight`) live in `docs/agent-led-verification-policy.md` + relevant skill bodies.

  ## 6. Web design spec (authoritative)

  | File | Role |
  |---|---|
  | `web/lib/design-system.md` | Type, spacing, motion, alias tables — canonical spec for the web design layer |
  | `web/lib/design-tokens.ts` + `web/app/globals.css` (`@theme`, `ds-*`) | Derived token surfaces; keep new `ds-*` in CSS, not a legacy `tailwind.config.ts` |

  **Page copy:** user-facing body strings in `web/app/**/page.tsx` stay full English; app shell, identifiers, and IA prose stay caveman — `ia/rules/agent-output-caveman.md` exceptions.
  ```

**Gate:**

```bash
cd /Users/javier/bacayo-studio/territory-developer && npm run validate:claude-imports && npm run validate:cache-block-sizing
```

**STOP:** If `validate:claude-imports` fails, shorten lines in the new table or remove bare `@` that trip the line budget; do not remove the `@` preamble block.

**MCP hints:** `backlog_issue` (TECH-681), `rule_content` (`agent-output-caveman`).

---
## §Plan Digest

### §Goal

Run root `npm run validate:web` and fix all lint, typecheck, unit test, and `next build` failures so Stage 29 unblocks **TECH-683**.

### §Acceptance

- [ ] `npm run validate:web` exits 0 from repo root.
- [ ] PR body lists the command, exit code, and files changed while fixing.

### §Test Blueprint

| test_name | inputs | expected | harness |
|-----------|--------|----------|---------|
| validate_web | branch with Stage 29 docs + web code | exit 0 | `npm run validate:web` |
| regen_progress | after green | optional | `npm run progress` (non-blocking) |

### §Examples

| Script | What it runs |
|--------|----------------|
| `validate:web` | `npm --prefix web` lint + tsc + vitest + `next build` |

### §Mechanical Steps

#### Step 1 — Run `validate:web` and fix to green

**Goal:** Full web CI-equivalent pass from monorepo root.

**Edits:** (per failure — not knowable a priori; track in PR.)

- `web/**/*.{ts,tsx}` — **before** / **after**: resolve the first reported ESLint or `tsc` error, then re-run the gate.
- `web/package.json` / `package.json` — only when the failure is a script or workspace wiring defect.

**Gate:**

```bash
cd /Users/javier/bacayo-studio/territory-developer && npm run validate:web
```

**STOP:** If the failure is outside `web/` and `package.json` wiring, file a new **TECH-** and park Stage 29 — do not hand-edit `ia/state/id-counter.json`.

**MCP hints:** `backlog_issue` (TECH-682), `project_spec_journal` (only when spec asks for a journal entry).

---
## §Plan Digest

### §Goal

Run Lighthouse on local `/` (port **4000**), compare to Stage 8.6 T27.7 baselines, and only then touch `Surface` `motion` on any `Surface` under landing + dashboard if CLS fails the cap.

### §Acceptance

- [ ] `depends_on` **TECH-682** satisfied before first Lighthouse run.
- [ ] LCP, CLS, TBT recorded; compared to T27.7 caps (LCP ≤ baseline × 1.1, CLS < 0.1).
- [ ] If CLS over cap, `Surface` `motion` set to `none` on all `Surface` under `web/components/landing/**` and `web/app/dashboard/**` that exist; re-run Lighthouse; PR has NB3 table.

### §Test Blueprint

| test_name | inputs | expected | harness |
|-----------|--------|----------|---------|
| lighthouse | dev server on 4000 | LCP, CLS, TBT | Chrome Lighthouse or `npx lighthouse` |
| post_rem | optional code + server | improved CLS or documented no-`Surface` | manual |

### §Examples

| Surface location | Grep target |
|------------------|-------------|
| Dashboard | `rg '<Surface' web/app/dashboard` |
| Landing | `rg '<Surface' web/components/landing` |

### §Mechanical Steps

#### Step 1 — Baseline measure (no code change)

**Goal:** Capture Lighthouse metrics for `/` without altering repo behavior.

**Edits:**

- `web/components/landing/HomeLanding.client.tsx` — **before**:
  ```
  'use client';
  ```
  **after**:
  ```
  'use client';
  ```
  (No behavior change; satisfies measurement-only pass anchor.)

**Gate:**

```bash
cd /Users/javier/bacayo-studio/territory-developer && npx --yes lighthouse http://127.0.0.1:4000/ --only-categories=performance --output=json --output-path=./lighthouse-stage29.json
```

**STOP:** Start `npm run dev` inside `web/` on port **4000** first (separate shell); if Lighthouse errors with connection refused, the server is not up.

**MCP hints:** `runtime_state` (optional: record scenario id in local dev notes; not required).

#### Step 2 — CLS remediation (only when Step 1 CLS ≥ 0.1)

**Goal:** For each `Surface` in landing + dashboard trees, set `motion="none"`.

**Edits:** (illustrative; expand to all matching files from grep.)

- `web/app/(dev)/design-system/page.tsx` — **before**:
  ```
  <Surface tone="raised" padding="md" motion="subtle">
  ```
  **after**:
  ```
  <Surface tone="raised" padding="md" motion="none">
  ```
  (Repeat for `gentle` / `deliberate` rows in the same file when present.)

**Gate:**

```bash
cd /Users/javier/bacayo-studio/territory-developer && npx --yes lighthouse http://127.0.0.1:4000/ --output=json --output-path=./lighthouse-t29.json
```

**STOP:** If grep finds **no** `Surface` in `web/components/landing` or `web/app/dashboard`, document **no Surface in remediation scope** in the PR, retain CLS numbers, and close without Step 2 file edits.

**MCP hints:** `backlog_issue` (TECH-683, TECH-682).


## Final gate

```bash
npm run validate:all
```
