import { DataTable, type Column } from '../../components/DataTable'
import { BadgeChip, type Status } from '../../components/BadgeChip'
import { StatBar } from '../../components/StatBar'
import { FilterChips } from '../../components/FilterChips'
import { HeatmapCell } from '../../components/HeatmapCell'
import { AnnotatedMap } from '../../components/AnnotatedMap'

// ---------------------------------------------------------------------------
// Fixtures — DataTable
// ---------------------------------------------------------------------------

type TaskRow = { id: string; name: string; owner: string; status: Status }

const TABLE_ROWS: TaskRow[] = [
  { id: 'T-01', name: 'Palette JSON export', owner: 'agent', status: 'done' },
  { id: 'T-02', name: 'StatBar threshold render', owner: 'agent', status: 'in-progress' },
  { id: 'T-03', name: 'AnnotatedMap SVG', owner: 'agent', status: 'pending' },
  { id: 'T-04', name: 'Auth gate (Step 4)', owner: 'jiramos87', status: 'blocked' },
]

const TABLE_COLS: Column<TaskRow>[] = [
  { key: 'id', header: 'ID' },
  { key: 'name', header: 'Task', sortable: true, sortDirection: 'ascending' },
  { key: 'owner', header: 'Owner' },
]

// ---------------------------------------------------------------------------
// Fixtures — FilterChips
// ---------------------------------------------------------------------------

const CHIPS = [
  { label: 'All', active: true },
  { label: 'Done', active: false },
  { label: 'In Progress', active: true },
  { label: 'Blocked', active: false },
]

// ---------------------------------------------------------------------------
// Fixtures — HeatmapCell (5-bucket ramp: 0 → 1 in steps of 0.2)
// ---------------------------------------------------------------------------

const HEATMAP_INTENSITIES = [0, 0.2, 0.4, 0.6, 0.8, 1.0]

// ---------------------------------------------------------------------------
// Fixtures — AnnotatedMap
// ---------------------------------------------------------------------------

const MAP_REGIONS = [
  { id: 'r-a', path: 'M100,100 L300,100 L300,250 L100,250 Z', intensity: 0.1 },
  { id: 'r-b', path: 'M320,100 L520,100 L520,250 L320,250 Z', intensity: 0.5 },
  { id: 'r-c', path: 'M540,100 L740,100 L740,250 L540,250 Z', intensity: 0.9 },
  { id: 'r-d', path: 'M100,270 L500,270 L500,420 L100,420 Z', intensity: 0.3 },
  { id: 'r-e', path: 'M520,270 L900,270 L900,420 L520,420 Z', intensity: 0.7 },
]

const MAP_ANNOTATIONS = [
  { x: 140, y: 190, label: 'Sector A' },
  { x: 360, y: 190, label: 'Sector B' },
  { x: 580, y: 190, label: 'Sector C' },
  { x: 260, y: 360, label: 'District West' },
  { x: 660, y: 360, label: 'District East' },
]

// ---------------------------------------------------------------------------
// Page
// ---------------------------------------------------------------------------

export default function DesignPage() {
  return (
    <main className="min-h-screen bg-bg-canvas text-text-primary px-6 py-8 space-y-12 font-mono">
      {/* Banner */}
      <header className="rounded border border-text-muted/20 bg-bg-panel px-4 py-3 text-xs text-text-muted">
        internal review. not public nav. auth gate Step 4.
      </header>

      {/* DataTable */}
      <section id="datatable" className="space-y-2">
        <h2 className="text-xs uppercase tracking-widest text-text-muted">DataTable + BadgeChip</h2>
        <DataTable
          rows={TABLE_ROWS}
          columns={TABLE_COLS}
          statusCell={(r) => <BadgeChip status={r.status} />}
          getRowKey={(r) => r.id}
        />
      </section>

      {/* BadgeChip standalone */}
      <section id="badgechip" className="space-y-2">
        <h2 className="text-xs uppercase tracking-widest text-text-muted">BadgeChip — all statuses</h2>
        <div className="flex flex-wrap gap-3">
          {(['done', 'in-progress', 'pending', 'blocked'] as const).map((s) => (
            <BadgeChip key={s} status={s} />
          ))}
        </div>
      </section>

      {/* StatBar */}
      <section id="statbar" className="space-y-3 max-w-sm">
        <h2 className="text-xs uppercase tracking-widest text-text-muted">StatBar — nominal / warn / critical</h2>
        <StatBar label="CPU" value={20} max={100} />
        <StatBar label="Memory" value={75} max={100} thresholds={{ warn: 70, critical: 90 }} />
        <StatBar label="Disk" value={95} max={100} thresholds={{ warn: 70, critical: 90 }} />
      </section>

      {/* FilterChips */}
      <section id="filterchips" className="space-y-2">
        <h2 className="text-xs uppercase tracking-widest text-text-muted">FilterChips — multi-select (SSR-frozen)</h2>
        <FilterChips chips={CHIPS} />
      </section>

      {/* HeatmapCell */}
      <section id="heatmapcell" className="space-y-2">
        <h2 className="text-xs uppercase tracking-widest text-text-muted">HeatmapCell — 5-bucket ramp (0 → 1)</h2>
        <div className="flex gap-1">
          {HEATMAP_INTENSITIES.map((v) => (
            <div key={v} className="flex flex-col items-center gap-1">
              <HeatmapCell intensity={v} />
              <span className="text-[10px] text-text-muted">{v.toFixed(1)}</span>
            </div>
          ))}
        </div>
      </section>

      {/* AnnotatedMap */}
      <section id="annotatedmap" className="space-y-2">
        <h2 className="text-xs uppercase tracking-widest text-text-muted">AnnotatedMap — SVG fixture</h2>
        <div className="max-w-2xl rounded border border-text-muted/20 bg-bg-panel p-2">
          <AnnotatedMap regions={MAP_REGIONS} annotations={MAP_ANNOTATIONS} />
        </div>
      </section>
    </main>
  )
}
