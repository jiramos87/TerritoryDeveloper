# FEAT-17 Mini-map — Scene Setup Instructions

Complete these steps in the Unity Editor to wire up the mini-map UI. The scripts (`MiniMapController.cs`, `ShowMiniMapButton.cs`) and UIManager changes are already in place.

## 1. Create MiniMapPanel

1. Open **MainScene**.
2. In the Hierarchy, locate the **Canvas** (under the main game UI).
3. Right-click Canvas → **UI** → **Panel**. Rename it to `MiniMapPanel`.
4. Select MiniMapPanel. In the RectTransform:
   - **Anchor**: Bottom-right (click the anchor preset, hold Alt, click bottom-right).
   - **Pivot**: (1, 0).
   - **Pos X**: -120 (or adjust for padding from edge).
   - **Pos Y**: 120.
   - **Width**: 220.
   - **Height**: 220.
   - **Anchors Min**: (1, 0), **Max**: (1, 0).
   - **Offset Min**: (-230, 10), **Offset Max**: (-10, 230) — adjust for your layout.

## 2. Add Layer Buttons Row (FEAT-30)

1. Right-click MiniMapPanel → **Create Empty**. Rename to `LayerButtonsRow`.
2. Select LayerButtonsRow. In RectTransform:
   - **Anchor**: Stretch horizontally at top (Anchor Min: 0, 1; Anchor Max: 1, 1).
   - **Pivot**: (0.5, 1).
   - **Left**: 2, **Right**: 2, **Top**: 0, **Bottom**: (parent height - 24) or **Height**: 24.
   - **Offset Min**: (2, -24), **Offset Max**: (-2, 0).
3. Add Component → **Horizontal Layout Group**:
   - **Spacing**: 2.
   - **Child Force Expand Width**: checked.
   - **Child Force Expand Height**: checked.
   - **Child Alignment**: Middle Center.
4. Create 5 child Buttons: Right-click LayerButtonsRow → **UI** → **Button**. Rename to `LayerButtonStreets`, `LayerButtonZones`, `LayerButtonForests`, `LayerButtonDesirability`, `LayerButtonCentroid`.
5. For each button:
   - Set child Text to "St", "Zn", "Fr", "De", "Ct" respectively (or use icons).
   - Add Component → **Mini Map Layer Button** (script).
   - Assign **Mini Map Controller** (drag from MiniMapPanel).
   - Assign **Layer**: Streets, Zones, Forests, Desirability, Centroid respectively.
   - Assign **Button Image** (the button's Image component or a child Image).
   - Set **Color When On** (e.g. green) and **Color When Off** (e.g. gray).
   - In Button **On Click ()**: Add listener → drag this button → **MiniMapLayerButton** → **OnClick**.
6. Ensure LayerButtonsRow is the first child of MiniMapPanel (above MiniMapImage) so it renders at the top.

## 3. Add RawImage for the Map

1. Right-click MiniMapPanel → **UI** → **Raw Image**. Rename to `MiniMapImage`.
2. Select MiniMapImage. In RectTransform:
   - Stretch to fill parent: Anchor Presets → hold Alt+Shift, click stretch (bottom-right).
   - **Left**: 2, **Top**: 26 (to leave room for LayerButtonsRow), **Right**: 2, **Bottom**: 2.
3. Set **Color** to white (255, 255, 255, 255).
4. Ensure **Raycast Target** is checked (required for click-to-navigate).

## 4. Add Viewport Rect Overlay

1. Right-click MiniMapPanel → **UI** → **Image**. Rename to `ViewportRect`.
2. Select ViewportRect. In RectTransform:
   - Stretch to fill parent initially: Anchor Min (0,0), Max (1,1), offsets 0.
   - The MiniMapController will update its anchors each frame to match the camera viewport.
3. In the Image component:
   - **Color**: White with alpha 0.6 (R:255, G:255, B:255, A:153) for the border.
   - **Raycast Target**: Uncheck (so clicks pass through to the map).
4. Add an **Outline** component:
   - **Effect Color**: White, alpha 0.6.
   - **Effect Distance**: (1, 1) or (2, 2) for a thin border.
5. Use the default UI Sprite (e.g. UISprite) or a 1x1 white sprite so the Outline draws.

## 5. Attach MiniMapController

1. Select **MiniMapPanel** (or create an empty child GameObject for the controller).
2. Add Component → **Mini Map Controller** (script).
3. Assign references in the Inspector:
   - **Grid Manager**: Drag GridManager from the scene.
   - **Water Manager**: Drag WaterManager.
   - **Interstate Manager**: Drag InterstateManager.
   - **Camera Controller**: Drag CameraController.
   - **Auto Zoning Manager**: Drag AutoZoningManager (for centroid layer; optional, uses FindObjectOfType if null).
   - **Map Image**: Drag the MiniMapImage RawImage.
   - **Viewport Rect**: Drag the ViewportRect RectTransform.
   - **Mini Map Panel**: Drag MiniMapPanel (or leave empty if the script is on the panel itself — the script uses `miniMapPanel` for Show/Hide; if null, it may use the GameObject it’s on).

If MiniMapController is on MiniMapPanel, set **Mini Map Panel** to the MiniMapPanel GameObject (or the script will use `gameObject` when `miniMapPanel` is null — check the script; it uses `miniMapPanel` for SetVisible, so assign it).

## 6. Add Toggle Button to DataPanelButtons

1. In the Hierarchy, expand **DataPanelButtons**.
2. Right-click DataPanelButtons → **UI** → **Button**. Rename to `ShowMiniMapButton`.
3. Position it next to ShowStatsButton / ShowTaxesButton (e.g. after ZoomOutButton).
4. Set the button’s child Text to "Map" or "Mini-map" (or use an icon).
5. Add Component → **Show Mini Map Button** (script).
6. Assign **Mini Map Controller** to the MiniMapController reference.
7. In the Button component, **On Click ()**:
   - Click **+** to add a listener.
   - Drag the ShowMiniMapButton GameObject (or the GameObject with ShowMiniMapButton script) into the object field.
   - Function: **ShowMiniMapButton** → **OnShowMiniMapButtonClick**.

## 7. Wire UIManager

1. Select the **UIManager** GameObject in the scene.
2. In the Inspector, find the **Mini-map** section.
3. Assign **Mini Map Controller** to the MiniMapController reference.

## 8. Wire GameSaveManager (FEAT-30)

1. Select the **GameSaveManager** GameObject in the scene.
2. In the Inspector, assign **Mini Map Controller** to the miniMapController reference (for save/load of layer selection).

## 9. Add DetailsDesirabilityText (FEAT-30)

1. In the Hierarchy, expand **DetailsPanel**.
2. Right-click DetailsPanel → **UI** → **Text - TextMeshPro** (or legacy Text). Rename to `DetailsDesirabilityText`.
3. Position it below the existing details text (e.g. DetailsOccupancyText).
4. Set the text content to "Desirability: 0" (placeholder).
5. Select the **UIManager** GameObject.
6. In the Inspector, find the **Details** section and assign **DetailsDesirabilityText** to the detailsDesirabilityText field.

## 10. Verify Default Visibility

The mini-map is visible by default. Ensure MiniMapPanel is **active** (checkbox checked) in the Hierarchy when the scene starts. When a cell is selected, the DetailsPanel shows its desirability value.

## Testing

- Enter Play mode. The mini-map should appear in the bottom-right.
- Click on the map: the camera should move to the clicked area.
- Open Load Game or Building Selector: the mini-map should hide.
- Close the popup: the mini-map should reappear (if it was visible before).
- Click the Map button in DataPanelButtons: the mini-map should toggle visibility.
- Click layer buttons (St, Zn, Fr, De): each toggles its layer; active buttons show colorWhenOn.
- Save and load: layer selection should persist.
- Select a cell: DetailsPanel shows "Desirability: X.X" for that cell.
