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
