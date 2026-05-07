# UI elements grilling — process design (pre-grilling)

> **Status.** This document is the **process spec** for how we will grill the game UI definitions. It does NOT contain the UI definitions themselves yet. Once we agree on the process, the grilling output goes to `docs/ui-element-definitions.md` and from there to a DB seed migration.

> **Agent binding (read first).** Every question to Javier during grilling MUST use the `AskUserQuestion` tool with **simple product / game language** in question text + option labels. No inline prose questions. No Unity / yaml / token-slug jargon facing the user. Translate agent-side shorthand (§3, §4) to product terms *before* asking. Full polling protocol: §5.0. This rule overrides any contradictory phrasing elsewhere in this doc.

---

## 1. Raw idea (Javier's original)

Define every Scene element by individual grill / polling to the user.

1. Define which panels exist + their names. Name infers function.
2. Define buttons per panel. Name infers function.
3. Define each panel positioning, dimensions, anchors, look.
4. Define each button positioning, dimensions, anchors, prefab, look.
5. Confirm interaction between elements.

Baseline = checkpoint commit `5a095d25` Scene look. Output → DB → bake → compile → human QA → iterate until calibration converges.

---

## 2. Augmented process (5 phases)

```
Phase 0 — Inventory      keep / replace / retire each existing panel from baseline
Phase 1 — Panel-level    name, function, layout, position, theme, visibility
Phase 2 — Button-level   slug, action, hotkey, icon, tooltip, disabled rule
Phase 3 — Data bindings  which labels show live data + cadence
Phase 4 — Interactions   modal stacking, input routing, cross-panel triggers
Phase 5 — Loop           write → seed DB → bake → screenshot → diff → iterate
```

Each phase = one polling pass. Within a phase, one element at a time.

---

## 3. Vocabulary — Unity → Web translation

This is the dictionary. **You speak web. I translate to Unity internally.** No Unity jargon comes back at you in polls.

| Web term you say | Unity reality (my problem, not yours) |
| --- | --- |
| `position: bottom-left, top-right, center` | `RectTransform.anchorMin / anchorMax + pivot` |
| `width: 100%` / `height: 60px` | `RectTransform.sizeDelta + anchor stretch` |
| `padding: 4 8 4 8` (TRBL) | `LayoutGroup.padding` |
| `gap: 8` | `LayoutGroup.spacing` |
| `flex-row` / `flex-col` | `HorizontalLayoutGroup` / `VerticalLayoutGroup` |
| `align: center` / `justify: space-between` | LayoutGroup `childAlignment` + LayoutElement flex |
| `z-index: 10` | `Canvas.sortingOrder` / sibling order |
| `hex: #f5e6c8` | `Color` (RGBA float) |
| `opacity: 0.8` | `CanvasGroup.alpha` |
| `display: none` / `visible: false` | `GameObject.SetActive(false)` |
| `bg-image: url('zoom-in.png')` | `Image.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(...)` |
| `class: btn-primary` | `IlluminatedButton + theme variant slug` |
| `component: <IconButton/>` | nested prefab instance + `CatalogPrefabRef` |

If I ever drift back into Unity-speak in a poll, call it out + I rewrite.

---

## 4. Design code — reusable primitives (React-like vocabulary)

The point: **never re-spec the same thing per button.** Define tokens + components once; reference them by name in polls.

### 4.1 Design tokens (named values)

```yaml
# Colors
color.bg.cream:        '#f5e6c8'   # button body default
color.bg.cream-pressed:'#d9c79c'
color.border.tan:      '#a37b3a'
color.icon.indigo:     '#4a3aff'
color.text.dark:       '#1a1a1a'
color.alert.red:       '#c53030'

# Sizing
size.icon:           64       # px
size.button.tall:    72
size.button.short:   48
size.strip.h:        80       # bottom HUD strip height
size.panel.card:     320      # default modal width

# Spacing
gap.tight:           4
gap.default:         8
gap.loose:           16
pad.button:          [4, 8, 4, 8]   # T R B L

# Layers (z-index)
z.world:             0
z.hud:               10
z.toast:             20
z.modal:             30
z.overlay:           40
```

### 4.2 Components (atoms + molecules)

```jsx
<HudStrip side="bottom" h="80px" bg="cream">
  // anchored full-width strip; auto-spaces zones
  <Zone slot="left"   align="start"  gap="default" />
  <Zone slot="center" align="center" gap="default" />
  <Zone slot="right"  align="end"    gap="default" />
</HudStrip>

<IconButton
  slug="zoom-in"
  icon="zoom-in-button-1-64"   // sprite asset (web: bg-image)
  size="icon"                   // 64×64
  variant="amber"               // theme slug
  hotkey="+"
  action="cmd:camera_zoom_in"
  tooltip="Zoom in"
/>

<Label
  slug="city-name"
  bind="city.name"             // data binding
  font="display"
  align="center"
/>

<Readout
  slug="treasury"
  bind="economy.money"
  format="currency"
  cadence="tick"
/>

<Toggle slug="auto-save" bind="settings.autoSave" />

<Modal slug="pause-menu" trapFocus closeOnEsc>
  ...
</Modal>
```

This is **design code**, not React JSX. We use it as **shorthand in polls** so I don't re-ask 12 questions per button.

### 4.3 Variants (state machine per component)

```
IconButton.variants = { default | hover | pressed | disabled | active }
HudStrip.variants   = { idle | dimmed }   // dimmed when modal on top
```

When I poll a button, I assume **default variant inherits theme**. You only override if a state needs custom hex / sprite.

---

## 5. Polling templates — what a question looks like to you

### 5.0 Polling protocol (binding for all agents)

**MANDATORY format.** Every grilling question to Javier uses the `AskUserQuestion` tool — never inline prose questions, never "answer Q1–Q7 in the doc". One field = one question = one `AskUserQuestion` call (batch when independent).

**MANDATORY vocabulary.** Question text + option labels = **simple product / game language**. Javier reads chat in product terms; agent jargon is a translation tax he should not pay.

| Use (product) | Avoid (jargon) |
| --- | --- |
| "Where on screen?" | "What `RectTransform.anchorMin/Max`?" |
| "Bottom strip across the whole width" | "anchored full-width with `sizeDelta.y = 80`" |
| "Show a zoom-in icon" | "Sprite asset `zoom-in-button-1-64-target.png`" |
| "Always visible" / "Hides during pause" | "visibility=always / toggled-by-pause" |
| "Stack above everything" | "z-layer = `z.modal`" |
| "Cream button with tan border" | "color.bg.cream + color.border.tan tokens" |
| "Press + to zoom in" | "hotkey binding `cmd:camera_zoom_in`" |

The §3 web-vocab table + §4 design code are the **agent's internal shorthand**, not user-facing question text. Translate before asking; record the agent-side slug only after Javier picks the product-language option.

**Defaults shown as options.** Each `AskUserQuestion` includes a "use default" option labelled in product terms (e.g. "Bottom strip, full width, always visible") so Javier picks rather than types. "Other" only when none fits.

**Audit ID trailer OK.** A single `Context: panel=hud-bar` line below the question is acceptable when the agent needs Javier to disambiguate between two similar panels — but the question itself stays product-language.

**Pacing.** One panel or one button at a time. Never batch 5 panels in one turn. Javier's decision latency is the bottleneck — keep each turn small enough to answer in <30 seconds.

### 5.1 Phase 1 panel poll (agent-side template — translate to product before asking)

```
Panel: hud-bar
- Function (one line):                                    [your answer]
- Position (top|bottom|left|right|center, full-width?):   [bottom, full-width]
- Height/width:                                           [80px / 100%]
- Layout (flex-row | flex-col | grid | absolute):         [flex-row, 3 zones]
- Theme variant:                                          [amber / cream]
- Visibility (always | toggled-by-X | modal):             [always]
- Z-layer:                                                [z.hud]
- Open/close anim:                                        [none | fade]
```

**What Javier actually sees** (one `AskUserQuestion` per row, batched):

> Q: Where on screen does the bottom HUD live?
> Options: ["Bottom strip across whole width" (default) | "Bottom-left corner only" | "Floating panel" | "Other"]
>
> Q: How tall?
> Options: ["Short (~50px)" | "Medium (~80px)" (default) | "Tall (~120px)" | "Other"]
>
> Q: Always visible, or hides during something?
> Options: ["Always visible" (default) | "Hides during pause menu" | "Hides during cutscene" | "Other"]

### 5.2 Phase 2 button poll (agent-side template — translate to product before asking)

```
Button: zoom-in (in panel: hud-bar, zone: left)
- Action:           [cmd:camera_zoom_in]
- Icon asset:       [zoom-in-button-1-64-target]
- Hotkey:           [+ / =]
- Tooltip:          [Zoom in]
- Disabled when:    [zoom == max]
- Component:        [IconButton size=icon variant=amber]
```

**What Javier actually sees**:

> Q: This button — what does it do when pressed?
> Options: ["Zoom camera in" (default) | "Zoom camera out" | "Recenter on city" | "Other"]
>
> Q: Hotkey?
> Options: ["+ / =" (default) | "Z" | "No hotkey" | "Other"]
>
> Q: When should this button grey out?
> Options: ["When already at max zoom" (default) | "Never greys out" | "During pause" | "Other"]

If the default fits, Javier picks the default option. Only "Other" forces a free-text answer.

---

## 6. Iteration loop (Phase 5 detail)

```
[YOU + ME poll]                  → fill docs/ui-element-definitions.md
       ↓
[ME generate]                    → db/migrations/0XXX_seed_ui_definitions.sql
       ↓
[ME run]                         → bake_ui_from_ir + open scene
       ↓
[ME capture]                     → screenshot via bridge + post here
       ↓
[YOU eyeball]                    → say "good" / "wrong: X"
       ↓
[ME diff & calibrate]            → if drift, fix bake/seed + loop;
                                   if doc wrong, re-poll just that field
       ↓
DONE when 2 cycles match.
```

---

## 7. Output document — `docs/ui-element-definitions.md` (target)

**Role.** This doc is the **annotation + JSON staging surface**. It is NOT the runtime source of truth.

- **DB = bake source of truth** (Unity bake reads DB rows, never the doc).
- **Doc = human-readable annotation + the JSON-as-text that seeds the DB.** Each Q&A turn fills prose + the corresponding JSON block. The seed migration is generated mechanically from those JSON blocks.
- **Calibration target.** Over many cycles, agents use the doc ↔ DB pairing to calibrate their human-input → Unity-UI translation tools. The doc is what makes that calibration auditable.

Single markdown file. Sections:

```
# UI element definitions

## Tokens
  - prose table of named values (§4.1)
  - ```json tokens block``` (consumed by seed gen)

## Components
  - prose list of reusable atoms/molecules (§4.2)
  - ```json components block```

## Panels
  ## hud-bar
    - prose meta (function, position, layout, visibility, z, theme)
    - prose children list (buttons + components referenced)
    - ```json panel block``` (full panel + children definition, exact DB shape)
  ## (next panel)
    ...

## Interactions
  - prose cross-panel rules
  - ```json interactions block```

## Baseline reference
  - read-only annotation of current scene look at the chosen baseline commit
  - NOT a definition — only commentary so the agent has visual context
  - format: per-panel screenshot description + observation ("hud-bar today extends past viewport right edge", etc.)

## Changelog
  - date + what changed (so DB seed can regen idempotently per doc revision)
```

**Flow each grilling turn:**

```
ask product-language Q  →  user picks option  →
agent writes prose annotation in doc  →
agent appends/updates JSON block in doc  →
(at end of phase) regenerate seed migration from JSON  →  apply to DB
```

---

## 8. Process decisions — LOCKED 2026-05-07

| # | Question | Decision |
| --- | --- | --- |
| Q1 | Baseline | **Current scene look** at the chosen checkpoint. Agent annotates the current look in `docs/ui-element-definitions.md` §Baseline reference (read-only commentary). User defines **ALL** elements from scratch — no keep / replace / retire branch. Phase 0 = baseline annotation, NOT inventory triage. |
| Q2 | Output doc shape | **Single file** — `docs/ui-element-definitions.md`. |
| Q3 | Vocabulary | All clear. Proceed with §3 web translation table as agent-side internal shorthand. |
| Q4 | Design code shorthand | **Yes** in agent-side notes only. User-facing questions stay in product language (§5.0 binding holds). |
| Q5 | DB seed strategy | **One full refresh per doc revision** — re-run safe idempotent migration regenerated from doc JSON blocks. |
| Q6 | DB ↔ doc relationship | Not a winner question. **Two roles:** DB = bake source of truth (Unity reads DB). Doc = human annotation + JSON-as-text staging area + translation calibration target. Seed flow: doc JSON → migration → DB → bake. Doc never read by Unity. |
| Q7 | Starter scope | **No starter set**. Define every panel (Phases 0–4) for the whole game UI first. Bake everything together at Phase 5. Larger blast radius accepted. |

---

## 9. What I add / correct vs your raw idea

| Your raw step | My augmentation |
| --- | --- |
| (missing) | **Phase 0 — baseline annotation** before Phase 1. Agent describes the current scene look as read-only reference comments in §Baseline reference of the output doc. User then defines all elements from scratch in Phases 1–4 (no keep / replace / retire — that branch was rejected at Q1). |
| 1 panels | + visibility rules, z-layer, modal-stacking, theme variant, animation. |
| 2 buttons | + action binding, hotkey, tooltip, disabled rule, audio cue. |
| 3 panel position | + responsive behavior on resolution change. |
| 4 button look | + state variants (hover, pressed, disabled, active). |
| 5 interactions | + input routing priority, modal focus-trap, cross-panel triggers. |
| (missing) | **Phase 3 — data bindings**: which labels show live data + update cadence (frame / tick / event). |
| (missing) | **Vocabulary translation table** (§3) — you stay in web-speak, I translate. |
| (missing) | **Design tokens + components** (§4) — reusable shorthand, saves polling time. |
| (missing) | **Iteration loop** (§6) with calibration step — not just "bake + QA". |
| (missing) | **Output doc shape** (§7) so DB seed generator has a deterministic target. |

---

## 10. Calibrated system — what this process builds for future agents

The grilling is **not** a one-time UI definition. It's the **training loop for a translation function** that turns "Javier says: bottom bar with zoom + pause buttons" into shippable Unity UI without re-grilling. Each turn produces calibration artifacts that compound.

### 10.1 What gets built incrementally

Each (product question → user choice → JSON emitted → DB row → baked prefab → eyeball verdict) tuple writes to **five durable surfaces**:

| Artifact | Where it lives | Future use |
| --- | --- | --- |
| **Design token registry** | DB table `ui_tokens` (seeded from §Tokens JSON block) + MCP `ui_token_get` / `ui_token_list` | Agents pick from registry instead of inventing colors / spacing. New panel = same tokens, guaranteed visual consistency. |
| **Component catalog** | DB table `ui_components` (seeded from §Components JSON) + MCP `ui_component_get` / `ui_component_list` | `<IconButton>`, `<HudStrip>`, `<Modal>` etc. become a parts catalog. New button = pick component + override 2–3 props, no full redefinition. |
| **Panel templates** | DB table `ui_panels` (seeded from §Panels JSON) + MCP `ui_panel_clone` | New panel similar to existing? Clone the template, override differences only. |
| **Translation corpus** | `ia/state/ui-calibration-corpus.jsonl` (one row per grilling turn) | Record of `{product_phrase, options_offered, user_choice, json_emitted}`. Agent pattern-matches new requests against this corpus before asking Javier. |
| **Calibration verdicts** | `ia/state/ui-calibration-verdicts.jsonl` (one row per bake-eyeball turn) | `{panel_slug, baked_screenshot_path, javier_verdict, drift_observed}`. Tells future agents which JSON shapes produced visual surprises so they avoid the same trap. |

### 10.2 How a future agent uses it (lookup-first, grill-only-when-novel)

When a future task says "add a settings panel":

```
1. agent: ui_panel_list  →  is there a similar panel template? (e.g. modal-card)
2. agent: ui_component_list  →  which atoms fit (Toggle, Slider, Label, Modal)?
3. agent: ui_token_list  →  reuse existing color/spacing tokens, no new ones
4. agent: query calibration corpus for "settings" / "preferences" / "modal" hits
5. IF coverage ≥ 80% → agent drafts JSON directly, skips grilling
   Javier reviews JSON only (1 turn, not 20)
6. IF coverage < 80% → agent runs targeted grilling on novel slots only
   (the 3 questions actually new, not 20 already-answered ones)
```

The grilling cost **drops monotonically** with each panel defined.

### 10.3 New MCP tools the calibrated system needs (proposed)

| Tool | Role |
| --- | --- |
| `ui_token_*` (`get` / `list` / `search`) | Read registry from DB |
| `ui_component_*` (`get` / `list` / `clone`) | Read component catalog + spawn clones |
| `ui_panel_*` (`get` / `list` / `clone` / `propose`) | Read templates + propose new panel JSON from product description |
| `ui_calibration_corpus_query` | Pattern-match a product phrase against the corpus, return coverage % + closest hits |
| `ui_calibration_verdict_record` | Append eyeball verdict + drift after bake |
| `ui_def_doc_to_seed` (script, not MCP) | Mechanical: read doc JSON blocks → emit idempotent migration |
| `ui_def_drift_scan` | Compare doc JSON ↔ DB rows ↔ baked prefab; report drift per slot |

These get scaffolded **incrementally** as the corpus fills — no big-bang tooling project. Each new MCP tool earns its keep against actual recurring grilling friction.

### 10.4 Convergence test (when is the system "calibrated")

```
test: agent receives a NEW UI request never seen before
       (e.g. "add a research-tree panel")
       → agent drafts full JSON from corpus + catalog alone
       → bakes to Unity
       → Javier eyeballs

PASS  if visual matches ≥ 85% of intent on first try (Javier's call)
       AND remaining drift fits in ≤ 3 corrective polls.

FAIL  if agent needs full Phase 1–4 grill from scratch.
       → corpus has gaps; targeted grilling fills them; retest.

DONE  when 3 consecutive new-panel requests PASS without full grilling.
```

After **DONE**, the calibrated system replaces grilling as the default path. Grilling stays available as a fallback for genuinely novel UI patterns (new genre features, new interaction modes).

### 10.5 Lessons → skill (process becomes reusable)

After convergence, the grilling protocol itself gets formalized as `ia/skills/ui-element-grill/SKILL.md` so any future contributor (or fresh agent) can:

- Run grilling on a brand-new panel domain (e.g. multiplayer lobby) without re-deriving the protocol.
- Reuse `AskUserQuestion` templates (§5.0) directly.
- Append to the same corpus + verdict logs.
- Trigger `ui_def_drift_scan` as a CI gate (catch doc ↔ DB ↔ baked-prefab divergence on every PR).

The skill body links back to this process spec; this doc becomes the long-term authoritative explanation of *why* the protocol exists.

### 10.6 Net payoff

| Before this process | After convergence |
| --- | --- |
| Each new panel = ad-hoc Unity work, no shared vocabulary, drift between intent + result | Each new panel = pick from catalog + 1–3 polls, JSON drafted by agent, bake matches intent first try |
| User describes intent → agent invents Unity wiring → human QA finds drift → re-do | User describes intent → agent looks up corpus → JSON proposed → bake → matches |
| UI consistency depends on memory + discipline | UI consistency enforced by token + component registry (impossible to drift without explicit override) |
| Adding a new feature blocked on UI design loops | UI is a near-mechanical step in feature delivery |

This is the long-term reason the grilling is worth doing slowly + correctly the first time.

---

## 11. Next step (process locked — ready to grill on "go")

Process decisions all locked in §8. On user "go", agent will:

1. Spin up `docs/ui-element-definitions.md` skeleton per §7 shape (Tokens, Components, Panels, Interactions, Baseline reference, Changelog — all empty + JSON code-fences ready).
2. Seed §Tokens + §Components blocks from §4.1 + §4.2 of this doc (locked at Q3 + Q4).
3. Run **Phase 0 — baseline annotation**: capture current scene look (panel-by-panel screenshot + prose description) into §Baseline reference. Read-only context, not a definition source.
4. Begin **Phase 1 — panel definition** via `AskUserQuestion` polling, one panel at a time, simple product language. Each turn: user picks options → agent writes prose + appends JSON block.
5. After all panels through Phase 4: regenerate single idempotent seed migration → apply to DB → bake all → screenshot diff → calibrate.

No grilling fires until user says "go".
