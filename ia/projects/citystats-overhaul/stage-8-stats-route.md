### Stage 8 — Multi-scale Rollup + Web Stats Surface / web/app/stats route

**Status:** Draft (tasks _pending_ — not yet filed)

**Objectives:** New `/stats` page in Next.js app: time-series line chart + sortable table backed by `city_metrics_history` Postgres table. Reuses existing web components without modification.

**Exit:**

- `web/app/stats/page.tsx` renders without runtime errors; `export const revalidate = 60`.
- Line chart: population + happiness + money over last 30 ticks via `PlanChartClient`.
- Sortable table: all `city_metrics_history` columns (tick index, date, population, money, happiness, demand_r/c/i, employment) newest-first via `DataTable`.
- `web/lib/db/statsQueries.ts` queries compile; typed return matches `CityMetricsInsertPayload` columns.
- `npm run validate:web` clean.
- Phase 1 — Route scaffold + Postgres query helpers.
- Phase 2 — Line chart + sortable table UI components.

**Tasks:**

| Task | Issue | Status | Intent |
| --- | --- | --- | --- |
| T8.1 | _pending_ | _pending_ | Add `web/app/stats/page.tsx` (new, Server Component): `export const revalidate = 60`; call `getLatestCityMetrics(100)` + `getCityMetricsSeries('population', 30)` from `statsQueries.ts`; pass arrays as props to client components. No UI yet — confirms data shape and `npm run validate:web` build succeeds. Follow ISR pattern from `web/app/dashboard/page.tsx`. |
| T8.2 | _pending_ | _pending_ | Add `web/lib/db/statsQueries.ts` (new): `getLatestCityMetrics(limit: number): Promise<CityMetricsRow[]>` (SELECT * FROM city_metrics_history ORDER BY simulation_tick_index DESC LIMIT $1) + `getCityMetricsSeries(metric: string, ticks: number): Promise<{tick: number, value: number}[]>`; use `web/lib/db/client.ts` client; define `CityMetricsRow` type matching `CityMetricsInsertPayload` columns. |
| T8.3 | _pending_ | _pending_ | Wire time-series chart in `web/app/stats/page.tsx`: pass `getCityMetricsSeries` data to `PlanChartClient` (`web/components/PlanChartClient.tsx` reused as-is); render three series — population, happiness, money — over last 30 ticks. Follow `PlanChartClient` prop contract from existing dashboard usage. |
| T8.4 | _pending_ | _pending_ | Wire sortable table: pass `getLatestCityMetrics(100)` rows to `DataTable` (`web/components/DataTable.tsx` reused as-is); columns: tick index, date, population, money, happiness, demand_r/c/i, employment; default sort descending by tick index. Follow `DataTable` prop contract from existing dashboard usage. |

---
