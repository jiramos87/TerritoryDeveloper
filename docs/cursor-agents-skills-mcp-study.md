# Study: Cursor agents, session persistence, Skills, MCP, and Territory Developer

**Audience:** Maintainers deciding how much to rely on the IDE versus repo-owned IA (Information Architecture).  
**Sources:** Cursor product behavior (public docs + host conventions), this repo’s `AGENTS.md`, `docs/mcp-ia-server.md`, `docs/mcp-markdown-ia-pattern.md`, `BACKLOG.md` (e.g. **TECH-17** / **TECH-18** / **TECH-19**), and **territory-ia** MCP tool outputs sampled during this write-up (`backlog_issue`, `spec_outline`, `invariants_summary`, `glossary_discover`, `router_for_task`).

---

## 1. Executive summary

| Question | Short answer |
|----------|----------------|
| Does Cursor persist “agent sessions”? | **Partially.** Conversation threads and UI history persist in the product; **durable, repo-owned “memory”** of decisions and domain state is **not** a substitute for specs, rules, backlog, and tools like **territory-ia**. |
| Should we implement persistence in development? | **Yes, at the information layer** (Markdown + MCP + Git), which this repo already does. **Optional:** third-party “memory” MCPs or future DB-backed IA (**TECH-19** / **TECH-18**) for richer retrieval—not for replacing specs. |
| Are Skills the same as agentic state? | **No.** **Skills** are **static, versioned instructions** the host may attach when relevant. **Agentic state** is **transient context** (thread, tool results, approvals). They complement each other. |
| Skills + existing MCP? | **Layered:** MCP = **pull structured facts** on demand; Skills = **how to behave** on recurring workflows. Avoid duplicating glossary/spec text inside Skills—point to MCP and `AGENTS.md` instead. |
| Specialist agents? | **Sometimes.** Narrow sub-tasks can **reduce** tokens if the parent passes a tight brief; **fan-out** and summarization can **increase** cost. **territory-ia** already targets token efficiency via **slices**. |

---

## 2. Cursor IDE: what “persists” for agents

### 2.1 What the product typically retains

- **Chat / Agent threads** in the IDE (history UI, resuming a conversation in the same workspace).
- **Project rules** under `.cursor/rules/` and workspace configuration (e.g. `.cursor/mcp.json`) — **Git-tracked**, so they persist across machines when committed.
- **Agent transcripts** (when enabled by the host) may be stored locally for review; this is **not** a contractual API for your game or CI — treat as **debug/audit**, not source of truth.

### 2.2 What does *not* replace project IA

- **Implicit model memory** across unrelated new chats is limited; starting a **new** thread does not automatically reload every past decision.
- **Tool outputs** from a past session are not automatically re-injected into a new session unless they appear in history or you re-run tools.
- **Cross-repo or cross-machine** consistency depends on **what is in Git** (specs, rules, backlog) and **what tools re-fetch** (MCP).

### 2.3 When to add custom “memory”

Consider an external memory MCP or DB only if you need **user-specific** or **high-churn** facts that do not belong in specs (e.g. personal preferences, experiment logs). **Domain rules** for Territory Developer should remain in `.cursor/specs/`, `.cursor/rules/`, and `BACKLOG.md` so humans and agents share one vocabulary (see `AGENTS.md` — terminology consistency).

---

## 3. Agentic state vs Skills

| Dimension | Agentic state (session) | Skills (`SKILL.md`) |
|-----------|-------------------------|---------------------|
| **Lifetime** | Bound to a conversation / task run | **Stable** until you edit the skill |
| **Content** | Messages, file edits, tool I/O, errors | Procedures, checklists, when-to-use descriptions |
| **Discovery** | Linear chat context | Host discovers skills from configured paths (e.g. project `.cursor/skills/`, user-level skills); activation is **description-driven** + optional explicit invocation |
| **Best for** | Executing the *current* change | **Repeatable workflows** (releases, backlog hygiene, MCP verification) |

**Conclusion:** Skills are **not** persistence of agent state; they are **packaged playbooks**. Persistence of *truth* for this game is **Git + MCP-backed docs**.

---

## 4. Integrating Skills with territory-ia MCP

### 4.1 Division of responsibility (recommended)

| Layer | Responsibility | Territory Developer example |
|-------|----------------|------------------------------|
| **Specs / glossary** | Canonical definitions | `.cursor/specs/glossary.md`, `isometric-geography-system.md` |
| **Rules** | Always-on guardrails | `.cursor/rules/invariants.mdc`, `agent-router.mdc` |
| **MCP (territory-ia)** | **On-demand slices** | `spec_section`, `glossary_lookup`, `backlog_issue`, `invariants_summary` |
| **Skills** | **Process** and **orchestration** | “Starting a TECH issue: call `backlog_issue`, then `router_for_task`, then implement; never paste full geo spec.” |

### 4.2 Anti-patterns

- **Duplicating** long spec sections inside Skills → drift from glossary/specs; use **short** pointers (“call `spec_section` for roads validation”).
- **Using Skills instead of backlog/spec updates** for decisions that affect game behavior → those belong in **`.cursor/projects/`** or **reference specs**, then migrated per project policy.

### 4.3 Practical incorporation steps

1. Add **project Skills** under `.cursor/skills/` (or team convention) for **non-domain** workflows: MCP verify, issue kickoff, PR checklist.
2. Keep **English** in Skill bodies aligned with **glossary** terms when touching domain language (same rule as MCP `glossary_*` tools).
3. Reference **TECH-18** / **TECH-19** roadmap: future **DB-backed** IA changes *implementation* of tools, not the **split** “facts in IA / procedures in Skills.”

---

## 5. Specialist agents, subagents, and token cost

### 5.1 When specialists help

- **Large codebase exploration** with a **narrow question** — a subagent can search and return a **compressed** answer.
- **Isolation** — risky experiments in a separate worktree (e.g. repo’s **best-of-n** patterns) without polluting the parent thread.

### 5.2 When specialists hurt

- **Handoff overhead:** parent must summarize task; child may re-read files unless constrained; merging results adds **coordination** tokens.
- **Duplicate context:** multiple agents each loading the same large spec defeats the purpose.

### 5.3 How this repo already mitigates breadth

- **territory-ia** returns **bounded** JSON (`max_chars`, sections, single backlog issue) — aligned with `docs/mcp-markdown-ia-pattern.md` (“slices, not dumps”).
- **agent-router** and `router_for_task` steer toward **minimal** reading lists.

**Conclusion:** Specialist agents are **not** inherently cheaper. They are **cheaper when the delegated task is small and the parent avoids shipping entire specs into the child**. For Territory Developer, **prefer MCP slices + focused prompts** before defaulting to many parallel agents.

---

## 6. Relation to the game (Territory Developer)

### 6.1 Why IA matters more than IDE session memory

The simulation stack (grid, **HeightMap** / **Cell.height**, roads, water, shores, **AUTO** pipeline) has **hard invariants** and dense cross-references. **Session-only** memory is a poor fit for:

- Explaining **why** a road must use the **road preparation family** (not `ComputePathPlan` alone).
- Keeping **glossary** terms consistent across C#, specs, and backlog.

Sample **invariants_summary** (abridged intent): sync height map and cell height; **InvalidateRoadCache** after roads; no new singletons; no **GridManager** bloat; shore and river constraints; specs live in `.cursor/specs/` vs `.cursor/projects/` per policy.

### 6.2 MCP sampling used for this document

- **`backlog_issue` (`TECH-17`):** Confirms shipped MCP tool set and file-backed sources — baseline for **TECH-18** evolution.
- **`spec_outline` (`AGENTS.md`):** Confirms documentation hierarchy (specs, project specs, backlog workflow) as the **canonical** agent guide.
- **`glossary_discover`:** No rows for generic “MCP agent information” keywords — expected; domain glossary targets **game** terms, not tooling vocabulary.
- **`router_for_task`:** Domain table is **game-domain** routed (roads, water, simulation, etc.); “documentation” strings do not map — tooling is covered by **rules + AGENTS**, not the geography router.

### 6.3 Backlog alignment

- **TECH-18 / TECH-19:** Longer-term **search and DB** for IA; complements Cursor Skills (procedures stay thin; facts stay queryable).
- **TECH-21** program (**TECH-40**–**TECH-42**): Machine-readable indexes (**TECH-40**), validated DTOs (**TECH-41**), future DB patterns (**TECH-42**) — **reduces** need for agents to “remember” schema details if CI and docs enforce them.

---

## 7. Recommendations

1. **Treat Git + territory-ia as primary persistence** for agent-relevant truth; treat Cursor threads as **convenient but not authoritative**.
2. **Introduce Skills sparingly** for **workflows** (issue kickoff, MCP verify, release checklist), not for **copying specs**.
3. **Use specialist agents** only when the task is clearly scoped; otherwise rely on **router + `spec_section` + `backlog_issue`** to control tokens.
4. **Do not conflate** Skills with **session memory**; if you need cross-session *preferences*, evaluate a **memory MCP** separately from **domain IA**.
5. **Keep game vocabulary** in glossary and specs so Skills and MCP prompts stay **searchable and consistent** (`AGENTS.md` terminology checklist).

---

## 8. References (in-repo)

- `AGENTS.md` — agent workflow, MCP-first retrieval, `.cursor/projects/` policy  
- `docs/mcp-ia-server.md` — territory-ia tools and policy  
- `docs/mcp-markdown-ia-pattern.md` — slice-based IA pattern  
- `BACKLOG.md` — **TECH-17** (shipped MCP), **TECH-18**, **TECH-19**, **TECH-21** program (**TECH-40**–**TECH-42**, **TECH-43**)  
- `.cursor/rules/invariants.mdc` — system invariants (also exposed via MCP `invariants_summary`)

---

*Document type: engineering study / ADR companion (not a reference spec). For permanent game behavior definitions, use `.cursor/specs/` and the glossary.*
