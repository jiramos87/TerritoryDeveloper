#!/usr/bin/env node
// validate-registry-resolve-pattern.mjs
// Flags three anti-patterns in Assets/Scripts/Domains/ service files:
//   1. [SerializeField] private XManager — direct manager field inject in services
//   2. new ConcreteService(...) — cross-Domain consumer-side direct construction (same-file factory exempt)
//   3. Resolve<>() called inside Awake() body — must only be in Start / method bodies
// Stage 8.0 Tier-F: gate promoted — always ON; no env flag required.

import { readFileSync, readdirSync, statSync } from 'fs';
import { join, resolve } from 'path';
import { fileURLToPath } from 'url';

const REPO_ROOT = resolve(fileURLToPath(import.meta.url), '../../..');

const DOMAINS_DIR = join(REPO_ROOT, 'Assets/Scripts/Domains');

function* walkCs(dir) {
    const st = statSync(dir, { throwIfNoEntry: false });
    if (!st || !st.isDirectory()) return;
    for (const entry of readdirSync(dir)) {
        const full = join(dir, entry);
        const s = statSync(full);
        if (s.isDirectory()) yield* walkCs(full);
        else if (entry.endsWith('.cs')) yield full;
    }
}

// Detect Resolve<> calls inside an Awake() body (simple heuristic: Awake block contains Resolve<)
function hasResolveInAwake(src) {
    // Find Awake method body between braces
    const awakeMatch = src.match(/void\s+Awake\s*\(\s*\)[^{]*\{([\s\S]*?)\n\s*\}/);
    if (!awakeMatch) return false;
    return awakeMatch[1].includes('Resolve<');
}

let violations = 0;

for (const absPath of walkCs(DOMAINS_DIR)) {
    const rel = absPath.slice(REPO_ROOT.length + 1).replace(/\\/g, '/');
    const src = readFileSync(absPath, 'utf8');

    // Anti-pattern 1: [SerializeField] private XManager field in services
    if (/\[SerializeField\]\s*private\s+\w*Manager/.test(src)) {
        process.stderr.write(`[validate-registry-resolve-pattern] VIOLATION (SerializeField Manager) ${rel}\n`);
        violations++;
    }

    // Anti-pattern 2: new ConcreteService(...) cross-Domain consumer-side
    // Files with '// registry-resolve-exempt:' are internal-factory orchestrators (Tier-E splits) — skip.
    if (!src.includes('// registry-resolve-exempt:')) {
        const newServiceMatches = src.match(/new\s+\w+Service\s*\(/g);
        if (newServiceMatches) {
            process.stderr.write(`[validate-registry-resolve-pattern] VIOLATION (new ConcreteService) ${rel}: ${newServiceMatches.join(', ')}\n`);
            violations++;
        }
    }

    // Anti-pattern 3: Resolve<> inside Awake()
    if (hasResolveInAwake(src)) {
        process.stderr.write(`[validate-registry-resolve-pattern] VIOLATION (Resolve in Awake) ${rel}\n`);
        violations++;
    }
}

if (violations > 0) {
    process.stderr.write(`[validate-registry-resolve-pattern] ${violations} violation(s) found. Fix anti-patterns.\n`);
    process.exit(1);
}

console.log('[validate-registry-resolve-pattern] OK: no registry anti-patterns found.');
process.exit(0);
