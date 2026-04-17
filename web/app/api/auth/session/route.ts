export async function GET(_req: Request): Promise<Response> {
  return Response.json({ error: 'Not Implemented' }, { status: 501 });
}
