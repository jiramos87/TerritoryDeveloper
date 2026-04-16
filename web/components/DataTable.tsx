import type { ReactNode } from 'react'
import { StatBar } from './StatBar'

export type Column<T> = {
  key: keyof T | string
  header: string
  sortable?: boolean
  sortDirection?: 'ascending' | 'descending' | 'none'
  render?: (row: T) => ReactNode
}

export type PctColumnConfig<T> = {
  dataKey: keyof T
  label?: string
  max?: number
}

function toFiniteNumber(raw: unknown): number {
  return typeof raw === 'number' && Number.isFinite(raw) ? raw : 0
}

interface Props<T> {
  columns: Column<T>[]
  rows: T[]
  statusCell?: (row: T) => ReactNode
  getRowKey?: (row: T, index: number) => string | number
  pctColumn?: PctColumnConfig<T>
}

export function DataTable<T,>({
  columns,
  rows,
  statusCell,
  getRowKey,
  pctColumn,
}: Props<T>): ReactNode {
  return (
    <div className="w-full overflow-x-auto">
      <table className="w-full border-collapse text-sm text-text-primary">
        <thead>
          <tr className="border-b border-text-muted/20">
            {statusCell && (
              <th
                scope="col"
                className="px-3 py-2 text-left font-mono text-xs text-text-muted uppercase tracking-wider"
              >
                Status
              </th>
            )}
            {columns.map((col) => (
              <th
                key={String(col.key)}
                scope="col"
                aria-sort={
                  col.sortable
                    ? (col.sortDirection ?? 'none')
                    : undefined
                }
                className="px-3 py-2 text-left font-mono text-xs text-text-muted uppercase tracking-wider"
              >
                <span className="inline-flex items-center gap-1">
                  {col.header}
                  {col.sortable && (
                    <span aria-hidden="true" className="opacity-40">
                      {col.sortDirection === 'ascending'
                        ? '↑'
                        : col.sortDirection === 'descending'
                        ? '↓'
                        : '↕'}
                    </span>
                  )}
                </span>
              </th>
            ))}
            {pctColumn && (
              <th
                scope="col"
                className="px-3 py-2 text-left font-mono text-xs text-text-muted uppercase tracking-wider"
              >
                {pctColumn.label ?? 'Progress'}
              </th>
            )}
          </tr>
        </thead>
        <tbody>
          {rows.map((row, rowIndex) => {
            const key = getRowKey ? getRowKey(row, rowIndex) : rowIndex
            return (
              <tr
                key={key}
                className="border-b border-text-muted/10 hover:bg-bg-panel/50 transition-colors"
              >
                {statusCell && (
                  <td className="px-3 py-2 align-middle">
                    {statusCell(row)}
                  </td>
                )}
                {columns.map((col) => (
                  <td
                    key={String(col.key)}
                    className="px-3 py-2 align-middle text-text-primary"
                  >
                    {col.render
                      ? col.render(row)
                      : String((row as Record<string, unknown>)[col.key as string] ?? '')}
                  </td>
                ))}
                {pctColumn && (
                  <td className="px-3 py-2 align-middle">
                    <StatBar
                      label={pctColumn.label ?? 'Progress'}
                      value={toFiniteNumber(row[pctColumn.dataKey])}
                      max={pctColumn.max ?? 100}
                    />
                  </td>
                )}
              </tr>
            )
          })}
        </tbody>
      </table>
    </div>
  )
}
