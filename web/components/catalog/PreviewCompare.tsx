"use client";

import { useState } from "react";

interface PreviewCompareProps {
  draftUrl: string;
  publishedUrl: string;
  title: string;
  loading?: boolean;
  error?: string;
}

export function PreviewCompare({
  draftUrl,
  publishedUrl,
  title,
  loading,
  error,
}: PreviewCompareProps) {
  const [diffMode, setDiffMode] = useState(false);

  if (error) {
    return (
      <div role="alert" className="rounded border border-red-500 bg-red-50 p-3 text-sm text-red-700">
        {error}
      </div>
    );
  }

  if (loading) {
    return (
      <div className="grid grid-cols-2 gap-4">
        <div className="animate-pulse h-48 rounded bg-gray-200" />
        <div className="animate-pulse h-48 rounded bg-gray-200" />
      </div>
    );
  }

  return (
    <div className="flex flex-col gap-3">
      <div className="grid grid-cols-2 gap-4">
        <div className="flex flex-col gap-1">
          <span className="text-xs font-medium text-gray-500">Draft — {title}</span>
          <img src={draftUrl} alt={`Draft preview of ${title}`} className="rounded border object-contain" />
        </div>
        <div className="flex flex-col gap-1">
          <span className="text-xs font-medium text-gray-500">Published</span>
          <img
            src={publishedUrl}
            alt={`Published version of ${title}`}
            className="rounded border object-contain"
            style={diffMode ? { mixBlendMode: "difference" } : undefined}
          />
        </div>
      </div>
      <button
        type="button"
        onClick={() => setDiffMode((v) => !v)}
        className="self-start rounded border px-3 py-1 text-xs"
      >
        {diffMode ? "Disable diff" : "Enable diff"}
      </button>
    </div>
  );
}
