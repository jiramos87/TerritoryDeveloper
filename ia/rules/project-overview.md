---
purpose: Territory Developer project identity and structure
audience: agent
loaded_by: always
slices_via: none
description: Territory Developer project identity and structure
alwaysApply: true
---

# Territory Developer — Project Overview

**Language:** All code, comments, XML docs, annotations, Debug.Log messages, and repository content must be in **English**.

**Stack:** Unity 2D isometric city-builder; C# MonoBehaviour classes; `Territory.*` namespaces (partial migration); no DI — Inspector fields + `FindObjectOfType<T>()` fallback in Awake/Start.

**Game vision and scales:** See [`ia/specs/game-overview.md`](../specs/game-overview.md).

**Key patterns:** GridManager is the central hub for cell operations. Only singleton: `GameNotificationManager.Instance`. Managers are MonoBehaviour scene components (never `new`). Dependencies via Inspector + `FindObjectOfType` fallback.

**Documentation hierarchy:** `AGENTS.md` (workflow) → `ia/rules/` (guardrails) → `ia/specs/` (deep reference) → `ARCHITECTURE.md` (dependency map). MCP: prefer `territory-ia` tools over reading whole spec files.

**Verification:** `npm run verify:local` from repo root. See `ARCHITECTURE.md` (Local verification) and `CLAUDE.md` §5.
