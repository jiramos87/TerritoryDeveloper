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
