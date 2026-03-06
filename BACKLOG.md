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

## Prioridad media

- [ ] **FEAT-01** — Agregar cambio delta al presupuesto total (ej: $25,000 (+$1,200))
  - Tipo: feature
  - Archivos: `EconomyManager.cs`, `CityStatsUIController.cs`, `UIManager.cs`
  - Notas: Feedback visual del flujo económico por turno.

- [ ] **FEAT-02** — Agregar contador de costo de construcción al cursor del mouse
  - Tipo: feature
  - Archivos: `CursorManager.cs`, `UIManager.cs`, `GridManager.cs`
  - Notas: Mostrar costo antes de confirmar placement.

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
3. **Agregar issue nuevo**: Asignar el siguiente ID disponible (BUG-XX, FEAT-XX, ART-XX, AUDIO-XX) y ubicar en la sección de prioridad correspondiente.
4. **Completar issue**: Mover a la sección "Completados" con fecha, marcar checkbox como `[x]`.
5. **En progreso**: Mover a la sección "En progreso" cuando se empiece a trabajar.
