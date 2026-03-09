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

## 2. Add RawImage for the Map

1. Right-click MiniMapPanel → **UI** → **Raw Image**. Rename to `MiniMapImage`.
2. Select MiniMapImage. In RectTransform:
   - Stretch to fill parent: Anchor Presets → hold Alt+Shift, click stretch (bottom-right).
   - **Left, Top, Right, Bottom**: 0 (or small padding like 2).
3. Set **Color** to white (255, 255, 255, 255).
4. Ensure **Raycast Target** is checked (required for click-to-navigate).

## 3. Add Viewport Rect Overlay

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

## 4. Attach MiniMapController

1. Select **MiniMapPanel** (or create an empty child GameObject for the controller).
2. Add Component → **Mini Map Controller** (script).
3. Assign references in the Inspector:
   - **Grid Manager**: Drag GridManager from the scene.
   - **Water Manager**: Drag WaterManager.
   - **Interstate Manager**: Drag InterstateManager.
   - **Camera Controller**: Drag CameraController.
   - **Map Image**: Drag the MiniMapImage RawImage.
   - **Viewport Rect**: Drag the ViewportRect RectTransform.
   - **Mini Map Panel**: Drag MiniMapPanel (or leave empty if the script is on the panel itself — the script uses `miniMapPanel` for Show/Hide; if null, it may use the GameObject it’s on).

If MiniMapController is on MiniMapPanel, set **Mini Map Panel** to the MiniMapPanel GameObject (or the script will use `gameObject` when `miniMapPanel` is null — check the script; it uses `miniMapPanel` for SetVisible, so assign it).

## 5. Add Toggle Button to DataPanelButtons

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

## 6. Wire UIManager

1. Select the **UIManager** GameObject in the scene.
2. In the Inspector, find the **Mini-map** section.
3. Assign **Mini Map Controller** to the MiniMapController reference.

## 7. Verify Default Visibility

The mini-map is visible by default. Ensure MiniMapPanel is **active** (checkbox checked) in the Hierarchy when the scene starts.

## Testing

- Enter Play mode. The mini-map should appear in the bottom-right.
- Click on the map: the camera should move to the clicked area.
- Open Load Game or Building Selector: the mini-map should hide.
- Close the popup: the mini-map should reappear (if it was visible before).
- Click the Map button in DataPanelButtons: the mini-map should toggle visibility.
