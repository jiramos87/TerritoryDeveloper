#!/usr/bin/env node
// cs-find-zero-caller-publics.mjs
// Scan Assets/Scripts/**/*.cs + Assets/Tests/** for public method declarations + count callers.
// Emits a JSON candidate-delete list per file. NEVER auto-deletes — sweep is owned by per-stage cutover tasks.
//
// Allowlist: any method touching invariant-load-bearing surfaces is excluded:
//   - HeightMap write / Cell.height write
//   - InvalidateRoadCache
//   - RefreshShoreTerrainAfterWaterUpdate
//   - cellArray / gridArray
//   - [ContextMenu] / [MenuItem] / [CustomEditor] attrs
//
// Output: JSON list per file to stdout.
// Usage: node tools/scripts/cs-find-zero-caller-publics.mjs [--output /path/out.json]

import { readFileSync, readdirSync, statSync, writeFileSync } from 'fs';
import { join, resolve, relative } from 'path';
import { fileURLToPath } from 'url';

const REPO_ROOT = resolve(fileURLToPath(import.meta.url), '../../..');

const SCAN_DIRS = [
    join(REPO_ROOT, 'Assets/Scripts'),
    join(REPO_ROOT, 'Assets/Tests'),
];

const ALLOWLIST_PATTERNS = [
    /HeightMap\[/,
    /Cell\.height\s*=/,
    /InvalidateRoadCache/,
    /RefreshShoreTerrainAfterWaterUpdate/,
    /cellArray/,
    /gridArray/,
    /\[ContextMenu\]/,
    /\[MenuItem\]/,
    /\[CustomEditor\]/,
];

const args = process.argv.slice(2);
const outputIdx = args.indexOf('--output');
const outputPath = outputIdx !== -1 ? args[outputIdx + 1] : null;

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

// Collect all sources keyed by repo-relative path
const allSources = new Map();
for (const root of SCAN_DIRS) {
    for (const absPath of walkCs(root)) {
        const rel = relative(REPO_ROOT, absPath).replace(/\\/g, '/');
        allSources.set(rel, readFileSync(absPath, 'utf8'));
    }
}

// Build a flat string of all source for caller scanning
const allSourceCombined = [...allSources.values()].join('\n');

// Parse public method names per file
// Match: public [static|virtual|override|abstract]* ReturnType MethodName(
const PUBLIC_METHOD_RE = /(?:\/\/[^\n]*\n\s*)*(?:\[(?:ContextMenu|MenuItem|CustomEditor)[^\]]*\]\s*\n\s*)?public\s+(?:static\s+|virtual\s+|override\s+|abstract\s+|async\s+|sealed\s+)*\w[\w<>\[\], ]*?\s+(\w+)\s*\(/gm;

const results = [];

for (const [relPath, src] of allSources) {
    const methodCandidates = [];

    let m;
    PUBLIC_METHOD_RE.lastIndex = 0;
    while ((m = PUBLIC_METHOD_RE.exec(src)) !== null) {
        const methodName = m[1];

        // Skip constructors (match class name) + common Unity callbacks
        const UNITY_CALLBACKS = new Set([
            'Awake', 'Start', 'Update', 'FixedUpdate', 'LateUpdate',
            'OnEnable', 'OnDisable', 'OnDestroy', 'OnApplicationQuit',
            'OnGUI', 'OnValidate', 'Reset', 'OnDrawGizmos', 'OnDrawGizmosSelected',
            'OnTriggerEnter', 'OnTriggerExit', 'OnCollisionEnter', 'OnCollisionExit',
        ]);
        if (UNITY_CALLBACKS.has(methodName)) continue;
        if (methodName[0] === methodName[0].toUpperCase() && relPath.endsWith(`/${methodName}.cs`)) continue;

        // Check allowlist: get method body (rough: from match pos to next blank line or closing brace)
        const bodyStart = m.index;
        const bodySlice = src.slice(bodyStart, bodyStart + 2000);
        const isAllowlisted = ALLOWLIST_PATTERNS.some(p => p.test(bodySlice));
        if (isAllowlisted) continue;

        // Check preceding line for allowlist attributes
        const preceding = src.slice(Math.max(0, bodyStart - 200), bodyStart);
        const hasAllowlistAttr = /\[(ContextMenu|MenuItem|CustomEditor)/.test(preceding);
        if (hasAllowlistAttr) continue;

        // Count callers across all sources
        const callerCount = (allSourceCombined.match(new RegExp(`\\b${methodName}\\s*\\(`, 'g')) || []).length - 1; // -1 for declaration itself
        if (callerCount === 0) {
            methodCandidates.push({ method: methodName, callers: 0 });
        }
    }

    if (methodCandidates.length > 0) {
        results.push({ file: relPath, zero_caller_publics: methodCandidates });
    }
}

const output = JSON.stringify(results, null, 2);

if (outputPath) {
    writeFileSync(outputPath, output, 'utf8');
    console.error(`[cs-find-zero-caller-publics] Written: ${outputPath} (${results.length} files with candidates)`);
} else {
    process.stdout.write(output + '\n');
}

const totalMethods = results.reduce((s, r) => s + r.zero_caller_publics.length, 0);
process.stderr.write(`[cs-find-zero-caller-publics] ${results.length} file(s), ${totalMethods} candidate public method(s) with zero callers.\n`);
