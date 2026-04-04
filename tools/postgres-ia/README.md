# postgres-ia-tools (TECH-44b / TECH-44c)

Small **Node** helpers for the game-owned **PostgreSQL** **IA** schema, **E1** **dev repro bundle** registry (**TECH-44c**), and **TECH-55** **Editor** export registry: apply ordered SQL under [`db/migrations/`](../../db/migrations/), seed sample **glossary** rows, **glossary-by-key** smoke read, **register-dev-repro**, and **register-editor-export**.

**Canonical setup** (Docker one-liner, **`DATABASE_URL`**, SQL examples): [`docs/postgres-ia-dev-setup.md`](../../docs/postgres-ia-dev-setup.md).

## Scripts

| npm script | Purpose |
|------------|---------|
| `migrate` | Run `apply-migrations.mjs` — requires **`psql`** on `PATH` |
| `glossary-by-key` | `node glossary-by-key.mjs <term_key>` — uses `ia_glossary_row_by_key` |
| `seed:glossary` | Parse first N rows from `.cursor/specs/glossary.md` and upsert |
| `register-repro` | `node register-dev-repro.mjs --issue …` — inserts **`dev_repro_bundle`** row |
| `register-editor-export` | `node register-editor-export.mjs --kind … --document-file …` — **TECH-55b** **`document jsonb`** insert |

From repo root:

```bash
npm --prefix tools/postgres-ia install
export DATABASE_URL='postgresql://...'
npm --prefix tools/postgres-ia run migrate
```

Root shortcuts (optional): `npm run db:migrate`, `npm run db:seed:glossary`, `npm run db:glossary -- <term_key>`, `npm run db:register-repro -- --issue TECH-44c …`, `npm run db:register-editor-export -- --kind agent_context --document-file tools/reports/.staging/body.json`.
