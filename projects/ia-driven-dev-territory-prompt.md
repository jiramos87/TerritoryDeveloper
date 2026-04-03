# IA aplicada al desarrollo de Territory Developer (Spec-Driven + agentes + territory-ia)

**Propósito:** Versión del prompt en [`ia-driven-dev.md`](ia-driven-dev.md) **ajustada a este repositorio**: vocabulario canónico (glossary + specs), **territory-ia** MCP, `AGENTS.md`, **reference specs** vs **project specs**, y la realidad técnica del juego (Unity 2D isométrico, **GridManager**, **Save data**, sin PostgreSQL en el core del producto salvo futuro **TECH-42**).

**Fuentes consultadas (territory-ia):** `invariants_summary`, `router_for_task` (save/load, simulation, unity), `glossary_discover` / `glossary_lookup`, `spec_section` (**unity-development-context** §10), `backlog_issue` (**TECH-21**).

---

## 1. Contraste: prompt genérico vs este proyecto

| Aspecto | Prompt original (genérico) | Territory Developer (este repo) |
|--------|----------------------------|----------------------------------|
| **Fuente de verdad de comportamiento** | Specs abstractas | [`.cursor/specs/`](.cursor/specs/glossary.md) (geo canónica: `isometric-geography-system.md`) + [`glossary.md`](.cursor/specs/glossary.md) |
| **Issues y pipeline humano** | Issues + cron genérico | [`BACKLOG.md`](BACKLOG.md) (`BUG-` / `FEAT-` / `TECH-` / …); skills [`.cursor/skills/project-spec-kickoff`](.cursor/skills/project-spec-kickoff/SKILL.md) y [project-spec-implement](.cursor/skills/project-spec-implement/SKILL.md) |
| **MCP** | Tools + skills genéricos | **territory-ia** (`backlog_issue`, `spec_section`, `glossary_*`, `router_for_task`, `invariants_summary`, …) — ver [`docs/mcp-ia-server.md`](../docs/mcp-ia-server.md) |
| **Persistencia / DB** | PostgreSQL en el ejemplo | **Save data** y **Load pipeline** en runtime Unity ([`persistence-system.md`](../.cursor/specs/persistence-system.md)); programa **JSON** **TECH-21** → **TECH-40**/**41**/**42** para esquemas, DTOs y *futuros* envelopes DB/API |
| **Exposición runtime → agente** | API HTTP, WebSockets, etc. (idea) | **Ya:** menús Editor **Territory Developer → Reports** → export JSON/Markdown bajo `tools/reports/` (**Agent context**, **Sorting debug**) — [`unity-development-context.md`](../.cursor/specs/unity-development-context.md) §10 |
| **Riesgos de arquitectura** | ECS, control loop genérico | **Invariants** estrictos: `HeightMap[x,y]` == `Cell.height`; **roads** vía familia de preparación → `PathTerraformPlan` + Phase-1 + `Apply`; sin nuevos singletons; sin `gridArray`/`cellArray` fuera de **GridManager** — usar **`GetCell(x, y)`** |

---

## 2. Prompt ajustado (copiar para otro agente o chat)

Use este bloque como **system / user prompt** cuando quieras que un agente trabaje **dentro** de Territory Developer:

```markdown
You are assisting on **Territory Developer**: Unity 2D isometric city-builder (C#, MonoBehaviour managers).

**Authoritative context (use in this order):**
1. **territory-ia MCP** when available: `backlog_issue` for BUG-/FEAT-/TECH- ids → `invariants_summary` → `router_for_task` for the task domain → `glossary_discover` / `glossary_lookup` (queries in **English**) → `spec_section` / `spec_outline`. Do not read entire `.cursor/specs/*.md` files when a slice suffices.
2. **AGENTS.md** workflow; **.cursor/rules/invariants.mdc** — never violate invariants or guardrails.
3. **Canonical geography**: `isometric-geography-system.md` wins over other docs for grid math, **HeightMap**, **Water map**, roads, rivers, **Sorting order**.

**Vocabulary:** Use glossary-linked terms: **Cell**, **HeightMap**, **Water map**, **Save data**, **Load pipeline**, **Road validation pipeline**, **Terraform plan**, **Shore band**, **River** / **River bed (H_bed)**, **Geography initialization**, **AUTO** simulation pipeline, **Sorting order**, etc. Do not invent synonyms for documented concepts.

**Code constraints:**
- Access cells only via **GridManager.GetCell(x, y)** — no direct **gridArray** / **cellArray**.
- On **HeightMap** or **Cell.height** writes, keep both in sync.
- After road graph changes, **InvalidateRoadCache()**.
- Road placement: **road preparation family** ending in **PathTerraformPlan** + Phase-1 + **Apply** — never **ComputePathPlan** alone.
- New managers: MonoBehaviour in scene, **SerializeField** + **FindObjectOfType** fallback in Awake — no new singletons (except documented **GameNotificationManager**).

**Specs:**
- Permanent behavior: `.cursor/specs/` only.
- Active feature/bug specs: `.cursor/projects/{ISSUE_ID}.md` from template; close by migrating lessons to canonical docs.

**Runtime → agent friction reduction:**
- Prefer **Editor** exports: **Territory Developer → Reports → Export Agent Context** → `tools/reports/agent-context-*.json` (bounded **grid** sample: **Cell**, **HeightMap**, **WaterMap** fields per spec).
- For **Sorting order** issues, **Export Sorting Debug (Markdown)** in **Play Mode** with initialized **GridManager**.

**Testing:** Prefer **Unity Test Framework** where added; align tests with spec acceptance and invariants. No broad test suite is assumed today — propose minimal tests per change.

**JSON / interchange program:** Respect **TECH-21** charter and children **TECH-40**, **TECH-41**, **TECH-42**; see `projects/TECH-21-json-use-cases-brainstorm.md` for snapshot / **cell** chunk / **Geography initialization** ideas.

Deliver: concrete file paths, spec citations, and changes that respect the above.
```

---

## 3. Respuestas al documento original (preguntas 1–6), ancladas al proyecto

### 3.1 Testing y validación automatizada

- **¿En qué etapa del pipeline integrar tests?**  
  Después de **enriquecer** el **project spec** y **antes** de dar por cerrada la implementación: la **Acceptance** del issue (y las secciones del spec) deben mapearse a pruebas ejecutables. En CI del repo, los chequeos **JSON Schema** (**TECH-40**) ya son una capa de validación *fuera* de Unity; dentro de Unity, el punto natural es **post-implementación** y **regresión** antes de mover el issue a **Completed** (solo con confirmación humana, según `AGENTS.md`).

- **¿Qué tipos de testing?**  
  - **Unit tests (Edit Mode):** lógica pura o helpers extraídos (alineado con *no* hinchar **GridManager** — extraer a clases helper testeables).  
  - **Play Mode tests:** escenas con **GridManager** inicializado, **Road validation pipeline**, **Water map**, **Load pipeline** (con coste mayor).  
  - **“Simulation testing”:** fijar **simulation tick** / **AUTO** en escenarios reproducibles (**TECH-16** / harness JSON en backlog relacionado) — encaja con el programa **JSON** y fixtures, no sustituye specs.

- **TDD + Spec-Driven + agentes:**  
  Escribir primero la **Acceptance** en **BACKLOG.md** y criterios en `.cursor/projects/{ISSUE_ID}.md` en vocabulario canónico; luego tests que fallen; luego implementación. Los agentes usan **territory-ia** para no “inventar” reglas que ya están en **geo §13** (roads) o **simulation-system**.

### 3.2 Testing en Unity con soporte de IA

- **Prácticas:** Unity Test Framework, ensamblados `*.Tests`, evitar **FindObjectOfType** en bucles de test por frame (mismo invariante que en juego).  
- **Generar / ejecutar / evaluar por agentes:** el agente lee resultados de `UnityTest` en CI o salida local; para **estado del mundo**, adjuntar `tools/reports/agent-context-*.json` al prompt tras exportar desde el Editor.

### 3.3 Exposición del estado del runtime

- **Técnicas ya previstas en spec:** exportación **machine-readable** §10 de **unity-development-context** (`agent-context` JSON, `sorting-debug` MD).  
- **Ampliación coherente con backlog:** **G1** / **G2** en [`TECH-21-json-use-cases-brainstorm.md`](TECH-21-json-use-cases-brainstorm.md) (**world_snapshot**, **cell_chunk**) — siempre **read-only** y respetando consistencia **HeightMap** / **Cell.height**.  
- **HTTP/WebSockets:** candidatas a **TECH-42** (envelopes API), no requisito inmediato si Editor + archivos + MCP cubren IA en IDE.

### 3.4 Integración Unity ↔ IA (IDE / agentes)

- **Reducir fricción:** menús **Reports** + `@tools/reports/...` en el chat; **territory-ia** para evitar pegar specs enteras.  
- **Patrones:**  
  - *Observability-first*: exports acotados y glosario-alineados (§10).  
  - *Debugging interfaces for agents*: **Sorting debug** MD + **Agent context** JSON.  
  - *Game state as a service*: solo si se formaliza servidor o batchmode headless; el producto actual es **Editor/Play Mode** centrado.

### 3.5 Herramientas y ecosistema

- **Unity Test Framework** — adecuado cuando se añadan tests.  
- **territory-ia** + índices **TECH-40** (spec/glossary indexes) — ya en el repo.  
- **Zod / JSON Schema** — validación CI para payloads (**Geography initialization**, etc.).  
- **Instrumentación:** no duplicar la fórmula de **Sorting order** fuera de **isometric-geography-system** §7; usar APIs públicas de **TerrainManager** donde el spec lo indique.

### 3.6 Diseño de herramientas propias (complejidad y arquitectura)

- **Complejidad:** exportar lecturas acotadas (JSON/MD) es **baja**; **control loop** remoto (acciones sobre el runtime) es **alta** (seguridad, determinismo, **invariants**).  
- **Arquitectura recomendada aquí:** **event-driven** en el sentido de “acción del jugador / simulación produce efectos acotados”; **cliente-servidor** solo si hay backend explícito (**TECH-42**). **ECS** no es el modelo dominante documentado; priorizar **managers** + helpers testeables.

---

## 4. Objetivo final (reformulado para Territory Developer)

- Los agentes **entienden** el estado vía **territory-ia**, specs parciales, y exports **Agent context** / **Sorting debug**.  
- **Validan** implementaciones contra **invariants**, **Road validation pipeline**, **Save data** / **Load pipeline**, y esquemas **JSON** del programa **TECH-21**.  
- **Proponen cambios** en código y specs temporales (`.cursor/projects/`) sin violar guardrails (**GridManager**, **roads**, **water**/**shore**).

---

## 5. Ejemplos breves (brainstorming) por propuesta

> Cada ejemplo es **ilustrativo** (no necesariamente implementado). Vocabulario en línea con glossary/specs.

### 5.1 Pipeline de issue → spec → tests → código

- **Ejemplo:** Issue **BUG-XX** “**wet run** mal clasificado en esquina de **Road validation pipeline**”. Agente: `backlog_issue` → `spec_section` **geo** §13 → test Play Mode que coloca un trazo y aserta prefab esperado → fix en helper de carreteras → `InvalidateRoadCache()` en el camino de apply.

### 5.2 Unit test (helper extraído de **GridManager**)

- **Ejemplo:** Extraer cálculo de **Chebyshev distance** o de coste de **Pathfinding cost model** a clase estática; test Edit Mode con tabla de casos tomada de **geo §10** comentarios de spec.

### 5.3 Play Mode: invariante **HeightMap** / **Cell.height**

- **Ejemplo:** Tras operación de terraform de prueba, iterar una muestra de celdas con **GetCell** y comprobar `height == HeightMap[x,y]`.

### 5.4 Uso de **Export Agent Context**

- **Ejemplo:** Tras reproducir un bug de **Water map** / **Shore band**, el desarrollador exporta JSON, lo referencia en Cursor como `@tools/reports/agent-context-….json`, y pide al agente comparar con **geo** §11 / **water-terrain-system**.

### 5.5 Uso de **Export Sorting Debug**

- **Ejemplo:** Bug visual en **Cliff** / capas 2D: export MD en Play Mode; el agente cruza con **Sorting order** en **isometric-geography-system** §7 sin rederivar la fórmula a mano.

### 5.6 **TDD + spec** para **Geography initialization**

- **Ejemplo:** Nuevo fixture `geography-init-params.good.json` bajo `docs/schemas/fixtures/` falla CI si el schema cambia; luego código **parse-once** (**TECH-41**) que consume el DTO.

### 5.7 Snapshot **G1** (brainstorm **TECH-21**)

- **Ejemplo:** Menú dev “Export **world_snapshot** (bounds 32×32)” escribe JSON con **cells** parciales y resumen de cuerpos de agua; agente externo valida con **Zod** y detecta **Junction** inconsistente.

### 5.8 **cell_chunk** **G2**

- **Ejemplo:** Script de revisión que pide por CLI un chunk centrado en (x,y) y valida **waterBodyId** contra reglas de **Open water** / **Rim** descritas en spec.

### 5.9 **territory-ia** como “API de lectura” para el agente

- **Ejemplo:** Antes de tocar **rivers**, `router_for_task` “water” → `spec_section` **water** o **geo** §12 → `glossary_lookup` “River bed (H_bed)” para no violar monotonía **H_bed** hacia la salida.

### 5.10 Regresión **Save data** (sin PostgreSQL)

- **Ejemplo:** Test o checklist manual: guardar partida, **Load pipeline**, comparar hash de subset de **CellData** o conteo de **zones** (según **persistence-system**).

### 5.11 Cron / CI (sustituto del “cron” del prompt original)

- **Ejemplo:** Workflow GitHub Actions que ejecuta `npm test` en `tools/mcp-ia-server` + validación **JSON Schema** de `docs/schemas/`; Unity tests opcionales cuando existan en el proyecto.

### 5.12 “Control loop” remoto (futuro, **TECH-42**)

- **Ejemplo:** Servicio dev que acepta solo comandos idempotentes (“simular **simulation tick** N”, “exportar snapshot”) — sin exponer escritura directa en **HeightMap** sin pasar por **Terraform plan** / managers.

### 5.13 Derivación de nuevos issues (paso 7 del pipeline original)

- **Ejemplo:** Al cerrar un spec, el agente propone **TECH-** o **BUG-** hijos en **BACKLOG.md** cuando la **Decision Log** deje deuda (p. ej. **InvalidateRoadCache** faltante en un camino).

### 5.14 Skill **project-spec-kickoff** + MCP

- **Ejemplo:** Para **FEAT-YY**, el humano dispara kickoff; el agente recorre `glossary_discover` con keywords en inglés (“growth ring”, “AUTO”) y reescribe **Open Questions** solo con términos canónicos.

### 5.15 Skill **project-spec-implement**

- **Ejemplo:** Checklist fase por fase: tras cada fase, export **Agent context** y un test mínimo nuevo que cubra la **Acceptance** añadida.

---

## 6. Checklist rápido para el autor del prompt

- [ ] ¿El issue id existe en **BACKLOG.md** y se llamó `backlog_issue`?  
- [ ] ¿Se leyeron **invariants** antes de tocar **roads** / **water** / **HeightMap**?  
- [ ] ¿Los términos de producto están en inglés canónico en **Open Questions** / **Acceptance**?  
- [ ] ¿Hay archivo `@tools/reports/agent-context-….json` o **Sorting debug** si el bug es de mundo o capas?  
- [ ] ¿Cambios **JSON** alineados con **TECH-21** y schemas en `docs/schemas/`?

---

*Documento generado para alinear el prompt genérico de `ia-driven-dev.md` con Territory Developer. Para detalle de payloads JSON, seguir [`TECH-21-json-use-cases-brainstorm.md`](TECH-21-json-use-cases-brainstorm.md) y los charter **TECH-21** / **TECH-40**–**42**.*
