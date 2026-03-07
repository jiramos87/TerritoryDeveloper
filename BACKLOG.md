# Backlog — Territory Developer

> Fuente de verdad para issues del proyecto. Ordenado por prioridad (mayor arriba).
> Para trabajar un issue: referenciarlo con `@BACKLOG.md` en la conversación de Cursor.

---

## En progreso


## Prioridad alta

- [ ] **BUG-01** — Save game, Load game y New game están rotos
  - Tipo: fix
  - Archivos: `GameSaveManager.cs`, `GridManager.cs` (GetGridData/RestoreGrid), `CellData.cs`, `GameManager.cs`
  - Notas: Funcionalidad core del juego, bloquea testing de cualquier sesión larga.
  - Depende de: BUG-09 (root cause)

- [ ] **BUG-09** — `Cell.GetCellData()` no serializa el estado de la celda
  - Tipo: fix (crítico)
  - Archivos: `Cell.cs` (GetCellData), `CellData.cs` (SetDefaults)
  - Notas: Root cause de BUG-01. `GetCellData()` llama `SetDefaults()` que sobreescribe todos los campos. Solo se preservan `x`, `y` e `isInterstate`. Se pierden: zoneType, prefabName, buildingType, population, roads, forests, desirability.

- [ ] **BUG-10** — `IndustrialHeavyZoning` nunca genera edificios
  - Tipo: fix
  - Archivos: `TimeManager.cs` (PlaceAllZonedBuildings)
  - Notas: `PlaceAllZonedBuildings` llama a 8 de 9 tipos de zona pero omite `IndustrialHeavyZoning`. Los edificios industriales pesados nunca se construyen.

- [ ] **BUG-11** — Demand usa `Time.deltaTime` causando dependencia del framerate
  - Tipo: fix
  - Archivos: `DemandManager.cs`
  - Notas: `Mathf.Lerp(..., demandSensitivity * Time.deltaTime)` hace que la demanda cambie diferente a 30 FPS vs 120 FPS. Debe usar delta fijo diario.

- [ ] **BUG-12** — Happiness UI siempre muestra 50%
  - Tipo: fix
  - Archivos: `CityStatsUIController.cs` (GetHappiness)
  - Notas: `GetHappiness()` retorna `50.0f` hardcodeado en vez de leer `cityStats.happiness`.

- [ ] **BUG-18** — Road preview and placement draw discontinuous lines instead of continuous paths
  - Tipo: fix
  - Archivos: `RoadManager.cs`, `GridManager.cs`
  - Notas: When drawing roads, moving the mouse produces discrete, broken line figures across cells instead of a continuous path. The underlying coordinate/cell calculation is likely continuous (floating-point) rather than discrete, causing gaps in the cell sequence. Fix should ensure the path is always a connected chain of cells. For diagonal gaps, use the existing diagonal road prefabs; for other discontinuities, insert an extra bridging prefab at each break point so the road is visually and logically continuous in both preview and final placement.

- [ ] **BUG-02** — Taxes no funcionan
  - Tipo: fix
  - Archivos: `EconomyManager.cs`, `CityStats.cs`
  - Notas: Sin impuestos no hay loop económico funcional.

- [ ] **BUG-03** — Growth % setea monto en vez de porcentaje del presupuesto total
  - Tipo: fix
  - Archivos: `GrowthManager.cs`, `GrowthBudgetManager.cs`, `CityStats.cs`
  - Notas: Debe ser porcentaje del presupuesto total de la ciudad.

- [ ] **BUG-04** — Modo pausa detiene movimiento de cámara; velocidad de cámara ligada a velocidad de simulación
  - Tipo: fix
  - Archivos: `CameraController.cs`, `TimeManager.cs`
  - Notas: Cámara y simulación deben ser independientes. Velocidad de cámara debe depender de la altura relativa de la cámara versus el mapa.

- [ ] **BUG-05** — No quitar cursor de edificios al construir
  - Tipo: fix
  - Archivos: `GridManager.cs`, `CursorManager.cs`, `UIManager.cs`
  - Notas: El usuario puede querer seguir construyendo el mismo edificio. Mantener selección activa post-placement.

- [ ] **BUG-14** — `FindObjectOfType` en Update/per-frame degrada performance
  - Tipo: fix (performance)
  - Archivos: `CursorManager.cs` (Update), `UIManager.cs` (UpdateUI)
  - Notas: `CursorManager.Update()` llama `FindObjectOfType<UIManager>()` cada frame. `UIManager.UpdateUI()` llama `FindObjectOfType` para 4 managers repetidamente. Deben cachearse en Start().

- [ ] **BUG-15** — `UrbanizationProposalManager` no está conectado a la simulación
  - Tipo: fix
  - Archivos: `SimulationManager.cs`, `UrbanizationProposalManager.cs`
  - Notas: `SimulationManager.ProcessSimulationTick()` nunca llama `UrbanizationProposalManager.ProcessTick()`. Las propuestas de urbanización están deshabilitadas.

## Prioridad media

- [ ] **BUG-13** — `FindObjectOfType<TimeManager>()` se llama cada tick en UrbanizationProposalManager
  - Tipo: fix (performance)
  - Archivos: `UrbanizationProposalManager.cs` (ProcessTick)
  - Notas: `FindObjectOfType` es costoso y se ejecuta en cada tick de simulación. Cachear en Start().

- [ ] **BUG-16** — Posible race condition en inicialización GeographyManager vs TimeManager
  - Tipo: fix
  - Archivos: `GeographyManager.cs`, `TimeManager.cs`, `GridManager.cs`
  - Notas: Unity no garantiza orden de Start(). Si TimeManager.Update() corre antes de que GeographyManager cree el grid, puede acceder a datos inexistentes. Usar Script Execution Order o gate con `isInitialized`.

- [ ] **BUG-17** — `cachedCamera` es null al crear `ChunkCullingSystem`
  - Tipo: fix
  - Archivos: `GridManager.cs`
  - Notas: En InitializeGrid() se crea ChunkCullingSystem con `cachedCamera`, pero esta se asigna recién en Update(). Puede causar NullReferenceException.

- [ ] **FEAT-01** — Agregar cambio delta al presupuesto total (ej: $25,000 (+$1,200))
  - Tipo: feature
  - Archivos: `EconomyManager.cs`, `CityStatsUIController.cs`, `UIManager.cs`
  - Notas: Feedback visual del flujo económico por turno.

- [ ] **FEAT-02** — Agregar contador de costo de construcción al cursor del mouse
  - Tipo: feature
  - Archivos: `CursorManager.cs`, `UIManager.cs`, `GridManager.cs`
  - Notas: Mostrar costo antes de confirmar placement.

- [ ] **FEAT-21** — Sistema de gastos y mantenimiento
  - Tipo: feature
  - Archivos: `EconomyManager.cs`, `CityStats.cs`
  - Notas: No hay gastos: ni mantenimiento de calles, ni costo de servicios, ni salarios. Sin gastos no hay tensión económica. Agregar upkeep por calles, edificios públicos y servicios.

- [ ] **FEAT-22** — Feedback de impuestos sobre demanda y felicidad
  - Tipo: feature
  - Archivos: `EconomyManager.cs`, `DemandManager.cs`, `CityStats.cs`
  - Notas: Impuestos altos no afectan demanda ni felicidad. Loop: impuestos altos → menos demanda residencial → menos crecimiento → menos ingresos.
  - Depende de: BUG-02

- [ ] **FEAT-23** — Happiness dinámico basado en condiciones de la ciudad
  - Tipo: feature
  - Archivos: `CityStats.cs`, `DemandManager.cs`, `EmploymentManager.cs`
  - Notas: Happiness solo sube al colocar zonas (+100 por edificio). No hay efecto de desempleo, impuestos, servicios ni contaminación. Debería ser cálculo continuo multi-factor con decaimiento.
  - Depende de: BUG-12

- [ ] **FEAT-24** — Auto-zoning de densidad Media y Pesada
  - Tipo: feature
  - Archivos: `AutoZoningManager.cs`, `DemandManager.cs`
  - Notas: AutoZoningManager solo coloca zonas Light. Debería soportar Medium/Heavy basado en demanda o nivel de desarrollo de la zona.

- [ ] **FEAT-25** — Presupuesto de crecimiento ligado a ingresos reales
  - Tipo: feature
  - Archivos: `GrowthBudgetManager.cs`, `EconomyManager.cs`
  - Notas: Budget usa monto fijo (default 5000) no relacionado con ingresos. Debería ser porcentaje del ingreso mensual proyectado.
  - Depende de: BUG-02, BUG-03

- [ ] **FEAT-26** — Usar desirability para selección de spawn de edificios
  - Tipo: feature
  - Archivos: `ZoneManager.cs`, `DemandManager.cs` (GetCellDesirabilityBonus)
  - Notas: `DemandManager.GetCellDesirabilityBonus()` existe pero no se usa para decidir dónde construir. Edificios deberían preferir zonas con mayor desirability.

- [ ] **BUG-06** — Calles no deben costar tanta energía
  - Tipo: fix/balance
  - Archivos: `RoadManager.cs`, `CityStats.cs`, `EconomyManager.cs`
  - Notas: Rebalancear costo energético de calles.

- [ ] **BUG-07** — Repartir mejor las zonas: menos aleatorias, más homogéneas por barrios/sectores
  - Tipo: fix
  - Archivos: `AutoZoningManager.cs`, `ZoneManager.cs`, `DemandManager.cs`
  - Notas: Las zonas se distribuyen de forma muy aleatoria y mezclada. Deberían agruparse en sectores coherentes.

- [ ] **FEAT-03** — Modo bosque mantener apretado (hold-to-place)
  - Tipo: feature
  - Archivos: `ForestManager.cs`, `GridManager.cs`
  - Notas: Actualmente requiere click por celda. Permitir drag continuo.

- [ ] **FEAT-04** — Herramienta de spray azaroso de bosque
  - Tipo: feature
  - Archivos: `ForestManager.cs`, `GridManager.cs`, `CursorManager.cs`
  - Notas: Colocar bosque en área con distribución aleatoria tipo spray/brush.

- [ ] **BUG-08** — Más ríos pequeños, ríos llegan a lagos, definir un mar en esquina/borde
  - Tipo: fix/feature
  - Archivos: `WaterManager.cs`, `WaterMap.cs`, `GeographyManager.cs`
  - Notas: Mejorar generación hídrica del mapa.

- [ ] **FEAT-05** — Calles deben poder subir por pendientes diagonales usando prefabs ortogonales
  - Tipo: feature
  - Archivos: `RoadManager.cs`, `TerrainManager.cs`, `GridManager.cs`
  - Notas: Actualmente las calles no suben pendientes diagonales.

- [ ] **FEAT-06** — Bosque que crece con el tiempo: sparse → medium → dense
  - Tipo: feature
  - Archivos: `ForestManager.cs`, `ForestMap.cs`, `SimulationManager.cs`
  - Notas: Sistema de maduración de bosques a lo largo del tiempo de simulación.

- [ ] **FEAT-07** — Probar que funcione randomized spawning para zones
  - Tipo: feature/test
  - Archivos: `ZoneManager.cs`, `GrowthManager.cs`
  - Notas: Verificar que el spawning aleatorio de edificios en zonas funcione correctamente.

- [ ] **FEAT-08** — Simulación de plusvalía, respawning y evolución a edificios mayores
  - Tipo: feature
  - Archivos: `GrowthManager.cs`, `ZoneManager.cs`, `DemandManager.cs`, `CityStats.cs`
  - Notas: Edificios existentes evolucionan a versiones mayores basado en plusvalía de la zona.

## Code Health (deuda técnica)

- [ ] **TECH-01** — Extraer responsabilidades de archivos gigantes (GridManager, TerrainManager, CityStats, ZoneManager, UIManager, RoadManager)
  - Tipo: refactor
  - Archivos: `GridManager.cs` (1538 líneas), `TerrainManager.cs` (1330), `CityStats.cs` (1199), `ZoneManager.cs` (1170), `UIManager.cs` (1054), `RoadManager.cs` (1019)
  - Notas: Ya se extrajeron helpers (GridPathfinder, GridSortingOrderService, etc.). Candidatos pendientes: BulldozeHandler (~200 líneas), GridInputHandler (~130 líneas), CoordinateConversionService (~230 líneas) desde GridManager.

- [ ] **TECH-02** — Cambiar campos públicos a `[SerializeField] private` en managers
  - Tipo: refactor
  - Archivos: `ZoneManager.cs`, `RoadManager.cs`, `GridManager.cs`, `CityStats.cs`, `AutoZoningManager.cs`, `AutoRoadBuilder.cs`, `UIManager.cs`, `WaterManager.cs`, `UrbanizationProposalManager.cs`
  - Notas: Dependencias y prefabs expuestos como `public` permiten acceso accidental desde cualquier clase. Usar `[SerializeField] private` para encapsular.

- [ ] **TECH-03** — Extraer magic numbers a constantes o ScriptableObjects
  - Tipo: refactor
  - Archivos: múltiples (GridManager, CityStats, RoadManager, UIManager, TimeManager, TerrainManager, WaterManager, EconomyManager, ForestManager, InterstateManager, etc.)
  - Notas: Costos de edificios, balance económico, parámetros de generación, sorting order offsets, fechas iniciales, probabilidades — todos hardcodeados. Extraer a constantes nombradas o ScriptableObject de configuración para facilitar tuning.

- [ ] **TECH-04** — Eliminar acceso directo a `gridArray`/`cellArray` fuera de GridManager
  - Tipo: refactor
  - Archivos: `WaterManager.cs`, `GridSortingOrderService.cs`, `GeographyManager.cs`, `BuildingPlacementService.cs`
  - Notas: Regla del proyecto: usar `GetCell(x, y)` en vez de acceso directo al array. Varias clases violan esto.

- [ ] **TECH-05** — Extraer patrón duplicado de resolución de dependencias
  - Tipo: refactor
  - Archivos: ~25+ managers con bloque `if (X == null) X = FindObjectOfType<X>()`
  - Notas: Considerar método helper, clase base, o extension method para reducir duplicación del patrón Inspector + FindObjectOfType fallback.

## Prioridad baja

- [ ] **FEAT-09** — Comercio / Producción / Sueldos
  - Tipo: feature (sistema nuevo)
  - Archivos: `EconomyManager.cs`, `CityStats.cs` (+ nuevos managers)
  - Notas: Sistema económico de producción, comercio entre zonas y sueldos.

- [ ] **FEAT-10** — Aporte regional: bonificación mensual por pertenecer al estado
  - Tipo: feature
  - Archivos: `EconomyManager.cs`, `CityStats.cs`, `RegionalMapManager.cs`
  - Notas: Ingreso adicional mensual por pertenecer a red regional.

- [ ] **FEAT-11** — Nivel educativo / Escuelas
  - Tipo: feature (sistema nuevo)
  - Archivos: nuevos managers + `CityStats.cs`, `DemandManager.cs`
  - Notas: Sistema de educación que afecta demanda y crecimiento.

- [ ] **FEAT-12** — Seguridad / Orden / Policía
  - Tipo: feature (sistema nuevo)
  - Archivos: nuevos managers + `CityStats.cs`
  - Notas: Sistema de seguridad pública.

- [ ] **FEAT-13** — Incendio / Riesgo de incendio / Bomberos
  - Tipo: feature (sistema nuevo)
  - Archivos: nuevos managers + `CityStats.cs`
  - Notas: Sistema de riesgo de incendio y servicio de bomberos.

- [ ] **FEAT-14** — Sistema de tránsito vehicular / animaciones de tránsito
  - Tipo: feature (sistema nuevo)
  - Archivos: nuevo manager + `RoadManager.cs`, `GridManager.cs`
  - Notas: Vehículos que circulan por las calles.

- [ ] **FEAT-15** — Sistema de puertos / animaciones de cargueros
  - Tipo: feature (sistema nuevo)
  - Archivos: nuevo manager + `WaterManager.cs`
  - Notas: Requiere sistema de agua con mar definido (depende de BUG-08).

- [ ] **FEAT-16** — Sistema de trenes / animaciones de trenes
  - Tipo: feature (sistema nuevo)
  - Archivos: nuevo manager + `GridManager.cs`
  - Notas: Red ferroviaria y animaciones.

- [ ] **FEAT-17** — Mini-map
  - Tipo: feature
  - Archivos: `CameraController.cs`, `UIManager.cs` (+ nuevo controller)
  - Notas: Vista miniatura del mapa completo para navegación rápida.

- [ ] **FEAT-18** — Terrain generator (mejorado)
  - Tipo: feature
  - Archivos: `TerrainManager.cs`, `GeographyManager.cs`, `HeightMap.cs`
  - Notas: Generador de terreno con más control y variedad.

- [ ] **FEAT-19** — Giro de mapa / prefabs
  - Tipo: feature
  - Archivos: `CameraController.cs`, `GridManager.cs`, todos los managers de rendering
  - Notas: Rotación de vista isométrica. Impacto alto en sorting order y rendering.

- [ ] **FEAT-20** — Pantalla de inicio
  - Tipo: feature
  - Archivos: nueva scene + managers de UI
  - Notas: Menú principal con New Game, Load Game, Settings.

- [ ] **ART-01** — Prefabs faltantes: bosques en pendiente SE, NE, SW, NW
  - Tipo: arte/assets
  - Archivos: prefabs en `Assets/Prefabs/`, `ForestManager.cs`

- [ ] **ART-02** — Prefabs faltantes: residencial (2 heavy 1x1/2x2, light 2x2, medium 1x1)
  - Tipo: arte/assets
  - Archivos: prefabs en `Assets/Prefabs/`, `ZoneManager.cs`

- [ ] **ART-03** — Prefabs faltantes: comercial (2 heavy 2x2/1x1, light 2x2, medium 2x2)
  - Tipo: arte/assets
  - Archivos: prefabs en `Assets/Prefabs/`, `ZoneManager.cs`

- [ ] **ART-04** — Prefabs faltantes: industrial (2 heavy 2x2/1x1, light 1x1, 2 medium 1x1/2x2)
  - Tipo: arte/assets
  - Archivos: prefabs en `Assets/Prefabs/`, `ZoneManager.cs`

- [ ] **AUDIO-01** — Audio FX: demolición, placement, zoning, forest, 3 temas musicales, efectos de ambiente
  - Tipo: audio/feature
  - Archivos: nuevo AudioManager + assets de audio
  - Notas: Efectos de ambiente deben variar según posición y altura de la cámara sobre el mapa.

---

## Completados (últimos 30 días)

- [x] **DONE** — Bosque no se puede colocar adyacente a agua (2026-03)
- [x] **DONE** — Demoler bosques en todas las alturas + todos los tipos de edificio (2026-03)
- [x] **DONE** — Al demoler bosque en pendiente se repone prefab de terreno correcto por lectura a heightMap (2026-03)
- [x] **DONE** — Interstate Road (2026-03)
- [x] **DONE** — CityNetwork sim (2026-03)
- [x] **DONE** — Bosques en pendientes (2026-03)
- [x] **DONE** — Simulación de crecimiento — modo AUTO (2026-03)
- [x] **DONE** — Optimización de simulación (2026-03)
- [x] **DONE** — Mejora de codebase para contextualización eficiente para agentes de IA (2026-03)

---

## Cómo usar este backlog

1. **Trabajar un issue**: Abrir chat en Cursor, referenciar `@BACKLOG.md` y pedir análisis o implementación del issue por su ID (ej: "Analiza BUG-01 y proponme un plan").
2. **Repriorizar**: Mover el issue hacia arriba o abajo dentro de su sección, o cambiar de sección.
3. **Agregar issue nuevo**: Asignar el siguiente ID disponible (BUG-XX, FEAT-XX, TECH-XX, ART-XX, AUDIO-XX) y ubicar en la sección de prioridad correspondiente.
4. **Completar issue**: Mover a la sección "Completados" con fecha, marcar checkbox como `[x]`.
5. **En progreso**: Mover a la sección "En progreso" cuando se empiece a trabajar.
6. **Dependencias**: Usar campo `Depende de: ID` cuando un issue requiere que otro se complete primero. Revisar dependencias antes de empezar.

### Convención de IDs
| Prefijo | Categoría |
|---------|-----------|
| `BUG-XX` | Bugs y funcionalidad rota |
| `FEAT-XX` | Features y mejoras |
| `TECH-XX` | Deuda técnica, refactors, code health |
| `ART-XX` | Assets de arte, prefabs, sprites |
| `AUDIO-XX` | Assets de audio y sistema de audio |

### Campos de un issue
- **Tipo**: fix, feature, refactor, arte/assets, audio/feature, etc.
- **Archivos**: archivos principales involucrados
- **Notas**: contexto, descripción del problema o solución esperada
- **Depende de** (opcional): IDs de issues que deben completarse primero

### Orden de secciones
1. En progreso (activamente en desarrollo)
2. Prioridad alta (bugs críticos, blockers de gameplay core)
3. Prioridad media (features importantes, balance, mejoras)
4. Code Health (deuda técnica, refactors, performance)
5. Prioridad baja (sistemas nuevos, polish, contenido)
6. Completados (últimos 30 días)
