# GameDebugInfoBuilder – Plan de trabajo e integración

## Objetivo
Centralizar la generación y el orden de los textos de depuración/info de juego para mejorar el análisis (p. ej. por LLM o desarrollador) a partir de pantallazos del mapa.

## Clase principal: `GameDebugInfoBuilder`
- **Ubicación:** `Assets/Scripts/Managers/GameManagers/GameDebugInfoBuilder.cs`
- **Tipo:** MonoBehaviour instanciable; referencias opcionales a GridManager, TerrainManager, WaterManager, UIManager (se resuelven con `FindObjectOfType` si no están asignadas).

### Métodos públicos
| Método | Descripción |
|--------|-------------|
| `GetCoordinatesLine(Vector2)` | Línea de coordenadas, ej. `"x: 12, y: 9"`. |
| `GetCellUnderCursorInfo(Vector2)` | Info de la celda: height, zoneType, isWater, hasForest, adjacentToWater. |
| `GetFootprintTilesLine(Vector2, int size)` | Lista de coordenadas del footprint, ej. `"Footprint: (12,9)(13,9)..."`. |
| `GetBuildingPlacementInfo(Vector2, int, string, bool isWaterPlant)` | Bloque: nombre edificio, tamaño, footprint, motivo de fallo (si hay), resumen por celda. |
| `GetFullDebugText(Vector2)` | Texto completo: coordenadas + celda bajo cursor + (si hay edificio seleccionado) info de colocación. |
| `GetShortDebugLine(Vector2)` | Una sola línea: coordenadas + resumen de celda (cuando el espacio en UI es limitado). |

### Métodos privados
| Método | Descripción |
|--------|-------------|
| `ResolveRefsIfNeeded()` | Asigna GridManager, TerrainManager, WaterManager, UIManager si no están asignados. |
| `GetFootprintSummary(Vector2, int)` | Resumen por celda del footprint: coords, height, zoneType, isWater, adjacentToWater. |

---

## Cambios en el repositorio

### 1. GridManager
- **`GetBuildingPlacementFailReason(Vector2 gridPosition, int buildingSize, bool isWaterPlant)`**  
  Devuelve el motivo concreto de fallo de colocación (o `null` si sería válido).
- **`TryValidateBuildingPlacement(...)`** (privado)  
  Centraliza la validación; `canPlaceBuilding` y `GetBuildingPlacementFailReason` lo usan.
- **`PlaceBuilding`**  
  Usa `GetBuildingPlacementFailReason` para mostrar el motivo real en la notificación en lugar del mensaje genérico.

### 2. UIManager
- Campos opcionales: `gameDebugInfoBuilder`, `useFullDebugText`.
- En **UpdateUI()**: si existe `GameDebugInfoBuilder` y `useFullDebugText` está activo, `gridCoordinatesText` se rellena con **GetFullDebugText(mouseGridPosition)**; si no, se mantiene solo `"x: ..., y: ..."`.

### 3. GameNotificationManager
- Sin cambios de API. Sigue recibiendo el mensaje de error desde `GridManager.PlaceBuilding`, que ahora envía el motivo específico (p. ej. "Water plant must be adjacent to water", "Terrain: slope...", "Tile (x,y) is not Grass...").

### 4. Escena
- Añadir el componente **GameDebugInfoBuilder** a un GameObject de la escena (p. ej. mismo que GameManager o UIManager) para que UIManager pueda encontrarlo con `FindObjectOfType` si no se asigna por inspector.
- Opcional: asignar en el inspector de UIManager la referencia a `GameDebugInfoBuilder` y marcar/desmarcar **Use Full Debug Text** según se quiera la línea corta o el bloque completo.

---

## Datos que se obtienen de otros GameObjects/Managers
- **GridManager:** celdas, bounds, `GetBuildingFootprintOffset`, `GetBuildingPlacementFailReason`, `mouseGridPosition`.
- **TerrainManager / HeightMap:** altura por (x,y).
- **WaterManager:** `IsWaterAt`, `IsAdjacentToWater`.
- **Cell (por GridManager.GetCell):** `zoneType`, `HasForest()`, `GetCellInstanceHeight()`.
- **UIManager.GetSelectedBuilding():** edificio seleccionado para mostrar nombre, tamaño y si es WaterPlant.

---

## Próximas extensiones opcionales
- Panel de detalles (Details popup): usar **GetCellUnderCursorInfo** (o equivalente para la celda clicada) para añadir una línea de debug (height, zone, adjWater).
- Panel de notificaciones: ya muestra el motivo específico porque PlaceBuilding lo pasa a **PostBuildingPlacementError**.
- Texto de debug en otro UI (p. ej. panel dedicado): enlazar un `Text` a **GetFullDebugText(gridManager.mouseGridPosition)** cada frame.
