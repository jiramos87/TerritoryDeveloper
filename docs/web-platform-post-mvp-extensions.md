# Web Platform — Post-MVP extensions

> **Status:** Exploration seed — ready for `/design-explore`. Multi-section doc; each `## {N}. …` section below owns its own Problem statement + Axes + Approaches list + Open questions + (later) Design Expansion block. Run `/design-explore docs/web-platform-post-mvp-extensions.md --section {N}` one section at a time; each section graduates independently.
>
> **Scope:** Extensions beyond `ia/projects/web-platform-master-plan.md` MVP (Steps 1–6). Ships after MVP validates. Nothing here blocks MVP close.
>
> **Companion to:**
> - `ia/projects/web-platform-master-plan.md` — MVP orchestrator (Steps 1–6 Done 2026-04-17).
> - `docs/web-platform-exploration.md` — MVP exploration source (locked; `## Design Expansion` block frozen at A3 — static-first hybrid Next.js app at `web/`).
>
> **Why this doc exists:** MVP exploration's `### Implementation Points → Deferred / out of scope` carries post-MVP items inline; master plan `§Orchestration guardrails` flags recommendation to split into a companion doc when Step 5 portal stage opens (now done). This doc preserves the 1:1 relation between MVP exploration ↔ MVP master plan by isolating post-MVP design surface here. New post-MVP ideas land as new sections in this doc; `/master-plan-extend` pulls from here into `web-platform-master-plan.md` as new Steps.
>
> **Hierarchy rules:** `ia/rules/project-hierarchy.md` (step > stage > phase > task). `ia/rules/orchestrator-vs-spec.md` (web-platform-master-plan = orchestrator, permanent). This doc = exploration surface, never closeable.
>
> **Caveman prose default** — [`ia/rules/agent-output-caveman.md`](../ia/rules/agent-output-caveman.md). Exception: `web/content/**` + page-body JSX strings in `web/app/**/page.tsx` stay full English (user-facing). This doc (agent-authored IA prose) = caveman.
>
> **Invariants** — `ia/rules/invariants.md` #1–#12 NOT implicated. Web-platform surface is tooling / docs only, zero runtime C# / Unity coupling. Unity WebGL export (§6 below) retriggers invariants review when that section opens.

---

## 1. Full-game MVP rollout — completion view

**Status:** Exploration seed. Approaches enumerated; awaiting `/design-explore` poll.

**Priority:** Primary target — user-requested extension driving this doc's creation (2026-04-17).

### Problem statement

`ia/projects/full-game-mvp-rollout-tracker.md` carries a 12-row × 7-column lifecycle matrix (cells (a) Enumerate → (g) Align per child master plan). Matrix state is the canonical "how much of the full game is built" signal. Current surfaces:

- Markdown table in the tracker doc itself — dense, grep-friendly, but zero visual completion density.
- `/dashboard` (Step 3–4) — per-plan task-level breakdown; does NOT surface umbrella rollout-lifecycle state; no per-cell completion glyph beyond raw `✓ / ◐ / — / ❓ / ⚠️`.

Gap: **no dashboard view visualizes rollout completion as a glanceable "what's done vs. what's left" tracker.** User want = new view marking completed items with green tick glyphs, distinct from per-task status breakdown the existing dashboard already shows.

### Hard constraints

- Read-only consumer of `ia/projects/full-game-mvp-rollout-tracker.md` (+ child master plans where cell drill-down requires). Tracker doc stays authoritative; view is projection.
- Re-uses Stage 1.2 design primitives (`DataTable`, `StatBar`, `BadgeChip`, `FilterChips`, `HeatmapCell`) + design tokens (NYT palette + `raw.green` + `bg-bg-status-done` alias from Stage 4). No new primitive unless approach comparison proves existing set insufficient.
- Behind same auth middleware as `/dashboard` (Step 5.3 landed `web/middleware.ts` matcher `['/dashboard']` — extend matcher array, do NOT fork middleware).
- `tools/progress-tracker/parse.mjs` + `web/lib/plan-loader.ts` stay untouched. New loader if tracker-specific parsing required.
- Free-tier compliant (no new paid service; Vercel SSG/ISR acceptable).

### Axes of exploration

Poll surface for `/design-explore` Phase 1 compare:

**A — Visualization metaphor**
- Heatmap grid (12 rows × 7 cols, green tick overlay on ✓)
- Kanban column board (7 lifecycle cols as kanban lanes, rows = cards, cards slide right as cells tick)
- Per-row horizontal completion bar w/ 7 segments (green tick on each ✓ segment; FUTBIN-style dense row layout)
- Tree / hierarchy drill-down (umbrella → row → child master plan steps → tasks; green tick at every leaf Done)
- Hybrid (matrix heatmap landing + drill-down per row on click)

**B — Route placement**
- New `/rollout` route (top-level sidebar entry alongside `/dashboard`)
- New `/dashboard/rollout` sub-route (tab within existing dashboard)
- Replace dashboard header with rollout summary + existing task breakdown below
- Embed on landing `/` as marketing "game build progress" panel (public — reopens auth-gate decision)

**C — Completion semantics**
- Lifecycle cell-level (cell = `✓` → green tick; `◐ / —` → muted)
- Child-plan-level (child master plan `Status: Final` → whole row green tick; partial = proportional `StatBar`)
- Task-level (aggregate across all child BACKLOG tasks; closest to existing dashboard semantics)
- Composite — cell-level tick + row-level completion bar + task-level drill-down in one view

**D — Data source + parser**
- New `web/lib/rollout-tracker-loader.ts` parsing tracker markdown table rows + cells
- Extend `plan-loader.ts` to recognize rollout-tracker files (breaks "wrapper-only" invariant if parser changes required; likely rejected)
- Generate tracker state JSON at build time via `tools/progress-tracker/` sibling script; loader reads JSON only
- Live parse on every request (RSC reads file each render; no build step)

**E — Multi-umbrella generality**
- Single-purpose `/rollout` bound to `full-game-mvp-rollout-tracker.md` only
- Generic `/rollout/[umbrella]` — discovers any `*-rollout-tracker.md` under `ia/projects/`, renders each; future umbrellas inherit view for free
- Hardcode full-game-mvp at MVP; generalize when second rollout tracker exists (YAGNI cut)

**F — Access gate**
- Inherit `/dashboard` auth middleware (matcher extension)
- Public view (remove auth gate for this surface only — marketing use case; public "we're X% built" narrative)
- Obscure-URL gate (Q14 pattern from Step 3) before auth migration (regression — likely rejected)

**G — Interactivity**
- SSR-only w/ query-param filters (consistent w/ Step 4.4 multi-select pattern)
- Client-hydrated w/ animated tick reveal on cell → ✓ transition (D3 tween? CSS transition?)
- Static snapshot only (no filter, no interactivity; pure glance view)

### Approaches (seed candidates for `/design-explore` poll)

| Id | Name | Viz metaphor | Route | Completion semantics | Data source | Generality | Access gate | Interactivity |
|----|------|--------------|-------|----------------------|-------------|------------|-------------|---------------|
| R1 | **Matrix heatmap + SSR filters** | Heatmap grid | `/rollout` | Cell-level | new `rollout-tracker-loader.ts` | Single-purpose (hardcode full-game-mvp) | Inherit dashboard auth | SSR + query-param filters |
| R2 | **Kanban lanes + row drill-down** | Kanban (7 lanes) | `/dashboard/rollout` tab | Composite (cell + row) | new loader + reuse `plan-loader.ts` per row | Single-purpose | Inherit dashboard auth | Client-hydrated drill-down |
| R3 | **Dense row bars — FUTBIN-style** | Per-row 7-segment bar | `/rollout` | Cell-level + per-row `StatBar` aggregate | new loader | Generic `/rollout/[umbrella]` from day one | Inherit dashboard auth | SSR-only |
| R4 | **Public marketing panel on `/`** | Row bars (compact) landing panel + full view at `/rollout` | landing `/` + `/rollout` | Child-plan-level (coarse — Final vs. not) | new loader + build-time JSON | Single-purpose at MVP | Public (landing) + auth-gated `/rollout` detail | SSR-only |
| R5 | **Tree drill-down w/ green-tick leaves** | Hierarchy tree | `/rollout` | Task-level (finest grain) | new loader + `plan-loader.ts` drill | Generic `/rollout/[umbrella]` | Inherit dashboard auth | Client-hydrated collapse/expand |

Design-explore should score each across Constraint fit / Effort / Output control / Maintainability / Dependencies + risk (per `ia/skills/design-explore/SKILL.md` Phase 1). All five approaches (R1–R5) remain live options — no pre-selection. R3 (Dense row bars) and R5 (Tree drill-down) in particular offer distinct tradeoffs: R3 = lowest-effort SSR-only, reuses existing primitives; R5 = highest granularity + generality, requires client hydration. User poll determines final pick.

## To be selected during /design-explore

Run `/design-explore docs/web-platform-post-mvp-extensions.md --section 1` to score approaches + select one. Do NOT pre-decide R3 or any other option here — design-explore poll is the decision gate.

### Open questions

Phase 0.5 interview candidates (one-per-turn, max 5, UI/UX language per skill rule):

1. Should the rollout view show **per-lifecycle-cell ticks** (granular, 84 cells for full-game-mvp) or **per-row summary ticks** (12 rows, one "done" indicator per child plan)? Affects visual density + what "completion" means to the viewer.
2. Is this view **public-facing** (marketing: "Territory Developer is 40% built — see progress") or **internal-only** (dev dashboard companion, same auth gate as `/dashboard`)? Unlocks R4 vs. locks into R1/R2/R3/R5.
3. Should the view **generalize to any future rollout tracker** (zone-s-economy, citystats-overhaul eventually get their own umbrellas) or **hardcode full-game-mvp** and generalize when second tracker exists?
4. Does "green tick" carry **separate semantics per completion tier** (e.g., amber half-tick for `◐` partial, green full tick for `✓`, muted glyph for `—` not started) or is it binary (tick-or-nothing)? Affects color token selection.
5. Should cell drill-down (click cell → see child plan state) be **in-page (client-hydrated panel)** or **route to child plan's `/dashboard?plan=…`** (SSR, reuses existing filter infra from Stage 4.4)?

### Expected handoff

On `/design-explore` complete:
- This section gains `## Design Expansion — §1 Rollout completion view` block (Chosen Approach + Architecture mermaid + Subsystem Impact + Implementation Points + Examples + Review Notes).
- `/master-plan-extend ia/projects/web-platform-master-plan.md docs/web-platform-post-mvp-extensions.md` appends new Step 7 — Full-game MVP rollout completion view, fully decomposed at author time (stages + phases + tasks `_pending_`).

---

## 2. Payment gateway

**Status:** Deferred. Needs `/design-explore --section 2` before master-plan extend.

**Source:** MVP exploration Q10 undecided; `### Deferred / out of scope` bullet 1.

### Problem statement

One-time-purchase gateway for Territory Developer. Architecture slot reserved in MVP (Step 5 §Portal placeholder) — no provider wired. Needs:
- Provider selection (Stripe / Paddle / Lemon Squeezy / Gumroad — free-tier-until-first-sale comparison)
- Entitlement check post-payment (`entitlement` table drafted in Stage 5.2 schema)
- Receipt + refund flow
- Tax handling (VAT / Merchant-of-Record decision)

### Approaches

*To be enumerated at `/design-explore` time. Candidate axes:* provider choice, MoR vs. direct, one-time vs. subscription, regional tax handling, entitlement validation cadence (JWT claim vs. DB lookup per request), fraud-check depth.

### Open questions

- Launch price + region strategy?
- Refund window?
- Bundled DLC / expansion model in scope, or pure one-shot purchase?
- Does entitlement unlock cloud saves (§3), map management, or ad-free (if ads enter scope)?

---

## 3. Cloud saves + map management

**Status:** Deferred. Needs `/design-explore --section 3`.

**Source:** MVP exploration Q12 portal killer feature; `### Deferred / out of scope` bullet 2.

### Problem statement

User portal surface. Store player game saves + user-authored maps in cloud; sync across devices; share / fork maps publicly. Foundations (schema draft for `save` table) landed in Stage 5.2; user-facing portal UX deferred.

### Approaches

*To be enumerated at design-explore. Candidate axes:* save serialization format (binary blob / JSON / Protobuf), storage backing (Postgres JSONB / Vercel Blob / S3-compatible free tier), sync strategy (manual / auto / last-write-wins / conflict UI), map share visibility (private / unlisted / public), map fork semantics, entitlement-gated save slot cap.

### Open questions

- Save file size ceiling?
- Offline-first or cloud-first UX?
- Does map-sharing require moderation (public host liability)?
- Versioning — do map edits keep history or just overwrite?

---

## 4. Community wiki edits

**Status:** Deferred. Needs `/design-explore --section 4`.

**Source:** MVP exploration Q5 "v2+"; `### Deferred / out of scope` bullet 3.

### Problem statement

MVP wiki (Step 2 Stage 2.2) is solo-authored MDX + glossary auto-index. v2+ opens community edits. Governance + moderation + attribution model unresolved.

### Approaches

*To be enumerated at design-explore. Candidate axes:* GitHub-PR-based edits vs. in-app editor vs. Discord-bot-authored, moderation (pre / post / no-moderation), attribution (git blame / per-page author list / anonymous), spam / vandalism defense (captcha / account-age gate / rate limit), entitlement gating (paying customers only vs. open).

### Open questions

- Does community edit eligibility require payment (§2) or free-tier account sufficient?
- Dispute resolution mechanism?
- Edit history UX — wiki-page revert flow or git-log read-only?

---

## 5. Internationalization (i18n)

**Status:** Deferred. Needs `/design-explore --section 5`.

**Source:** MVP exploration `### Deferred / out of scope` bullet 4 — English only at MVP.

### Problem statement

Public site + dashboard + future portal currently English-only. i18n requires string extraction + translation pipeline + locale routing + RTL support.

### Approaches

*To be enumerated at design-explore. Candidate axes:* framework (`next-intl` / `next-i18next` / custom), locale routing (subpath `/es/` vs. subdomain `es.` vs. `Accept-Language` negotiation), translation source (human / MT-seeded + human edit / fully MT), MDX content translation strategy (parallel files per locale vs. frontmatter fallback), glossary term translation scope.

### Open questions

- Target locale set (Spanish for v1 given user bilingual? + ? + ?)?
- Date / number / currency localization alongside text?
- RTL layout support (Arabic / Hebrew) in scope or parked?

---

## 6. Unity WebGL export

**Status:** Deferred. Needs `/design-explore --section 6` + invariants re-review.

**Source:** MVP exploration `### Deferred / out of scope` bullet 5 — "separate future track".

### Problem statement

Currently Unity builds = desktop only. WebGL export = playable demo embed on landing. Unity WebGL constraints: no multithreading (as of Unity 2022.3), memory caps, initial download size, asset streaming.

### Approaches

*To be enumerated at design-explore. Candidate axes:* demo vs. full game (demo likely; full game size prohibitive for web), save compat (does WebGL demo save sync to portal?), asset compression (Brotli / LZ4), streaming (on-demand chunks vs. upfront), engine upgrade (Unity 6.x multithread support reconsiders).

### Open questions

- Is WebGL build a **marketing demo** (narrow vertical slice, 10 min experience) or **full-product web delivery** (same as desktop)?
- Revisit trigger after Unity 6.x migration (sibling `blip-post-mvp-extensions.md` §2 raises same engine-upgrade gate)?
- Hosting — Vercel static? GCS bucket? Unity Play?

### Invariants retrigger

Unity WebGL export couples web platform to runtime C# / Unity subsystems — `ia/rules/invariants.md` #1–#12 re-apply. Design-explore Phase 5 (Subsystem Impact) MUST run `invariants_summary` before approach lock.

---

## 7. Developer-experience utilities

**Status:** Deferred. Needs `/design-explore --section 7` (or may fold as phases into §1 / §8 implementation).

**Source:** MVP exploration `### Review Notes → Suggestions`.

### 7.1 Glossary hot-reload dev script

Watch `ia/specs/glossary.md` during `cd web && npm run dev`; auto-regenerate wiki glossary import + search index on change. Killed wait: no more dev-server restart after glossary edits.

### 7.2 PR-preview URL comment bot

GitHub Action posts Vercel preview URL as PR comment on every push. Wiki / devlog diffs reviewable visually in-PR. Free-tier compatible (GitHub Actions + Vercel preview).

### Approaches

*To be enumerated at design-explore. Each item single-file / single-config; low-effort; may batch into one stage alongside §1 rollout view.*

### Open questions

- Is 7.1 valuable enough to ship standalone, or fold into §1 as a sub-task (rollout-tracker watcher benefits from same infra)?
- Does 7.2 require custom Action, or suffice w/ existing Vercel GitHub app comment?

---

## 8. UI/UX visual design direction — navigation, typography, component polish

**Status:** Exploration seed. Seeded 2026-04-17 from two external reference screenshots reviewed by user. Awaiting `/design-explore --section 8` to produce Design Expansion + feed `/master-plan-extend`.

**Source:** User-provided design reference screenshots — Shopify dev docs (dark developer portal) + Dribbble breadcrumb navigation pattern.

### Problem statement

Current web platform has a functional but aesthetically thin UI: minimal component hierarchy, monospace-heavy type scale, no visual rhythm system beyond basic dark tokens. Two reviewed references surface concrete, derivable patterns that align well with Territory's dark-mode developer-facing aesthetic. Goal: translate these patterns into a coherent visual design layer applicable across all existing pages without replacing the token system.

### Design references

#### Shopify dev docs — developer portal aesthetic

Observations (structure/patterns only — not color palette):

- **Sidebar navigation tree**: collapsible category headers with disclosure arrow + child item indentation; active item has left-border accent + subtle panel background. Mirrors our border-l-2 step hierarchy in dashboard — opportunity to unify the visual language.
- **Type/category badges**: colored pill chips (`ID!` / `required` / query type) inline with identifiers. Maps to our `BadgeChip` and `FilterChips` — suggests tighter badge sizing, semantic color slots beyond gray.
- **Two-panel content layout**: prose on left, sticky code panel on right with language tab switcher (GQL / cURL / React Router / Node.js). Applicable to devlog and wiki pages with code examples.
- **Section separators**: thin full-width `<hr>`-style rules between major sections (`Arguments`, `Possible returns`). Currently absent from our wiki/devlog pages.
- **Search in sidebar**: inline `Filter` input at top of sidebar tree. Territory sidebar has 4 hard-coded links — expandable to filterable nav as section count grows.
- **Feedback row**: "Was this section helpful? Yes / No" inline below each section. Low-effort engagement signal for devlog posts.
- **Typography pairing**: bold monospace for type/query identifiers at page top, system sans for prose. Territory already uses this split — reference validates the pattern; suggests more deliberate size contrast (identifier heading larger relative to body).
- **Copy button on code blocks**: icon button top-right of code panel. Missing from devlog MDX code blocks.

#### Dribbble breadcrumb — navigation tab pattern

Observations:

- **Separator**: `/` (slash) not `›` — reads more like a filesystem path; pairs better with developer tool aesthetic. **Applied immediately** to `web/components/Breadcrumb.tsx`.
- **Font size**: `text-base` (≥16px), system sans (not mono). Breadcrumb is a primary orientation landmark, not decorative metadata. **Applied immediately.**
- **Current segment weight**: `font-medium` on the active/final crumb to distinguish from ancestors. **Applied immediately.**
- **Segment as dropdown affordance**: current segment has a pill-shaped dark background + up/down chevron, revealing sibling navigation on click. Post-MVP opportunity: make current crumb an interactive dropdown for wiki category siblings or devlog date-range siblings.
- **Ancestor spacing**: generous `gap-2` between crumbs (not cramped). **Applied immediately.**
- **Crumb height**: nav has visible vertical breathing room (`py-3`) — breadcrumb bar reads as a proper row, not an afterthought. **Applied immediately.**

### Design axes for `/design-explore`

1. **Component-by-component vs. design-system-first** — tackle each component in isolation (breadcrumb → sidebar → badges → code blocks) OR define a token + spacing + type scale spec first and derive components from it.
2. **Inline-style pages vs. Tailwind-first migration** — most pages use `tokens.*` inline styles; dashboard uses Tailwind. Unify before polishing (Tailwind-first) OR keep split and polish both surfaces in parallel.
3. **New primitives vs. extend existing** — introduce `CodeBlock`, `SectionRule`, `FeedbackRow`, `SidebarTree` as new components OR extend `DataTable`, `FilterChips`, `BadgeChip` to cover the gaps.

### Hard constraints

- Zero change to token palette (colors are locked by NYT-derived `palette.json`). Shape, spacing, weight, type scale = free to adjust.
- No runtime/Unity coupling. All changes are `web/` surface only.
- Breadcrumb immediate fixes already landed (separator, size, weight, spacing) — §8 builds on that base.

### Open questions

- Which page is the highest-leverage starting point for visual polish? (Wiki detail page or devlog post — both have clear prose + code content requiring hierarchy treatment.)
- Does the sidebar need expandable tree navigation now (for wiki categories), or is 4-link flat sufficient through Steps 7–9?
- Should code blocks in devlog MDX get a copy-to-clipboard button + language tab switcher, or is the Shopify two-panel approach overkill for our content volume?
- Is "Was this helpful?" feedback worth wiring to any backend, or just cosmetic (hidden form action)?

### Candidate approaches (to be scored at `/design-explore`)

- **A: Design-system-first** — author `web/lib/design-system.md` spec (type scale, spacing scale, component map), then derive component edits from it. Slower to ship, more coherent long-term.
- **B: Component-by-component polish** — ranked priority: Breadcrumb (done) → wiki detail → devlog post → sidebar → dashboard badges. Ship iteratively; infer system rules from outcomes.
- **C: Hybrid** — define a 1-page "visual rhythm" doc (5 rules max) immediately, then run component-by-component under those rules.

---

## 9. Candidate additions (unclaimed; add here as they surface)

Items raised in future conversations, not yet assigned a section number. Promote to own § when priority lands.

- (none yet)

---

## Scope guardrail

MVP locked decisions (`ia/projects/web-platform-master-plan.md` header `**Locked decisions (do not reopen in this plan):**`) remain authoritative for Steps 1–6. Changes to MVP scope require explicit re-decision + sync edit to both orchestrator + `docs/web-platform-exploration.md`. Do NOT silently promote a post-MVP item from this doc into an MVP stage.

Extensions above land as **new Steps** on `web-platform-master-plan.md` via `/master-plan-extend`, never as edits to existing Steps 1–6. Each new Step fully decomposed at author time (same cardinality gate as `/master-plan-new`: ≥2 tasks per phase, ≤6 tasks per phase).

Candidate extensions surfaced during MVP implementation (bugs, refactor opportunities exposed by real authoring) land here as new rows under the matching §.

---

## Expansion workflow (for future agents)

1. **Pick a section** — §1 has concrete approaches seeded and is the current priority. §§2–8 need axes+approaches enumeration before `/design-explore` can poll-compare.
2. **Run `/design-explore docs/web-platform-post-mvp-extensions.md`** — skill loads, detects multi-section layout via `## {N}. …` headers. If section arg omitted, skill asks which section to expand.
3. **Phase 0.5 interview** — skill asks user one question per turn, max 5, from the section's `### Open questions` list (+ inferred constraints).
4. **Phase 1–2 compare + select** — skill builds criteria matrix from hard constraints, scores each approach, user confirms or overrides via `APPROACH_HINT`.
5. **Phase 3–6 expand** — skill authors `## Design Expansion — §{N} {section title}` block back into this doc.
6. **Handoff to `/master-plan-extend`** — `master-plan-extend ia/projects/web-platform-master-plan.md docs/web-platform-post-mvp-extensions.md` reads the new Design Expansion block + appends new Step to orchestrator, fully decomposed, tasks `_pending_`.
7. **`/stage-file` + `/project-new` + `/kickoff` + `/implement` + `/verify-loop` + `/closeout`** per standard lifecycle.

Repeat for each section as priority lands. Doc grows monotonically; existing sections never rewritten post-expansion (new findings → new section or new item under § 9).
