// TECH-1352 fixture — handler with no `routeMeta` export.
// Validator must report missing routeMeta.requires for POST.
export async function POST() {
  return new Response();
}
