#!/usr/bin/env node
/**
 * parse-nunit-xml.mjs
 * Reads a NUnit 3 XML result file (<test-run> root) and emits structured output.
 *
 * Usage:
 *   node parse-nunit-xml.mjs <path-to-xml> [--format=json|text]
 *
 * JSON output (default):
 *   { passed, failed, errors, skipped, failures: [fullname, ...] }
 *
 * Text output (--format=text):
 *   Passed: N  Failed: M  Errors: K  Skipped: S
 *   FAILED: <fullname1>
 *   FAILED: <fullname2>
 *
 * Exit codes:
 *   0  All tests passed (failed + errors == 0)
 *   1  One or more tests failed or errored
 *   2  Usage / file error
 */

import { readFileSync } from 'node:fs';

const args = process.argv.slice(2);
const formatArg = args.find(a => a.startsWith('--format='));
const format = formatArg ? formatArg.split('=')[1] : 'json';
const xmlPath = args.filter(a => !a.startsWith('--'))[0];

if (!xmlPath) {
  console.error('Usage: parse-nunit-xml.mjs <path-to-xml> [--format=json|text]');
  process.exit(2);
}

let xml;
try {
  xml = readFileSync(xmlPath, 'utf8');
} catch (err) {
  console.error(`parse-nunit-xml: cannot read file: ${xmlPath} — ${err.message}`);
  process.exit(2);
}

// Extract test-run attributes (total, passed, failed, errors, skipped)
function attr(xmlStr, name) {
  const m = new RegExp(`<test-run[^>]+\\b${name}="(\\d+)"`).exec(xmlStr);
  return m ? parseInt(m[1], 10) : 0;
}

const passed  = attr(xml, 'passed');
const failed  = attr(xml, 'failed');
const errors  = attr(xml, 'errors');
const skipped = attr(xml, 'skipped') + attr(xml, 'inconclusive');

// Collect failing test fullnames
const failures = [];
// Match <test-case ... result="Failed" ... (any order of attributes, possible multi-line)
const testCaseRe = /<test-case\s[^>]*?>/gs;
let m;
while ((m = testCaseRe.exec(xml)) !== null) {
  const tag = m[0];
  if (/\bresult="Failed"/i.test(tag) || /\bresult="Error"/i.test(tag)) {
    const fnMatch = /\bfullname="([^"]+)"/.exec(tag);
    if (fnMatch) {
      failures.push(fnMatch[1]);
    } else {
      // Fallback: name attribute
      const nMatch = /\bname="([^"]+)"/.exec(tag);
      if (nMatch) failures.push(nMatch[1]);
    }
  }
}

const result = { passed, failed, errors, skipped, failures };

if (format === 'text') {
  process.stdout.write(`Passed: ${passed}  Failed: ${failed}  Errors: ${errors}  Skipped: ${skipped}\n`);
  for (const f of failures) {
    process.stdout.write(`FAILED: ${f}\n`);
  }
} else {
  process.stdout.write(JSON.stringify(result, null, 2) + '\n');
}

if (failed + errors > 0) {
  process.exit(1);
}
process.exit(0);
