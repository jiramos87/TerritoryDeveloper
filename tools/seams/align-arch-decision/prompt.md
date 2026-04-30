# Seam: align-arch-decision

You receive a single architecture decision record (`current_record`) and a `proposed_change` description. Evaluate whether the change warrants an amendment, a supersession, a deprecation, or is a no-op. Return the aligned record and your reasoning.

## Input

```json
{{input}}
```

## Rules

1. **amend** — factual error, typo, missing context, or minor extension that does not change the decision's outcome or rationale. Edit `body` and/or `title` in place. `status` stays `active`.
2. **supersede** — the proposed change introduces a materially different approach that replaces the existing decision. Create a new `aligned_record` with the new approach. Set `status=active`. Fill `supersedes` with `decision_id`. The caller will set the old record to `status=superseded`.
3. **deprecate** — the decision is no longer relevant (feature removed, approach retired). Set `status=deprecated`. `body` may note the reason.
4. **noop** — the proposed change is already reflected in the current record or is not valid. Return the current record unchanged. Explain in `rationale`.

## Escalation contract

If the proposed change is ambiguous (could be amend or supersede), choose **amend** and note the ambiguity in `rationale`. If you cannot determine a safe `change_kind`, return `noop` with a detailed `rationale` asking for clarification.

## Output format

Return a JSON object matching the `AlignArchDecisionOutput` schema. Do not include markdown fences — raw JSON only.

Fields:
- `aligned_record` — the updated decision row (all required fields present)
- `change_kind` — one of `amend | supersede | deprecate | noop`
- `supersedes` — decision id string (only when `change_kind=supersede`)
- `rationale` — one or two sentences explaining the choice
