/**
 * Rounding consistent with Unity Mathf.RoundToInt (halfway cases away from zero).
 * Used for world → grid so Node golden tests match GridManager.GetGridPosition.
 */
export function roundLikeUnity(n: number): number {
  if (!Number.isFinite(n)) {
    throw new Error("roundLikeUnity: value must be finite");
  }
  if (n >= 0) {
    return Math.floor(n + 0.5);
  }
  return Math.ceil(n - 0.5);
}
