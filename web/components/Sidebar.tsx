'use client';

import { useState } from 'react';
import Link from 'next/link';
import { usePathname } from 'next/navigation';
import { Home, BookOpen, Newspaper, LayoutDashboard, Layers3, Menu, X } from 'lucide-react';

const LINKS = [
  { href: '/', label: 'Home', Icon: Home },
  { href: '/wiki', label: 'Wiki', Icon: BookOpen },
  { href: '/devlog', label: 'Devlog', Icon: Newspaper },
  { href: '/dashboard', label: 'Dashboard', Icon: LayoutDashboard },
  { href: '/dashboard/releases', label: 'Releases', Icon: Layers3 },
];

export default function Sidebar() {
  const pathname = usePathname();
  const [open, setOpen] = useState(false);

  return (
    <>
      <button
        aria-label="Toggle navigation"
        className="md:hidden fixed top-4 left-4 z-50 text-[var(--ds-text-primary)]"
        onClick={() => setOpen((v) => !v)}
      >
        {open ? <X size={24} /> : <Menu size={24} />}
      </button>
      <nav
        className={`flex flex-col gap-2 p-4 fixed inset-y-0 left-0 w-48 z-40 transform transition-transform bg-[var(--ds-bg-canvas)] text-[var(--ds-text-primary)] md:static md:translate-x-0 ${
          open ? 'translate-x-0' : '-translate-x-full'
        }`}
      >
        {LINKS.map(({ href, label, Icon }) => {
          const active = pathname === href;
          return (
            <Link
              key={href}
              href={href}
              className={
                active
                  ? 'flex items-center gap-2 rounded px-2 py-1 text-[var(--ds-text-accent-warn)] bg-[var(--ds-bg-panel)]'
                  : 'flex items-center gap-2 rounded px-2 py-1 text-[var(--ds-text-muted)]'
              }
              onClick={() => setOpen(false)}
            >
              <Icon size={24} />
              <span>{label}</span>
            </Link>
          );
        })}
      </nav>
    </>
  );
}
