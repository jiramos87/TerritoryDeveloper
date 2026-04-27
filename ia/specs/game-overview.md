---
purpose: "Reference spec for Territory Developer game vision and identity."
audience: both
loaded_by: router
slices_via: spec_section
description: "Game vision, multi-scale identity, and core design principles."
---
# Game Overview — Territory Developer

> Canonical game identity and design principles. For runtime architecture, see `ARCHITECTURE.md`. For project structure and workflow, see `AGENTS.md`. For the multi-scale MVP plan, render the DB-backed master plan slug `multi-scale` via `mcp__territory-ia__master_plan_render({slug: "multi-scale"})`.

## Vision

**Territory Developer** is a deep, low-fidelity city-builder that lets the player move **up and down through simulation scales**, each with its own model, vocabulary, and tempo:

```
city  →  region  →  country   (MVP target)
world  →  solar   (post-MVP)
```

### Core design principles

- **Deep, not wide.** Cheap visuals, long run times, many interlocking systems.
- **One active scale at a time.** The scale the player views runs its full simulation loop; other scales are dormant (no live tick).
- **Dormant scales evolve algorithmically at scale-switch time.** Evolution is a pure function `evolve(snapshot, Δt, params) → snapshot'`, owned by the parent-scale entity.
- **Three scales is the minimum target.** Two scales admits shortcuts; three forces the architecture to generalize to N.
- **Every scale runs on the same real-time calendar.** No per-scale tick periods, no dilation.

The player can in principle play as mayor, governor, or head of state using the same codebase and a different active scale.

## Stack

Unity 2D isometric city-builder; C# MonoBehaviour classes; `Territory.*` namespaces (partial migration); no DI — Inspector fields + `FindObjectOfType<T>()` fallback in Awake/Start. Full stack details: `ia/rules/project-overview.md`.

## Scales

| Scale | Player role | Scope | Status |
|-------|------------|-------|--------|
| City | Mayor | Streets, zones, buildings, economy, simulation tick | Active (single-scale) |
| Region | Governor | Cities, inter-city trade, migration, founding new cities | MVP target |
| Country | Head of state | Regions, national budget, infrastructure projects | MVP target |
| World | — | Global climate, commodity prices | Post-MVP |
| Solar | — | Epochs, catastrophes, long-term resource depletion | Post-MVP |
