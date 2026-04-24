### Stage 3 — HIGH band (IP1–IP5) / Skill wiring + docs

**Status:** Final

**Backlog state (Stage 1.3):** 4 filed

**Objectives:** Update the skill bodies that shell out to `reserve-id.sh` + manually construct yaml + manually invoke `materialize-backlog.sh` so they document the MCP-first call path (`reserve_backlog_ids`, `backlog_record_validate`). Keep the bash fallback so skills work even when MCP is unavailable. Update `docs/mcp-ia-server.md` tool catalog.

**Exit:**

- `ia/skills/stage-file/SKILL.md` — call-path step for batch id reservation names `reserve_backlog_ids` MCP tool first; bash fallback kept as alternative.
- `ia/skills/project-new/SKILL.md` — single-id reservation step names `reserve_backlog_ids (count: 1)` first; bash fallback kept.
- `ia/skills/project-spec-close/SKILL.md` — no call-path change (closeout does not reserve ids); add a note that `backlog_record_validate` may lint the archive-bound yaml before move.
- `docs/mcp-ia-server.md` — three new tools documented in the catalog (inputs, outputs, when to use).
- `CLAUDE.md` §2 MCP-first ordering — add the three new tools to the suggested order where relevant.
- Phase 1 — Skill body updates (`stage-file`, `project-new`, `project-spec-close`).
- Phase 2 — Tool catalog + CLAUDE ordering updates. (TECH-345, TECH-346 Done)

**Tasks:**

| Task | Name | Issue | Status | Intent |
| --- | --- | --- | --- | --- |
| T3.1 | Wire MCP tools into `stage-file` + `project-new` skills | **TECH-343** | Done (archived) | Edit `ia/skills/stage-file/SKILL.md` + `ia/skills/project-new/SKILL.md` — reserve-id step names `reserve_backlog_ids` MCP tool first; `backlog_record_validate` step added after yaml body authoring + before disk write; bash fallbacks kept as "if MCP unavailable" alternative. Caveman prose. |
| T3.2 | Note `backlog_record_validate` use in close skill | **TECH-344** | Done (archived) | Edit `ia/skills/project-spec-close/SKILL.md` — add a single-line note that `backlog_record_validate` may lint the archive-destination yaml before the move (defensive; optional). No behavior change. |
| T3.3 | Document new tools in `docs/mcp-ia-server.md` | **TECH-345** | Done (archived) | Add three catalog entries in `docs/mcp-ia-server.md` for `reserve_backlog_ids`, `backlog_list`, `backlog_record_validate` — input schema, output shape, canonical use case. Preserve existing catalog ordering. |
| T3.4 | Update `CLAUDE.md` §2 MCP-first ordering | **TECH-346** | Done (archived) | Edit `CLAUDE.md` §2 "MCP first" — insert `reserve_backlog_ids` / `backlog_record_validate` into the suggested order for issue-creation flows, and `backlog_list` for structured list queries. Do not rewrite the full ordering block — additive edits only. |

---
