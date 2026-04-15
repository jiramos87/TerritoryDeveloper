import type { Metadata } from "next";
import { Geist, Geist_Mono } from "next/font/google";
import Link from "next/link";
import "./globals.css";
import { getBaseUrl } from "@/lib/site/base-url";
import { siteTitle, siteTagline } from "@/lib/site/metadata";
import { tokens } from "@/lib/tokens";

const geistSans = Geist({
  variable: "--font-geist-sans",
  subsets: ["latin"],
});

const geistMono = Geist_Mono({
  variable: "--font-geist-mono",
  subsets: ["latin"],
});

export const metadata: Metadata = {
  metadataBase: new URL(getBaseUrl()),
  title: {
    default: siteTitle,
    template: `%s · ${siteTitle}`,
  },
  description: siteTagline,
  openGraph: {
    siteName: siteTitle,
    type: "website",
    locale: "en_US",
  },
};

export default function RootLayout({
  children,
}: Readonly<{
  children: React.ReactNode;
}>) {
  return (
    <html
      lang="en"
      className={`${geistSans.variable} ${geistMono.variable} h-full antialiased`}
    >
      <body className="min-h-full flex flex-col">
        {children}
        <footer
          style={{
            borderTop: `1px solid ${tokens.colors['bg-panel']}`,
            padding: `${tokens.spacing[4]} ${tokens.spacing[4]}`,
            display: "flex",
            gap: tokens.spacing[4],
            justifyContent: "center",
            fontSize: tokens.fontSize.sm[0],
            lineHeight: tokens.fontSize.sm[1],
            fontFamily: tokens.fontFamily.mono.join(", "),
            color: tokens.colors['text-muted'],
          }}
        >
          <Link href="/devlog" style={{ color: tokens.colors['text-muted'], textDecoration: "underline" }}>
            Devlog
          </Link>
          <Link href="/feed.xml" style={{ color: tokens.colors['text-muted'], textDecoration: "underline" }}>
            RSS
          </Link>
        </footer>
      </body>
    </html>
  );
}
