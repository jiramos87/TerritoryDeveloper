# Reference: Night-City Dark Theme

Stage 9.5 seed — canned visual reference for LLM theme proposer (TECH-15229).

## Mood

Industrial nighttime city. Deep navy-to-black surfaces. Blue-white accent primary.
Positive = teal-green. Negative = warm red. Typography near-white on dark backgrounds.

## Token palette

| Slug | Hex | Role |
|---|---|---|
| ds-surface-base | #111827 | Deepest chrome / fullscreen tint base |
| ds-surface-card | #1c2333 | HUD / popup card backgrounds |
| ds-surface-elevated | #283044 | Controls, active tool highlight |
| ds-surface-toolbar | #0f172a | Toolbar strip (slightly darker than card) |
| ds-text-primary | #e8eaf6 | Primary readable text |
| ds-text-secondary | #8b8fa8 | Secondary / muted text |
| ds-accent-primary | #4a9eff | Interactive accent / links |
| ds-accent-positive | #34c759 | Positive feedback (income, growth) |
| ds-accent-negative | #ff453a | Negative feedback (debt, error) |
| ds-border-subtle | #2e3650 | 1 px dividers, panel edges |

## Inspiration sources

- Night-city isometric games (SimCity 4 night mode, Cities Skylines dark UI mods)
- Tailwind slate/blue color ramp (slate-900 → slate-800 → slate-700)
- Blue-accent: iOS system blue (#007AFF) shifted toward perceptual mid-range for dark bg

## Usage notes

- Use ds-surface-card for HUD panels + pickers; ds-surface-base for fullscreen overlays.
- ds-accent-primary on interactive buttons only (not decorative elements).
- Reserve ds-accent-negative for error states + destructive actions.
- ds-text-secondary for stat labels, timestamps, secondary metadata.

## Status

Canned reference — Stage 9.5 stub. LLM proposer will use this as few-shot example in Stage N.
