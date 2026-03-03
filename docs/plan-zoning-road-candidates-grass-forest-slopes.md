# Plan: Candidatos de zona y calle = Grass, Forest y pendientes N-S / E-O

## Objetivo

Tratar como candidatos válidos para **colocación de zona** y para **expansión de calle** no solo celdas Grass, sino también:

- **Forest** (celda con bosque)
- **Pendientes** de tipo **norte-sur** o **este-oeste** (y Flat), cuando el terreno lo permita.

Así se evita restringir en exceso el crecimiento automático a “solo Grass” y se alinea la lógica con terreno construible (zonas y calles sobre Flat/N/S/E/W).

---

## 1. Definición de “celda zoneable/expandible”

- **Zoneable (candidato a zona):**  
  `zoneType == Grass` **o** `HasForest()` **o** celda cuyo terreno sea colocable para zona (Flat, North, South, East, West).  
  En la práctica: toda celda que sea Grass (con o sin forest) ya es zoneable; si en el futuro hubiera celdas con otro `zoneType` pero pendiente N-S/E-O, incluirlas con un helper que consulte `TerrainManager.GetTerrainSlopeTypeAt`.

- **Expandible para calle (vecino válido de un borde de calle):**  
  Misma idea: Grass, Forest, agua (`GetCellInstanceHeight() == 0`), o terreno aceptado por `TerrainManager.CanPlaceRoad` (ya incluye Flat, N, S, E, W y diagonales si se mantiene el cambio previo).

---

## 2. Cambios por archivo

### 2.1 GridManager.cs

| Ubicación | Cambio |
|----------|--------|
| **GetRoadEdgePositions** (líneas 1863–1866) | Considerar vecino “expandible” si es Grass **o** `n.HasForest()` **o** agua (`n.GetCellInstanceHeight() == 0`). Sustituir la condición actual por algo como: `(n.zoneType == Zone.ZoneType.Grass \|\| n.HasForest() \|\| n.GetCellInstanceHeight() == 0)`. |
| **CountGrassNeighbors** (líneas 1873–1890) | Opción A: Mantener nombre y ampliar criterio: contar vecinos que sean **zoneable** (Grass, HasForest, o pendiente Flat/N/S/E/W). Opción B: Añadir `CountZoneableNeighbors(int gx, int gy)` que use un helper `IsZoneableNeighbor(Cell c)` (Grass \|\| HasForest \|\| slope Flat/N/S/E/W vía `terrainManager.GetTerrainSlopeTypeAt`) y usar este método en la reserva de auto-zoning. Si se usa Opción B, `IsReservedForRoadExpansion` en AutoZoningManager debe usar `CountZoneableNeighbors` en lugar de `CountGrassNeighbors`. |

Recomendación: **Opción A** para no tocar la firma pública ni el nombre del método usado desde AutoZoningManager; solo ampliar la condición de conteo a “zoneable” (Grass, HasForest, y si `terrainManager != null` y slope es Flat/North/South/East/West, contar también).

---

### 2.2 AutoZoningManager.cs

| Ubicación | Cambio |
|----------|--------|
| **GetCandidatesAdjacentToRoad** (líneas 128–142) | Incluir celda si es candidata a zona: **Grass, HasForest(), o terreno zoneable**. Añadir referencia a `TerrainManager` (FindObjectOfType si hace falta). Helper local o privado: `IsZoneableCandidate(Cell c, int x, int y)` → `c != null && (c.zoneType == Zone.ZoneType.Grass \|\| c.HasForest() \|\| IsSlopePlaceableForZone(x, y))`. `IsSlopePlaceableForZone(x, y)` usa `terrainManager.GetTerrainSlopeTypeAt(x, y)` y devuelve true para Flat, North, South, East, West. |
| **ProcessTick – comprobación de celda** (líneas 86–91) | Sustituir la condición actual por el mismo criterio: `IsZoneableCandidate(cell, p.x, p.y)` (o equivalente: Grass, HasForest, o slope N-S/E-O). |
| **IsReservedForRoadExpansion** | Sigue usando `CountGrassNeighbors` del GridManager; cuando GridManager cuente “zoneable” (ver 2.1), la reserva seguirá siendo coherente sin cambiar esta función. |

Nota: Si no se quiere que AutoZoningManager dependa de TerrainManager, el “slope zoneable” puede centralizarse en GridManager con un método público `IsCellZoneableTerrain(int x, int y)` que consulte `terrainManager` y devuelva true para Flat/N/S/E/W; AutoZoningManager solo llamaría a ese método además de Grass/HasForest.

---

### 2.3 AutoRoadBuilder.cs

| Ubicación | Cambio |
|----------|--------|
| **IsCellPlaceableForRoad** (líneas 434–442) | Ya acepta Grass y HasForest(); no hace falta cambiar. La colocación efectiva de road ya pasa por `TerrainManager.CanPlaceRoad`, que acepta Flat y N/S/E/W (y diagonales si se mantiene). |
| **GetCellPlaceableRejectReason** (líneas 446–460) | Opcional: en el caso “zone not grass/water”, aclarar que se acepta Grass o Forest; el mensaje puede seguir siendo “zone not grass/water” o “zone not grass/forest/water” según prefieras. |
| **CountGrassNeighbors** (AutoRoadBuilder, línea 471) | Usado para priorizar bordes con más “espacio”. Opcional: contar también Forest para consistencia: `(c.zoneType == Zone.ZoneType.Grass \|\| c.HasForest())`. |

Resumen: en AutoRoadBuilder la lógica de “qué celda es colocable para calle” ya está alineada con Grass/Forest y terreno (vía TerrainManager); solo falta que **GetRoadEdgePositions** en GridManager considere Forest como vecino expandible (y opcionalmente unificar el conteo de vecinos “expandibles” con Grass+Forest).

---

### 2.4 ZoneManager / GridManager – validación de zona (PlaceZoneAt / canPlaceZone)

- **ZoneManager.PlaceZoneAt** llama a `canPlaceZone(..., requireInterstate: false)`.
- **canPlaceZone** usa `gridManager.canPlaceBuilding(gridPosition, 1)`.
- **TryValidateBuildingPlacement** (GridManager) exige `zoneType == Grass` para cada celda del footprint.

Si en el juego **todas** las celdas colocables son `zoneType == Grass` (con o sin forest, con o sin pendiente en el terreno), no hace falta cambiar esta validación. Si en el futuro existieran celdas con otro `zoneType` pero pendiente N-S/E-O que deban ser zoneables, habría que:

- Añadir en GridManager un método tipo `IsCellValidForZonePlacement(int x, int y)` que devuelva true si `zoneType == Grass` **o** `HasForest()` **o** (terreno Flat/N/S/E/W según TerrainManager), y
- Hacer que la rama de validación para **tamaño 1** (solo zona) use ese método en lugar de solo `zoneType == Grass`.

Queda como **opcional / fase 2** en el plan.

---

## 3. Orden sugerido de implementación

1. **GridManager**
   - GetRoadEdgePositions: añadir `n.HasForest()` a la condición de vecino expandible.
   - CountGrassNeighbors: ampliar a “zoneable” (Grass, HasForest, y slope Flat/N/S/E/W vía TerrainManager). Mantener nombre o documentar que ahora cuenta “zoneable”.
2. **AutoZoningManager**
   - Añadir referencia a TerrainManager (o usar GridManager si se añade allí `IsCellZoneableTerrain`).
   - GetCandidatesAdjacentToRoad: incluir solo celdas que pasen `IsZoneableCandidate` (Grass, HasForest, o slope N-S/E-O).
   - ProcessTick: misma condición al comprobar la celda antes de colocar zona.
3. **AutoRoadBuilder** (opcional)
   - CountGrassNeighbors interno: contar también HasForest() para priorizar bordes.
4. **Validación zona 1x1** (opcional/fase 2)
   - Solo si se introducen celdas no-Grass zoneables; entonces usar `IsCellValidForZonePlacement` en la validación de building tamaño 1.

---

## 4. Resumen de criterios unificados

- **Vecino expandible para calle (borde):** Grass **o** Forest **o** agua (height 0). Pendientes N-S/E-O ya se consideran en TerrainManager.CanPlaceRoad al colocar el tile.
- **Candidato a zona:** Grass **o** HasForest() **o** terreno con pendiente Flat / North / South / East / West.
- **Reserva para expansión de calle:** Seguir usando el conteo de vecinos “zoneable” (tras el cambio en CountGrassNeighbors/CountZoneableNeighbors) para no zonificar donde la calle debe crecer.

Con esto, tanto las calles como las zonas consideran **Grass, Forest y pendientes norte-sur o este-oeste** de forma coherente en todo el flujo automático.
