---
purpose: "Componentization strategy for the large-file atomization sweep — locked for all Stage 2..N moves."
audience: agent
loaded_by: on-demand
---

# Large-file atomization — componentization strategy

> **Status: LOCKED.** Do not amend without a new stage plan task. Stage 1.1 owns this lock.
> Cross-ref: `docs/explorations/large-file-atomization-refactor.md §Chosen Approach`.

## §Strategy γ rationale

Strategy γ = service-extraction + mandatory facade interface + per-domain folder + per-domain asmdef. Selected over:

- α (partial-class only) — preserves mega-class shape; no testability gain; no asmdef boundary.
- β (per-concern subfolder) — clean file boundary, no facade contract; consumers still bind to concrete.
- δ (hybrid by tier) — three parallel patterns = methodology drift, harder skill-train.

γ delivers: full testability, DI-ready services, asmdef-enforceable concern boundaries, aligns with invariant #5 `*Service.cs` carve-out.

**Partial-class α preserved only** for trivial concern splits where service extraction over-engineers (e.g. `UIManager.Theme.cs` if all concerns map cleanly to partials and no external consumer calls service methods directly).

## §Folder shape

```
Assets/Scripts/Domains/{X}/
  I{X}.cs                  // public facade interface — mandatory
  {X}.cs                   // facade impl, MonoBehaviour, thin orchestrator
  Services/
    {Concern}Service.cs    // POCO services, one per concern — no MonoBehaviour
    ...
  Data/                    // POCO / struct DTOs, enums shared within domain
  Editor/                  // Editor-only helpers (sub-asmdef Domains/{X}/Editor/{X}.Editor.asmdef)
  Tests/                   // EditMode + PlayMode tests (or at Assets/Tests/EditMode/Atomization/{StageSlug}/)
  {X}.asmdef               // one asmdef per domain
```

Cross-domain refs flow through facade interfaces only — no direct Manager-to-Manager dependency.

## §Naming rules

- Facade interface: `I{X}.cs` — `public interface I{X}`.
- Facade impl: `{X}.cs` — MonoBehaviour, holds `GridManager` or legacy manager ref during migration, thin.
- Services: `{Concern}Service.cs` — POCO class, instantiated by facade impl, holds composition ref to facade MonoBehaviour.
- Data: `{Concern}Data.cs` or `{Concern}Dto.cs` — no MonoBehaviour.
- Namespace: `Domains.{X}` (facade + interface) / `Domains.{X}.Services` (services) / `Domains.{X}.Data` (data).

## §Asmdef boundary rule

One asmdef per domain folder: `Assets/Scripts/Domains/{X}/{X}.asmdef`. Rules:
- References Unity engine + UI essentials only. No cross-domain runtime asmdef refs — consumers depend on the facade interface type, not the concrete impl.
- Editor sub-asmdef: `Assets/Scripts/Domains/{X}/Editor/{X}.Editor.asmdef` references `{X}.asmdef` + Editor assemblies.
- Legacy `TerritoryDeveloper.Game.asmdef` kept in place during migration; domain asmdefs coexist until full migration complete.

## §Anti-patterns + escape-hatch grammar

**Anti-patterns (never do):**
- Instantiate a service via `new` from a consumer outside its domain — pass through facade.
- Add a `[SerializeField]` to a POCO service — services are not scene components.
- Reference `cellArray` / `gridArray` from a service without the invariant #5 carve-out doc comment.
- Add a `using` import from another domain's concrete `Services/` namespace.
- Let a facade impl grow beyond thin orchestration — extract new `{Concern}Service.cs` at first sign.

**Escape-hatch grammar** (suppresses lint error, logs warning):
```csharp
// long-method-allowed: {reason}
public void SomeLongMethod(...)
{
    // reason must be non-empty; reviewer approves at PR stage
}
```

Escape-hatch suppresses the 80-LOC method hard-cap error; still emits a warn. Same grammar for file-LOC (`// long-file-allowed: {reason}` at top of file).
