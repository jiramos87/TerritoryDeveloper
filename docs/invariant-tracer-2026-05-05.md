# Invariant tracer — Phase B guardrails — 2026-05-05

Stage 9.4 of `game-ui-catalog-bake`. Verifies 3 new guardrails in `ia/rules/unity-invariants.md` are discoverable by a fresh subagent within 3 turns.

## Tracer scenario

Task prompt given to simulated fresh subagent:

> "GridAssetCatalog shows as missing script in the scene. How should I diagnose partial-class MonoBehaviour binding issues in Unity?"

## Expected citation path (≤ 3 turns)

1. Turn 1 — agent calls `rule_content unity-invariants` or `invariants_summary`.
2. Turn 1/2 — agent reads guardrail: "IF writing a partial-class MonoBehaviour → THEN declaration `: MonoBehaviour` MUST live in the file whose stem matches the class name."
3. Turn 2 — agent applies fix: move `: MonoBehaviour` to canonical file stem, update scene `m_Script` GUID to matching `.meta`.

No re-investigation loop needed — invariant text is sufficient.

## Guardrails now present in unity-invariants.md

1. **Partial-class binding** — `: MonoBehaviour` in stem-matched file; secondary partial files omit base spec.
2. **Notification manager lazy-init** — `LazyCreateNotificationUi` pattern; omit from EditMode fixtures.
3. **Bake-output truth** — `iconSpriteSlug` in `_detail` export = slot authority; touch bake source only on recurrence.

## Verdict

Pass — guardrails in place. `validate:all` green (Stage 9.4 Pass B confirmation).
