# Repository config (versioned)

## `postgres-dev.json`

Default **local** PostgreSQL URI for **Information Architecture** dev tools (`npm run db:migrate`, **territory-ia** `project_spec_journal_*` when the MCP process has no `DATABASE_URL`, scripts under `tools/postgres-ia/`).

**Precedence:** environment variable **`DATABASE_URL`** overrides this file when set.

**CI:** when **`CI=true`** or **`GITHUB_ACTIONS`** is set, Node tools and **territory-ia** **do not** read this file (no accidental `localhost` connections in GitHub Actions). Set **`DATABASE_URL`** explicitly in a job only when you run integration tests against a service container.

**Scope:** committed defaults for a shared local database name and host — **not** for production secrets. Use `.env` / `.env.local` (gitignored) for passwords or non-shared URLs; agents and CI typically cannot read those files.

See [`docs/postgres-ia-dev-setup.md`](../docs/postgres-ia-dev-setup.md).
