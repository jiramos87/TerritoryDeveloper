/**
 * Re-export interchange v1 Zod schema from territory-compute-lib (single source of truth; glossary Computational MCP tools (TECH-39)).
 */

export {
  geographyInitParamsZodSchema,
  type GeographyInitParamsV1,
  parseGeographyInitParamsV1,
  safeParseGeographyInitParamsV1,
} from "territory-compute-lib";
