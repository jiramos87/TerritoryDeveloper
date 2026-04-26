// TECH-1352 fixture — routeMeta references a capability id absent from the
// `capability` table seed. Validator must report unknown capability id.
export async function POST() {
  return new Response();
}
export const routeMeta = { POST: { requires: "does.not.exist" } } as const;
