---
purpose: "Tier 1 stable cross-Stage cache block — rules @-concat target for agent body emission."
audience: both
loaded_by: preamble
cache_control: ephemeral_1h
---

# Stable prefix — Tier 1 cache block

> Descriptor only. Actual `cache_control: {"type":"ephemeral","ttl":"1h"}` wiring lives in the
> agent body block that `@`-loads this file. See `docs/prompt-caching-mechanics.md` §3 Tier 1.
>
> Order is fixed (F5 invalidation cascade — upstream → downstream). Do NOT reorder.

@ia/rules/invariants.md
@ia/rules/terminology-consistency.md
@ia/rules/agent-output-caveman.md
@ia/rules/project-hierarchy.md
@ia/rules/orchestrator-vs-spec.md

---

<!-- Glossary preamble slice (header + authorship note; per-term tables excluded — Tier 2 scope) -->

# Glossary — Territory Developer (preamble)

> Quick-reference for **domain concepts** (game logic + system behavior). Class names, methods,
> and backlog ID rules live in technical specs and `BACKLOG.md` — see `ia/specs/managers-reference.md`,
> `roads-system.md`, etc.
> Canonical detail is always in the linked spec — defer to the spec when they differ.

> **Spec abbreviations:** geo = `isometric-geography-system.md`, roads = `roads-system.md`,
> water = `water-terrain-system.md`, sim = `simulation-system.md`, persist = `persistence-system.md`,
> mgrs = `managers-reference.md` — **§Zones**, **§Demand**, **§World**, **§Notifications**;
> **geo §14.5** = road stroke, lip, grass, Chebyshev, etc.; **sim §Rings** = centroid + growth rings;
> ui = `ui-design-system.md`; unity-dev = `unity-development-context.md`; arch = `ia/specs/architecture/`
> (`layers.md`, `data-flows.md`, `interchange.md`, `decisions.md`).

> **Authorship rule (per-term tables excluded from Tier 1):** full glossary rows live in
> `ia/specs/glossary.md`; they are Tier 2 scope (per-Stage subset via `glossary_discover`). Tier 1
> stays cross-Stage stable — edits to glossary term rows must NOT cascade-invalidate Tier 1 blocks.
> Per `docs/prompt-caching-mechanics.md` §6 (F5 cascade) + §3 Tier 2 scope.
