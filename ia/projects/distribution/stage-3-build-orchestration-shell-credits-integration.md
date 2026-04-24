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
