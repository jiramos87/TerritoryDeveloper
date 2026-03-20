# UI / UX Design System — Program Charter

## Purpose

This document is the **single index** for the cross-cutting effort to introduce a coherent **UI / UX design system** for Territory Developer. It does not replace individual backlog issues; it **guides** issue breakdown, records program-level decisions, and links to technical context and specs.

**Related documents**

| Document | Role |
|----------|------|
| [ui-design-system-context.md](ui-design-system-context.md) | Current UI architecture, inventory, constraints, known pain points |
| [../.cursor/specs/ui-design-system.md](../.cursor/specs/ui-design-system.md) | Foundations, components, patterns, acceptance criteria (implementation-facing) |
| [../BACKLOG.md](../BACKLOG.md) | Executable issues (`FEAT-XX`, `TECH-XX`, `BUG-XX`) — add links here as work is ticketed |

## Goals

- **Consistency** — Shared visual language (color, type, spacing, states) across HUD, toolbars, popups, and menus.
- **Velocity** — New screens and flows reuse documented components and patterns instead of one-off styling.
- **Maintainability** — Clear ownership of UI prefabs and scripts; fewer ad-hoc references and duplicated layout.
- **Quality** — Predictable interaction patterns (focus, feedback, errors) and alignment with Unity UI constraints used in this project.

## Non-goals (initial phase)

- Replacing the entire stack with UI Toolkit in one step (evaluate per workstream).
- Brand or marketing assets outside the game client.
- Full accessibility audit (WCAG) unless explicitly scoped in a future issue.

## Success criteria (program-level)

Checklist to revisit when closing the program or a major phase:

- [ ] Foundations (tokens / theme rules) are documented in `.cursor/specs/ui-design-system.md` and applied to at least one pilot surface.
- [ ] A minimal **component set** (e.g. primary button, panel, list row) exists as prefabs or documented variants with naming conventions.
- [ ] New UI-related backlog issues reference the spec section they implement.
- [ ] Decision log below stays current for material trade-offs.

## Phases (suggested)

1. **Discovery** — Complete and maintain `ui-design-system-context.md` (inventory, constraints).
2. **Foundations** — Define tokens and global rules in the spec; implement theme or shared assets as ticketed work.
3. **Components** — Build or standardize core components; document variants and usage.
4. **Migration** — Apply system to high-traffic UI (HUD, main popups) in prioritized backlog issues.
5. **Hardening** — Remove legacy one-off styles where replaced; update `AGENTS.md` / rules if patterns stabilize.

Adjust phase boundaries in this file as the team learns.

## Backlog bridge

When issues are created, add rows here (issue ID, title, spec section).

| Workstream | Backlog ID | Spec section | Depends on |
|------------|------------|--------------|------------|
| Toolbar / ControlPanel — left sidebar | [TECH-07](../BACKLOG.md) | §3.3, §1.3, §4.3 | — |

## Decision log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-------------|-------------------------|
| *YYYY-MM-DD* | *e.g. Canvas scale mode* | *…* | *…* |
| 2026-03-20 | Ticket **TECH-07** and cross-link docs (charter, context, spec §3.3, AGENTS, ARCHITECTURE, managers-guide). Completed as **TECH-08** in `BACKLOG.md`. | Traceable backlog for ControlPanel sidebar; implementation work stays on TECH-07. | Single issue mixing docs + Unity layout (rejected: split meta vs implementation). |

---

*Last updated: 2026-03-20 — TECH-07 in backlog bridge; **TECH-08** = documentation pass (see `BACKLOG.md`).*
