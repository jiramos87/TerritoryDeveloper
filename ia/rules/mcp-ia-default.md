---
purpose: Default to territory-ia MCP for IA retrieval in Agent mode
audience: agent
loaded_by: always
slices_via: none
description: Default to territory-ia MCP for IA retrieval in Agent mode
alwaysApply: true
---

# MCP territory-ia — default retrieval

When **territory-ia** tools are available (Cursor Agent + MCP enabled), use them first: **`backlog_issue`** for a known issue id, then specs, **`glossary_discover`** / **`glossary_lookup`** (pass **English**; translate from the conversation if needed — glossary is English-only), router table, invariants, and rule bodies. See `AGENTS.md` step 3 and `docs/mcp-ia-server.md`. Avoid loading full `ia/specs/*.md` files when `spec_section`, **`spec_sections`** (batch slices), or `spec_outline` suffices. For **project-spec-close** prep on `ia/projects/{ISSUE_ID}.md`, prefer **`project_spec_closeout_digest`** after **`backlog_issue`**.
