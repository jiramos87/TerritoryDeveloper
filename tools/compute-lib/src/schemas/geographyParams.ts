/**
 * Zod mirror of docs/schemas/geography-init-params.v1.schema.json (TECH-41 interchange v1).
 * Shared with MCP validation and fixture checks.
 */

import { z } from "zod";

const mapSchema = z
  .object({
    width: z.number().int().min(1),
    height: z.number().int().min(1),
  })
  .strict();

const waterSchema = z
  .object({
    seaBias: z.number().min(0).max(1),
  })
  .strict();

const riversSchema = z
  .object({
    enabled: z.boolean(),
  })
  .strict();

const forestSchema = z
  .object({
    coverageTarget: z.number().min(0).max(1),
  })
  .strict();

/** Matches interchange v1 — same constraints as JSON Schema pilot. */
export const geographyInitParamsZodSchema = z
  .object({
    artifact: z.literal("geography_init_params"),
    schema_version: z.literal(1),
    seed: z.number().int(),
    map: mapSchema,
    water: waterSchema.optional(),
    rivers: riversSchema.optional(),
    forest: forestSchema.optional(),
  })
  .strict();

export type GeographyInitParamsV1 = z.infer<typeof geographyInitParamsZodSchema>;

export function parseGeographyInitParamsV1(data: unknown): GeographyInitParamsV1 {
  return geographyInitParamsZodSchema.parse(data);
}

export function safeParseGeographyInitParamsV1(data: unknown) {
  return geographyInitParamsZodSchema.safeParse(data);
}
