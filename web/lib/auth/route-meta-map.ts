const map: Record<string, Record<string, { requires: string }>> = {
  '/api/catalog/assets':                  { GET: { requires: 'catalog.entity.create' }, POST: { requires: 'catalog.entity.create' } },
  '/api/catalog/assets/[id]':             { GET: { requires: 'catalog.entity.create' }, PATCH: { requires: 'catalog.entity.edit' } },
  '/api/catalog/assets/[id]/retire':      { POST: { requires: 'catalog.entity.retire' } },
  '/api/catalog/preview-diff':            { POST: { requires: 'render.run' } },
  '/api/render/runs':                     { POST: { requires: 'render.run' } },
  '/api/render/runs/[job_id]':            { GET: { requires: 'audit.read' } },
  '/api/render/runs/[run_id]/replay':     { POST: { requires: 'render.run' } },
  '/api/render/runs/[run_id]/identical':  { POST: { requires: 'render.run' } },
};

export default map;
