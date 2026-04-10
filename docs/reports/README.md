# Committed report snapshots

Some **Editor** exports are normally **gitignored** under `tools/reports/*.json`. This folder holds **versioned** copies used for documentation and agent workflows.

| File | Source | Refresh |
|------|--------|---------|
| `ui-inventory-as-built-baseline.json` | **`editor_export_ui_inventory.document`** (Postgres) or **Territory Developer → Reports → Export UI Inventory (JSON)** | After **MainMenu** / **MainScene** UI hierarchy changes, or when updating **`ia/specs/ui-design-system.md`** **as-built** tables. |

**Postgres vs committed baseline:** The **`document`** column should match this file when promotion is correct; normalize both JSON payloads (e.g. key-sorted stringify) before diffing if property order differs.
