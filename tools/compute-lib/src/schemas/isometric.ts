import { z } from "zod";

/** Raw shape for MCP `registerTool` inputSchema (shared with Zod object below). */
export const isometricWorldToGridInputShape = {
  world_x: z.number().finite(),
  world_y: z.number().finite(),
  tile_width: z.number().finite().positive(),
  tile_height: z.number().finite().positive(),
  origin_x: z.number().finite().optional(),
  origin_y: z.number().finite().optional(),
};

/** MCP / interchange: planar world → grid (matches territory-ia isometric_world_to_grid). */
export const isometricWorldToGridInputSchema = z.object(
  isometricWorldToGridInputShape,
);

export type IsometricWorldToGridInput = z.infer<
  typeof isometricWorldToGridInputSchema
>;
