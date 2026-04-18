'use client';

import { useState } from 'react';
import Link from 'next/link';
import { usePathname } from 'next/navigation';
import { Home, BookOpen, Newspaper, LayoutDashboard, Layers3, Menu, X } from 'lucide-react';
import { tokens } from '@/lib/tokens';

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
        className="md:hidden fixed top-4 left-4 z-50"
        style={{ color: tokens.colors['text-primary'] }}
        onClick={() => setOpen((v) => !v)}
      >
        {open ? <X size={24} /> : <Menu size={24} />}
      </button>
      <nav
        className={`flex flex-col gap-2 p-4 fixed inset-y-0 left-0 w-48 z-40 transform transition-transform md:static md:translate-x-0 ${
          open ? 'translate-x-0' : '-translate-x-full'
        }`}
        style={{ backgroundColor: tokens.colors['bg-canvas'], color: tokens.colors['text-primary'] }}
      >
        {LINKS.map(({ href, label, Icon }) => {
          const active = pathname === href;
          return (
            <Link
              key={href}
              href={href}
              className="flex items-center gap-2 rounded px-2 py-1"
              style={
                active
                  ? { color: tokens.colors['text-accent-warn'], backgroundColor: tokens.colors['bg-panel'] }
                  : { color: tokens.colors['text-muted'] }
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
