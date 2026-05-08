#!/usr/bin/env node
// validate-no-domain-game-cycle.mjs
// Scans Assets/Scripts/Domains/**/*.asmdef for Game GUID back-reference.
// Exit 0 = clean. Exit 1 = cycle detected; stderr lists offending paths.
// Wire into validate:all BEFORE unity:compile-check (CY-E early fail gate).

import { readdirSync, readFileSync, statSync } from 'fs';
import { join, resolve } from 'path';
import { fileURLToPath } from 'url';

const GAME_GUID = 'GUID:7d8f9e2a1b4c5d6e7f8a9b0c1d2e3f4a';
const REPO_ROOT = resolve(fileURLToPath(import.meta.url), '../../..');
const DOMAINS_DIR = join(REPO_ROOT, 'Assets/Scripts/Domains');

// Known exceptions: Roads.asmdef retains Game GUID until AutoBuildService is
// interface-abstracted (Roads→Game cycle tracked in Stage 20 notes).
// Remove entry from this list when the Roads domain cycle is resolved.
const KNOWN_EXCEPTIONS = new Set([
    'Assets/Scripts/Domains/Roads/Roads.asmdef',
]);

function* walkAsmdef(dir) {
    for (const entry of readdirSync(dir)) {
        const full = join(dir, entry);
        const st = statSync(full);
        if (st.isDirectory()) {
            yield* walkAsmdef(full);
        } else if (entry.endsWith('.asmdef')) {
            yield full;
        }
    }
}

let violations = 0;

for (const absPath of walkAsmdef(DOMAINS_DIR)) {
    const repoRelPath = absPath.slice(REPO_ROOT.length + 1).replace(/\\/g, '/');
    let parsed;
    try {
        parsed = JSON.parse(readFileSync(absPath, 'utf8'));
    } catch (e) {
        process.stderr.write(`[validate-no-domain-game-cycle] PARSE ERROR ${repoRelPath}: ${e.message}\n`);
        violations++;
        continue;
    }
    const refs = Array.isArray(parsed.references) ? parsed.references : [];
    if (refs.includes(GAME_GUID)) {
        if (KNOWN_EXCEPTIONS.has(repoRelPath)) {
            process.stderr.write(`[validate-no-domain-game-cycle] KNOWN EXCEPTION (cycle not yet resolved): ${repoRelPath} contains ${GAME_GUID}\n`);
            continue;
        }
        process.stderr.write(`[validate-no-domain-game-cycle] CYCLE DETECTED: ${repoRelPath} contains ${GAME_GUID}\n`);
        violations++;
    }
}

if (violations > 0) {
    process.stderr.write(`[validate-no-domain-game-cycle] FAIL: ${violations} violation(s). Domains must not reference Game GUID directly.\n`);
    process.exit(1);
}

process.stdout.write('[validate-no-domain-game-cycle] OK: zero Domain→Game GUID back-refs detected.\n');
process.exit(0);
