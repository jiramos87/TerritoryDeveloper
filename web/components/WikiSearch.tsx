'use client';

import Fuse from 'fuse.js';
import Link from 'next/link';
import { useEffect, useMemo, useRef, useState } from 'react';
import type { SearchRecord } from '@/lib/search/types';
import { tokens } from '@/lib/tokens';

const MAX_RESULTS = 10;

export function WikiSearch() {
  const [records, setRecords] = useState<SearchRecord[]>([]);
  const [query, setQuery] = useState('');
  const abortRef = useRef<AbortController | null>(null);

  useEffect(() => {
    const controller = new AbortController();
    abortRef.current = controller;

    fetch('/search-index.json', { signal: controller.signal })
      .then((res) => res.json())
      .then((data: SearchRecord[]) => {
        if (!controller.signal.aborted) setRecords(data);
      })
      .catch(() => {
        // aborted or network error — silently ignore
      });

    return () => {
      controller.abort();
    };
  }, []);

  const fuse = useMemo(
    () =>
      new Fuse(records, {
        keys: ['title', 'body', 'category'],
        threshold: 0.35,
        includeScore: false,
      }),
    [records]
  );

  const results = useMemo(() => {
    if (!query.trim()) return [];
    return fuse.search(query).slice(0, MAX_RESULTS);
  }, [fuse, query]);

  return (
    <div
      style={{
        marginTop: tokens.spacing[4],
        marginBottom: tokens.spacing[2],
      }}
    >
      <input
        type="search"
        placeholder="Search wiki..."
        value={query}
        onChange={(e) => setQuery(e.target.value)}
        style={{
          width: '100%',
          boxSizing: 'border-box',
          padding: `${tokens.spacing[2]} ${tokens.spacing[3]}`,
          fontSize: tokens.fontSize.base[0],
          lineHeight: tokens.fontSize.base[1],
          fontFamily: tokens.fontFamily.sans.join(', '),
          color: tokens.colors['text-primary'],
          backgroundColor: tokens.colors['bg-panel'],
          border: `1px solid ${tokens.colors['text-muted']}`,
          borderRadius: '4px',
          outline: 'none',
        }}
      />

      {results.length > 0 && (
        <ul
          style={{
            listStyle: 'none',
            margin: `${tokens.spacing[1]} 0 0 0`,
            padding: 0,
            border: `1px solid ${tokens.colors['text-muted']}`,
            borderRadius: '4px',
            backgroundColor: tokens.colors['bg-panel'],
            overflow: 'hidden',
          }}
        >
          {results.map(({ item }) => (
            <li key={item.slug}>
              <Link
                href={`/wiki/${item.slug}`}
                style={{
                  display: 'flex',
                  alignItems: 'center',
                  gap: tokens.spacing[2],
                  padding: `${tokens.spacing[2]} ${tokens.spacing[3]}`,
                  textDecoration: 'none',
                  color: tokens.colors['text-primary'],
                  fontSize: tokens.fontSize.base[0],
                  lineHeight: tokens.fontSize.base[1],
                  fontFamily: tokens.fontFamily.sans.join(', '),
                  borderTop: `1px solid ${tokens.colors['bg-canvas']}`,
                }}
              >
                <span style={{ flex: 1 }}>{item.title}</span>
                <span
                  style={{
                    display: 'inline-block',
                    fontSize: tokens.fontSize.xs[0],
                    fontFamily: tokens.fontFamily.mono.join(', '),
                    color: tokens.colors['text-muted'],
                    backgroundColor: tokens.colors['bg-canvas'],
                    borderRadius: '9999px',
                    padding: `2px ${tokens.spacing[2]}`,
                    whiteSpace: 'nowrap',
                  }}
                >
                  {item.category || item.type}
                </span>
              </Link>
            </li>
          ))}
        </ul>
      )}

      {query.trim() && results.length === 0 && records.length > 0 && (
        <p
          style={{
            marginTop: tokens.spacing[2],
            fontSize: tokens.fontSize.sm[0],
            color: tokens.colors['text-muted'],
            fontFamily: tokens.fontFamily.sans.join(', '),
          }}
        >
          No results for &ldquo;{query}&rdquo;.
        </p>
      )}
    </div>
  );
}
