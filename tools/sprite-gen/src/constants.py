"""Sprite-gen dimensional constants (DAS-aligned, Stage 6 T6.4)."""

from __future__ import annotations

# DAS §2.4 level heights per class. Keys track spec._DEFAULT_GROUND.
LEVEL_H: dict[str, int] = {
    "residential_small": 12,
    "commercial_store": 12,
    "commercial_small": 12,
    "residential_heavy": 16,
    "residential_dense_tower": 16,
    "commercial_dense": 16,
    "industrial_light": 16,
    "industrial_heavy": 16,
    "power_nuclear": 16,
    "waterplant": 16,
}
DEFAULT_LEVEL_H = 12
