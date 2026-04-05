# UI visual polish — exploration and discussion (FEAT-50)

> **Backlog:** [FEAT-50](../BACKLOG.md)  
> **Project spec:** [`.cursor/projects/FEAT-50.md`](../.cursor/projects/FEAT-50.md)  
> **Related program:** **UI-as-code program** (**glossary**; **§ Completed** — [`BACKLOG-ARCHIVE.md`](../BACKLOG-ARCHIVE.md) **Recent archive**) — structural baseline in **`ui-design-system.md`** **§5.2**  
> **Reference spec:** [`.cursor/specs/ui-design-system.md`](../.cursor/specs/ui-design-system.md)  
> **As-built snapshot:** [`docs/reports/ui-inventory-as-built-baseline.json`](reports/ui-inventory-as-built-baseline.json)

This document is a **discussion charter** for product and design alignment before wide **Prefab** / scene edits. It complements the normative **Implementation Plan** in `.cursor/projects/FEAT-50.md`.

---

## 1. Problem statement

The **city** **HUD**, **ControlPanel** / **toolbar**, and **`MainMenu`** already function, but the overall look can feel utilitarian or inconsistent (mixed emphasis, **color** drift, uneven spacing). **FEAT-50** scopes a **visual polish** pass that improves perceived quality and readability **without** changing underlying **gameplay** or **simulation** rules.

**Boundary vs structure work:** The **UI-as-code** program delivers **UiTheme**, prefab v0s, **`UIManager`** partials/facades, **modal**/input policy, and **Editor** tooling (normative **`ui-design-system.md`** **§5.2**). **FEAT-50** is the **aesthetic** layer: what it should *feel* like on top of that baseline (or on the current hierarchy if polish ships first).

---

## 2. Dimensions to decide

Use this checklist in reviews; record decisions in the project spec **Decision Log** or in **ui-design-system** **Target** notes.

| Dimension | Prompts |
|-----------|---------|
| **Palette** | Keep near-current dark neutrals vs shift temperature? How many **accent** colors are allowed simultaneously on screen? |
| **Typography** | Hierarchy for **HUD** keys vs values (size/weight/muting). Respect **`ui-design-system.md`** **§1.2**: legacy **`Text`** on **city** **HUD** unless a future issue migrates **TMP**. |
| **Spacing & layout** | Grid rhythm for **toolbar** groups; **padding** on **panels**; alignment of stat columns. |
| **Iconography** | Outline vs filled icons; consistent stroke or pixel scale; when to use text-only controls. |
| **Surfaces** | **Panel** backgrounds, dividers, **overlay-dim** strength over the **map** view. |
| **Motion** | Fade/slide defaults; duration caps; “reduced motion” stance (future setting vs always-subtle). |
| **Audio** | Optional **UI** click/hover sounds—out of scope unless **AUDIO-** issue exists (note here only). |

---

## 3. References and constraints

- **As-built** **token** table: **ui-design-system** §1.1–1.2 (sourced from baseline JSON).
- **Critique proposals:** `docs/ui-as-built-ui-critique.md` (**P1–P9**) may inspire polish but are not automatically in scope—pick explicitly.
- **Input / scroll:** coordinate with **BUG-19** if polish touches scrollable **modals** or **camera** boundaries.
- **Performance:** avoid per-frame **layout** thrash; prefer static **Prefab** states over continuous **Animator** on **HUD** stats.

---

## 4. Suggested review workflow

1. **Mood:** 3–5 reference adjectives (e.g. “calm,” “technical,” “warm”) and 1–2 **not** goals (“not neon,” “not skeuomorphic leather”).
2. **Mock or paint-over:** optional—screenshots with markup or a single **MainScene** **Canvas** branch for experiment.
3. **Token sheet:** list final **RGBA** / font sizes for **ui-text-primary**, **ui-surface-dark**, accents, etc.
4. **Rollout order:** **MainMenu** first (short session) vs **HUD** first (long session)—team choice.
5. **Sign-off:** product owner approves **Target** row or **Decision Log** before repo-wide **Prefab** merge.

---

## 5. Open threads (non-normative)

- Parallel work: now that **`UiTheme`** and prefab **v0** exist, should **FEAT-50** bind polish to those assets first to avoid double-touching the same objects?
- **Localization:** if future strings lengthen, does the **HUD** layout still hold with the new **typography** scale?
- **Minimap** and secondary **panels:** same **chrome** rules as **ControlPanel** or intentionally subdued?

Update this file as decisions land; migrate stable norms into **ui-design-system** **as-built** when shipped.
