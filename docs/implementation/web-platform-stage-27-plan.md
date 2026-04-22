# Web platform — Stage 27 implementation aggregate

Compiled during stage-file-main-session. Specs: TECH-655 … TECH-661.

## Tasks

| Issue | Task |
|-------|------|
| TECH-655 | T27.1 audit harness |
| TECH-656 | T27.2 landing |
| TECH-657 | T27.3 dashboard |
| TECH-658 | T27.4 releases |
| TECH-659 | T27.5 progress |
| TECH-660 | T27.6 design-system |
| TECH-661 | T27.7 lighthouse |

## §Plan Digest rubric

Each task spec carries **Gate:** `npm run validate:web` plus **STOP** + **Edits:** blocks per plan-digest contract.

**Gate:**

```bash
npm run validate:web
```

**STOP:**

Re-run digest lint when aggregate edits change.

**Edits:**

- `docs/implementation/web-platform-stage-27-plan.md` — this aggregate file.