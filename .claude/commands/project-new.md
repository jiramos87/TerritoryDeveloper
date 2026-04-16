---
description: Create one BACKLOG.md issue + bootstrap `ia/projects/{ISSUE_ID}.md` from a user prompt. Dispatches the `project-new` subagent in isolated context. NOT for bulk stage filing (= `/stage-file` once it ships) or spec enrichment (= `/kickoff`).
argument-hint: "{free-text intent} [--type BUG|FEAT|TECH|ART|AUDIO]"
---

# /project-new — dispatch `project-new` subagent

Use `project-new` subagent (`.claude/agents/project-new.md`) to create a single BACKLOG row + project spec stub from `$ARGUMENTS`.

`$ARGUMENTS` carries the free-text intent (title + product prompt). Optional trailing `--type {prefix}` overrides prefix inference (`BUG` / `FEAT` / `TECH` / `ART` / `AUDIO`); subagent asks the user when ambiguous.

## Subagent prompt (forward verbatim)

Forward via Agent tool with `subagent_type: "project-new"`:

> Follow `caveman:caveman` for all responses. Standard exceptions: code, commits, security/auth, verbatim error/tool output, structured MCP payloads, BACKLOG row text + project-spec stub prose. Anchor: `ia/rules/agent-output-caveman.md`.
>
> ## Mission
>
> Run `project-new` skill (`ia/skills/project-new/SKILL.md`) end-to-end against the user prompt:
>
> ```
> $ARGUMENTS
> ```
>
> Infer issue prefix (`BUG-` / `FEAT-` / `TECH-` / `ART-` / `AUDIO-`) from the prompt; if ambiguous, ask the user before assigning the next id. Honor any `--type {prefix}` override token in the prompt.
>
> ## MCP first
>
> 1. `mcp__territory-ia__glossary_discover` — `keywords` JSON array, English tokens from prompt.
> 2. `mcp__territory-ia__glossary_lookup` — high-confidence terms.
> 3. `mcp__territory-ia__router_for_task` — 1–3 domains matching `ia/rules/agent-router.md` table vocabulary.
> 4. `mcp__territory-ia__spec_section` — only sections prompt implies; set `max_chars`.
> 5. `mcp__territory-ia__invariants_summary` — if runtime C# / game subsystem touched.
> 6. `mcp__territory-ia__backlog_issue` — every Depends on / Related id surfaced. Hard dep unsatisfied → align or wait.
>
> ## File + backlog checklist
>
> - Next id = max(prefix) across BACKLOG + BACKLOG-ARCHIVE + 1 (monotonic; never reuse).
> - Insert BACKLOG row in correct Priority section per `AGENTS.md`. Row: Type / Files / Notes / `Spec: ia/projects/{ISSUE_ID}.md` / Depends on / Acceptance.
> - Copy `ia/templates/project-spec-template.md` → `ia/projects/{ISSUE_ID}.md`. Fill header, §1 Summary, §2 Goals, §7 stub Implementation Plan, Open Questions per `ia/projects/PROJECT-SPEC-STRUCTURE.md`.
> - Run `npm run validate:dead-project-specs` — must exit 0.
>
> ## Hard boundaries
>
> - Do NOT bulk-file multiple issues. One issue per invocation; bulk is `stage-file`.
> - Do NOT enrich spec body beyond template stub — that is `/kickoff`.
> - Do NOT implement — that is `/implement`.
> - Do NOT close / delete spec — that is `/closeout`.
> - Do NOT reuse retired ids.
> - Do NOT cite Depends on / Related ids that fail `backlog_issue` lookup.
> - Do NOT skip `validate:dead-project-specs`.
>
> ## Output
>
> Single concise caveman message: issue id + prefix + priority section, spec stub path, glossary terms anchored, router domains matched, Depends on `depends_on_status` summary, `validate:dead-project-specs` exit code, next step (`claude-personal "/ship {ISSUE_ID}"`).
