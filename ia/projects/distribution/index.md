# Distribution — Master Plan (Full-Game MVP Bucket 10)

> **Status:** In Progress — Step 1 / Stage 1.1
>
> **Scope:** Unity build pipeline + unsigned platform-native installers (`.pkg` mac, `.exe` win) + semver BuildInfo manifest + private `/download` web surface + in-game update notifier + trainable release skill, for a curated 20–50 tester audience. Signing, patch deltas, Linux, WebGL, Steam, public itch are explicitly out of scope per umbrella Hard deferrals.
>
> **Exploration source:** `docs/distribution-exploration.md` (§Design Expansion — Chosen approach, Architecture, Subsystem impact, Implementation points IP-1 … IP-10).
>
> **Umbrella:** `ia/projects/full-game-mvp-master-plan.md` §Distribution gating (Tier E), Bucket 10 row.
>
> **Locked decisions (do not reopen in this plan):**
> - Approach B — manual-script build + platform-native installers (`.pkg` via `pkgbuild` + `productbuild`; `.exe` via Inno Setup).
> - Unsigned tier — Gatekeeper + SmartScreen bypass documented on `/download` page; no notarization.
> - macOS + Windows only — no Linux, no WebGL.
> - Single release lane — one version everyone is on; ship → feedback → next release. No patch channel.
> - Manual script trigger — no CI auto-build; trainable skill captures process.
> - `/download` page private until MVP ships (`robots.ts` disallow + unlinked route); goes public on release.
> - In-game notifier = launch-time check only, silent network failure, opens `/download` via `Application.OpenURL`.
> - Access control = direct link sharing — no token gate, no password.
> - Semver shape `MAJOR.MINOR.PATCH[-PRERELEASE]` stamped into `PlayerSettings.bundleVersion` + `BuildInfo.asset`.
>
> **Hierarchy rules:** `ia/rules/project-hierarchy.md` (step > stage > phase > task). `ia/rules/orchestrator-vs-spec.md` (this doc = orchestrator, never closeable via `/closeout`).
>
> **Read first if landing cold:**
> - `docs/distribution-exploration.md` — full design + architecture + examples. Design Expansion block is ground truth.
> - `ia/projects/full-game-mvp-master-plan.md` §Bucket 10 — umbrella scope boundary.
> - `ia/rules/project-hierarchy.md` + `ia/rules/orchestrator-vs-spec.md` — doc semantics + phase/task cardinality rule (≥2 tasks per phase).
> - `ia/rules/invariants.md` — #3 (no `FindObjectOfType` in hot loops — gates Stage 2.3 `UpdateNotifier.Awake` pattern), #4 (no new singletons — gates Stage 2.3 Inspector-wired component pattern).
> - MCP: `backlog_issue {id}` per referenced id once tasks file; never full `BACKLOG.md` read.

---

## Stages

> **Tracking legend:** Step / Stage `Status:` uses enum `Draft | In Review | In Progress — {active child} | Final` (per `ia/rules/project-hierarchy.md`). Phase bullets use `- [ ]` / `- [x]`. Task tables carry a **Status** column: `_pending_` (not filed) → `Draft` → `In Review` → `In Progress` → `Done (archived)`. Markers flipped by lifecycle skills: `stage-file` → task rows gain `Issue` id + `Draft` status; `/author` (`plan-author`) → `In Review`; `/implement` → `In Progress`; the Stage-scoped `/closeout` pair (`stage-closeout-plan` → `plan-applier` Mode `stage-closeout`) → task rows `Done (archived)` + stage `Final` + stage-level rollup.

### Stage index

- [Stage 1 — Unity build pipeline + versioning manifest / BuildInfo SO + semver compare helper](stage-1-buildinfo-so-semver-compare-helper.md) — _In Progress (TECH-347, TECH-348, TECH-349, TECH-350 filed)_
- [Stage 2 — Unity build pipeline + versioning manifest / Unity editor build script](stage-2-unity-editor-build-script.md) — _Draft (tasks _pending_ — not yet filed)_
- [Stage 3 — Unity build pipeline + versioning manifest / Build orchestration shell + Credits integration](stage-3-build-orchestration-shell-credits-integration.md) — _Draft (tasks _pending_ — not yet filed)_
- [Stage 4 — Unsigned packaging + `/download` publication + in-game notifier / Platform packaging scripts](stage-4-platform-packaging-scripts.md) — _Draft (tasks _pending_ — not yet filed)_
- [Stage 5 — Unsigned packaging + `/download` publication + in-game notifier / `/download` web surface + latest.json](stage-5-download-web-surface-latest-json.md) — _Draft (tasks _pending_ — not yet filed)_
- [Stage 6 — Unsigned packaging + `/download` publication + in-game notifier / In-game UpdateNotifier + trainable release skill](stage-6-in-game-updatenotifier-trainable-release-skill.md) — _Draft (tasks _pending_ — not yet filed)_

## Orchestration guardrails

**Do:**

- Open one stage at a time. Next stage opens only after current stage's `/closeout` pair (`stage-closeout-plan` → `plan-applier` Mode `stage-closeout`) runs.
- Run `claude-personal "/stage-file ia/projects/distribution-master-plan.md Stage {N}.{M}"` to materialize pending tasks → BACKLOG rows + `ia/projects/{ISSUE_ID}.md` stubs.
- Update stage / step `Status` + phase checkboxes as lifecycle skills flip them — do NOT edit by hand.
- Preserve locked decisions (see header block). Changes require explicit re-decision + sync edit to `docs/distribution-exploration.md` + umbrella `full-game-mvp-master-plan.md` Bucket 10 row.
- Keep this orchestrator synced with umbrella — on Bucket 10 final-stage close, flip umbrella Bucket 10 row per `/closeout` umbrella-sync rule.
- When ToastService from Bucket 6 lands, revisit T2.3.2 wire-up per Review notes carry-over.

**Do not:**

- Close this orchestrator via `/closeout` — orchestrators are permanent (see `ia/rules/orchestrator-vs-spec.md`). Only the terminal stage (2.3) landing triggers a final `Status: Final`; the file stays.
- Silently promote post-MVP items (signing, Linux, WebGL, patch deltas, Steam, public itch) into MVP stages — they belong in a future extensions doc.
- Merge partial stage state — every stage must land on a green bar (`npm run unity:compile-check` + `npm run validate:all` + EditMode tests where applicable).
- Insert BACKLOG rows directly into this doc — only `stage-file` materializes them.
- Sign artifacts in MVP scope — unsigned tier is locked. Signing work triggers a scope-boundary extension, not an in-plan change.

---
