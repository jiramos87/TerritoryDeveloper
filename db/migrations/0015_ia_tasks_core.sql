-- IA dev system DB-primary refactor — step 1 (schema foundation).
-- Source: docs/ia-dev-db-refactor-implementation.md §Step 1
-- Design: docs/master-plan-foldering-refactor-design.md §4.2 + §4.5 (F1–F15)

BEGIN;

-- Extensions --------------------------------------------------------------

CREATE EXTENSION IF NOT EXISTS pg_trgm;

-- Enum types (F1) ---------------------------------------------------------

DO $$
BEGIN
  IF NOT EXISTS (SELECT 1 FROM pg_type WHERE typname = 'task_status') THEN
    CREATE TYPE task_status AS ENUM (
      'pending',
      'implemented',
      'verified',
      'done',
      'archived'
    );
  END IF;
  IF NOT EXISTS (SELECT 1 FROM pg_type WHERE typname = 'stage_status') THEN
    CREATE TYPE stage_status AS ENUM (
      'pending',
      'in_progress',
      'done'
    );
  END IF;
  IF NOT EXISTS (SELECT 1 FROM pg_type WHERE typname = 'stage_verdict') THEN
    CREATE TYPE stage_verdict AS ENUM (
      'pass',
      'fail',
      'partial'
    );
  END IF;
  IF NOT EXISTS (SELECT 1 FROM pg_type WHERE typname = 'ia_task_dep_kind') THEN
    CREATE TYPE ia_task_dep_kind AS ENUM (
      'depends_on',
      'related'
    );
  END IF;
END
$$;

-- Master plans ------------------------------------------------------------

CREATE TABLE IF NOT EXISTS ia_master_plans (
  slug              text PRIMARY KEY,
  title             text NOT NULL,
  source_spec_path  text,
  created_at        timestamptz NOT NULL DEFAULT now(),
  updated_at        timestamptz NOT NULL DEFAULT now()
);

-- Stages (composite PK: slug + stage_id) ----------------------------------

CREATE TABLE IF NOT EXISTS ia_stages (
  slug              text NOT NULL REFERENCES ia_master_plans (slug) ON DELETE CASCADE,
  stage_id          text NOT NULL,
  title             text,
  objective         text,
  exit_criteria     text,
  status            stage_status NOT NULL DEFAULT 'pending',
  source_file_path  text,
  created_at        timestamptz NOT NULL DEFAULT now(),
  updated_at        timestamptz NOT NULL DEFAULT now(),
  PRIMARY KEY (slug, stage_id)
);

CREATE INDEX IF NOT EXISTS ia_stages_slug_idx ON ia_stages (slug);
CREATE INDEX IF NOT EXISTS ia_stages_status_idx ON ia_stages (status);

-- Tasks (F2: task_id text PRIMARY KEY; F4: dual GIN) ----------------------

CREATE TABLE IF NOT EXISTS ia_tasks (
  task_id       text PRIMARY KEY,
  prefix        text NOT NULL CHECK (prefix IN ('TECH', 'FEAT', 'BUG', 'ART', 'AUDIO')),
  slug          text,
  stage_id      text,
  title         text NOT NULL,
  status        task_status NOT NULL DEFAULT 'pending',
  priority      text,
  type          text,
  notes         text,
  body          text NOT NULL DEFAULT '',
  body_tsv      tsvector GENERATED ALWAYS AS (
                  to_tsvector('english', coalesce(body, ''))
                ) STORED,
  created_at    timestamptz NOT NULL DEFAULT now(),
  updated_at    timestamptz NOT NULL DEFAULT now(),
  completed_at  timestamptz,
  archived_at   timestamptz,
  CONSTRAINT ia_tasks_stage_fk
    FOREIGN KEY (slug, stage_id) REFERENCES ia_stages (slug, stage_id)
    ON DELETE RESTRICT
    DEFERRABLE INITIALLY DEFERRED
);

CREATE INDEX IF NOT EXISTS ia_tasks_stage_idx ON ia_tasks (slug, stage_id);
CREATE INDEX IF NOT EXISTS ia_tasks_status_idx ON ia_tasks (status);
CREATE INDEX IF NOT EXISTS ia_tasks_prefix_idx ON ia_tasks (prefix);
CREATE INDEX IF NOT EXISTS ia_tasks_updated_idx ON ia_tasks (updated_at DESC);
CREATE INDEX IF NOT EXISTS ia_tasks_body_tsv_idx ON ia_tasks USING GIN (body_tsv);
CREATE INDEX IF NOT EXISTS ia_tasks_body_trgm_idx ON ia_tasks USING GIN (body gin_trgm_ops);

-- Task deps (F3: separate join table with kind discriminator) -------------

CREATE TABLE IF NOT EXISTS ia_task_deps (
  task_id         text NOT NULL REFERENCES ia_tasks (task_id) ON DELETE CASCADE,
  depends_on_id   text NOT NULL REFERENCES ia_tasks (task_id) ON DELETE RESTRICT,
  kind            ia_task_dep_kind NOT NULL,
  created_at      timestamptz NOT NULL DEFAULT now(),
  PRIMARY KEY (task_id, depends_on_id, kind)
);

CREATE INDEX IF NOT EXISTS ia_task_deps_depends_on_idx ON ia_task_deps (depends_on_id);
CREATE INDEX IF NOT EXISTS ia_task_deps_kind_idx ON ia_task_deps (kind);

-- Task spec history (F5: full snapshots) ----------------------------------

CREATE TABLE IF NOT EXISTS ia_task_spec_history (
  id              bigserial PRIMARY KEY,
  task_id         text NOT NULL REFERENCES ia_tasks (task_id) ON DELETE CASCADE,
  body            text NOT NULL,
  recorded_at     timestamptz NOT NULL DEFAULT now(),
  actor           text,
  git_sha         text,
  change_reason   text
);

CREATE INDEX IF NOT EXISTS ia_task_spec_history_task_idx
  ON ia_task_spec_history (task_id, recorded_at DESC);

-- Task commits ------------------------------------------------------------

CREATE TABLE IF NOT EXISTS ia_task_commits (
  id            bigserial PRIMARY KEY,
  task_id       text NOT NULL REFERENCES ia_tasks (task_id) ON DELETE CASCADE,
  commit_sha    text NOT NULL,
  commit_kind   text NOT NULL CHECK (commit_kind IN ('feat', 'fix', 'chore', 'docs', 'refactor', 'test')),
  message       text,
  recorded_at   timestamptz NOT NULL DEFAULT now(),
  CONSTRAINT uq_ia_task_commits UNIQUE (task_id, commit_sha)
);

CREATE INDEX IF NOT EXISTS ia_task_commits_task_idx ON ia_task_commits (task_id, recorded_at DESC);

-- Stage verifications (F11: latest upsert, history preserved) -------------

CREATE TABLE IF NOT EXISTS ia_stage_verifications (
  id            bigserial PRIMARY KEY,
  slug          text NOT NULL,
  stage_id      text NOT NULL,
  verdict       stage_verdict NOT NULL,
  commit_sha    text,
  notes         text,
  verified_at   timestamptz NOT NULL DEFAULT now(),
  actor         text,
  CONSTRAINT ia_stage_verifications_stage_fk
    FOREIGN KEY (slug, stage_id) REFERENCES ia_stages (slug, stage_id)
    ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS ia_stage_verifications_stage_idx
  ON ia_stage_verifications (slug, stage_id, verified_at DESC);

-- Ship-stage journal (F7: discriminated union payload) --------------------

CREATE TABLE IF NOT EXISTS ia_ship_stage_journal (
  id              bigserial PRIMARY KEY,
  session_id      text NOT NULL,
  task_id         text REFERENCES ia_tasks (task_id) ON DELETE RESTRICT,
  slug            text,
  stage_id        text,
  phase           text NOT NULL,
  payload_kind    text NOT NULL,
  payload         jsonb NOT NULL,
  recorded_at     timestamptz NOT NULL DEFAULT now()
);

CREATE INDEX IF NOT EXISTS ia_ship_stage_journal_session_idx
  ON ia_ship_stage_journal (session_id, recorded_at);
CREATE INDEX IF NOT EXISTS ia_ship_stage_journal_task_idx
  ON ia_ship_stage_journal (task_id);
CREATE INDEX IF NOT EXISTS ia_ship_stage_journal_stage_idx
  ON ia_ship_stage_journal (slug, stage_id, recorded_at);
CREATE INDEX IF NOT EXISTS ia_ship_stage_journal_payload_kind_idx
  ON ia_ship_stage_journal (payload_kind);

-- Fix-plan tuples (F8: soft-delete via applied_at + 30-day TTL) -----------

CREATE TABLE IF NOT EXISTS ia_fix_plan_tuples (
  id            bigserial PRIMARY KEY,
  task_id       text NOT NULL REFERENCES ia_tasks (task_id) ON DELETE CASCADE,
  round         int  NOT NULL,
  tuple_index   int  NOT NULL,
  tuple         jsonb NOT NULL,
  created_at    timestamptz NOT NULL DEFAULT now(),
  applied_at    timestamptz,
  CONSTRAINT uq_ia_fix_plan_tuples UNIQUE (task_id, round, tuple_index)
);

-- TTL (30 days post-applied_at) computed in query layer — Postgres
-- rejects STORED generation over `timestamptz + interval` (not immutable
-- across session_timezone). Query layer uses `applied_at + interval '30 days'`.

CREATE INDEX IF NOT EXISTS ia_fix_plan_tuples_task_idx
  ON ia_fix_plan_tuples (task_id, round);
CREATE INDEX IF NOT EXISTS ia_fix_plan_tuples_unapplied_idx
  ON ia_fix_plan_tuples (task_id, round)
  WHERE applied_at IS NULL;
CREATE INDEX IF NOT EXISTS ia_fix_plan_tuples_applied_at_idx
  ON ia_fix_plan_tuples (applied_at)
  WHERE applied_at IS NOT NULL;

-- Sequences per id prefix (seeds from ia/state/id-counter.json 2026-04-24) -

CREATE SEQUENCE IF NOT EXISTS tech_id_seq  AS bigint START WITH 777 MINVALUE 1;
CREATE SEQUENCE IF NOT EXISTS feat_id_seq  AS bigint START WITH 54  MINVALUE 1;
CREATE SEQUENCE IF NOT EXISTS bug_id_seq   AS bigint START WITH 59  MINVALUE 1;
CREATE SEQUENCE IF NOT EXISTS art_id_seq   AS bigint START WITH 5   MINVALUE 1;
CREATE SEQUENCE IF NOT EXISTS audio_id_seq AS bigint START WITH 2   MINVALUE 1;

-- Guard: if sequences pre-exist from a prior partial migration, fast-forward
-- them so nextval never regresses below the filesystem counter.

SELECT setval('tech_id_seq',  GREATEST(777, (SELECT last_value FROM tech_id_seq)),  false);
SELECT setval('feat_id_seq',  GREATEST(54,  (SELECT last_value FROM feat_id_seq)),  false);
SELECT setval('bug_id_seq',   GREATEST(59,  (SELECT last_value FROM bug_id_seq)),   false);
SELECT setval('art_id_seq',   GREATEST(5,   (SELECT last_value FROM art_id_seq)),   false);
SELECT setval('audio_id_seq', GREATEST(2,   (SELECT last_value FROM audio_id_seq)), false);

COMMIT;
