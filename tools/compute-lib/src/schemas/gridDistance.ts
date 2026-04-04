import { z } from "zod";
import { DEFAULT_MAX_GRID_DIMENSION } from "../constants/computeLimits.js";

export const gridDistanceInputShape = {
  ax: z.number().int(),
  ay: z.number().int(),
  bx: z.number().int(),
  by: z.number().int(),
  mode: z.enum(["chebyshev", "manhattan"]),
  map_width: z.number().int().min(1).max(DEFAULT_MAX_GRID_DIMENSION).optional(),
  map_height: z.number().int().min(1).max(DEFAULT_MAX_GRID_DIMENSION).optional(),
};

export const gridDistanceInputSchema = z.object(gridDistanceInputShape).strict();

export type GridDistanceInput = z.infer<typeof gridDistanceInputSchema>;
