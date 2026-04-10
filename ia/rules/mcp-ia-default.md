---
purpose: Default to territory-ia MCP for IA retrieval in Agent mode
audience: agent
loaded_by: always
slices_via: none
description: Default to territory-ia MCP for IA retrieval in Agent mode
alwaysApply: true
---

# MCP territory-ia — default retrieval

Prefer `mcp__territory-ia__*` tools over loading full `ia/specs/*.md`. Ordering, fallback, and schema-cache caveat live in `CLAUDE.md` §2 "MCP first". This rule exists as an `@`-loadable anchor for that directive.
