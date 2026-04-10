# Study: Cursor agents, session persistence, Skills, MCP, and Territory Developer

**Audience:** Maintainers deciding how much to rely on the IDE versus repo-owned IA (Information Architecture).  
**Sources:** Cursor product behavior (public docs + host conventions), this repo’s `AGENTS.md`, `docs/mcp-ia-server.md`, `docs/mcp-markdown-ia-pattern.md`, `BACKLOG.md` / `BACKLOG-ARCHIVE.md`, and **territory-ia** MCP tool outputs sampled during this write-up (`backlog_issue`, `spec_outline`, `invariants_summary`, `glossary_discover`, `router_for_task`).

---

## 1. Executive summary

| Question | Short answer |
|----------|----------------|
| Does Cursor persist “agent sessions”? | **Partially.** Conversation threads and UI history persist in the product; **durable, repo-owned “memory”** of decisions and domain state is **not** a substitute for specs, rules, backlog, and tools like **territory-ia**. |
| Should we implement persistence in development? | **Yes, at the information layer** (Markdown + MCP + Git), which this repo already does. **Optional:** third-party “memory” MCPs or future DB-backed IA (see [`BACKLOG.md`](../BACKLOG.md), [`docs/postgres-ia-dev-setup.md`](postgres-ia-dev-setup.md)) for richer retrieval—not for replacing specs. |
| Are Skills the same as agentic state? | **No.** **Skills** are **static, versioned instructions** the host may attach when relevant. **Agentic state** is **transient context** (thread, tool results, approvals). They complement each other. |
| Skills + existing MCP? | **Layered:** MCP = **pull structured facts** on demand; Skills = **how to behave** on recurring workflows. Avoid duplicating glossary/spec text inside Skills—point to MCP and `AGENTS.md` instead. |
| Specialist agents? | **Sometimes.** Narrow sub-tasks can **reduce** tokens if the parent passes a tight brief; **fan-out** and summarization can **increase** cost. **territory-ia** already targets token efficiency via **slices**. |

---

## 2. Cursor IDE: what “persists” for agents

### 2.1 What the product typically retains

- **Chat / Agent threads** in the IDE (history UI, resuming a conversation in the same workspace).
- **Project rules** under `ia/rules/` and workspace configuration (e.g. `.mcp.json`) — **Git-tracked**, so they persist across machines when committed.
- **Agent transcripts** (when enabled by the host) may be stored locally for review; this is **not** a contractual API for your game or CI — treat as **debug/audit**, not source of truth.

### 2.2 What does *not* replace project IA

- **Implicit model memory** across unrelated new chats is limited; starting a **new** thread does not automatically reload every past decision.
- **Tool outputs** from a past session are not automatically re-injected into a new session unless they appear in history or you re-run tools.
- **Cross-repo or cross-machine** consistency depends on **what is in Git** (specs, rules, backlog) and **what tools re-fetch** (MCP).

### 2.3 When to add custom “memory”

Consider an external memory MCP or DB only if you need **user-specific** or **high-churn** facts that do not belong in specs (e.g. personal preferences, experiment logs). **Domain rules** for Territory Developer should remain in `ia/specs/`, `ia/rules/`, and `BACKLOG.md` so humans and agents share one vocabulary (see `AGENTS.md` — terminology consistency).

---

## 3. Agentic state vs Skills

| Dimension | Agentic state (session) | Skills (`SKILL.md`) |
|-----------|-------------------------|---------------------|
| **Lifetime** | Bound to a conversation / task run | **Stable** until you edit the skill |
| **Content** | Messages, file edits, tool I/O, errors | Procedures, checklists, when-to-use descriptions |
| **Discovery** | Linear chat context | Host discovers skills from configured paths (e.g. project `ia/skills/`, user-level skills); activation is **description-driven** + optional explicit invocation |
| **Best for** | Executing the *current* change | **Repeatable workflows** (releases, backlog hygiene, MCP verification) |

**Conclusion:** Skills are **not** persistence of agent state; they are **packaged playbooks**. Persistence of *truth* for this game is **Git + MCP-backed docs**.

---

## 4. Integrating Skills with territory-ia MCP

### 4.1 Division of responsibility (recommended)

| Layer | Responsibility | Territory Developer example |
|-------|----------------|------------------------------|
| **Specs / glossary** | Canonical definitions | `ia/specs/glossary.md`, `isometric-geography-system.md` |
| **Rules** | Always-on guardrails | `ia/rules/invariants.md`, `agent-router.mdc` |
| **MCP (territory-ia)** | **On-demand slices** | `spec_section`, `glossary_lookup`, `backlog_issue`, `invariants_summary` |
| **Skills** | **Process** and **orchestration** | “Starting a **BACKLOG** tech row: call `backlog_issue`, then `router_for_task`, then implement; never paste full geo spec.” |

### 4.2 Anti-patterns

- **Duplicating** long spec sections inside Skills → drift from glossary/specs; use **short** pointers (“call `spec_section` for roads validation”).
- **Using Skills instead of backlog/spec updates** for decisions that affect game behavior → those belong in **`ia/projects/`** or **reference specs**, then migrated per project policy.

### 4.3 Practical incorporation steps

1. Add **project Skills** under `ia/skills/` (or team convention) for **non-domain** workflows: MCP verify, issue kickoff, PR checklist.
2. Keep **English** in Skill bodies aligned with **glossary** terms when touching domain language (same rule as MCP `glossary_*` tools).
3. Reference **open** [`BACKLOG.md`](../BACKLOG.md) rows for **DB-backed** IA: future work changes *implementation* of tools, not the **split** “facts in IA / procedures in Skills.”

### 4.4 Shipped repo skills (Part 1 + kickoff + implement + validation + close)

- **Index:** [`ia/skills/README.md`](../ia/skills/README.md) — naming rules, thin-skill policy, **`glossary_discover`** array requirement.
- **Kickoff skill:** [`ia/skills/project-spec-kickoff/SKILL.md`](../ia/skills/project-spec-kickoff/SKILL.md) — **numbered** **territory-ia** tool recipe for `ia/projects/*.md` review *(shipped — trace [`BACKLOG-ARCHIVE.md`](../BACKLOG-ARCHIVE.md))*.
- **Implement skill:** [`ia/skills/project-spec-implement/SKILL.md`](../ia/skills/project-spec-implement/SKILL.md) — **per-phase** **territory-ia** recipe to execute a project spec’s **Implementation Plan** after the spec is ready *(shipped — trace [`BACKLOG-ARCHIVE.md`](../BACKLOG-ARCHIVE.md))*.
- **Validation skill:** [`ia/skills/project-implementation-validation/SKILL.md`](../ia/skills/project-implementation-validation/SKILL.md) — ordered **`npm`** checks (**dead project spec** paths, **MCP** tests, **fixtures**, **IA index** `--check`, optional **`verify`**) aligned with **IA tools** **CI**; use after **MCP** / **schema** / index-source edits.
- **Close skill:** [`ia/skills/project-spec-close/SKILL.md`](../ia/skills/project-spec-close/SKILL.md) — **persist IA first**, delete `ia/projects/{ISSUE_ID}.md`, **`npm run validate:dead-project-specs`**, **remove** the row from **`BACKLOG.md`**, **append** **`[x]`** to **`BACKLOG-ARCHIVE.md`**, **purge** the closed id from durable docs — see skill body *(shipped — trace [`BACKLOG-ARCHIVE.md`](../BACKLOG-ARCHIVE.md))*.
- **Paste template:** [`ia/templates/project-spec-review-prompt.md`](../ia/templates/project-spec-review-prompt.md) when Skills are not loaded; **kickoff** tool order remains authoritative in **`project-spec-kickoff/SKILL.md`**; **implementation** order in **`project-spec-implement/SKILL.md`**; **post-implementation Node checks** in **`project-implementation-validation/SKILL.md`**; **closeout** order in **`project-spec-close/SKILL.md`**. **Router hint:** `router_for_task` **`domain`** strings should match **agent-router.mdc** table labels (persisted in [`ia/skills/README.md`](../ia/skills/README.md) **Lessons learned**).
- **MCP follow-up:** discovery from project-spec prose (ranked glossary / section queue) — open [`BACKLOG.md`](../BACKLOG.md).

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

Sample **invariants_summary** (abridged intent): sync height map and cell height; **InvalidateRoadCache** after roads; no new singletons; no **GridManager** bloat; shore and river constraints; specs live in `ia/specs/` vs `ia/projects/` per policy.

### 6.2 MCP sampling used for this document

- **`backlog_issue`:** Confirms shipped MCP tool set and file-backed sources — baseline for future **DB-backed** evolution.
- **`spec_outline` (`AGENTS.md`):** Confirms documentation hierarchy (specs, project specs, backlog workflow) as the **canonical** agent guide.
- **`glossary_discover`:** No rows for generic “MCP agent information” keywords — expected; domain glossary targets **game** terms, not tooling vocabulary.
- **`router_for_task`:** Domain table is **game-domain** routed (roads, water, simulation, etc.); “documentation” strings do not map — tooling is covered by **rules + AGENTS**, not the geography router.

### 6.3 Backlog alignment

- **DB-backed IA / search:** Longer-term **search and DB** for IA; complements Cursor Skills (procedures stay thin; facts stay queryable) — see [`BACKLOG.md`](../BACKLOG.md).
- **JSON interchange program** + **Postgres interchange patterns** ([`docs/postgres-interchange-patterns.md`](postgres-interchange-patterns.md), **glossary**, [`BACKLOG-ARCHIVE.md`](../BACKLOG-ARCHIVE.md)): machine-readable indexes, validated DTOs, DB/API patterns — **reduces** need for agents to “remember” schema details if **CI** and docs enforce them. **E1** repro registry shipped; **E2**/**E3** follow-ups remain on [`BACKLOG.md`](../BACKLOG.md).

---

## 7. Recommendations

1. **Treat Git + territory-ia as primary persistence** for agent-relevant truth; treat Cursor threads as **convenient but not authoritative**.
2. **Introduce Skills sparingly** for **workflows** (issue kickoff, MCP verify, release checklist), not for **copying specs**.
3. **Use specialist agents** only when the task is clearly scoped; otherwise rely on **router + `spec_section` + `backlog_issue`** to control tokens.
4. **Do not conflate** Skills with **session memory**; if you need cross-session *preferences*, evaluate a **memory MCP** separately from **domain IA**.
5. **Keep game vocabulary** in glossary and specs so Skills and MCP prompts stay **searchable and consistent** (`AGENTS.md` terminology checklist).

---

## 8. References (in-repo)

- `AGENTS.md` — agent workflow, MCP-first retrieval, `ia/projects/` policy  
- `docs/mcp-ia-server.md` — territory-ia tools and policy  
- `docs/mcp-markdown-ia-pattern.md` — slice-based IA pattern  
- `BACKLOG.md` / `BACKLOG-ARCHIVE.md` — open vs completed rows; **no** backlog ids in **glossary** / **reference specs** (see **terminology-consistency**)  
- `ia/rules/invariants.md` — system invariants (also exposed via MCP `invariants_summary`)

---

*Document type: engineering study / ADR companion (not a reference spec). For permanent game behavior definitions, use `ia/specs/` and the glossary.*
