import { ImageResponse } from 'next/og';
import { siteTitle, siteTagline } from '@/lib/site/metadata';
import { tokens } from '@/lib/tokens';

export const alt = siteTitle;
export const size = { width: 1200, height: 630 };
export const contentType = 'image/png';

export default async function OgImage() {
  const bg = tokens.colors['bg-canvas'];
  const fg = tokens.colors['text-primary'];
  const muted = tokens.colors['text-muted'];
  // Access raw green accent from the resolved token map (bg-status-done maps to raw.green)
  const accent = tokens.colors['bg-status-done'];

  return new ImageResponse(
    (
      <div
        style={{
          width: '1200px',
          height: '630px',
          backgroundColor: bg,
          display: 'flex',
          flexDirection: 'column',
          justifyContent: 'center',
          padding: '80px',
          position: 'relative',
        }}
      >
        {/* Accent rule */}
        <div
          style={{
            position: 'absolute',
            top: 0,
            left: 0,
            right: 0,
            height: '4px',
            backgroundColor: accent,
          }}
        />
        {/* Title */}
        <div
          style={{
            fontSize: '72px',
            fontFamily: 'monospace',
            color: fg,
            lineHeight: 1.1,
            marginBottom: '24px',
          }}
        >
          {siteTitle}
        </div>
        {/* Tagline */}
        <div
          style={{
            fontSize: '32px',
            fontFamily: 'sans-serif',
            color: muted,
            lineHeight: 1.4,
          }}
        >
          {siteTagline}
        </div>
      </div>
    ),
    { ...size }
  );
}
