# Recipe runner — seam escalation handoff file format

When a seam step fails (schema_out, dispatch_unavailable, schema_in, refusal, timeout), the recipe engine writes a handoff file to `ia/state/recipe-runs/{run_id}/seam-{step_id}-error.md`. This file is the Q5 escalation artifact (see `docs/agent-as-recipe-runner.md §Q5`).

## Mandatory lines

Each handoff file must contain these lines (order not enforced):

```
recipe: {recipe_slug}          step: {step_id}          seam: {seam_name}
input: ia/state/recipe-runs/{run_id}/seam-{step_id}-input.json
attempted-output: ia/state/recipe-runs/{run_id}/seam-{step_id}-attempted-output.json
validation-error: {error_code} {json_details}
resume-cursor: {step_id}
human-options: [1] fix-in-place [2] accept-as-is [3] abort
```

`attempted-output:` line reads `(not produced)` when the seam was never dispatched (e.g. `dispatch_unavailable`).

## Sidecar files

| File | Present when |
|---|---|
| `seam-{step_id}-input.json` | Always — validated seam input payload |
| `seam-{step_id}-attempted-output.json` | schema_out or refusal — the bad output |

## Parent re-dispatch flow

After human reviews the handoff, three options:

1. **Fix-in-place**: agent edits `seam-{step_id}-attempted-output.json` to a valid payload, sets `attempted-output` path in step `expected_output`, re-runs recipe from `resume-cursor`.
2. **Accept-as-is**: agent sets step `expected_output` to current attempted output bypassing schema gate (emergency only).
3. **Abort**: discard run; no resume.

Parent re-dispatch reads `resume-cursor` to skip already-completed steps via the recipe engine's resume gate (reads `ia_recipe_runs` DB table rows with `status=ok` for the run_id).

## No-retry contract (Q5)

The recipe engine does **not** auto-retry on seam failure. One invocation per step per run. Retry is a human decision. This avoids burning LLM tokens on a failure the model already knows about.
