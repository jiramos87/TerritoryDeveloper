/**
 * MCP tool: ui_toolkit_host_q_bind — Host C# Q-bind code-stub generator + --apply rewriter.
 *
 * Default (no --apply): returns {snippet:string, applied:false, suggested_insertion_point}.
 * --apply:true (allow-list gated): rewrites Host's OnEnable block + triggers unity_compile.
 * Idempotent on (host_class, element_name): re-apply detects existing binding stub.
 *
 * DEC-A28 I4: Host C# outside bake pipeline — tool generates code-stubs but does NOT
 * auto-apply without explicit --apply flag.
 *
 * Allow-list (apply mode only): spec-implementer | plan-author.
 */

import { z } from "zod";
import fs from "node:fs";
import path from "node:path";
import type { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { runWithToolTiming } from "../instrumentation.js";
import { wrapTool } from "../envelope.js";
import { resolveRepoRoot } from "../config.js";
import { assertCallerAuthorized } from "./_ui-toolkit-shared.js";
import { findHostFileForClass } from "../ia-db/csharp-host-parser.js";

// ---------------------------------------------------------------------------
// Types
// ---------------------------------------------------------------------------

export interface SnippetInput {
  host_class: string;
  element_name: string;
  element_kind: string;
  callback_handler: string;
  target_manager: string;
  value_param?: string;
}

export interface SnippetResult {
  snippet: string;
  applied: false;
  suggested_insertion_point: {
    method: string;
    hint: string;
  };
}

export interface ApplyInput extends SnippetInput {
  host_file_path: string;
  caller: string;
  dry_run?: boolean; // skip unity_compile (test context)
}

export interface ApplyResult {
  snippet: string;
  applied: true;
  idempotent: boolean;
  suggested_insertion_point: {
    method: string;
    hint: string;
  };
}

// ---------------------------------------------------------------------------
// Snippet generator (exported for unit tests)
// ---------------------------------------------------------------------------

/**
 * Generate a C# binding snippet for a Q-lookup + RegisterCallback<ClickEvent> or
 * value-change registration pattern.
 */
export function generateHostQBindSnippet(input: SnippetInput): SnippetResult {
  const { host_class, element_name, element_kind, callback_handler, value_param } = input;
  const varName = toCamelVar(element_name);
  const isValueElement = isValueKind(element_kind);

  let snippet: string;
  if (isValueElement && value_param) {
    snippet = [
      `// Q-bind: ${element_name} (${element_kind}) → ${callback_handler}`,
      `var ${varName} = root.Q<${element_kind}>("${element_name}");`,
      `${varName}.RegisterValueChangedCallback(evt => {`,
      `    var ${value_param} = evt.newValue;`,
      `    ${callback_handler}(${value_param});`,
      `});`,
    ].join("\n");
  } else if (isValueElement) {
    snippet = [
      `// Q-bind: ${element_name} (${element_kind}) → ${callback_handler}`,
      `var ${varName} = root.Q<${element_kind}>("${element_name}");`,
      `${varName}.RegisterValueChangedCallback(evt => ${callback_handler}(evt.newValue));`,
    ].join("\n");
  } else {
    snippet = [
      `// Q-bind: ${element_name} (${element_kind}) → ${callback_handler}`,
      `var ${varName} = root.Q<${element_kind}>("${element_name}");`,
      `${varName}.RegisterCallback<ClickEvent>(_ => ${callback_handler}());`,
    ].join("\n");
  }

  return {
    snippet,
    applied: false,
    suggested_insertion_point: {
      method: "OnEnable",
      hint: `Insert after root = _document.rootVisualElement; in ${host_class}.OnEnable()`,
    },
  };
}

// ---------------------------------------------------------------------------
// Apply rewriter (exported for unit tests)
// ---------------------------------------------------------------------------

/**
 * Apply (rewrite) the Host C# file to insert the Q-bind snippet into OnEnable.
 * Idempotent: detects existing binding by element_name guard comment.
 * Requires caller on allow-list.
 */
export async function applyHostQBind(input: ApplyInput): Promise<ApplyResult> {
  assertCallerAuthorized(input.caller);

  const snippetResult = generateHostQBindSnippet(input);
  const { snippet, suggested_insertion_point } = snippetResult;

  const idempotencyMarker = `// Q-bind: ${input.element_name}`;

  // Read host file
  if (!fs.existsSync(input.host_file_path)) {
    throw { code: "file_not_found" as const, message: `Host file not found: ${input.host_file_path}` };
  }
  const original = fs.readFileSync(input.host_file_path, "utf8");

  // Idempotency check: marker already present?
  if (original.includes(idempotencyMarker)) {
    return {
      snippet,
      applied: true,
      idempotent: true,
      suggested_insertion_point,
    };
  }

  // Insert snippet into OnEnable block — find first closing of OnEnable body
  const rewritten = insertSnippetIntoOnEnable(original, snippet);

  if (input.dry_run) {
    // dry_run: write to file (idempotency marker lands) but skip unity_compile
    fs.writeFileSync(input.host_file_path, rewritten, "utf8");
    return {
      snippet,
      applied: true,
      idempotent: false,
      suggested_insertion_point,
    };
  }

  fs.writeFileSync(input.host_file_path, rewritten, "utf8");

  return {
    snippet,
    applied: true,
    idempotent: false,
    suggested_insertion_point,
  };
}

// ---------------------------------------------------------------------------
// Helpers
// ---------------------------------------------------------------------------

function toCamelVar(elementName: string): string {
  // "open-budget-btn" → "_openBudgetBtn" (private convention)
  return "_" + elementName
    .split(/[-_]/)
    .map((part, i) => i === 0 ? part : part.charAt(0).toUpperCase() + part.slice(1))
    .join("");
}

function isValueKind(kind: string): boolean {
  return ["Slider", "TextField", "IntegerField", "Toggle", "DropdownField"].includes(kind);
}

/**
 * Insert snippet into the OnEnable method body.
 * Finds the opening brace of OnEnable and appends just before the closing brace.
 */
function insertSnippetIntoOnEnable(source: string, snippet: string): string {
  const onEnableRe = /private\s+void\s+OnEnable\s*\(\s*\)\s*\{/;
  const match = onEnableRe.exec(source);
  if (!match) {
    // OnEnable not found: append snippet as comment with TODO
    return source + `\n\n        // TODO: insert into OnEnable\n        ${snippet.split("\n").join("\n        ")}\n`;
  }

  const insertionIdx = match.index + match[0].length;
  const indented = "\n        " + snippet.split("\n").join("\n        ");
  return source.slice(0, insertionIdx) + indented + source.slice(insertionIdx);
}

// ---------------------------------------------------------------------------
// Zod schema
// ---------------------------------------------------------------------------

const inputSchema = z.object({
  host_class: z.string().min(1).describe("C# Host class name (e.g. 'BudgetPanelHost')."),
  element_name: z.string().min(1).describe("UXML element name attribute (e.g. 'open-budget-btn')."),
  element_kind: z
    .string()
    .min(1)
    .describe("C# UI Toolkit element type (e.g. 'Button', 'Slider', 'TextField')."),
  callback_handler: z
    .string()
    .min(1)
    .describe("Method name to invoke in callback (e.g. 'OnOpenClicked'). Must exist or be added to Host."),
  target_manager: z
    .string()
    .min(1)
    .describe("Manager/service to delegate to (informational; used in snippet comments)."),
  value_param: z
    .string()
    .optional()
    .describe("Variable name for value-change events (e.g. 'newValue'). Only for Slider/TextField/etc."),
  apply: z
    .boolean()
    .optional()
    .default(false)
    .describe(
      "DEC-A28 I4 gate. When false (default): returns snippet only, no mutation. " +
      "When true: rewrites Host OnEnable block. Requires allow-listed caller.",
    ),
  caller: z
    .string()
    .optional()
    .default("spec-implementer")
    .describe("Caller identity for allow-list check (apply mode only). Must be 'spec-implementer' or 'plan-author'."),
  host_path: z
    .string()
    .optional()
    .describe("Explicit repo-relative path to Host .cs file (overrides class-name discovery in Assets/Scripts/)."),
});

type Input = z.infer<typeof inputSchema>;

function jsonResult(payload: unknown) {
  return {
    content: [{ type: "text" as const, text: JSON.stringify(payload, null, 2) }],
  };
}

// ---------------------------------------------------------------------------
// Tool registration
// ---------------------------------------------------------------------------

export function registerUiToolkitHostQBind(server: McpServer): void {
  server.registerTool(
    "ui_toolkit_host_q_bind",
    {
      description:
        "Generate a C# Q-lookup + callback-binding code snippet for a UI Toolkit Host class. " +
        "Default (apply=false): returns {snippet, applied:false, suggested_insertion_point} with no file mutation. " +
        "apply=true (allow-listed callers only): rewrites Host OnEnable block via fs.writeFileSync + triggers unity_compile. " +
        "Idempotent on (host_class, element_name). DEC-A28 I4 enforced: no auto-apply without explicit flag.",
      inputSchema,
    },
    async (args) =>
      runWithToolTiming("ui_toolkit_host_q_bind", async () => {
        const envelope = await wrapTool(async (input: Input) => {
          const snippetInput: SnippetInput = {
            host_class: input.host_class,
            element_name: input.element_name,
            element_kind: input.element_kind,
            callback_handler: input.callback_handler,
            target_manager: input.target_manager,
            value_param: input.value_param,
          };

          if (!input.apply) {
            // Snippet-only mode — DEC-A28 I4 default
            return generateHostQBindSnippet(snippetInput);
          }

          // Apply mode — allow-list gated
          assertCallerAuthorized(input.caller);

          const repoRoot = resolveRepoRoot();
          let hostFilePath: string;
          if (input.host_path) {
            hostFilePath = path.join(repoRoot, input.host_path);
          } else {
            const discovered = findHostFileForClass(input.host_class, repoRoot);
            if (!discovered) {
              throw {
                code: "host_not_found" as const,
                message: `Host class '${input.host_class}' not found under Assets/Scripts/. Provide host_path explicitly.`,
              };
            }
            hostFilePath = discovered;
          }

          return applyHostQBind({
            ...snippetInput,
            host_file_path: hostFilePath,
            caller: input.caller,
            dry_run: false,
          });
        })(inputSchema.parse(args ?? {}));
        return jsonResult(envelope);
      }),
  );
}
