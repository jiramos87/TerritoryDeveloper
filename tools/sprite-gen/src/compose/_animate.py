"""_animate.py — Per-primitive animate: reservation guard (TECH-738 / Stage 6.7)."""

from __future__ import annotations


def _check_animate(entry: dict) -> dict:
    """Strip + validate the per-primitive ``animate:`` reservation key.

    Rules (Stage 6.7 / TECH-738):
        - Key absent or ``animate: none`` → return *entry* copy without
          ``animate`` so the downstream primitive never sees the kwarg.
        - Any other value → ``NotImplementedError("Animation deferred; see
          DAS §12")`` with the offending value quoted for debugging.
    """
    animate = entry.get("animate", None)
    if animate is None or animate == "none":
        return {k: v for k, v in entry.items() if k != "animate"}
    raise NotImplementedError(
        f"Animation deferred; see DAS §12 "
        f"(unsupported animate value: {animate!r})"
    )
