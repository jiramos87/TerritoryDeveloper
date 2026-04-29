import type { SearchResultRow } from "@/lib/catalog/search-query";
import { SearchResultRow as Row } from "./SearchResultRow";

type Props = {
  kind: string;
  rows: SearchResultRow[];
  selectedId: string | null;
  onSelect: (row: SearchResultRow) => void;
  idPrefix: string;
};

export function SearchResultGroup({ kind, rows, selectedId, onSelect, idPrefix }: Props) {
  if (rows.length === 0) return null;

  return (
    <li role="presentation">
      <div className="flex items-center gap-[var(--ds-spacing-sm)] px-[var(--ds-spacing-md)] py-[var(--ds-spacing-xs)]">
        <span className="text-[length:var(--ds-font-size-caption)] uppercase tracking-wide text-[var(--ds-text-muted)]">
          {kind}
        </span>
        <span
          aria-label={`${rows.length} results`}
          className="rounded-full bg-[var(--ds-bg-canvas)] px-[var(--ds-spacing-xs)] text-[length:var(--ds-font-size-caption)] text-[var(--ds-text-muted)]"
        >
          {rows.length}
        </span>
      </div>
      <ul role="group" aria-label={kind}>
        {rows.map((row) => {
          const rowId = `${idPrefix}-${row.entity_id}`;
          return (
            <Row
              key={row.entity_id}
              id={rowId}
              row={row}
              selected={selectedId === rowId}
              onSelect={() => onSelect(row)}
            />
          );
        })}
      </ul>
    </li>
  );
}
