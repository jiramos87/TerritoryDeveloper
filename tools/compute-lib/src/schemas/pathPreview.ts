import { z } from "zod";
import { DEFAULT_MAX_GRID_DIMENSION } from "../constants/computeLimits.js";

const cell = z.object({
  x: z.number().int(),
  y: z.number().int(),
});

export const pathfindingCostPreviewInputShape = {
  from_cell: cell,
  to_cell: cell,
  unit_cost_per_step: z.number().finite().positive().optional(),
  map_width: z.number().int().min(1).max(DEFAULT_MAX_GRID_DIMENSION).optional(),
  map_height: z.number().int().min(1).max(DEFAULT_MAX_GRID_DIMENSION).optional(),
};

export const pathfindingCostPreviewInputSchema = z
  .object(pathfindingCostPreviewInputShape)
  .strict();

export type PathfindingCostPreviewInput = z.infer<
  typeof pathfindingCostPreviewInputSchema
>;
