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

> **Tracking legend:** Step / Stage `Status:` uses enum `Draft | In Review | In Progress — {active child} | Final` (per `ia/rules/project-hierarchy.md`). Phase bullets use `- [ ]` / `- [x]`. Task tables carry a **Status** column: `_pending_` (not filed) → `Draft` → `In Review` → `In Progress` → `Done (archived)`. Markers flipped by lifecycle skills: `stage-file` → task rows gain `Issue` id + `Draft` status; `/kickoff` → `In Review`; `/implement` → `In Progress`; `/closeout` → `Done (archived)` + phase box when last task of phase closes; `project-stage-close` → stage `Final` + stage-level rollup.

### Stage 1 — Unity build pipeline + versioning manifest / BuildInfo SO + semver compare helper

**Status:** In Progress (TECH-347, TECH-348, TECH-349, TECH-350 filed)

**Objectives:** Land the runtime data model (BuildInfo ScriptableObject) + pure semver compare helper with EditMode coverage. Both are inert dependencies for the editor build script in Stage 1.2 and the notifier in Stage 2.3. No build pipeline wiring yet.

**Exit:**

- `Assets/Scripts/Runtime/Distribution/BuildInfo.cs` compiles; `[CreateAssetMenu]` populates Unity menu; `WriteFields` gated on `#if UNITY_EDITOR`.
- `Assets/Resources/BuildInfo.asset` instance created via editor menu; default values `0.0.0-dev` / `unknown` / `unknown`; `Resources.Load<BuildInfo>("BuildInfo")` returns non-null at runtime.
- `Assets/Scripts/Runtime/Distribution/SemverCompare.cs` static `Compare(string a, string b) → int` handles `MAJOR.MINOR.PATCH` + optional `-PRERELEASE` suffix per Design Expansion IP-8 subset.
- EditMode test `Assets/Tests/EditMode/Distribution/SemverCompareTests.cs` exercises ≥6 truth-table cases (equal, greater major, greater minor, greater patch, prerelease ordering, malformed input fallback).
- Glossary rows added to `ia/specs/glossary.md` — **BuildInfo ScriptableObject**, **Unsigned installer tier**, **Release manifest (`latest.json`)**, **Update notifier** (forward-ref the latter two to Stage 2.2 / 2.3).
- `npm run unity:compile-check` + EditMode tests green.
- Phase 1 — Author BuildInfo ScriptableObject + committed asset instance.
- Phase 2 — Author SemverCompare helper + EditMode test coverage.
- Phase 3 — Register distribution glossary rows in `ia/specs/glossary.md`.

**Tasks:**

| Task | Name | Issue | Status | Intent |
| --- | --- | --- | --- | --- |
| T1.1 | BuildInfo SO type | **TECH-347** | Draft | Author `Assets/Scripts/Runtime/Distribution/BuildInfo.cs` matching Design Expansion IP-3 verbatim — `[CreateAssetMenu(fileName = "BuildInfo", menuName = "Territory/BuildInfo")]`, private serialized `version` / `gitSha` / `buildTimestamp` fields with default `"0.0.0-dev"` / `"unknown"` / `"unknown"`, public getters, editor-gated `WriteFields(string, string, string)` under `#if UNITY_EDITOR`. |
| T1.2 | BuildInfo asset instance | **TECH-348** | Draft | Create `Assets/Resources/BuildInfo.asset` via the Territory/BuildInfo menu command; commit both `.asset` + `.asset.meta`. Verify `Resources.Load<BuildInfo>("BuildInfo")` returns non-null in an EditMode fixture. |
| T1.3 | SemverCompare helper + tests | **TECH-349** | Draft | Author `Assets/Scripts/Runtime/Distribution/SemverCompare.cs` static `Compare(string, string) → int` per IP-8 (subset: MAJOR.MINOR.PATCH + optional `-PRERELEASE`). Author `Assets/Tests/EditMode/Distribution/SemverCompareTests.cs` with truth table (equal, major >, minor >, patch >, prerelease ordering, malformed input → 0 fallback). No external semver library. |
| T1.4 | Glossary rows for distribution terms | **TECH-350** | Draft | Append rows to `ia/specs/glossary.md` for **BuildInfo ScriptableObject** (ref `Assets/Scripts/Runtime/Distribution/BuildInfo.cs`), **Release manifest (`latest.json`)** (forward-ref Stage 2.2), **Update notifier** (forward-ref Stage 2.3), **Unsigned installer tier** (forward-ref Stage 2.1). Follow glossary authoring rules in `ia/rules/terminology-consistency-authoring.md`. |

### Stage 2 — Unity build pipeline + versioning manifest / Unity editor build script

**Status:** Draft (tasks _pending_ — not yet filed)

**Objectives:** Land `ReleaseBuilder.cs` under `Assets/Editor/` — the Unity batch-mode entry point that reads env vars, stamps `BuildInfo.asset`, updates `PlayerSettings.bundleVersion`, and runs `BuildPipeline.BuildPlayer` for each target. Editor-only code, no runtime impact.

**Exit:**

- `Assets/Editor/ReleaseBuilder.cs` exposes `public static void BuildMac()` + `public static void BuildWindows()`.
- Reads `BUILD_VERSION` / `BUILD_SHA` / `BUILD_TIMESTAMP` via `System.Environment.GetEnvironmentVariable`; fails with non-zero Unity exit on missing vars.
- `UpdateBuildInfoAsset(version, sha, timestamp)` helper loads `Assets/Resources/BuildInfo.asset` via `AssetDatabase.LoadAssetAtPath`, calls `WriteFields`, `EditorUtility.SetDirty`, `AssetDatabase.SaveAssets`.
- Writes `PlayerSettings.bundleVersion = version` before BuildPlayer invocation.
- Calls `BuildPipeline.BuildPlayer` with `BuildTarget.StandaloneOSX` (BuildMac) / `BuildTarget.StandaloneWindows64` (BuildWindows), output paths `Builds/mac/Territory.app` / `Builds/win/Territory.exe`.
- Local dry-run (manual invocation from Unity editor on a test semver) produces a `BuildInfo.asset` with correct fields + a built binary in `Builds/`.
- Phase 1 — Author ReleaseBuilder skeleton + env var reader + BuildInfo writer helper.
- Phase 2 — Wire platform-specific BuildPipeline invocations.
- Phase 3 — Local dry-run validation on both targets (mac in-repo; win documented).

**Tasks:**

| Task | Name | Issue | Status | Intent |
| --- | --- | --- | --- | --- |
| T2.1 | ReleaseBuilder skeleton + env reader | _pending_ | _pending_ | Author `Assets/Editor/ReleaseBuilder.cs` with `ReadEnv(string key)` helper that throws a descriptive exception when var missing, and top-level try/catch that `EditorApplication.Exit(1)` on any error so the shell script in Stage 1.3 propagates failure. |
| T2.2 | UpdateBuildInfoAsset helper | _pending_ | _pending_ | Add `UpdateBuildInfoAsset(string version, string sha, string timestamp)` in `ReleaseBuilder.cs` — `AssetDatabase.LoadAssetAtPath<BuildInfo>("Assets/Resources/BuildInfo.asset")`, call editor-gated `WriteFields`, `EditorUtility.SetDirty(asset)`, `AssetDatabase.SaveAssets()`, `AssetDatabase.Refresh()`. Fail loudly if asset missing (points the user at T1.1.2). |
| T2.3 | BuildMac + BuildWindows entry methods | _pending_ | _pending_ | Implement `public static void BuildMac()` + `public static void BuildWindows()` in `ReleaseBuilder.cs` — read env vars, call `UpdateBuildInfoAsset`, set `PlayerSettings.bundleVersion`, invoke `BuildPipeline.BuildPlayer` with the right `BuildPlayerOptions` (target, locationPathName `Builds/mac/Territory.app` / `Builds/win/Territory.exe`, `BuildOptions.None`, explicit scene list from `EditorBuildSettings.scenes`). Check `BuildReport.summary.result` and exit non-zero on failure. |
| T2.4 | Local dry-run validation | _pending_ | _pending_ | Run `BuildMac` once from the Unity editor menu with hand-set env vars (`BUILD_VERSION=0.0.0-dev-test`, `BUILD_SHA=abc1234`, `BUILD_TIMESTAMP=...`); confirm `Assets/Resources/BuildInfo.asset` updated + `Builds/mac/Territory.app` produced. Capture command + output in a scratch note (eventually lands in Stage 2.3 trainable skill). Document the Windows machine invocation (cannot run locally) as a placeholder for Stage 2.1. |

### Stage 3 — Unity build pipeline + versioning manifest / Build orchestration shell + Credits integration

**Status:** Draft (tasks _pending_ — not yet filed)

**Objectives:** Ship `tools/scripts/build-release.sh` — semver-validated shell entry that wires env vars + invokes Unity batch mode on macOS, and prints a copy-pasteable Windows command block. Land the Credits screen wire-up so testers actually see `BuildInfo.Version` in-game.

**Exit:**

- `tools/scripts/build-release.sh` matches Design Expansion Example A shape; chmod +x.
- Semver regex gate rejects malformed args with clear error + exit 1.
- Exports `BUILD_VERSION` / `BUILD_SHA` (from `git rev-parse --short HEAD`) / `BUILD_TIMESTAMP` (UTC ISO8601).
- Resolves `$UNITY_EDITOR_PATH` via `tools/scripts/load-repo-env.inc.sh`.
- `--platform {mac|win|all}` flag wired; `mac` runs Unity locally; `win` prints the Windows-machine command.
- Credits screen displays `{version} ({gitSha})` via `Resources.Load<BuildInfo>("BuildInfo")` on screen open.
- End-to-end dry-run: `tools/scripts/build-release.sh --version 0.0.0-dev-test --platform mac` produces an updated `BuildInfo.asset` + `Builds/mac/Territory.app`, and launching the built app shows the version string on Credits.
- Phase 1 — Author `build-release.sh` entry with semver validation + env var export.
- Phase 2 — Wire Credits screen consumer of `BuildInfo`.
- Phase 3 — End-to-end dry-run + doc-string capture.

**Tasks:**

| Task | Name | Issue | Status | Intent |
| --- | --- | --- | --- | --- |
| T3.1 | build-release.sh entry + semver gate | _pending_ | _pending_ | Author `tools/scripts/build-release.sh` (chmod +x) per Example A: `set -euo pipefail`, semver regex `^[0-9]+\.[0-9]+\.[0-9]+(-[A-Za-z0-9.-]+)?$` validation, `git rev-parse --short HEAD` for SHA, UTC ISO8601 timestamp, source `tools/scripts/load-repo-env.inc.sh` for `$UNITY_EDITOR_PATH`. Fail fast on missing Unity path. |
| T3.2 | Platform dispatch (mac/win/all) | _pending_ | _pending_ | Add `build_mac` shell function that invokes `$UNITY -batchmode -nographics -quit -projectPath $(pwd) -executeMethod ReleaseBuilder.BuildMac -logFile Logs/build-mac-$VERSION.log` and checks exit code. Add `build_win` that prints the Windows-machine command block (since cross-compile to Win from Mac is the Windows-box responsibility per exploration §Cons). Wire `case "$PLATFORM"` dispatch for `mac`/`win`/`all`. |
| T3.3 | Credits screen BuildInfo wire-up | _pending_ | _pending_ | Locate existing Credits screen component under `Assets/Scripts/UI/` (path resolves at kickoff). Add `[SerializeField] private BuildInfo buildInfo;` + `Awake` fallback `buildInfo ??= Resources.Load<BuildInfo>("BuildInfo")`. Render `$"v{buildInfo.Version} ({buildInfo.GitSha})"` in the version label. Invariant #4 — Inspector-wire pattern, no singleton. |
| T3.4 | End-to-end mac dry-run + handoff note | _pending_ | _pending_ | Run `tools/scripts/build-release.sh --version 0.0.0-dev-test --platform mac` on the dev Mac. Verify `Assets/Resources/BuildInfo.asset` stamped, `Builds/mac/Territory.app` built, launching the app shows the version string on Credits. Capture command + output into a working note that seeds the trainable skill in Stage 2.3 (`ia/skills/distribution-release/SKILL.md`). |

---

### Stage 4 — Unsigned packaging + `/download` publication + in-game notifier / Platform packaging scripts

**Status:** Draft (tasks _pending_ — not yet filed)

**Objectives:** Land the two packager scripts + their config templates (macOS `.pkg` via `pkgbuild`/`productbuild`; Windows `.exe` via Inno Setup `iscc.exe`). Both emit unsigned artifacts into `Dist/`. Wire `build-release.sh` to call `package-mac.sh` after `BuildMac` succeeds, and to emit the Windows packaging command in the win-platform block.

**Exit:**

- `tools/scripts/package-mac.sh` takes `$APP_PATH` + `$VERSION`, runs `pkgbuild` → component pkg, then `productbuild --distribution` → final `Dist/TerritoryDeveloper-$VERSION.pkg`.
- `tools/dist/mac/distribution.xml.template` committed with `$VERSION` placeholder — `package-mac.sh` envsubsts into a temp `distribution.xml`.
- `tools/scripts/package-win.ps1` takes `$BuildPath` + `$Version`, invokes `iscc.exe /DMyAppVersion=... /DMyAppPath=... tools\dist\win\territory.iss`.
- `tools/dist/win/territory.iss` committed with `{#MyAppVersion}` + `{#MyAppPath}` Inno directives.
- `build-release.sh` `build_mac` invokes `tools/scripts/package-mac.sh` after successful Unity build.
- Smoke dry-run: mac pkg installs on a clean user account (double-click → Gatekeeper right-click-Open workflow); Windows `.exe` installer runs on a Win machine (SmartScreen "More info → Run anyway" workflow).
- Phase 1 — macOS packaging script + distribution.xml template.
- Phase 2 — Windows packaging script + Inno Setup `.iss`.
- Phase 3 — Wire packagers into `build-release.sh` + smoke installs.

**Tasks:**

| Task | Name | Issue | Status | Intent |
| --- | --- | --- | --- | --- |
| T4.1 | package-mac.sh + distribution.xml | _pending_ | _pending_ | Author `tools/scripts/package-mac.sh` (chmod +x) per IP-4: `pkgbuild --root` (dirname of `.app`) `--identifier studio.bacayo.territorydeveloper --version $VERSION --install-location /Applications` → component pkg, then `envsubst < tools/dist/mac/distribution.xml.template > /tmp/distribution.xml` + `productbuild --distribution /tmp/distribution.xml --package-path Dist` → `Dist/TerritoryDeveloper-$VERSION.pkg`. Commit `distribution.xml.template` alongside with `$VERSION` placeholder. |
| T4.2 | package-win.ps1 + Inno .iss | _pending_ | _pending_ | Author `tools/scripts/package-win.ps1` per IP-5 — calls `iscc.exe /DMyAppVersion=$Version /DMyAppPath=$BuildPath tools\dist\win\territory.iss`. Author `tools/dist/win/territory.iss` with `[Setup]` (`AppName=Territory Developer`, `AppVersion={#MyAppVersion}`, `DefaultDirName={autopf}\Territory Developer`, `OutputDir=..\..\..\Dist`, `OutputBaseFilename=TerritoryDeveloper-Setup-{#MyAppVersion}`, unsigned), `[Files]` section globbing `{#MyAppPath}\*`, default Inno wizard pages. |
| T4.3 | Wire packagers into build-release.sh | _pending_ | _pending_ | Edit `tools/scripts/build-release.sh` `build_mac` function — after Unity build succeeds, invoke `tools/scripts/package-mac.sh "Builds/mac/Territory.app" "$VERSION"`. Update the `build_win` command-hint block to show the powershell + package-win invocation. Add `--skip-package` debug flag per IP-1. |
| T4.4 | Smoke install dry-run on both OSes | _pending_ | _pending_ | Run `tools/scripts/build-release.sh --version 0.0.0-smoke-1 --platform mac` end-to-end; double-click the produced `.pkg` on a clean macOS test user + run through Gatekeeper right-click-Open. On the Windows machine, run the powershell packager + double-click the `.exe`; capture the SmartScreen "More info → Run anyway" path. Capture both flows + screenshots as inputs for the `/download` bypass copy (Stage 2.2) + skill (Stage 2.3). |

### Stage 5 — Unsigned packaging + `/download` publication + in-game notifier / `/download` web surface + latest.json

**Status:** Draft (tasks _pending_ — not yet filed)

**Objectives:** Land the Vercel `/download` page that lists artifacts + Gatekeeper/SmartScreen bypass steps, the `latest.json` manifest schema + asset, and the private-route disallow. Serve artifacts statically; set `Cache-Control: no-cache` on `latest.json` so testers see new versions without CDN lag.

**Exit:**

- `web/public/download/latest.json` matches Example B schema — `version`, `releasedAt`, `notes`, `downloads.{mac,win}.{url,size,sha256}`, `bypass.{mac,win}`.
- `web/app/download/page.tsx` Server Component reads `latest.json` at build time via `fs.readFile`, renders artifact table (platform, filename, size, SHA256) + bypass section anchors (`#gatekeeper`, `#smartscreen`).
- `web/content/pages/download.mdx` carries full-English bypass copy (Gatekeeper right-click-Open steps + SmartScreen "More info → Run anyway" steps) with inline screenshot slots — caveman-exception per `ia/rules/agent-output-caveman.md` §exceptions.
- `web/app/robots.ts` disallows `/download` — covered by an `if (private)` gate wired to a single env var or const so the MVP-ship flip is a one-liner.
- `web/vercel.json` `headers` config sets `Cache-Control: no-cache, must-revalidate` for `/download/latest.json`.
- `npm run validate:web` green; Vercel preview deploy via `npm run deploy:web:preview` loads `/download` correctly (with a placeholder `latest.json`).
- Phase 1 — Author latest.json schema + placeholder manifest committed.
- Phase 2 — Author `/download` page + MDX bypass copy + robots + cache header.
- Phase 3 — Preview-deploy validation.

**Tasks:**

| Task | Name | Issue | Status | Intent |
| --- | --- | --- | --- | --- |
| T5.1 | latest.json schema + placeholder | _pending_ | _pending_ | Author `web/public/download/latest.json` matching Design Expansion Example B verbatim. Seed with `version: "0.0.0-dev-placeholder"`, `releasedAt` = current UTC, `notes` = "Placeholder — not a shipped build.", `downloads.mac.url` + `downloads.win.url` pointing at `/download/` paths that will exist post-first-release, placeholder zeroed `size` + `sha256: "pending"`. Schema is the contract `UpdateNotifier` reads at Stage 2.3. |
| T5.2 | /download page RSC + artifact table | _pending_ | _pending_ | Author `web/app/download/page.tsx` Next.js Server Component: `const manifest = JSON.parse(await fs.readFile("web/public/download/latest.json", "utf8"))`, render version + releasedAt + notes heading, render a `<table>` row per platform (mac / win) with filename, size (formatted via existing `web/lib/` helper if present, else inline KB formatter), SHA256 (truncated 8+8), download link. Anchor links to `#gatekeeper` + `#smartscreen` bypass sections imported from `web/content/pages/download.mdx`. Backend-derives/frontend-renders pattern per `ia/rules/web-backend-logic.md`. |
| T5.3 | download.mdx bypass copy + robots + cache | _pending_ | _pending_ | Author `web/content/pages/download.mdx` with two sections: `## Gatekeeper (macOS)` step-by-step right-click-Open flow with screenshot placeholders, `## SmartScreen (Windows)` More-info-Run-anyway flow with screenshot placeholders — full English per caveman-exception. Edit `web/app/robots.ts` (create if missing) to `disallow: ["/download", "/download/*"]` gated on a `DOWNLOAD_PUBLIC` const default `false`. Edit `web/vercel.json` to add `{ "source": "/download/latest.json", "headers": [{ "key": "Cache-Control", "value": "no-cache, must-revalidate" }] }`. |
| T5.4 | Preview deploy + /download smoke | _pending_ | _pending_ | Run `npm run validate:web` + `npm run deploy:web:preview`. Load the preview `/download` URL — confirm artifact table renders from the placeholder manifest, bypass MDX renders, `curl -I {preview}/download/latest.json` shows `Cache-Control: no-cache`. Confirm Google prod site does NOT show `/download` (robots disallow). Note preview URL in the handoff for Stage 2.3 kickoff — the notifier fetches this URL during dev. |

### Stage 6 — Unsigned packaging + `/download` publication + in-game notifier / In-game UpdateNotifier + trainable release skill

**Status:** Draft (tasks _pending_ — not yet filed)

**Objectives:** Land the `UpdateNotifier` MonoBehaviour that polls `/download/latest.json` on launch + nudges testers when a newer version ships. Capture the full release process as a trainable skill under `ia/skills/distribution-release/` so any agent can drive a release cold. This is the final MVP-shipping piece of Bucket 10.

**Exit:**

- `Assets/Scripts/UI/Distribution/UpdateNotifier.cs` MonoBehaviour authored per Example C — `[SerializeField] private BuildInfo localBuildInfo;` + `[SerializeField] private ToastService toast;` with Inspector-wire + `Awake` `FindObjectOfType` fallback (invariant #4), coroutine fired once in `Start` (invariant #3 — not per-frame), `UnityWebRequest.Get` with 5s timeout, silent-fail on network error, `SemverCompare.Compare` gate, `Application.OpenURL` action.
- `Assets/Scripts/UI/Distribution/ReleaseManifest.cs` — `[System.Serializable]` DTO matching `latest.json` top-level fields consumed by `JsonUtility.FromJson`.
- Scene wire-up: `UpdateNotifier` + `BuildInfo` reference dropped onto the root UI canvas in the main scene (Inspector); no singleton.
- ToastService integration documented — if Bucket 6 `ToastService` primitive not yet shipped, fallback to existing `IUserPrompt` modal per Review notes; revisit once Bucket 6 lands.
- `ia/skills/distribution-release/SKILL.md` authored per IP-10 — preflight checklist, version-bump, build, package, artifact verification (smoke-install on clean users), upload, deploy, news-post coordination, feedback-window; follows `ia/skills/README.md` authoring conventions + existing skill shape refs.
- Full end-to-end dry-run — author bumps to `0.0.0-dry-2`, runs the skill cold, produces artifacts, deploys to a preview, launches a `0.0.0-dry-1`-built local app, sees the update toast + lands on the `/download` page.
- `npm run unity:compile-check` green; `npm run validate:all` green.
- Phase 1 — Author UpdateNotifier + ReleaseManifest DTO + scene wire-up.
- Phase 2 — Author trainable release skill.
- Phase 3 — End-to-end dry-run + skill-iteration loop.

**Tasks:**

| Task | Name | Issue | Status | Intent |
| --- | --- | --- | --- | --- |
| T6.1 | UpdateNotifier MonoBehaviour + DTO | _pending_ | _pending_ | Author `Assets/Scripts/UI/Distribution/UpdateNotifier.cs` matching Example C verbatim — private `const string ManifestUrl`, Inspector-wired `BuildInfo localBuildInfo` + `ToastService toast` with `Awake` fallbacks (`Resources.Load<BuildInfo>("BuildInfo")` + `FindObjectOfType<ToastService>()`, invariant #4), `Start()` → `StartCoroutine(CheckForUpdate())`, coroutine using `UnityWebRequest.Get` w/ `timeout = 5`, silent-fail on non-Success result, `JsonUtility.FromJson<ReleaseManifest>`, `SemverCompare.Compare` gate, `toast?.Show(...)` with `Application.OpenURL` action. Author `Assets/Scripts/UI/Distribution/ReleaseManifest.cs` `[System.Serializable]` DTO. Invariant #3 — coroutine fires once in `Start`, no per-frame work. |
| T6.2 | Scene wire-up + ToastService fallback | _pending_ | _pending_ | Drop `UpdateNotifier` onto the main-scene root UI canvas. Inspector-wire `localBuildInfo` → `Assets/Resources/BuildInfo.asset`. If Bucket 6 `ToastService` already exists in repo, wire directly; else leave `toast` null + rely on `Awake` `FindObjectOfType` fallback (harmless when ToastService lands later). Document fallback path in task Notes (revisit when Bucket 6 ships per Review notes). |
| T6.3 | ia/skills/distribution-release skill | _pending_ | _pending_ | Author `ia/skills/distribution-release/SKILL.md` per IP-10 — YAML frontmatter (purpose, audience agent, triggers "ship a release", "cut a tester build"), sections: Preflight checklist (clean git tree, Unity license, `$UNITY_EDITOR_PATH` set, Windows machine reachable), Version bump, `tools/scripts/build-release.sh` invocation, packaging, artifact verification (smoke-install on clean mac user + Windows VM), upload (copy artifacts + updated `latest.json` into `web/public/download/`, commit), deploy (`npm run deploy:web`), news-post coordination, feedback window. Follow `ia/skills/README.md` conventions. Cite Windows-VM fallback per Review notes. |
| T6.4 | Skill self-review + worked example | _pending_ | _pending_ | Add a "Worked example" section to `ia/skills/distribution-release/SKILL.md` that walks through shipping semver `0.1.0-beta.1` step-by-step with real command lines + expected output snippets (referencing T1.3.4 + T2.1.4 + T2.2.4 dry-run captures). Cross-link the skill from `ia/skills/README.md` index + from the umbrella Bucket 10 row. Validate frontmatter against existing skill conventions (`npm run validate:frontmatter`). |
| T6.5 | End-to-end release dry-run | _pending_ | _pending_ | Execute the distribution-release skill cold against semver `0.0.0-dry-2` — full build + package (mac side; win side via Windows machine if available, else documented fallback), deploy to Vercel preview, launch an older `0.0.0-dry-1` build locally, observe the UpdateNotifier toast firing, click through → lands on `/download` preview page. Capture timing + any skill-step friction. |
| T6.6 | Fold dry-run friction back into skill | _pending_ | _pending_ | Edit `ia/skills/distribution-release/SKILL.md` based on T2.3.5 friction log — tighten unclear steps, add missing preflight checks, update command snippets with observed variants. Run `npm run validate:all` + `npm run unity:compile-check` final gate. Handoff note for Bucket 10 close rollup into umbrella `full-game-mvp-master-plan.md`. |

---

## Orchestration guardrails

**Do:**

- Open one stage at a time. Next stage opens only after current stage's `project-stage-close` runs.
- Run `claude-personal "/stage-file ia/projects/distribution-master-plan.md Stage {N}.{M}"` to materialize pending tasks → BACKLOG rows + `ia/projects/{ISSUE_ID}.md` stubs.
- Update stage / step `Status` + phase checkboxes as lifecycle skills flip them — do NOT edit by hand.
- Preserve locked decisions (see header block). Changes require explicit re-decision + sync edit to `docs/distribution-exploration.md` + umbrella `full-game-mvp-master-plan.md` Bucket 10 row.
- Keep this orchestrator synced with umbrella — on Bucket 10 final-stage close, flip umbrella Bucket 10 row per `project-spec-close` umbrella-sync rule.
- When ToastService from Bucket 6 lands, revisit T2.3.2 wire-up per Review notes carry-over.

**Do not:**

- Close this orchestrator via `/closeout` — orchestrators are permanent (see `ia/rules/orchestrator-vs-spec.md`). Only the terminal stage (2.3) landing triggers a final `Status: Final`; the file stays.
- Silently promote post-MVP items (signing, Linux, WebGL, patch deltas, Steam, public itch) into MVP stages — they belong in a future extensions doc.
- Merge partial stage state — every stage must land on a green bar (`npm run unity:compile-check` + `npm run validate:all` + EditMode tests where applicable).
- Insert BACKLOG rows directly into this doc — only `stage-file` materializes them.
- Sign artifacts in MVP scope — unsigned tier is locked. Signing work triggers a scope-boundary extension, not an in-plan change.

---
