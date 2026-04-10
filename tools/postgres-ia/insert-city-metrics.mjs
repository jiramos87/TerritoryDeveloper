#!/usr/bin/env node
/**
 * Inserts one row into city_metrics_history from a UTF-8 JSON payload file.
 *
 * Usage: node insert-city-metrics.mjs --payload-file <absolute-path>
 *
 * Payload shape (JSON object):
 *   simulation_tick_index, game_date (YYYY-MM-DD), population, money, happiness,
 *   demand_r, demand_c, demand_i, employment_rate (0..1), forest_coverage (0..1),
 *   scenario_id (optional string), metadata (optional object)
 */

import { readFileSync } from 'node:fs';
import { dirname, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';
import pg from 'pg';
import { resolveDatabaseUrl } from './resolve-database-url.mjs';

const __dirname = dirname(fileURLToPath(import.meta.url));
const REPO_ROOT = resolve(__dirname, '../..');

function parseArgs(argv) {
  let payloadFile = null;
  for (let i = 2; i < argv.length; i++) {
    const a = argv[i];
    if (a === '--payload-file' && argv[i + 1]) {
      payloadFile = argv[++i];
    }
  }
  return { payloadFile };
}

function requireNum(v, field) {
  const n = Number(v);
  if (!Number.isFinite(n)) {
    console.error(`Invalid or missing numeric field: ${field}`);
    process.exit(1);
  }
  return n;
}

function requireInt(v, field) {
  const n = requireNum(v, field);
  if (!Number.isInteger(n)) {
    console.error(`Expected integer for field: ${field}`);
    process.exit(1);
  }
  return n;
}

function requireStr(v, field) {
  if (v === undefined || v === null || String(v).trim() === '') {
    console.error(`Missing string field: ${field}`);
    process.exit(1);
  }
  return String(v).trim();
}

async function main() {
  const { payloadFile } = parseArgs(process.argv);
  if (!payloadFile) {
    console.error('Usage: node insert-city-metrics.mjs --payload-file <path>');
    process.exit(1);
  }

  const dbUrl = resolveDatabaseUrl(REPO_ROOT);
  if (!dbUrl) {
    console.error('Missing DATABASE_URL (or config/postgres-dev.json).');
    process.exit(1);
  }

  let raw;
  try {
    raw = readFileSync(payloadFile, 'utf8');
  } catch (e) {
    console.error('Cannot read payload file:', payloadFile, e.message);
    process.exit(1);
  }

  let doc;
  try {
    doc = JSON.parse(raw);
  } catch (e) {
    console.error('Invalid JSON payload:', e.message);
    process.exit(1);
  }

  const simulationTickIndex = requireInt(doc.simulation_tick_index, 'simulation_tick_index');
  const gameDate = requireStr(doc.game_date, 'game_date');
  const population = requireInt(doc.population, 'population');
  const money = requireInt(doc.money, 'money');
  const happiness = requireNum(doc.happiness, 'happiness');
  const demandR = requireNum(doc.demand_r, 'demand_r');
  const demandC = requireNum(doc.demand_c, 'demand_c');
  const demandI = requireNum(doc.demand_i, 'demand_i');
  const employmentRate = requireNum(doc.employment_rate, 'employment_rate');
  const forestCoverage = requireNum(doc.forest_coverage, 'forest_coverage');

  let scenarioId = null;
  if (doc.scenario_id != null && String(doc.scenario_id).trim() !== '') {
    scenarioId = String(doc.scenario_id).trim();
  }

  let metadata = null;
  if (doc.metadata != null && typeof doc.metadata === 'object') {
    metadata = doc.metadata;
  }

  const client = new pg.Client({ connectionString: dbUrl });
  await client.connect();
  try {
    await client.query(
      `INSERT INTO city_metrics_history (
        simulation_tick_index, game_date, population, money, happiness,
        demand_r, demand_c, demand_i, employment_rate, forest_coverage,
        scenario_id, metadata
      ) VALUES ($1,$2,$3,$4,$5,$6,$7,$8,$9,$10,$11,$12)`,
      [
        simulationTickIndex,
        gameDate,
        population,
        money,
        happiness,
        demandR,
        demandC,
        demandI,
        employmentRate,
        forestCoverage,
        scenarioId,
        metadata,
      ],
    );
  } finally {
    await client.end();
  }

  console.log('ok');
}

main().catch((e) => {
  console.error(e);
  process.exit(1);
});
