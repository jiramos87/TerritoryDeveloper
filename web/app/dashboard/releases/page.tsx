import Link from 'next/link'
import { Breadcrumb } from '@/components/Breadcrumb'
import { releases } from '@/lib/releases'

/**
 * ReleasesPage — RSC release picker.
 * Lists all known releases; each row links to the per-release progress page.
 * No "use client" — pure server component.
 */
export default async function ReleasesPage() {
  return (
    <main className="mx-auto max-w-5xl px-4 py-8 space-y-6">
      <Breadcrumb
        crumbs={[
          { label: 'Home', href: '/' },
          { label: 'Dashboard', href: '/dashboard' },
          { label: 'Releases' },
        ]}
      />
      <h1 className="text-2xl font-semibold text-text-primary">Releases</h1>
      <ul className="space-y-3">
        {releases.map((r) => (
          <li key={r.id} className="border border-border-subtle rounded-lg px-4 py-3">
            <Link
              href={`/dashboard/releases/${r.id}/progress`}
              className="text-text-primary hover:text-text-muted transition-colors underline-offset-2 hover:underline font-medium"
            >
              {r.label}
            </Link>
          </li>
        ))}
      </ul>
    </main>
  )
}
