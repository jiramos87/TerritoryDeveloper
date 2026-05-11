#!/usr/bin/env node
// validate-no-hub-fat.mjs
// Scans Assets/Scripts/Managers/**/*.cs + Controllers/**/*.cs for files >200 LOC.
// Honors: // long-file-allowed: {reason}
// Gated behind ATOMIZATION_GATES=1 env flag; exits 0 when flag absent.
// Wire: package.json validate:no-hub-fat; validate:all reference deferred to Stage 8.0.

import { readFileSync, readdirSync, statSync } from 'fs';
import { join, resolve } from 'path';
import { fileURLToPath } from 'url';

const REPO_ROOT = resolve(fileURLToPath(import.meta.url), '../../..');
const GATE_ACTIVE = process.env.ATOMIZATION_GATES === '1';
const LOC_LIMIT = 200;

if (!GATE_ACTIVE) {
    console.log('[validate-no-hub-fat] ATOMIZATION_GATES not set — skipping (stub OFF).');
    process.exit(0);
}

const SCAN_DIRS = [
    join(REPO_ROOT, 'Assets/Scripts/Managers'),
    join(REPO_ROOT, 'Assets/Scripts/Controllers'),
];

function* walkCs(dir) {
    if (!statSync(dir, { throwIfNoEntry: false })) return;
    for (const entry of readdirSync(dir)) {
        const full = join(dir, entry);
        const st = statSync(full);
        if (st.isDirectory()) yield* walkCs(full);
        else if (entry.endsWith('.cs')) yield full;
    }
}

let violations = 0;

for (const root of SCAN_DIRS) {
    for (const absPath of walkCs(root)) {
        const src = readFileSync(absPath, 'utf8');
        if (src.includes('// long-file-allowed:')) continue;
        const lines = src.split('\n').length;
        if (lines > LOC_LIMIT) {
            const rel = absPath.slice(REPO_ROOT.length + 1).replace(/\\/g, '/');
            process.stderr.write(`[validate-no-hub-fat] VIOLATION ${rel} (${lines} LOC > ${LOC_LIMIT})\n`);
            violations++;
        }
    }
}

if (violations > 0) {
    process.stderr.write(`[validate-no-hub-fat] ${violations} hub(s) exceed ${LOC_LIMIT} LOC. Add '// long-file-allowed: {reason}' or split.\n`);
    process.exit(1);
}

console.log(`[validate-no-hub-fat] OK: no hub file exceeds ${LOC_LIMIT} LOC.`);
process.exit(0);
