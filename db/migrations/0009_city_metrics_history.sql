-- Per-tick city metric snapshots for optional dev/analytics (fire-and-forget from Unity).
-- Not Save data; see persistence-system Load pipeline for authoritative game state.

BEGIN;

CREATE TABLE IF NOT EXISTS city_metrics_history (
    id                     bigserial PRIMARY KEY,
    recorded_at            timestamptz NOT NULL DEFAULT now(),
    simulation_tick_index  integer NOT NULL,
    game_date              date NOT NULL,
    population             integer NOT NULL,
    money                  integer NOT NULL,
    happiness              real NOT NULL,
    demand_r               real NOT NULL,
    demand_c               real NOT NULL,
    demand_i               real NOT NULL,
    employment_rate        real NOT NULL,
    forest_coverage        real NOT NULL,
    scenario_id            text,
    metadata               jsonb
);

CREATE INDEX IF NOT EXISTS city_metrics_history_recorded_at_idx
    ON city_metrics_history (recorded_at DESC);

CREATE INDEX IF NOT EXISTS city_metrics_history_scenario_id_idx
    ON city_metrics_history (scenario_id)
    WHERE scenario_id IS NOT NULL;

CREATE INDEX IF NOT EXISTS city_metrics_history_tick_idx
    ON city_metrics_history (simulation_tick_index);

COMMIT;
