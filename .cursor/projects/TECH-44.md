# TECH-44 — PostgreSQL + interchange patterns (merged implementation program)

> **Program charter** (merged **TECH-19** + **TECH-42**, retired). **Issues:** [TECH-44a](../../BACKLOG.md), [TECH-44b](../../BACKLOG.md), [TECH-44c](../../BACKLOG.md), [TECH-53](../../BACKLOG.md), [TECH-54](../../BACKLOG.md)  
> **Parent program:** [TECH-21](TECH-21.md) (**Phase C** documentation = **TECH-44a**)  
> **Status:** Draft  
> **Created:** 2026-04-03  
> **Last updated:** 2026-04-03

## 1. Summary

Single **implementation program** for: (1) **normative JSON/SQL patterns** for future **PostgreSQL** rows and HTTP/sync clients (**B1** row + **JSONB**, **B3** idempotent **patch** **envelope**, **P5** streaming guidance, SQL vs **Interchange JSON** naming); (2) **first real database** milestone (**IA** tables, migrations, minimal read surface); (3) **E1** — **repro bundle registry** (**TECH-44c**). **E2** and **E3** are tracked as **[TECH-53](../../BACKLOG.md)** and **[TECH-54](../../BACKLOG.md)** (backlog-only rows, no project specs). Player **Save data** (`GameSaveData` / **CellData** / **WaterMapData**, **Load pipeline**) stays on **persistence-system** until a **dedicated migration issue**.

**Execute in order:** **TECH-44a** → **TECH-44b** → **TECH-44c**; **TECH-53** / **TECH-54** typically after **TECH-44b** (see each row’s **Depends on**).

| Phase | Issue | Spec | Deliverable type |
|-------|-------|------|------------------|
| A | **TECH-44a** | [TECH-44a.md](TECH-44a.md) | **Documentation** — patterns (**B1**, **B3**, **P5**, naming table) |
| B | **TECH-44b** | [TECH-44b.md](TECH-44b.md) | **Implementation** — Postgres migrations, **IA** schema, read path pilot |
| C | **TECH-44c** | [TECH-44c.md](TECH-44c.md) | **Implementation** — **E1** repro bundle registry |
| Follow-up | **TECH-53** | none | **E2** — schema validation history (backlog-only) |
| Follow-up | **TECH-54** | none | **E3** — agent patch proposal staging (backlog-only) |

## 2. Is this a good structure?

**Pros:** One charter avoids split-brain between “patterns only” and “DB only”; **44a** gates naming before **44b** writes migrations; **44c** forces a thin vertical slice. **Cons:** More backlog rows; **TECH-21** Phase C completion is only **44a** (docs), while **TECH-44** program completion for the umbrella checklist is **44a** + **44b** + **44c** — **TECH-53**/**TECH-54** are optional program extensions tracked separately.

## 3. Extension IDs (**E1**–**E3**) — canonical mapping

| ID | Direction | Backlog row | Spec |
|----|-----------|-------------|------|
| **E1** | **Repro bundle registry** | **TECH-44c** | [TECH-44c.md](TECH-44c.md) |
| **E2** | **Schema validation history** | **TECH-53** | none |
| **E3** | **Agent patch proposal staging** | **TECH-54** | none |

**Game-adjacent analysis (carry-forward):** Offline **FEAT-47** / **FEAT-48** studies may still use **B1** **JSONB** payloads when implemented under future issues; record field conventions in **TECH-44a** **Decision Log** when those issues land.

**Explicit out of scope here:** Player **Save data** migration; **Markdown** replacement (**TECH-18**); **B2** append-only lines (**TECH-43**). Former **E4**–**E8** ideas were **discarded** from this program (no backlog rows).

## 4. Program acceptance (TECH-44)

- [ ] **TECH-44a** §8 acceptance satisfied (patterns documented).
- [ ] **TECH-44b** §8 acceptance satisfied (migrations + read pilot).
- [ ] **TECH-44c** §8 acceptance satisfied (**E1** rows + docs).
- [ ] **TECH-21** Phase C: **TECH-44a** satisfies former **TECH-42** charter slot (update [TECH-21.md](TECH-21.md) when closing).

## 5. Related docs

- [`projects/ia-driven-dev-backend-database-value.md`](../../projects/ia-driven-dev-backend-database-value.md) — workflow mapping to **B1**/**B3**/**P5**.
- [`projects/TECH-21-json-use-cases-brainstorm.md`](../../projects/TECH-21-json-use-cases-brainstorm.md) — **G1**/**G2**, **B1**/**B3**/**P5** pointers.
- [TECH-18](TECH-18.md) — **Depends on** **TECH-44b**.

## 6. Decision Log

| Date | Decision | Rationale |
|------|----------|-----------|
| 2026-04-03 | Merge **TECH-19** + **TECH-42** → **TECH-44** + **44a**/**b**/**c** | Single charter; ordered delivery; **E1** as first vertical slice |
| 2026-04-03 | **TECH-44c** = **E1** only | Highest leverage for **AI-assisted** debugging |
| 2026-04-03 | **E2**/**E3** → **TECH-53**/**TECH-54** (no project specs); **E4**–**E8** removed | Narrow program scope; discard unscheduled extensions |

## Open Questions

- **TECH-44b** milestone 1: normalized **IA** only vs **JSONB** column for experiments — record in **TECH-44b** **Decision Log** (**TECH-44a** patterns apply if **JSONB** is chosen).
