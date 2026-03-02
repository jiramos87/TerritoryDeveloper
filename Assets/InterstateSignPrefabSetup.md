# InterstateSign Prefab Setup (Manual Unity Editor)

The City Network feature places highway signs at map borders showing destination city names. Create the prefab as follows and assign it to **RegionalMapManager.interstateSignPrefab**.

## Hierarchy

Create a root GameObject (e.g. `InterstateSignPrefab`) and add the **InterstateSign** component to it. Add children:

1. **SignBackground** – GameObject with a **SpriteRenderer**
   - Use a rectangular sprite (e.g. green highway-sign style). Scale to fit text.
   - Sorting order is set at runtime (base + 20).

2. **CityNameText** – GameObject with **TextMesh**
   - Anchor: Middle Center. Font size ~0.15. White text.
   - This shows the destination city name.

3. **CategoryText** – GameObject with **TextMesh**
   - Anchor: Middle Center. Smaller font (~0.08). Position slightly below city name (e.g. Y offset -0.12).
   - Shows category and population (e.g. "Town - Pop. 23K").

4. **ArrowNorth** – GameObject with **SpriteRenderer** (arrow sprite pointing up)
   - Default: **inactive** (SetActive(false)). Activated when border = North (1).

5. **ArrowSouth** – Same, arrow down. Default inactive. Activated when border = South (0).

6. **ArrowEast** – Arrow right. Default inactive. Activated when border = East (3).

7. **ArrowWest** – Arrow left. Default inactive. Activated when border = West (2).

## Component Wiring

On the root GameObject's **InterstateSign** component, assign in the Inspector:

- **Sign Background** → SignBackground's SpriteRenderer
- **City Name Text** → CityNameText's TextMesh
- **Category Text** → CategoryText's TextMesh
- **Arrow North** → ArrowNorth GameObject
- **Arrow South** → ArrowSouth GameObject
- **Arrow East** → ArrowEast GameObject
- **Arrow West** → ArrowWest GameObject

## Saving as Prefab

Drag the configured root GameObject from the Hierarchy into the Project window (e.g. under `Assets/Prefabs/`) to create the prefab. Assign this prefab to **RegionalMapManager.interstateSignPrefab** in the scene (or leave it unassigned; signs will simply not be placed until the prefab is set).
