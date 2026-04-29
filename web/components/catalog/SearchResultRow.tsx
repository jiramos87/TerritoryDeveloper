import type { SearchResultRow as SearchRow } from "@/lib/catalog/search-query";

type Props = {
  row: SearchRow;
  selected: boolean;
  onSelect: () => void;
  id: string;
};

export function SearchResultRow({ row, selected, onSelect, id }: Props) {
  return (
    <li
      id={id}
      role="option"
      aria-selected={selected}
      data-testid="search-result-row"
      onClick={onSelect}
      onKeyDown={(e) => e.key === "Enter" && onSelect()}
      tabIndex={-1}
      className={[
        "flex cursor-pointer items-center gap-[var(--ds-spacing-sm)] px-[var(--ds-spacing-md)] py-[var(--ds-spacing-xs)] text-[length:var(--ds-font-size-body-sm)]",
        selected
          ? "bg-[var(--ds-bg-canvas)] text-[var(--ds-text-accent-warn)]"
          : "text-[var(--ds-text-primary)] hover:bg-[var(--ds-bg-canvas)]",
      ].join(" ")}
    >
      <span className="shrink-0 rounded px-[var(--ds-spacing-xs)] text-[length:var(--ds-font-size-caption)] text-[var(--ds-text-muted)] ring-1 ring-[var(--ds-border-subtle)]">
        {row.kind}
      </span>
      <span className="truncate font-medium">{row.display_name}</span>
      <span className="ml-auto shrink-0 text-[var(--ds-text-muted)]">{row.slug}</span>
    </li>
  );
}
