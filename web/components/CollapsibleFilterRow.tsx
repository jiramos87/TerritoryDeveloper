'use client'

import { useState } from 'react'

type Props = {
  label: string
  activeCount?: number
  children: React.ReactNode
}

export function CollapsibleFilterRow({ label, activeCount = 0, children }: Props) {
  const [open, setOpen] = useState(false)

  return (
    <div className="space-y-1">
      <button
        onClick={() => setOpen((v) => !v)}
        className="flex items-center gap-2 text-xs font-mono text-text-muted hover:text-text-primary transition-colors"
      >
        <span className="w-14 shrink-0 text-left">{label}</span>
        {activeCount > 0 && !open && (
          <span className="bg-panel text-primary rounded px-1.5 py-0.5 text-xs">
            {activeCount} active
          </span>
        )}
        <svg
          className={`w-3 h-3 transition-transform ${open ? 'rotate-180' : ''}`}
          fill="none"
          viewBox="0 0 24 24"
          stroke="currentColor"
          strokeWidth={2.5}
        >
          <path strokeLinecap="round" strokeLinejoin="round" d="M19 9l-7 7-7-7" />
        </svg>
      </button>
      {open && <div className="pl-16">{children}</div>}
    </div>
  )
}
