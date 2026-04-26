// TECH-1352 fixture — green path: routeMeta declares a capability id present
// in the seed (catalog.entity.create). Validator must exit 0.
export async function POST() {
  return new Response();
}
export const routeMeta = { POST: { requires: "catalog.entity.create" } } as const;
