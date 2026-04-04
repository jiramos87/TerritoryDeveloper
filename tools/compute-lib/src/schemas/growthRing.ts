import { z } from "zod";
import { MAX_CENTROIDS } from "../constants/computeLimits.js";

const point2 = z.object({
  x: z.number().finite(),
  y: z.number().finite(),
});

const centroid = point2.extend({
  weight: z.number().finite().positive().optional(),
});

/** Optional [inner, mid, outer] multipliers vs urban radius (C# defaults: 0.70, 1.00, 1.80). */
export const ringBoundaryFractionsSchema = z.tuple([
  z.number().finite().positive(),
  z.number().finite().positive(),
  z.number().finite().positive(),
]);

export const growthRingClassifyInputShape = {
  cell: point2,
  centroids: z.array(centroid).min(0).max(MAX_CENTROIDS),
  urban_cell_count: z.number().int().nonnegative().optional(),
  urban_radius: z.number().finite().positive().optional(),
  ring_boundary_fractions: ringBoundaryFractionsSchema.optional(),
  fallback_ring: z.enum(["Inner", "Mid", "Outer", "Rural"]).optional(),
};

export const growthRingClassifyInputSchema = z
  .object(growthRingClassifyInputShape)
  .strict()
  .refine(
    (v) => v.urban_cell_count !== undefined || v.urban_radius !== undefined,
    { message: "Provide urban_cell_count or urban_radius" },
  );

export type GrowthRingClassifyInput = z.infer<typeof growthRingClassifyInputSchema>;
