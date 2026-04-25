/**
 * Subset markdown renderer for dashboard surfaces (preamble / objective / task body).
 *
 * Server-safe (zero client JS, zero runtime deps). Intentionally bounded to the
 * shapes the IA emits: H1–H3, paragraphs, ul / ol, blockquotes, hr, fenced code,
 * inline `**bold**`, `*italic*`, `` `code` `` (→ glossary tooltip when match), and
 * `[text](url)` links.
 *
 * Anything outside the subset falls through as plain paragraph text — the IA never
 * embeds raw HTML, tables, or images in these surfaces today.
 */
import type { ReactNode } from "react";
import type { GlossaryIndex } from "@/lib/glossary/index-build";
import { Tooltip } from "@/components/Tooltip";

export interface MarkdownProps {
  source: string;
  glossary?: GlossaryIndex;
  /** Optional className for outer wrapper. */
  className?: string;
}

// ---------- inline ----------

const INLINE_RE =
  /(\*\*[^*]+\*\*|\*[^*\n]+\*|`[^`\n]+`|\[[^\]]+\]\([^)]+\))/g;

function renderInline(
  text: string,
  glossary: GlossaryIndex | undefined,
  keyBase: string,
): ReactNode[] {
  const out: ReactNode[] = [];
  let lastIdx = 0;
  let match: RegExpExecArray | null;
  let n = 0;

  INLINE_RE.lastIndex = 0;
  while ((match = INLINE_RE.exec(text)) !== null) {
    if (match.index > lastIdx) {
      out.push(text.slice(lastIdx, match.index));
    }
    const tok = match[0];
    const k = `${keyBase}-i${n++}`;

    if (tok.startsWith("**") && tok.endsWith("**")) {
      out.push(<strong key={k}>{tok.slice(2, -2)}</strong>);
    } else if (tok.startsWith("`") && tok.endsWith("`")) {
      const inner = tok.slice(1, -1);
      const term = glossary?.lookup(inner) ?? null;
      if (term) {
        out.push(
          <Tooltip key={k} content={term.definition} label={term.term}>
            <strong className="font-semibold">{inner}</strong>
          </Tooltip>,
        );
      } else {
        out.push(
          <strong key={k} className="font-semibold">
            {inner}
          </strong>,
        );
      }
    } else if (tok.startsWith("[")) {
      const m = /^\[([^\]]+)\]\(([^)]+)\)$/.exec(tok);
      if (m) {
        out.push(
          <a
            key={k}
            href={m[2]}
            className="text-[var(--ds-raw-blue)] hover:underline"
          >
            {m[1]}
          </a>,
        );
      } else {
        out.push(tok);
      }
    } else if (tok.startsWith("*") && tok.endsWith("*")) {
      out.push(<em key={k}>{tok.slice(1, -1)}</em>);
    } else {
      out.push(tok);
    }
    lastIdx = match.index + tok.length;
  }
  if (lastIdx < text.length) out.push(text.slice(lastIdx));
  return out;
}

// ---------- block ----------

interface ListBuf {
  ordered: boolean;
  items: string[];
}

export function Markdown({ source, glossary, className }: MarkdownProps) {
  if (!source || !source.trim()) return null;

  const lines = source.replace(/\r\n/g, "\n").split("\n");
  const blocks: ReactNode[] = [];
  let i = 0;
  let listBuf: ListBuf | null = null;
  let para: string[] = [];
  let blockquote: string[] = [];

  const flushPara = () => {
    if (para.length === 0) return;
    const text = para.join(" ").trim();
    if (text) {
      blocks.push(
        <p
          key={`p-${blocks.length}`}
          className="text-sm leading-relaxed text-[var(--ds-text-primary)]"
        >
          {renderInline(text, glossary, `p-${blocks.length}`)}
        </p>,
      );
    }
    para = [];
  };

  const flushList = () => {
    if (!listBuf) return;
    const Tag = listBuf.ordered ? "ol" : "ul";
    const items = listBuf.items;
    blocks.push(
      <Tag
        key={`l-${blocks.length}`}
        className={[
          listBuf.ordered ? "list-decimal" : "list-disc",
          "ml-5 space-y-1 text-sm leading-relaxed text-[var(--ds-text-primary)]",
        ].join(" ")}
      >
        {items.map((it, idx) => (
          <li key={idx}>{renderInline(it, glossary, `l-${blocks.length}-${idx}`)}</li>
        ))}
      </Tag>,
    );
    listBuf = null;
  };

  const flushQuote = () => {
    if (blockquote.length === 0) return;
    const text = blockquote.join(" ").trim();
    if (text) {
      blocks.push(
        <blockquote
          key={`bq-${blocks.length}`}
          className="border-l-2 border-[var(--ds-border-subtle)] pl-3 text-sm italic text-[var(--ds-text-muted)]"
        >
          {renderInline(text, glossary, `bq-${blocks.length}`)}
        </blockquote>,
      );
    }
    blockquote = [];
  };

  const flushAll = () => {
    flushPara();
    flushList();
    flushQuote();
  };

  while (i < lines.length) {
    const line = lines[i];
    const trimmed = line.trim();

    if (trimmed === "") {
      flushAll();
      i++;
      continue;
    }

    // fenced code block
    if (trimmed.startsWith("```")) {
      flushAll();
      i++;
      const code: string[] = [];
      while (i < lines.length && !lines[i].trim().startsWith("```")) {
        code.push(lines[i]);
        i++;
      }
      i++; // skip closing ```
      blocks.push(
        <pre
          key={`code-${blocks.length}`}
          className="overflow-x-auto rounded-md border border-[var(--ds-border-subtle)] bg-[var(--ds-bg-panel)] p-3 font-mono text-xs leading-snug text-[var(--ds-text-primary)]"
        >
          <code>{code.join("\n")}</code>
        </pre>,
      );
      continue;
    }

    // hr
    if (/^---+$/.test(trimmed) || /^\*\*\*+$/.test(trimmed)) {
      flushAll();
      blocks.push(
        <hr
          key={`hr-${blocks.length}`}
          className="my-3 border-t border-[var(--ds-border-subtle)]"
        />,
      );
      i++;
      continue;
    }

    // headings
    const h = /^(#{1,6})\s+(.*)$/.exec(trimmed);
    if (h) {
      flushAll();
      const level = Math.min(h[1].length, 3);
      const txt = h[2];
      const sizes = ["text-lg font-semibold", "text-base font-semibold", "text-sm font-semibold uppercase tracking-wide"];
      const cls = sizes[level - 1];
      const Tag = (`h${level + 2}` as unknown) as "h3" | "h4" | "h5"; // shift down so dashboard h2 still wins
      blocks.push(
        <Tag key={`h-${blocks.length}`} className={`${cls} text-[var(--ds-text-primary)]`}>
          {renderInline(txt, glossary, `h-${blocks.length}`)}
        </Tag>,
      );
      i++;
      continue;
    }

    // blockquote
    if (trimmed.startsWith(">")) {
      flushPara();
      flushList();
      blockquote.push(trimmed.replace(/^>\s?/, ""));
      i++;
      continue;
    }

    // ordered list
    const ol = /^(\d+)\.\s+(.*)$/.exec(trimmed);
    if (ol) {
      flushPara();
      flushQuote();
      if (!listBuf || !listBuf.ordered) {
        flushList();
        listBuf = { ordered: true, items: [] };
      }
      listBuf.items.push(ol[2]);
      i++;
      continue;
    }

    // unordered list
    const ul = /^[-*]\s+(.*)$/.exec(trimmed);
    if (ul) {
      flushPara();
      flushQuote();
      if (!listBuf || listBuf.ordered) {
        flushList();
        listBuf = { ordered: false, items: [] };
      }
      listBuf.items.push(ul[1]);
      i++;
      continue;
    }

    // paragraph continuation
    flushList();
    flushQuote();
    para.push(trimmed);
    i++;
  }

  flushAll();

  if (blocks.length === 0) return null;

  return (
    <div className={["space-y-2", className ?? ""].join(" ").trim()}>
      {blocks}
    </div>
  );
}
