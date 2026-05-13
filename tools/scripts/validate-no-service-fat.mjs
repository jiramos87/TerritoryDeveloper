#!/usr/bin/env node
// validate-no-service-fat.mjs
// Scans Assets/Scripts/Domains/*/Services/*.cs for files >500 LOC.
// Honors: // long-file-allowed: {reason}
// Stage 8.0 Tier-F: gate promoted — always ON; no env flag required.

import { readFileSync, readdirSync, statSync } from 'fs';
import { join, resolve } from 'path';
import { fileURLToPath } from 'url';

const REPO_ROOT = resolve(fileURLToPath(import.meta.url), '../../..');
const LOC_LIMIT = 500;

const DOMAINS_DIR = join(REPO_ROOT, 'Assets/Scripts/Domains');

function* walkServices(domainsDir) {
    for (const domain of readdirSync(domainsDir)) {
        const servicesDir = join(domainsDir, domain, 'Services');
        const st = statSync(servicesDir, { throwIfNoEntry: false });
        if (!st || !st.isDirectory()) continue;
        for (const entry of readdirSync(servicesDir)) {
            if (entry.endsWith('.cs')) yield join(servicesDir, entry);
        }
    }
}

let violations = 0;

for (const absPath of walkServices(DOMAINS_DIR)) {
    const src = readFileSync(absPath, 'utf8');
    if (src.includes('// long-file-allowed:')) continue;
    const lines = src.split('\n').length;
    if (lines > LOC_LIMIT) {
        const rel = absPath.slice(REPO_ROOT.length + 1).replace(/\\/g, '/');
        process.stderr.write(`[validate-no-service-fat] VIOLATION ${rel} (${lines} LOC > ${LOC_LIMIT})\n`);
        violations++;
    }
}

if (violations > 0) {
    process.stderr.write(`[validate-no-service-fat] ${violations} service(s) exceed ${LOC_LIMIT} LOC. Add '// long-file-allowed: {reason}' or split.\n`);
    process.exit(1);
}

console.log(`[validate-no-service-fat] OK: no service file exceeds ${LOC_LIMIT} LOC.`);
process.exit(0);
