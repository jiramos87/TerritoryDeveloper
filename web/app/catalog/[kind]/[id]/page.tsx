"use client";

import { use, useState } from "react";
import { PreviewCompare } from "@/components/catalog/PreviewCompare";

type Ctx = { params: Promise<{ kind: string; id: string }> };

export default function CatalogEntityPage({ params }: Ctx) {
  const { kind, id } = use(params);
  const [activeTab, setActiveTab] = useState<"details" | "preview">("details");
  const [screenshotUrl, setScreenshotUrl] = useState("");
  const [previewLoading, setPreviewLoading] = useState(false);
  const [previewError, setPreviewError] = useState<string | undefined>(undefined);

  async function triggerPreview() {
    setPreviewLoading(true);
    setPreviewError(undefined);
    setScreenshotUrl("");
    try {
      const res = await fetch(`/api/catalog/${kind}/${id}/preview`, { method: "POST" });
      const data = (await res.json()) as { ok: boolean; screenshotUrl?: string; error?: string };
      if (data.ok && data.screenshotUrl) {
        setScreenshotUrl(data.screenshotUrl);
      } else {
        setPreviewError(data.error ?? "Preview failed");
      }
    } catch {
      setPreviewError("Network error requesting preview");
    } finally {
      setPreviewLoading(false);
    }
  }

  function handlePreviewTab() {
    setActiveTab("preview");
    if (!screenshotUrl && !previewLoading) {
      void triggerPreview();
    }
  }

  return (
    <div className="flex flex-col gap-4 p-6">
      <div className="flex flex-col gap-1">
        <span className="text-xs uppercase tracking-wide text-gray-500">{kind}</span>
        <h1 className="text-xl font-semibold">{id}</h1>
      </div>

      <div className="flex gap-2 border-b">
        <button
          type="button"
          onClick={() => setActiveTab("details")}
          className={`px-4 py-2 text-sm ${activeTab === "details" ? "border-b-2 border-blue-600 font-medium" : "text-gray-600"}`}
        >
          Details
        </button>
        <button
          type="button"
          onClick={handlePreviewTab}
          className={`px-4 py-2 text-sm ${activeTab === "preview" ? "border-b-2 border-blue-600 font-medium" : "text-gray-600"}`}
        >
          Preview
        </button>
      </div>

      {activeTab === "details" && (
        <div className="text-sm text-gray-600">Entity details for <code>{id}</code>.</div>
      )}

      {activeTab === "preview" && (
        <PreviewCompare
          draftUrl={screenshotUrl}
          publishedUrl=""
          title={id}
          loading={previewLoading}
          error={previewError}
        />
      )}
    </div>
  );
}
