# Distribution exploration — Territory Developer full-game MVP Bucket 10

> **Status:** Exploration — ready for `/master-plan-new` after Design Expansion review.
>
> **Upstream:** `ia/projects/full-game-mvp-master-plan.md` §Distribution gating (Tier E), §Tier E lane, Bucket 10 row.
>
> **Downstream dependents:** `ia/projects/web-platform-master-plan.md` (`/download` page surface), Bucket 9 Step 7 (private download page + feedback form), Credits screen consumer in main game UI.
>
> **Scope boundary:** Unity build pipeline + unsigned packaging + versioning manifest + `/download` publication + in-game update notifier. Everything else (signing, patch-channel auto-delivery, Linux, WebGL, Steam, itch public) is explicitly deferred per umbrella Hard deferrals list.

---

## 1. Problem statement

Territory Developer needs a repeatable way to deliver playable Unity builds (macOS `.app` + Windows `.exe`) to a curated 20–50 dev-savvy tester audience, gated behind a private `/download` page on the Vercel-hosted web site, with a version manifest that testers see in-game (Credits screen) and a lightweight notification hook that tells testers when a new release exists. No signing, no auto-update, no patch deltas — one version at a time, manual re-download.

Hard constraints (locked from umbrella `full-game-mvp-master-plan.md` §Distribution gating, confirmed this session):

- **Signing tier:** Free (unsigned). macOS unsigned `.app` inside `.pkg` installer. Windows unsigned `.exe` installer wizard. Gatekeeper + SmartScreen bypass documented on `/download` page. No notarization, no code-sign scripts.
- **Platforms:** macOS + Windows only. No Linux. No WebGL.
- **Audience:** 20–50 curated dev-savvy testers. Trust model = friends-of-project; warnings acceptable.
- **Install experience:** proper installer for both platforms — `.pkg` on macOS, `.exe` installer wizard on Windows. No zip/portable fallback in MVP scope.
- **Versioning:** semver (e.g. `0.1.0-beta.1`) written to build metadata + a `BuildInfo` ScriptableObject consumed by the in-game Credits screen.
- **Web surface:** Vercel `/download` page (new — not the existing `/install` marketing page). Private until MVP ships — `robots.txt` disallow + unlinked route. Made public on MVP release.
- **Update delivery:** in-game notification that links out to the `/download` page, coordinated with a news post on the web site. No binary patch channel. Testers manually re-download when a new version ships.
- **Access control:** direct link sharing only. No token gate, no password, no signed URL — curated 20–50 audience does not warrant auth complexity.
- **Release model:** single lane. One version everyone is on. No stable/preview split. MVP ships → gather feedback → cut next release → repeat.
- **Build trigger:** manual script invocation. No CI auto-build. A trainable skill captures the process so it is reproducible across agents + future-Javier.

Open questions resolved during the pre-Design-Expansion interview:

- **Q1 — Install experience:** proper installers both OSes. Locked.
- **Q2 — Update delivery:** in-game notice → `/download` link, news post coordination. No binary patches. Locked.
- **Q3 — Access control:** private page until public MVP release; no tokens. Locked.
- **Q4 — Build trigger:** manual script + trainable skill to encode the process. Locked.
- **Q5 — Release model:** single lane, release → feedback → next release. Locked.

---

## 2. Approaches surveyed

### Approach A — Simple zip/archive distribution

Unity editor menu → `Build Settings` → platform target → output folder. Zip the folder manually. Drop zip onto `/download` page as a raw asset link. Tester unzips + runs from extracted folder.

**Pros:** zero tooling cost; works today with stock Unity; no installer authoring.

**Cons:** no install experience (testers drag loose folder); macOS quarantine flag more hostile on raw `.app` copy vs `.pkg`; no version manifest automation — relies on human remembering to bump `PlayerSettings.bundleVersion`; no repeatability — every build is a manual point-and-click sequence; no captured process for agents to reproduce; does not meet locked "proper installer" constraint.

**Effort:** 1 day.

### Approach B — Manual-script build + platform-native installers (selected)

Author a Unity build script (editor-triggered via batch-mode CLI) that compiles both macOS `.app` and Windows `.exe` from a single invocation, stamps a semver version manifest into build metadata + a `BuildInfo` ScriptableObject, and shells out to platform-native packaging tools (`pkgbuild` / `productbuild` on macOS; Inno Setup or NSIS on Windows) to produce the final `.pkg` + `.exe` installer artifacts. A trainable skill encodes the process end-to-end so any agent can re-run it. Artifacts uploaded by hand to the Vercel `/download` page content directory. In-game notifier fetches a small JSON manifest from `/download/latest.json` (hosted alongside `/download` page) and surfaces a "new version available" toast linking to the page.

**Pros:** meets all locked constraints (proper installers, semver manifest, in-game notifier, single release lane, manual-trigger with captured process); platform-native installer experience (double-click `.pkg` / run `.exe` wizard); trainable skill makes the process reproducible; no recurring infra cost; BuildInfo SO pattern already idiomatic in Territory Developer codebase; keeps signing deferral clean — if tier upgrades (Apple-only $99, etc.), only the signing sub-phase of the script changes.

**Cons:** higher upfront authoring cost than Approach A (build script + installer configs + skill authoring); two platform-specific packaging toolchains to learn (`pkgbuild` + Inno Setup); macOS packaging requires a Mac to run (Windows side can cross-compile from Mac with Unity + Wine-free Inno Setup is a cross-compile complication — acceptable since Javier owns a Mac and can run Windows packaging separately via a Windows VM or parallel Windows machine; interview answer accepted this constraint implicitly via "manual script" trigger).

**Effort:** 4–6 days across the 2 umbrella Steps (Step 1 pipeline + manifest; Step 2 packaging + publication + notifier).

### Approach C — CI-automated pipeline (GitHub Actions / Unity Cloud Build)

Configure GitHub Actions (or Unity Cloud Build) to trigger a Unity build on push to a release tag, package artifacts, and auto-upload to `/download`. Version manifest written from the tag.

**Pros:** maximal repeatability; no human in the loop after tag push; artifacts archive automatically; release cadence can accelerate.

**Cons:** Unity license activation in CI is a known pain point (requires GitHub secrets + `game-ci/unity-builder` action + license manifest); macOS runners on GitHub Actions are costly (~10x Linux minute cost); overkill for 20–50 curated testers on a release cadence of "when there is something to show"; obscures the build process — harder to debug when something fails; interview answer explicitly chose manual trigger + trainable skill over CI automation to keep cognitive overhead low during MVP iteration.

**Effort:** 6–10 days initial; ongoing CI-config maintenance.

---

## 3. Recommendation

**Approach B — Manual-script build + platform-native installers.** Matches every locked constraint, captures process as a trainable skill (reusable across agents), avoids CI complexity that interview explicitly declined, and preserves clean upgrade paths if signing tier escalates post-MVP.

---

## 4. Open questions (for review pass)

- Inno Setup vs NSIS for the Windows installer wizard — both free, both unsigned-compatible. Inno Setup has simpler scripting (`.iss` files), NSIS has more flexibility. Default recommendation: **Inno Setup** for MVP; revisit if tester feedback flags installer-wizard polish as insufficient.
- `BuildInfo` ScriptableObject location — proposed `Assets/Resources/BuildInfo.asset` so it is loadable via `Resources.Load<BuildInfo>("BuildInfo")` without an Inspector reference. Alternative: Addressables-loaded. Resources-loaded simpler for MVP.
- In-game notifier check cadence — proposed: on game launch only. Avoids polling + avoids interrupting play sessions. Revisit if testers ask for play-session notification.
- `/download/latest.json` schema — proposed minimal `{ "version": "0.1.0-beta.1", "releasedAt": "2026-05-15T00:00:00Z", "notes": "one-line changelog" }`. Hosted as a static file under `web/public/download/latest.json`.

---

## Design Expansion

### Chosen approach

**Approach B — Manual-script build + platform-native installers.** Unity batch-mode CLI invocation runs a single editor script that builds both macOS `.app` + Windows `.exe` targets, stamps semver into `Application.version` + `BuildInfo` ScriptableObject, and invokes platform-native packagers (`pkgbuild` + `productbuild` for macOS `.pkg`, Inno Setup for Windows `.exe` installer). Output artifacts + a `latest.json` manifest are hand-uploaded to `web/public/download/` in the Vercel workspace, served under `/download/`. In-game notifier fetches `/download/latest.json` on game launch and surfaces a toast with a link to the `/download` page when a newer semver is seen. The whole end-to-end flow is captured as a trainable skill under `ia/skills/distribution-release/SKILL.md` so any agent can drive a release cold.

### Architecture

Four concerns, four components:

1. **Build orchestration layer** — `tools/scripts/build-release.sh` (new). Bash entry point. Parses semver arg (`--version 0.1.0-beta.1`), derives git SHA, invokes Unity in batch mode twice (one per platform target), and hands off to the packagers. Runs on the release machine (macOS primary; Windows packaging step may shell out to a separate invocation on a Windows machine when needed).
2. **Unity editor build script** — `Assets/Editor/ReleaseBuilder.cs` (new). Called by Unity batch mode (`-executeMethod ReleaseBuilder.BuildMac` / `BuildWindows`). Reads semver from CLI arg (`-buildVersion`), writes `PlayerSettings.bundleVersion`, generates `Assets/Resources/BuildInfo.asset` (or updates it) with version + git SHA + build timestamp, invokes `BuildPipeline.BuildPlayer` with StandaloneOSX / StandaloneWindows64 target. Editor-only C# — no runtime impact.
3. **Packaging layer** — `tools/scripts/package-mac.sh` + `tools/scripts/package-win.ps1` (new). `package-mac.sh` takes the built `.app` path + version, runs `pkgbuild` + `productbuild` to emit `TerritoryDeveloper-{version}.pkg`. `package-win.ps1` takes the built `.exe` folder + version, invokes Inno Setup compiler (`iscc.exe`) against a version-templated `.iss` script to emit `TerritoryDeveloper-Setup-{version}.exe`.
4. **In-game update notifier** — `Assets/Scripts/UI/Distribution/UpdateNotifier.cs` (new MonoBehaviour, scene component under a root UI manager). Fires once on `Start()`. Uses `UnityWebRequest` to GET `https://{vercel-host}/download/latest.json`. Compares returned `version` to local `BuildInfo.Version`. If remote is newer (semver compare), enqueues a toast / modal with text "New version available — visit /download" and a button that calls `Application.OpenURL("https://{vercel-host}/download")`. Silent failure on network error (no tester sees errors).

Data flow (release → tester):

```
Javier @ Mac
   │
   ▼
tools/scripts/build-release.sh --version 0.1.0-beta.1
   │
   ├─► Unity -batchmode -executeMethod ReleaseBuilder.BuildMac
   │      └─► writes Assets/Resources/BuildInfo.asset + Builds/mac/Territory.app
   │
   ├─► tools/scripts/package-mac.sh Builds/mac/Territory.app 0.1.0-beta.1
   │      └─► Dist/TerritoryDeveloper-0.1.0-beta.1.pkg
   │
   ├─► (on Windows machine) Unity -batchmode -executeMethod ReleaseBuilder.BuildWindows
   │      └─► Builds/win/Territory.exe + _Data/
   │
   ├─► (on Windows machine) tools/scripts/package-win.ps1 Builds/win 0.1.0-beta.1
   │      └─► Dist/TerritoryDeveloper-Setup-0.1.0-beta.1.exe
   │
   └─► manual copy: Dist/*.pkg + Dist/*.exe + latest.json → web/public/download/
          │
          ▼
     git commit + push → Vercel deploy
          │
          ▼
     /download page lists new artifacts; /download/latest.json served statically
          │
          ▼
     Tester launches existing installed build
          │
          ▼
     UpdateNotifier fetches /download/latest.json on Start()
          │
          ▼
     Toast: "New version available" → Application.OpenURL("/download")
          │
          ▼
     Tester downloads .pkg or .exe, runs installer wizard, launches new version
```

### Subsystem impact

| Subsystem | Touch | Invariant / rule flags |
|---|---|---|
| Unity editor tooling (`Assets/Editor/`) | New `ReleaseBuilder.cs` — editor-only, no runtime code path | None — editor-only code is outside runtime invariant scope |
| Unity runtime — UI (`Assets/Scripts/UI/`) | New `UpdateNotifier.cs` MonoBehaviour | Invariant #3 (no `FindObjectOfType` in Update) — notifier fires once in `Start()`, not per-frame; Invariant #4 (no singletons) — Inspector scene component pattern |
| Unity runtime — data assets (`Assets/Resources/`) | New `BuildInfo.asset` ScriptableObject | None — standard SO pattern |
| Unity runtime — Credits screen | Add reference to `BuildInfo` to display version string | None — read-only consumer |
| Build tooling (`tools/scripts/`) | New `build-release.sh`, `package-mac.sh`, `package-win.ps1` | None — shell-only; governed by `ia/rules/agent-output-caveman-authoring.md` for any inline caveman comments |
| Web workspace (`web/`) | New `/download` page (`web/app/download/page.tsx` + `web/content/pages/download.mdx`), new static assets under `web/public/download/` (`latest.json`, `.pkg`, `.exe` binaries), `robots.ts` disallow for `/download` until public release, Bucket 9 Step 7 already plans feedback-form integration here | Caveman-exception — user-facing MDX prose in full English per `ia/rules/agent-output-caveman.md` §exceptions |
| IA — skills | New `ia/skills/distribution-release/SKILL.md` to capture manual-trigger process (trainable by agents) | None — standard skill authoring |
| IA — glossary | New terms: **BuildInfo ScriptableObject**, **Release manifest (`latest.json`)**, **Update notifier**, **Unsigned installer tier**. Register in `ia/specs/glossary.md` as Bucket 10 lands | Invariant #12 — reference specs under `ia/specs/`; project spec stays under `ia/projects/` |
| Invariants file | No new invariants for MVP. Post-beta when signing tier escalates, add a "signed-tier" invariant gating notarization + Gatekeeper friendly launch | — |

No touched subsystem spec missing from MCP — all impacts are net-new tooling or new runtime components that do not intersect existing spec-governed domains (roads, water, grid math, persistence).

### Implementation points

**IP-1 — Build orchestration entry point.** `tools/scripts/build-release.sh` accepts `--version` (required, validated as semver), `--platform {mac|win|all}` (default `all`), `--skip-package` (debug flag). Resolves git SHA via `git rev-parse --short HEAD`. Exports env vars consumed by the Unity editor script (`BUILD_VERSION`, `BUILD_SHA`, `BUILD_TIMESTAMP`). Invokes Unity via `$UNITY_EDITOR_PATH` (already conventional in this repo — see `package.json` `unity:compile-check` script for precedent). Fails fast on missing dependencies (`pkgbuild`, `productbuild`, `iscc.exe`).

**IP-2 — Unity editor build script.** `Assets/Editor/ReleaseBuilder.cs` exposes two public static methods (`BuildMac`, `BuildWindows`) called via Unity `-executeMethod`. Reads env vars via `System.Environment.GetEnvironmentVariable`. Calls a helper `UpdateBuildInfoAsset(version, sha, timestamp)` that loads (or creates) `Assets/Resources/BuildInfo.asset` via `AssetDatabase`, writes fields, `EditorUtility.SetDirty` + `AssetDatabase.SaveAssets`. Then `PlayerSettings.bundleVersion = version` + `BuildPipeline.BuildPlayer(...)` with the right target. Fails the Unity process with non-zero exit on any step error so the shell script propagates failure.

**IP-3 — BuildInfo ScriptableObject schema.**

```csharp
// Assets/Scripts/Runtime/Distribution/BuildInfo.cs
[CreateAssetMenu(fileName = "BuildInfo", menuName = "Territory/BuildInfo")]
public class BuildInfo : ScriptableObject
{
    [SerializeField] private string version = "0.0.0-dev";
    [SerializeField] private string gitSha = "unknown";
    [SerializeField] private string buildTimestamp = "unknown";

    public string Version => version;
    public string GitSha => gitSha;
    public string BuildTimestamp => buildTimestamp;

    // Editor-only writer — called from Assets/Editor/ReleaseBuilder.cs
#if UNITY_EDITOR
    public void WriteFields(string v, string sha, string ts)
    {
        version = v; gitSha = sha; buildTimestamp = ts;
    }
#endif
}
```

Loaded at runtime via `Resources.Load<BuildInfo>("BuildInfo")`. Credits screen + UpdateNotifier both consume this.

**IP-4 — macOS packaging.** `tools/scripts/package-mac.sh` runs:

```bash
pkgbuild --root "$APP_PATH/.." \
         --identifier "studio.bacayo.territorydeveloper" \
         --version "$VERSION" \
         --install-location "/Applications" \
         "Dist/TerritoryDeveloper-component-$VERSION.pkg"
productbuild --distribution distribution.xml \
             --package-path Dist \
             "Dist/TerritoryDeveloper-$VERSION.pkg"
```

`distribution.xml` templated in `tools/dist/mac/distribution.xml.template` with `$VERSION` placeholder. No signing flags. Result is unsigned — Gatekeeper on tester's Mac will require right-click → Open first time, documented on `/download` page.

**IP-5 — Windows packaging.** `tools/scripts/package-win.ps1` invokes Inno Setup compiler:

```powershell
& "C:\Program Files (x86)\Inno Setup 6\ISCC.exe" `
    /DMyAppVersion=$Version `
    /DMyAppPath=$BuildPath `
    "tools\dist\win\territory.iss"
```

`territory.iss` authored once, parameterized on `MyAppVersion` + `MyAppPath`. Emits `TerritoryDeveloper-Setup-$Version.exe` in `Dist/`. Unsigned — SmartScreen will show "Unrecognized app" warning, documented on `/download` page (More info → Run anyway).

**IP-6 — Release manifest (`latest.json`).** Committed alongside binaries under `web/public/download/latest.json`:

```json
{
  "version": "0.1.0-beta.1",
  "releasedAt": "2026-05-15T00:00:00Z",
  "notes": "First closed beta — city sim loop + save/load + CityStats dashboard.",
  "downloads": {
    "mac": "/download/TerritoryDeveloper-0.1.0-beta.1.pkg",
    "win": "/download/TerritoryDeveloper-Setup-0.1.0-beta.1.exe"
  }
}
```

Served statically by Vercel; in-game notifier fetches this URL directly.

**IP-7 — In-game update notifier.** `Assets/Scripts/UI/Distribution/UpdateNotifier.cs`:

- MonoBehaviour scene component sitting under a root UI manager GameObject.
- `[SerializeField] private BuildInfo localBuildInfo;` Inspector-wired; `Awake()` falls back to `Resources.Load<BuildInfo>("BuildInfo")` if unset (per invariant #4 pattern).
- `Start()` kicks off a coroutine that `UnityWebRequest.Get("https://{vercel-host}/download/latest.json")`. Timeout 5s. Silent fail on error (testers do not care about offline-launch UX warnings).
- Parses JSON via `JsonUtility.FromJson<ReleaseManifest>`. Compares `remoteVersion` to `localBuildInfo.Version` using a tiny semver compare helper (see IP-8).
- On newer remote: enqueues a one-shot toast + modal via existing UI surface (TBD — resolves at kickoff; Bucket 6 UI work provides the toast primitive).
- Modal action button: `Application.OpenURL("https://{vercel-host}/download")`.

**IP-8 — Semver compare helper.** `Assets/Scripts/Runtime/Distribution/SemverCompare.cs`. Pure function `int Compare(string a, string b)` returning `-1 / 0 / +1`. Parses `MAJOR.MINOR.PATCH[-PRERELEASE]` — only the subset we use. Unit-coverable (test-mode friendly). Not a full semver library (avoids dependency bloat).

**IP-9 — `/download` web surface.** `web/app/download/page.tsx` RSC reads `web/public/download/latest.json` at build time (via `fs.readFile`) and renders a table of platform + artifact link + SHA/size, plus the Gatekeeper/SmartScreen bypass copy (screenshots + step-by-step). `web/content/pages/download.mdx` carries the bypass prose (full English — caveman-exception). `robots.ts` disallow until public release; remove disallow on MVP ship. No auth gate (access via direct link sharing).

**IP-10 — Trainable skill.** `ia/skills/distribution-release/SKILL.md` encodes the end-to-end process: preflight checklist (clean git tree, Unity license activated, `$UNITY_EDITOR_PATH` set, Windows machine reachable for the Win packaging step), version bump procedure, build invocation, packaging invocation, artifact verification (smoke-launch the `.pkg` install on a clean macOS user + smoke-launch the `.exe` on a Windows VM), upload procedure, `/download` page publication, in-game news post coordination, post-release feedback-collection window. Follows existing skill authoring conventions under `ia/skills/README.md`.

### Examples

**Example A — Build script outline** (`tools/scripts/build-release.sh`):

```bash
#!/usr/bin/env bash
set -euo pipefail

VERSION="${1:?semver required, e.g. 0.1.0-beta.1}"
PLATFORM="${2:-all}"

# semver regex validation
[[ "$VERSION" =~ ^[0-9]+\.[0-9]+\.[0-9]+(-[A-Za-z0-9.-]+)?$ ]] \
  || { echo "invalid semver: $VERSION" >&2; exit 1; }

export BUILD_VERSION="$VERSION"
export BUILD_SHA="$(git rev-parse --short HEAD)"
export BUILD_TIMESTAMP="$(date -u +%Y-%m-%dT%H:%M:%SZ)"

UNITY="${UNITY_EDITOR_PATH:?set UNITY_EDITOR_PATH via .env}"
PROJECT="$(pwd)"

build_mac() {
  "$UNITY" -batchmode -nographics -quit \
    -projectPath "$PROJECT" \
    -executeMethod ReleaseBuilder.BuildMac \
    -logFile "Logs/build-mac-$VERSION.log"
  tools/scripts/package-mac.sh "Builds/mac/Territory.app" "$VERSION"
}

build_win() {
  # runs on the Windows machine; mirrors build_mac but invokes BuildWindows + package-win.ps1
  echo "run this block on the Windows machine:"
  echo "  $UNITY -batchmode -executeMethod ReleaseBuilder.BuildWindows"
  echo "  tools/scripts/package-win.ps1 Builds\\win $VERSION"
}

case "$PLATFORM" in
  mac) build_mac ;;
  win) build_win ;;
  all) build_mac; build_win ;;
  *) echo "unknown platform: $PLATFORM" >&2; exit 1 ;;
esac

echo "built $VERSION — artifacts in Dist/"
```

**Example B — Version manifest shape** (`web/public/download/latest.json` — committed to the Vercel workspace):

```json
{
  "version": "0.1.0-beta.1",
  "releasedAt": "2026-05-15T00:00:00Z",
  "notes": "Closed beta — full game loop end-to-end. Feedback form linked from /download.",
  "downloads": {
    "mac": {
      "url": "/download/TerritoryDeveloper-0.1.0-beta.1.pkg",
      "size": 287309184,
      "sha256": "abc123..."
    },
    "win": {
      "url": "/download/TerritoryDeveloper-Setup-0.1.0-beta.1.exe",
      "size": 312456789,
      "sha256": "def456..."
    }
  },
  "bypass": {
    "mac": "/download#gatekeeper",
    "win": "/download#smartscreen"
  }
}
```

**Example C — In-game notifier hook** (`Assets/Scripts/UI/Distribution/UpdateNotifier.cs`):

```csharp
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class UpdateNotifier : MonoBehaviour
{
    private const string ManifestUrl = "https://territory.bacayo.studio/download/latest.json";

    [SerializeField] private BuildInfo localBuildInfo;
    [SerializeField] private ToastService toast; // Bucket 6 UI primitive — Inspector-wired

    private void Awake()
    {
        if (localBuildInfo == null)
            localBuildInfo = Resources.Load<BuildInfo>("BuildInfo");
        if (toast == null)
            toast = FindObjectOfType<ToastService>(); // invariant #4 fallback
    }

    private void Start() => StartCoroutine(CheckForUpdate());

    private IEnumerator CheckForUpdate()
    {
        using var req = UnityWebRequest.Get(ManifestUrl);
        req.timeout = 5;
        yield return req.SendWebRequest();
        if (req.result != UnityWebRequest.Result.Success) yield break;

        var manifest = JsonUtility.FromJson<ReleaseManifest>(req.downloadHandler.text);
        if (manifest == null || string.IsNullOrEmpty(manifest.version)) yield break;

        if (SemverCompare.Compare(manifest.version, localBuildInfo.Version) > 0)
        {
            toast?.Show(
                title: "New version available",
                body: $"Territory {manifest.version} ready on /download.",
                action: () => Application.OpenURL("https://territory.bacayo.studio/download")
            );
        }
    }

    [System.Serializable]
    private class ReleaseManifest
    {
        public string version;
        public string releasedAt;
        public string notes;
    }
}
```

### Review notes

No BLOCKING items from self-review. NON-BLOCKING follow-ups (carry into orchestrator Step-authoring discussions, not gates):

- **ToastService dependency** — Bucket 6 UI work must land (or at least stub) a `ToastService` primitive before UpdateNotifier can Inspector-wire it. If Bucket 6 slips, fall back to a cheap `GameObject.CreatePrimitive`-free debug log + modal dialog via existing `IUserPrompt` surface, revisit when Bucket 6 ships.
- **Windows build machine availability** — Approach B assumes a Windows machine is reachable for the Win packaging step. If Javier does not own Windows hardware at release time, fallback: run the Windows build + packaging inside a Parallels / UTM / remote Windows VM. Document the fallback in the trainable skill.
- **`latest.json` cache** — Vercel CDN might cache `/download/latest.json` for minutes after a push. Set `Cache-Control: no-cache` header via `web/public/download/_headers` (or `vercel.json` headers config) so testers see the new version immediately. Resolve at `/download` page kickoff.
- **Signing tier upgrade path** — when tester feedback triggers the $99 Apple-only tier, only `package-mac.sh` changes: add `productsign` + `xcrun notarytool submit` steps after `productbuild`. Script + skill scaffold absorbs this cleanly without structural rework.
- **Glossary rows** — add **BuildInfo ScriptableObject**, **Release manifest**, **Update notifier**, **Unsigned installer tier** to `ia/specs/glossary.md` during Bucket 10 Step 1 Stage 1.1 (or whenever the first task that names them files).

### Expansion metadata

- **Date:** 2026-04-18
- **Model:** claude-opus-4-7
- **Approach selected:** B — Manual-script build + platform-native installers
- **Blocking items resolved:** 0
- **Non-blocking carried to Review notes:** 5
