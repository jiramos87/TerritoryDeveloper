# Project memory — Territory Developer

Index of architectural decisions and durable context that doesn't fit cleanly in
specs, commit messages, or BACKLOG rows. One line per entry. Promote an entry to
its own file under `.claude/memory/{slug}.md` only when it grows past ~10 lines
(per **TECH-85** Q12 resolution).

Format: `- [Title](path-or-anchor) — one-line hook`

## Architecture decisions

- [TECH-85 Opción C: ia/ namespace + native Claude Code layer](ia/projects/TECH-85-ia-migration.md) — IA migrated to a tool-neutral `ia/` namespace with Cursor back-compat via symlinks; native Claude Code layer (subagents, hooks, slash commands, output styles, project memory) added without breaking Cursor; 5-stage execution model with one fresh agent per stage and a new `project-stage-close` skill bootstrapped in Stage 1.
