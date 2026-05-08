#!/usr/bin/env node
/**
 * validate-domain-facade-interfaces.mjs
 *
 * Asserts every Assets/Scripts/Domains/{X}/ folder carries I{X}.cs
 * declaring a public interface I{X}.
 *
 * Exit codes:
 *   0 = all domains have facade interface (or no Domains/ folder — initial run OK)
 *   1 = one or more domains missing I{X}.cs or interface declaration
 *
 * Flags:
 *   --domains-root <path>  override default scan root (for tests)
 */

import { readdirSync, readFileSync, existsSync, statSync } from "node:fs";
import { resolve, dirname, join, basename } from "node:path";
import { fileURLToPath } from "node:url";

const __filename = fileURLToPath(import.meta.url);
const __dirname = dirname(__filename);
const REPO_ROOT = resolve(__dirname, "../..");

const DOMAINS_ARG_IDX = process.argv.indexOf("--domains-root");
const DOMAINS_ROOT = DOMAINS_ARG_IDX !== -1
  ? resolve(process.argv[DOMAINS_ARG_IDX + 1])
  : resolve(REPO_ROOT, "Assets/Scripts/Domains");

// ---------------------------------------------------------------------------
// Scan
// ---------------------------------------------------------------------------

function listDomainFolders(domainsRoot) {
  if (!existsSync(domainsRoot)) return [];
  return readdirSync(domainsRoot)
    .filter((name) => {
      const full = join(domainsRoot, name);
      return statSync(full).isDirectory();
    });
}

function checkDomain(domainsRoot, domainName) {
  const facadeFile = join(domainsRoot, domainName, `I${domainName}.cs`);
  if (!existsSync(facadeFile)) {
    return { ok: false, reason: `I${domainName}.cs absent` };
  }

  const src = readFileSync(facadeFile, "utf8");
  const interfaceRe = new RegExp(`public\\s+interface\\s+I${domainName}\\b`);
  if (!interfaceRe.test(src)) {
    return { ok: false, reason: `I${domainName}.cs exists but does not declare 'public interface I${domainName}'` };
  }

  // Must have at least one method/property signature (non-empty public surface)
  // Minimal heuristic: any line containing ';' inside the interface block
  const braceIdx = src.indexOf("{", src.search(interfaceRe));
  if (braceIdx === -1) {
    return { ok: false, reason: `I${domainName}: interface body not found` };
  }
  const body = src.slice(braceIdx);
  const memberRe = /[a-zA-Z\[\]<>?*]+\s+\w+\s*(\(|{|;)/;
  if (!memberRe.test(body)) {
    return { ok: false, reason: `I${domainName}: interface appears empty (no visible member)` };
  }

  return { ok: true, reason: "" };
}

// ---------------------------------------------------------------------------
// Main
// ---------------------------------------------------------------------------

function main() {
  const domains = listDomainFolders(DOMAINS_ROOT);

  if (domains.length === 0) {
    console.log(`validate-domain-facades: Domains/ folder absent or empty — pass (initial run).`);
    process.exit(0);
  }

  let errors = 0;
  for (const domain of domains) {
    const result = checkDomain(DOMAINS_ROOT, domain);
    if (result.ok) {
      console.log(`[OK ] ${domain}: I${domain}.cs present + interface declared`);
    } else {
      console.error(`[ERR] ${domain}: ${result.reason}`);
      errors++;
    }
  }

  console.log(`\nvalidate-domain-facades: ${domains.length} domain(s) checked. Errors=${errors}`);
  if (errors > 0) process.exit(1);
}

main();
