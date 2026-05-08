# Mission

Atomize one C# mega-file per Strategy γ (`docs/large-file-atomization-componentization-strategy.md`). Stage-scoped: one file per invocation. Hand off to `/ship-cycle` after validators green.

# Phase sequence

1. **Phase 1 — Read + LOC** — Read target file. Count LOC. Identify public API surface (public methods + properties). Determine domain name `{X}` from class name or `--domain` arg.

2. **Phase 2 — Derive concerns + sub-stage count** — Group methods/properties into concerns (≤5 concerns per sub-stage). Apply threshold table:
   - ≤ 2500 LOC → 1 sub-stage
   - > 2500 ≤ 3500 LOC → 2 sub-stages
   - > 3500 LOC → 3 sub-stages
   If `--force-substages N` given, override threshold.

3. **Phase 3 — Seed folder** — Create `Assets/Scripts/Domains/{X}/` with:
   - `I{X}.cs` — public interface, all extracted public methods declared.
   - `{X}.cs` — MonoBehaviour facade impl, holds composition ref to legacy manager, thin orchestrator.
   - `{X}.asmdef` — no legacy Managers/ ref.
   - `Editor/{X}.Editor.asmdef` — Editor sub-asmdef, references `{X}.asmdef`.
   Run `validate:domain-facades` to confirm facade detected.

4. **Phase 4 — Extract services loop** — For each concern in this sub-stage:
   - `git mv {source}.cs Assets/Scripts/Domains/{X}/Services/{Concern}Service.cs`
   - `git mv {source}.cs.meta Assets/Scripts/Domains/{X}/Services/{Concern}Service.cs.meta`
   - Update namespace to `Domains.{X}.Services`.
   - Add `// long-method-allowed: {reason}` escape-hatch where needed.
   - Wire service reference into facade impl.
   Run `unity:compile-check` after each service move.

5. **Phase 5 — Extend composed test** — Add `[Test]` methods to `Assets/Tests/EditMode/Atomization/{stage-slug}/{X}AtomizationTests.cs`:
   - Assembly name assertion: `typeof(Domains.{X}.Services.{Concern}Service).Assembly.GetName().Name == "{X}"`
   - Behavior parity: key public method returns expected value for known input.

6. **Phase 6 — Verify** — Run in order:
   - `npm run validate:all`
   - `npm run lint:csharp`
   - `npm run validate:domain-facades`
   - `npm run unity:compile-check`
   All must exit 0. On failure: fix inline, re-run.

7. **Phase 7 — Hand off** — Output: list of files created/moved + test results + next step `/ship-cycle {SLUG} {STAGE_ID}`.

# Boundary markers (when called from ship-cycle)

```
<!-- TASK:{ISSUE_ID} START -->
[all file creates + git mv ops]
<!-- TASK:{ISSUE_ID} END -->
```

# Hard limits

- One domain per invocation.
- Do NOT commit — ship-cycle owns the single stage commit.
- Do NOT cross sub-stage boundary — stop at the concern count for this sub-stage.
- Do NOT add runtime deps from domain asmdef to legacy `TerritoryDeveloper.Game.asmdef`.
