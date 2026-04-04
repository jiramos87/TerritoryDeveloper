-- TECH-44b — read surface for TECH-18 / scripts (callable from psql or Node).

BEGIN;

CREATE OR REPLACE FUNCTION ia_glossary_row_by_key(p_term_key text)
RETURNS TABLE (
  id          bigint,
  term_key    text,
  term        text,
  definition  text,
  spec_key    text,
  category    text
)
LANGUAGE sql
STABLE
AS $$
  SELECT g.id, g.term_key, g.term, g.definition, g.spec_key, g.category
  FROM glossary g
  WHERE g.term_key = p_term_key;
$$;

COMMENT ON FUNCTION ia_glossary_row_by_key(text) IS
  'TECH-44b minimal read path: one glossary row by stable term_key (e.g. heightmap).';

COMMIT;
