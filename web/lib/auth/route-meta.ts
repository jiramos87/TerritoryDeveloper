export type RouteMeta = {
  GET?:    { requires: string };
  POST?:   { requires: string };
  PUT?:    { requires: string };
  PATCH?:  { requires: string };
  DELETE?: { requires: string };
};

export function forbiddenEnvelope(required: string, role: string): {
  ok: false;
  error: { code: 'forbidden'; message: string; details: { required: string; role: string } };
} {
  return {
    ok: false,
    error: {
      code: 'forbidden',
      message: `Capability '${required}' required`,
      details: { required, role },
    },
  };
}
