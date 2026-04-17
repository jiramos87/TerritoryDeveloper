// stub login page — redirect target for middleware (TECH-265)
// user-facing copy in full English per caveman-exception (web/app/**/page.tsx boundary)
export default function LoginPage() {
  return (
    <main className="mx-auto max-w-md px-4 py-12">
      <section className="space-y-6 rounded border border-text-muted/20 bg-bg-panel p-6">
        <h1 className="text-2xl font-semibold text-text-primary">Sign in</h1>
        <p className="rounded border border-text-muted/20 bg-bg-canvas px-4 py-3 text-sm text-text-accent-warn">
          Authentication not yet available — coming soon.
        </p>
        <form className="space-y-3">
          <input
            type="email"
            placeholder="Email"
            disabled
            className="w-full rounded border border-text-muted/20 bg-bg-canvas px-3 py-2 text-text-primary"
          />
          <input
            type="password"
            placeholder="Password"
            disabled
            className="w-full rounded border border-text-muted/20 bg-bg-canvas px-3 py-2 text-text-primary"
          />
          <button
            type="submit"
            disabled
            className="w-full rounded bg-bg-status-progress px-3 py-2 text-sm font-medium text-text-primary opacity-50"
          >
            Sign in
          </button>
        </form>
      </section>
    </main>
  );
}
