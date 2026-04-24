### Stage 3 — ThemedPrimitive ring / IThemed + ThemedPrimitiveBase

**Status:** Draft (tasks _pending_ — not yet filed)

**Objectives:** Finalize `IThemed` contract + `ThemedPrimitiveBase` with `Awake`-cached theme resolution. Covers invariant #3 + #4 foundation pattern every downstream primitive inherits.

**Exit:**

- `Assets/Scripts/UI/Primitives/IThemed.cs` — full interface with `void ApplyTheme(UiTheme)` + XML doc.
- `Assets/Scripts/UI/Primitives/ThemedPrimitiveBase.cs` — `abstract class ThemedPrimitiveBase : MonoBehaviour, IThemed` with `[SerializeField] protected UiTheme theme` + `Awake`-cached `FindObjectOfType<UiTheme>()` fallback + `ApplyTheme(theme)` call.
- EditMode coverage — base class `Awake` binding path + fallback path.
- Phase 1 — Contract + base class.
- Phase 2 — Base class tests + glossary row.

**Tasks:**

| Task | Name | Issue | Status | Intent |
| --- | --- | --- | --- | --- |
| T3.1 | IThemed interface (finalize) | _pending_ | _pending_ | Promote Stage 1.2 stub `IThemed.cs` to final form. Add XML doc citing token swap lifecycle + `Awake`-cache rule + Editor `OnValidate` entry path. Put under `Assets/Scripts/UI/Primitives/IThemed.cs`. Move stub from Stage 1.2 if it landed elsewhere. |
| T3.2 | ThemedPrimitiveBase MonoBehaviour | _pending_ | _pending_ | `Assets/Scripts/UI/Primitives/ThemedPrimitiveBase.cs` — `abstract class ThemedPrimitiveBase : MonoBehaviour, IThemed`. `[SerializeField] protected UiTheme theme`. `Awake`: if `theme == null` → `theme = FindObjectOfType<UiTheme>()` once; call `ApplyTheme(theme)`. `ApplyTheme` abstract. Invariant #3 rationale in XML doc: no per-frame scan. |
| T3.3 | EditMode tests for base + fallback | _pending_ | _pending_ | `Assets/Tests/EditMode/UI/ThemedPrimitiveBaseTests.cs`: two fixtures — (a) `theme` serialized → `Awake` calls `ApplyTheme` once with serialized ref; (b) `theme` null → `FindObjectOfType` fallback resolves before `ApplyTheme`. Mock subclass captures call count. |
| T3.4 | Glossary rows (primitive contracts) | _pending_ | _pending_ | Add to `ia/specs/glossary.md`: `Themed primitive` (MonoBehaviour under `Assets/Scripts/UI/Primitives/*` implementing `IThemed`), `IThemed contract` (single-method repaint interface), `ThemedPrimitiveBase` (abstract base with `Awake`-cached theme). |
