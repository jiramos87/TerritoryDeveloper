---
purpose: Use canonical vocabulary from glossary and specs across code, docs, backlog, and MCP
audience: agent
loaded_by: always
slices_via: none
description: Use canonical vocabulary from glossary and specs across code, docs, backlog, and MCP
alwaysApply: true
---

# Terminology and information consistency

- Use `ia/specs/glossary.md` vocabulary across code, specs, rules, `BACKLOG.md`, `docs/`, MCP tool names. Prefer glossary table names (`HeightMap`, `wet run`, `road stroke`, etc.) over ad-hoc synonyms. If glossary and a spec disagree, **the spec wins** (per glossary header).
- Issue ids (`BUG-` / `FEAT-` / `TECH-` / `ART-` / `AUDIO-`) appear **only** in `BACKLOG.md` (open) and `BACKLOG-ARCHIVE.md` (closed) — never in glossary, reference specs, rules (except this line), skills, `docs/`, or code comments.
- New domain term → add a glossary row **and** update the authoritative spec section. Do not leave the term only in backlog or chat.

Authoring guidance (C# conventions, MCP tool registration, project-spec / reference-spec authoring): see [`ia/rules/terminology-consistency-authoring.md`](terminology-consistency-authoring.md) — fetch via `rule_content terminology-consistency-authoring` when editing those surfaces. Full workflow: [`AGENTS.md`](../../AGENTS.md) — Terminology and information consistency.
