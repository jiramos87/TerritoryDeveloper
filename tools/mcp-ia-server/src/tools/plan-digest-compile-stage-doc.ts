/**
 * MCP tool: plan_digest_compile_stage_doc — stitch per-Task §Plan Digest slices into a single stage doc.
 * Input: { master_plan_path: string, stage_id: string, task_spec_paths: string[], mode?: "stage"|"audit" }.
 * Output: { compiled_path: string, bytes: number, task_count: number } — writes docs/implementation/<slug>-stage-<id>-plan.md.
 * Does NOT author §Plan Digest slices — only concatenates existing slices + header + final gate.
 * Mode `audit` is a flag-scaffold — returns { error: "mode_audit_not_ready" } unless env PLAN_DIGEST_AUDIT_MODE=1.
 */

import { z } from "zod";
import fs from "node:fs";
import path from "node:path";
import type { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { resolveRepoRoot } from "../config.js";
import { runWithToolTiming } from "../instrumentation.js";
import { wrapTool } from "../envelope.js";

const inputShape = {
  master_plan_path: z.string().min(1).describe("Repo-relative path to the master plan markdown."),
  stage_id: z.string().min(1).describe("Stage id, e.g. 7.1"),
  task_spec_paths: z.array(z.string().min(1)).min(1).describe("Repo-relative project spec paths in order."),
  mode: z.enum(["stage", "audit"]).optional().default("stage"),
};

function isWithinRepoRoot(abs: string, root: string): boolean {
  const a = path.resolve(abs);
  const r = path.resolve(root);
  return a === r || a.startsWith(r + path.sep);
}

function jsonResult(payload: unknown) {
  return {
    content: [
      {
        type: "text" as const,
        text: JSON.stringify(payload, null, 2),
      },
    ],
  };
}

function extractDigestSlice(body: string): string | null {
  let nl = body.indexOf("\n## §Plan Digest");
  let after: string;
  if (nl >= 0) {
    after = body.slice(nl + 1);
  } else if (body.startsWith("## §Plan Digest")) {
    after = body;
  } else {
    return null;
  }
  const end = after.search(/\n## (?!§Plan Digest)/);
  return end < 0 ? after : after.slice(0, end);
}

export function registerPlanDigestCompileStageDoc(server: McpServer): void {
  server.registerTool(
    "plan_digest_compile_stage_doc",
    {
      description:
        "Stitch per-Task §Plan Digest slices into docs/implementation/<slug>-stage-<id>-plan.md.",
      inputSchema: inputShape,
    },
    async (args) =>
      runWithToolTiming("plan_digest_compile_stage_doc", async () => {
        const envelope = await wrapTool(
          async (input: {
            master_plan_path: string;
            stage_id: string;
            task_spec_paths: string[];
            mode: "stage" | "audit";
          }) => {
            if (input.mode === "audit" && process.env.PLAN_DIGEST_AUDIT_MODE !== "1") {
              return { error: "mode_audit_not_ready" as const };
            }
            const root = resolveRepoRoot();
            const slug = path
              .basename(input.master_plan_path)
              .replace(/-master-plan\.md$/, "")
              .replace(/\.md$/, "");
            const outRel = `docs/implementation/${slug}-stage-${input.stage_id}-plan.md`;
            const outAbs = path.resolve(root, outRel);
            fs.mkdirSync(path.dirname(outAbs), { recursive: true });
            const slices: string[] = [];
            for (const p of input.task_spec_paths) {
              const abs = path.resolve(root, p);
              if (!isWithinRepoRoot(abs, root) || !fs.existsSync(abs)) {
                throw { code: "invalid_input" as const, message: `missing_spec:${p}` };
              }
              const slice = extractDigestSlice(fs.readFileSync(abs, "utf8"));
              if (!slice) {
                throw { code: "invalid_input" as const, message: `no_plan_digest_in:${p}` };
              }
              slices.push(slice);
            }
            const header = `# ${slug} — Stage ${input.stage_id} Plan Digest\n\nCompiled ${new Date().toISOString().slice(0, 10)} from ${slices.length} task spec(s).\n\n---\n`;
            const final = `${header}\n${slices.join("\n---\n")}\n\n## Final gate\n\n\`\`\`bash\nnpm run validate:all\n\`\`\`\n`;
            fs.writeFileSync(outAbs, final, "utf8");
            return { compiled_path: outRel, bytes: final.length, task_count: slices.length };
          },
        )({
          master_plan_path: (args as { master_plan_path?: string }).master_plan_path ?? "",
          stage_id: (args as { stage_id?: string }).stage_id ?? "",
          task_spec_paths: (args as { task_spec_paths?: string[] }).task_spec_paths ?? [],
          mode: (args as { mode?: "stage" | "audit" }).mode ?? "stage",
        });
        return jsonResult(envelope);
      }),
  );
}
