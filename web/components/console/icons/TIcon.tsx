import type { SVGProps } from 'react';

/**
 * 13-glyph media transport / mixer icon set from CD `console-assets.jsx` (TIcon).
 * All vector paths use currentColor for ds theme wiring.
 */
export type TIconGlyphProps = SVGProps<SVGSVGElement> & {
  size?: number;
};

const BASE = (size: number) =>
  ({
    width: size,
    height: size,
    viewBox: '0 0 24 24',
    fill: 'currentColor' as const,
  }) as const;

function PlayIcon({ size = 24, className, 'aria-label': ariaLabel, ...rest }: TIconGlyphProps) {
  const b = BASE(size);
  return (
    <svg
      {...b}
      className={className}
      aria-label={ariaLabel}
      stroke="currentColor"
      strokeWidth={1.5}
      strokeLinejoin="round"
      {...rest}
    >
      <path d="M7 5 L19 12 L7 19 Z" />
    </svg>
  );
}

function PauseIcon({ size = 24, className, 'aria-label': ariaLabel, ...rest }: TIconGlyphProps) {
  const b = BASE(size);
  return (
    <svg
      {...b}
      className={className}
      aria-label={ariaLabel}
      stroke="currentColor"
      strokeWidth={1.5}
      strokeLinejoin="round"
      {...rest}
    >
      <rect x="6" y="5" width="4" height="14" rx="0.5" />
      <rect x="14" y="5" width="4" height="14" rx="0.5" />
    </svg>
  );
}

function StopIcon({ size = 24, className, 'aria-label': ariaLabel, ...rest }: TIconGlyphProps) {
  const b = BASE(size);
  return (
    <svg
      {...b}
      className={className}
      aria-label={ariaLabel}
      stroke="currentColor"
      strokeWidth={1.5}
      strokeLinejoin="round"
      {...rest}
    >
      <rect x="5" y="5" width="14" height="14" rx="0.5" />
    </svg>
  );
}

function RecordIcon({ size = 24, className, 'aria-label': ariaLabel, ...rest }: TIconGlyphProps) {
  const b = BASE(size);
  return (
    <svg
      {...b}
      className={className}
      aria-label={ariaLabel}
      stroke="currentColor"
      strokeWidth={1.5}
      {...rest}
    >
      <circle cx="12" cy="12" r="7" />
    </svg>
  );
}

function RewindEndIcon({ size = 24, className, 'aria-label': ariaLabel, ...rest }: TIconGlyphProps) {
  const b = BASE(size);
  return (
    <svg
      {...b}
      className={className}
      aria-label={ariaLabel}
      stroke="currentColor"
      strokeWidth={1.5}
      strokeLinejoin="round"
      {...rest}
    >
      <rect x="4" y="5" width="2" height="14" />
      <path d="M20 5 L8 12 L20 19 Z" />
    </svg>
  );
}

function FastForwardEndIcon({
  size = 24,
  className,
  'aria-label': ariaLabel,
  ...rest
}: TIconGlyphProps) {
  const b = BASE(size);
  return (
    <svg
      {...b}
      className={className}
      aria-label={ariaLabel}
      stroke="currentColor"
      strokeWidth={1.5}
      strokeLinejoin="round"
      {...rest}
    >
      <rect x="18" y="5" width="2" height="14" />
      <path d="M4 5 L16 12 L4 19 Z" />
    </svg>
  );
}

function RewindIcon({ size = 24, className, 'aria-label': ariaLabel, ...rest }: TIconGlyphProps) {
  const b = BASE(size);
  return (
    <svg
      {...b}
      className={className}
      aria-label={ariaLabel}
      stroke="currentColor"
      strokeWidth={1.5}
      strokeLinejoin="round"
      {...rest}
    >
      <path d="M13 5 L5 12 L13 19 Z" />
      <path d="M22 5 L14 12 L22 19 Z" />
    </svg>
  );
}

function FastForwardIcon({
  size = 24,
  className,
  'aria-label': ariaLabel,
  ...rest
}: TIconGlyphProps) {
  const b = BASE(size);
  return (
    <svg
      {...b}
      className={className}
      aria-label={ariaLabel}
      stroke="currentColor"
      strokeWidth={1.5}
      strokeLinejoin="round"
      {...rest}
    >
      <path d="M2 5 L10 12 L2 19 Z" />
      <path d="M11 5 L19 12 L11 19 Z" />
    </svg>
  );
}

function EjectIcon({ size = 24, className, 'aria-label': ariaLabel, ...rest }: TIconGlyphProps) {
  const b = BASE(size);
  return (
    <svg
      {...b}
      className={className}
      aria-label={ariaLabel}
      stroke="currentColor"
      strokeWidth={1.5}
      strokeLinejoin="round"
      {...rest}
    >
      <path d="M12 3 L21 15 L3 15 Z" />
      <rect x="3" y="17" width="18" height="4" />
    </svg>
  );
}

function LoopIcon({ size = 24, className, 'aria-label': ariaLabel, ...rest }: TIconGlyphProps) {
  const b = BASE(size);
  return (
    <svg
      {...b}
      className={className}
      aria-label={ariaLabel}
      fill="none"
      stroke="currentColor"
      strokeWidth={2}
      strokeLinecap="round"
      strokeLinejoin="round"
      {...rest}
    >
      <path d="M4 11 A8 8 0 0 1 20 11" />
      <path d="M20 7 L20 11 L16 11" />
      <path d="M20 13 A8 8 0 0 1 4 13" />
      <path d="M4 17 L4 13 L8 13" />
    </svg>
  );
}

function ShuffleIcon({ size = 24, className, 'aria-label': ariaLabel, ...rest }: TIconGlyphProps) {
  const b = BASE(size);
  return (
    <svg
      {...b}
      className={className}
      aria-label={ariaLabel}
      fill="none"
      stroke="currentColor"
      strokeWidth={2}
      strokeLinecap="round"
      strokeLinejoin="round"
      {...rest}
    >
      <path d="M3 6 h4 l10 12 h4" />
      <path d="M3 18 h4 l10 -12 h4" />
      <path d="M19 4 L22 6 L19 8" />
      <path d="M19 16 L22 18 L19 20" />
    </svg>
  );
}

function MuteIcon({ size = 24, className, 'aria-label': ariaLabel, ...rest }: TIconGlyphProps) {
  const b = BASE(size);
  return (
    <svg
      {...b}
      className={className}
      aria-label={ariaLabel}
      stroke="currentColor"
      strokeWidth={1.5}
      strokeLinejoin="round"
      {...rest}
    >
      <path d="M3 9 L8 9 L13 5 L13 19 L8 15 L3 15 Z" />
      <path d="M17 9 L22 14 M22 9 L17 14" stroke="currentColor" strokeWidth={2} fill="none" />
    </svg>
  );
}

function SoloIcon({ size = 24, className, 'aria-label': ariaLabel, ...rest }: TIconGlyphProps) {
  const b = BASE(size);
  return (
    <svg
      {...b}
      className={className}
      aria-label={ariaLabel}
      fill="currentColor"
      stroke="currentColor"
      strokeWidth={1.5}
      {...rest}
    >
      <circle cx="12" cy="12" r="8" fill="none" stroke="currentColor" />
      <text
        x="12"
        y="16"
        textAnchor="middle"
        fontSize="10"
        fontFamily="ui-monospace, Geist Mono, monospace"
        fontWeight="700"
        fill="currentColor"
        stroke="none"
      >
        S
      </text>
    </svg>
  );
}

/** Namespace export — CD `TIcon` 13-glyph set. */
export const TIcon = {
  Play: PlayIcon,
  Pause: PauseIcon,
  Stop: StopIcon,
  Record: RecordIcon,
  Rewind: RewindIcon,
  FastForward: FastForwardIcon,
  RewindEnd: RewindEndIcon,
  FastForwardEnd: FastForwardEndIcon,
  Eject: EjectIcon,
  Loop: LoopIcon,
  Shuffle: ShuffleIcon,
  Mute: MuteIcon,
  Solo: SoloIcon,
} as const;
