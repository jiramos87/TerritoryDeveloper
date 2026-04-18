// =============================================================
// Territory Developer — Console Asset Library
// Logo suite (8d), media transport icons (8b), hero art (8a),
// feature pillars (8c). All vector, all palette-locked.
// =============================================================

// -------- LOGO SUITE (8d) --------
// Concept: "TD" lettermark inside a transmitter/signal disc, with
// concentric rings echoing a radar PPI scope — the Territory HUD
// motif reduced to pure geometry. Amber glyph on black,
// monogram reversible to black-on-amber for light contexts.

const Logomark = ({ size = 32, variant = "amber-on-black", glow = true }) => {
  const bg = variant.endsWith("on-black") ? "#0a0a0a" : "#e8a33d";
  const fg = variant.startsWith("amber") ? "#e8a33d" : (variant.startsWith("black") ? "#0a0a0a" : "#e8e8e8");
  const uid = React.useId().replace(/:/g,"");
  return (
    <svg width={size} height={size} viewBox="0 0 64 64" aria-label="Territory Developer" role="img">
      <defs>
        <radialGradient id={`lg-${uid}`} cx="50%" cy="50%" r="50%">
          <stop offset="0%" stopColor={bg} stopOpacity="1"/>
          <stop offset="100%" stopColor="#000" stopOpacity="1"/>
        </radialGradient>
        {glow && (
          <filter id={`gl-${uid}`} x="-20%" y="-20%" width="140%" height="140%">
            <feGaussianBlur stdDeviation="0.8" />
          </filter>
        )}
      </defs>
      <rect width="64" height="64" rx="8" fill={`url(#lg-${uid})`}/>
      {/* concentric PPI rings */}
      <g stroke={fg} strokeWidth="0.6" fill="none" opacity="0.4">
        <circle cx="32" cy="32" r="26"/>
        <circle cx="32" cy="32" r="19"/>
        <circle cx="32" cy="32" r="12"/>
        <line x1="6" y1="32" x2="58" y2="32"/>
        <line x1="32" y1="6" x2="32" y2="58"/>
      </g>
      {/* sweep */}
      <path d="M32 32 L54 22 A24 24 0 0 1 54 42 Z" fill={fg} opacity="0.12"/>
      {/* TD lettermark — chiseled geometric */}
      <g fill={fg} filter={glow ? `url(#gl-${uid})` : undefined}>
        <path d="M14 22 h16 v4 h-6 v16 h-4 v-16 h-6z"/>
        <path d="M33 22 h8 a9 10 0 0 1 9 10 a9 10 0 0 1 -9 10 h-8 z M37 26 v12 h4 a5 6 0 0 0 5 -6 a5 6 0 0 0 -5 -6 z"/>
      </g>
      {/* signal pip */}
      <circle cx="50" cy="14" r="2" fill={fg}/>
      <circle cx="50" cy="14" r="3.5" fill="none" stroke={fg} strokeWidth="0.5" opacity="0.5"/>
    </svg>
  );
};

const Wordmark = ({ height = 28, variant = "amber-on-black" }) => {
  const fg = variant.startsWith("amber") ? "#e8a33d" : (variant.startsWith("black") ? "#0a0a0a" : "#e8e8e8");
  const muted = variant.startsWith("amber") ? "#e8e8e8" : "#6a6a6a";
  return (
    <svg height={height} viewBox="0 0 340 64" aria-label="Territory Developer" role="img">
      <Logomark size={64} variant={variant} glow={false}/>
      <g transform="translate(76, 14)">
        <text x="0" y="22" fontFamily="Geist, system-ui, sans-serif" fontSize="22" fontWeight="700" letterSpacing="0.02em" fill={fg}>TERRITORY</text>
        <text x="0" y="42" fontFamily="Geist Mono, ui-monospace, monospace" fontSize="11" letterSpacing="0.3em" fill={muted}>DEVELOPER</text>
      </g>
    </svg>
  );
};

const Lettermark = ({ size = 32, variant = "amber-on-black" }) => {
  const fg = variant.startsWith("amber") ? "#e8a33d" : (variant.startsWith("black") ? "#0a0a0a" : "#e8e8e8");
  const bg = variant.endsWith("on-black") ? "#0a0a0a" : "#e8a33d";
  return (
    <svg width={size} height={size} viewBox="0 0 64 64" aria-label="TD" role="img">
      <rect width="64" height="64" rx="4" fill={bg}/>
      <g fill={fg}>
        <path d="M14 22 h16 v4 h-6 v16 h-4 v-16 h-6z"/>
        <path d="M33 22 h8 a9 10 0 0 1 9 10 a9 10 0 0 1 -9 10 h-8 z M37 26 v12 h4 a5 6 0 0 0 5 -6 a5 6 0 0 0 -5 -6 z"/>
      </g>
    </svg>
  );
};

// Strap line lockup (co-brand / footer)
const StraplineLockup = ({ height = 48, variant = "amber-on-black" }) => {
  const fg = variant.startsWith("amber") ? "#e8a33d" : "#e8e8e8";
  const muted = "#6a6a6a";
  return (
    <svg height={height} viewBox="0 0 480 64" aria-label="Territory Developer — Internal engineering portal" role="img">
      <Logomark size={48} variant={variant} glow={false}/>
      <g transform="translate(60, 12)">
        <text x="0" y="20" fontFamily="Geist" fontSize="18" fontWeight="700" fill={fg}>TERRITORY DEVELOPER</text>
        <line x1="0" y1="28" x2="360" y2="28" stroke={muted} strokeWidth="0.5"/>
        <text x="0" y="44" fontFamily="Geist Mono" fontSize="10" fill={muted} letterSpacing="0.2em">INTERNAL ENGINEERING PORTAL // EST. 2026</text>
      </g>
    </svg>
  );
};

// -------- MEDIA TRANSPORT ICONS (8b) --------
// Chunky solid+outline family — 24×24, with 1.5px outer outline
// for embossed reading on dark panels. Inline amber fill for active state.
const TIcon = {
  Play: (p) => <svg width="24" height="24" viewBox="0 0 24 24" fill="currentColor" stroke="currentColor" strokeWidth="1.5" strokeLinejoin="round" {...p}><path d="M7 5 L19 12 L7 19 Z"/></svg>,
  Pause: (p) => <svg width="24" height="24" viewBox="0 0 24 24" fill="currentColor" stroke="currentColor" strokeWidth="1.5" strokeLinejoin="round" {...p}><rect x="6" y="5" width="4" height="14" rx="0.5"/><rect x="14" y="5" width="4" height="14" rx="0.5"/></svg>,
  Stop: (p) => <svg width="24" height="24" viewBox="0 0 24 24" fill="currentColor" stroke="currentColor" strokeWidth="1.5" strokeLinejoin="round" {...p}><rect x="5" y="5" width="14" height="14" rx="0.5"/></svg>,
  Record: (p) => <svg width="24" height="24" viewBox="0 0 24 24" fill="currentColor" stroke="currentColor" strokeWidth="1.5" {...p}><circle cx="12" cy="12" r="7"/></svg>,
  RewindEnd: (p) => <svg width="24" height="24" viewBox="0 0 24 24" fill="currentColor" stroke="currentColor" strokeWidth="1.5" strokeLinejoin="round" {...p}><rect x="4" y="5" width="2" height="14"/><path d="M20 5 L8 12 L20 19 Z"/></svg>,
  FastForwardEnd: (p) => <svg width="24" height="24" viewBox="0 0 24 24" fill="currentColor" stroke="currentColor" strokeWidth="1.5" strokeLinejoin="round" {...p}><rect x="18" y="5" width="2" height="14"/><path d="M4 5 L16 12 L4 19 Z"/></svg>,
  Rewind: (p) => <svg width="24" height="24" viewBox="0 0 24 24" fill="currentColor" stroke="currentColor" strokeWidth="1.5" strokeLinejoin="round" {...p}><path d="M13 5 L5 12 L13 19 Z"/><path d="M22 5 L14 12 L22 19 Z"/></svg>,
  FastForward: (p) => <svg width="24" height="24" viewBox="0 0 24 24" fill="currentColor" stroke="currentColor" strokeWidth="1.5" strokeLinejoin="round" {...p}><path d="M2 5 L10 12 L2 19 Z"/><path d="M11 5 L19 12 L11 19 Z"/></svg>,
  Eject: (p) => <svg width="24" height="24" viewBox="0 0 24 24" fill="currentColor" stroke="currentColor" strokeWidth="1.5" strokeLinejoin="round" {...p}><path d="M12 3 L21 15 L3 15 Z"/><rect x="3" y="17" width="18" height="4"/></svg>,
  Loop: (p) => <svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" {...p}><path d="M4 11 A8 8 0 0 1 20 11"/><path d="M20 7 L20 11 L16 11"/><path d="M20 13 A8 8 0 0 1 4 13"/><path d="M4 17 L4 13 L8 13"/></svg>,
  Shuffle: (p) => <svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" {...p}><path d="M3 6 h4 l10 12 h4"/><path d="M3 18 h4 l10 -12 h4"/><path d="M19 4 L22 6 L19 8"/><path d="M19 16 L22 18 L19 20"/></svg>,
  Mute: (p) => <svg width="24" height="24" viewBox="0 0 24 24" fill="currentColor" stroke="currentColor" strokeWidth="1.5" strokeLinejoin="round" {...p}><path d="M3 9 L8 9 L13 5 L13 19 L8 15 L3 15 Z"/><path d="M17 9 L22 14 M22 9 L17 14" stroke="currentColor" strokeWidth="2" fill="none"/></svg>,
  Solo: (p) => <svg width="24" height="24" viewBox="0 0 24 24" fill="currentColor" stroke="currentColor" strokeWidth="1.5" {...p}><circle cx="12" cy="12" r="8"/><text x="12" y="16" textAnchor="middle" fontSize="10" fontFamily="Geist Mono" fontWeight="700" fill="#0a0a0a" stroke="none">S</text></svg>,
};

// -------- HERO ART (8a) --------
// Stylized planetary viewport: deep-space black, warm amber sun rim,
// blue atmospheric scatter, grid graticule (Territory HUD motif),
// orbital arc showing mission trajectory.
const HeroArt = ({ className }) => (
  <svg className={className} viewBox="0 0 800 900" preserveAspectRatio="xMidYMid slice" role="img" aria-label="Territory planet viewport">
    <defs>
      <radialGradient id="sky" cx="50%" cy="20%" r="90%">
        <stop offset="0%" stopColor="#1a2540"/>
        <stop offset="35%" stopColor="#0a0f1c"/>
        <stop offset="100%" stopColor="#000"/>
      </radialGradient>
      <radialGradient id="planet" cx="35%" cy="40%" r="70%">
        <stop offset="0%" stopColor="#2a3a55"/>
        <stop offset="55%" stopColor="#0a0f1c"/>
        <stop offset="100%" stopColor="#000"/>
      </radialGradient>
      <linearGradient id="rim" x1="0%" y1="0%" x2="100%" y2="100%">
        <stop offset="0%" stopColor="#e8a33d" stopOpacity="0"/>
        <stop offset="85%" stopColor="#e8a33d" stopOpacity="0.0"/>
        <stop offset="98%" stopColor="#e8a33d" stopOpacity="0.9"/>
        <stop offset="100%" stopColor="#e8a33d" stopOpacity="0"/>
      </linearGradient>
      <radialGradient id="glow" cx="72%" cy="18%" r="30%">
        <stop offset="0%" stopColor="#e8a33d" stopOpacity="0.35"/>
        <stop offset="100%" stopColor="#e8a33d" stopOpacity="0"/>
      </radialGradient>
      <radialGradient id="atmos" cx="50%" cy="50%" r="50%">
        <stop offset="88%" stopColor="#4a7bc8" stopOpacity="0"/>
        <stop offset="96%" stopColor="#4a7bc8" stopOpacity="0.35"/>
        <stop offset="100%" stopColor="#4a7bc8" stopOpacity="0"/>
      </radialGradient>
    </defs>
    {/* deep-space backdrop */}
    <rect width="800" height="900" fill="url(#sky)"/>
    {/* star field */}
    <g fill="#e8e8e8">
      {Array.from({length: 120}).map((_,i) => {
        const seed = i * 7919;
        const x = (seed * 13) % 800;
        const y = (seed * 17) % 900;
        const r = ((seed % 7) / 10) + 0.2;
        const o = 0.3 + ((seed % 7) / 10);
        return <circle key={i} cx={x} cy={y} r={r} opacity={o}/>;
      })}
    </g>
    {/* distant nebula */}
    <circle cx="640" cy="180" r="180" fill="url(#glow)"/>
    {/* atmospheric halo on planet */}
    <circle cx="380" cy="560" r="340" fill="url(#atmos)"/>
    {/* planet */}
    <circle cx="380" cy="560" r="320" fill="url(#planet)"/>
    {/* terminator rim */}
    <circle cx="380" cy="560" r="320" fill="url(#rim)" opacity="0.85"/>
    {/* continents — abstracted land masses */}
    <g fill="#1a2a18" opacity="0.85">
      <path d="M260 420 Q300 380 360 400 Q400 420 380 460 Q340 480 300 470 Q260 450 260 420 Z"/>
      <path d="M420 520 Q480 500 520 540 Q540 580 500 600 Q450 610 420 580 Q400 550 420 520 Z"/>
      <path d="M300 620 Q340 600 400 620 Q440 650 410 680 Q360 690 310 670 Q280 645 300 620 Z"/>
      <path d="M220 520 Q260 510 280 540 Q290 570 260 580 Q230 575 220 560 Z"/>
    </g>
    {/* graticule — HUD grid */}
    <g stroke="#4a7bc8" strokeWidth="0.4" fill="none" opacity="0.25">
      <path d="M60 560 a320 320 0 0 1 640 0" />
      <path d="M60 560 a320 240 0 0 1 640 0" />
      <path d="M60 560 a320 160 0 0 1 640 0" />
      <path d="M60 560 a320 80 0 0 1 640 0" />
      <path d="M60 560 a320 320 0 0 0 640 0" />
      <path d="M60 560 a320 240 0 0 0 640 0" />
      <line x1="380" y1="240" x2="380" y2="880"/>
      <line x1="250" y1="260" x2="250" y2="860"/>
      <line x1="510" y1="260" x2="510" y2="860"/>
    </g>
    {/* orbital arc with satellite */}
    <g fill="none" stroke="#e8a33d" strokeWidth="0.8" opacity="0.7">
      <ellipse cx="380" cy="560" rx="460" ry="380" transform="rotate(-18 380 560)"/>
    </g>
    <g transform="rotate(-18 380 560) translate(840 560)">
      <circle r="4" fill="#e8a33d"/>
      <circle r="8" fill="none" stroke="#e8a33d" opacity="0.5"/>
    </g>
    {/* HUD readouts — corner tickers */}
    <g fontFamily="Geist Mono" fontSize="11" fill="#4a7bc8" letterSpacing="0.2em">
      <text x="32" y="42">TER/DEV//001</text>
      <text x="32" y="58" opacity="0.6">LAT 42.1184 N</text>
      <text x="32" y="72" opacity="0.6">LNG 71.4128 W</text>
      <text x="640" y="42" textAnchor="start">T+00:04:23:17</text>
      <text x="640" y="58" textAnchor="start" opacity="0.6">ORBIT STABLE</text>
    </g>
    <g fontFamily="Geist Mono" fontSize="10" fill="#e8a33d" letterSpacing="0.15em" opacity="0.8">
      <text x="32" y="868">SIGNAL: NOMINAL</text>
      <text x="32" y="882" opacity="0.6">PAYLOAD: 7/12 NODES</text>
    </g>
    {/* bracket corners */}
    <g stroke="#e8a33d" strokeWidth="1.2" fill="none" opacity="0.8">
      <path d="M20 100 L20 140 L60 140"/>
      <path d="M780 100 L780 140 L740 140"/>
      <path d="M20 800 L20 760 L60 760"/>
      <path d="M780 800 L780 760 L740 760"/>
    </g>
  </svg>
);

// Smaller hero crop (social/card)
const HeroCrop = ({ className }) => (
  <svg className={className} viewBox="0 0 800 420" preserveAspectRatio="xMidYMid slice" role="img" aria-label="Territory orbital view">
    <defs>
      <radialGradient id="skyC" cx="70%" cy="10%" r="80%">
        <stop offset="0%" stopColor="#1a2540"/>
        <stop offset="100%" stopColor="#000"/>
      </radialGradient>
      <radialGradient id="planetC" cx="40%" cy="50%" r="70%">
        <stop offset="0%" stopColor="#2a3a55"/>
        <stop offset="100%" stopColor="#000"/>
      </radialGradient>
      <radialGradient id="rimC" cx="85%" cy="20%" r="45%">
        <stop offset="0%" stopColor="#e8a33d" stopOpacity="0.4"/>
        <stop offset="100%" stopColor="#e8a33d" stopOpacity="0"/>
      </radialGradient>
    </defs>
    <rect width="800" height="420" fill="url(#skyC)"/>
    {Array.from({length: 70}).map((_,i) => {
      const x = (i * 131) % 800; const y = (i * 83) % 420;
      return <circle key={i} cx={x} cy={y} r={0.4 + (i%4)/8} fill="#e8e8e8" opacity={0.4}/>;
    })}
    <circle cx="240" cy="380" r="300" fill="url(#planetC)"/>
    <circle cx="240" cy="380" r="300" fill="url(#rimC)"/>
    <g fontFamily="Geist Mono" fontSize="10" fill="#4a7bc8" letterSpacing="0.2em" opacity="0.7">
      <text x="20" y="28">TERRITORY // 001</text>
    </g>
  </svg>
);

// -------- FEATURE PILLARS (8c) --------
// Each is a palette-locked scene — not marketing photography, but a
// schematic "instrument view" tied to its feature.

const PillarPlanet = ({ className }) => (
  <svg className={className} viewBox="0 0 400 300" preserveAspectRatio="xMidYMid slice">
    <defs>
      <radialGradient id="pp-sky" cx="50%" cy="0%" r="80%"><stop offset="0%" stopColor="#1a1f2e"/><stop offset="100%" stopColor="#000"/></radialGradient>
      <radialGradient id="pp-pl" cx="40%" cy="50%" r="60%"><stop offset="0%" stopColor="#3a9b4a" stopOpacity="0.2"/><stop offset="50%" stopColor="#1a2a18"/><stop offset="100%" stopColor="#000"/></radialGradient>
    </defs>
    <rect width="400" height="300" fill="url(#pp-sky)"/>
    {Array.from({length:40}).map((_,i)=>{const x=(i*97)%400;const y=(i*53)%300;return <circle key={i} cx={x} cy={y} r={0.5} fill="#e8e8e8" opacity={0.5}/>;})}
    <circle cx="200" cy="260" r="180" fill="url(#pp-pl)"/>
    <g stroke="#4a7bc8" strokeWidth="0.4" fill="none" opacity="0.3">
      <path d="M20 260 a180 180 0 0 1 360 0"/>
      <path d="M20 260 a180 120 0 0 1 360 0"/>
      <path d="M20 260 a180 60 0 0 1 360 0"/>
    </g>
    <g stroke="#e8a33d" strokeWidth="1" fill="none" opacity="0.8">
      <path d="M10 20 L10 40 L30 40"/><path d="M390 20 L390 40 L370 40"/>
    </g>
  </svg>
);

const PillarSignal = ({ className }) => (
  <svg className={className} viewBox="0 0 400 300">
    <rect width="400" height="300" fill="#0a0a0a"/>
    {/* waveform */}
    <g stroke="#3a9b4a" fill="none" strokeWidth="1.2">
      <path d="M0 150 L20 150 L30 80 L45 220 L60 100 L75 180 L95 140 L110 160 L130 90 L145 200 L160 130 L180 150 L200 110 L220 170 L235 140 L255 160 L275 100 L295 190 L315 130 L335 150 L360 150 L400 150" opacity="0.9"/>
      <path d="M0 150 L20 150 L30 80 L45 220 L60 100 L75 180 L95 140 L110 160 L130 90 L145 200 L160 130 L180 150 L200 110 L220 170 L235 140 L255 160 L275 100 L295 190 L315 130 L335 150 L360 150 L400 150" opacity="0.3" transform="translate(0 2)"/>
    </g>
    {/* grid */}
    <g stroke="#e8e8e8" strokeWidth="0.3" opacity="0.08">
      {Array.from({length:20}).map((_,i)=><line key={i} x1={i*20} y1="0" x2={i*20} y2="300"/>)}
      {Array.from({length:15}).map((_,i)=><line key={i} x1="0" y1={i*20} x2="400" y2={i*20}/>)}
    </g>
    <g fontFamily="Geist Mono" fontSize="9" fill="#e8a33d" letterSpacing="0.2em" opacity="0.7">
      <text x="16" y="30">CH 01 // SIGNAL</text><text x="16" y="44" opacity="0.5">48kHz · 24bit</text>
    </g>
  </svg>
);

const PillarMixer = ({ className }) => (
  <svg className={className} viewBox="0 0 400 300">
    <rect width="400" height="300" fill="#141414"/>
    {/* faders */}
    {Array.from({length:8}).map((_,i)=>{
      const x = 40 + i*42;
      const h = 50 + ((i*37)%180);
      return (
        <g key={i}>
          <rect x={x-3} y="60" width="6" height="180" fill="#050505" stroke="#000"/>
          <rect x={x-12} y={60+h-8} width="24" height="16" rx="2" fill="#2a2a2a" stroke="#000"/>
          <line x1={x-8} y1={60+h} x2={x+8} y2={60+h} stroke="#e8a33d" strokeWidth="1.5"/>
          <text x={x} y={260} fontFamily="Geist Mono" fontSize="8" fill="#6a6a6a" textAnchor="middle" letterSpacing="0.1em">CH{String(i+1).padStart(2,'0')}</text>
        </g>
      );
    })}
    {/* LEDs */}
    {Array.from({length:8}).map((_,i)=>{
      const x = 40 + i*42;
      const lit = (i*i) % 3;
      return <circle key={i} cx={x} cy={40} r={3} fill={lit===0?"#3a9b4a":lit===1?"#e8a33d":"#d63838"} opacity={0.9}/>;
    })}
    <g fontFamily="Geist Mono" fontSize="9" fill="#e8a33d" letterSpacing="0.2em" opacity="0.8">
      <text x="16" y="24">MIX // MASTER</text>
    </g>
  </svg>
);

const PillarRadar = ({ className }) => (
  <svg className={className} viewBox="0 0 400 300">
    <defs>
      <radialGradient id="pr-bg" cx="50%" cy="50%" r="60%"><stop offset="0%" stopColor="#0a1510"/><stop offset="100%" stopColor="#000"/></radialGradient>
    </defs>
    <rect width="400" height="300" fill="url(#pr-bg)"/>
    <g transform="translate(200 150)" stroke="#3a9b4a" fill="none" opacity="0.7">
      <circle r="120" strokeWidth="0.6"/>
      <circle r="90" strokeWidth="0.5" opacity="0.6"/>
      <circle r="60" strokeWidth="0.5" opacity="0.5"/>
      <circle r="30" strokeWidth="0.4" opacity="0.4"/>
      <line x1="-120" y1="0" x2="120" y2="0" strokeWidth="0.3"/>
      <line x1="0" y1="-120" x2="0" y2="120" strokeWidth="0.3"/>
      <line x1="-85" y1="-85" x2="85" y2="85" strokeWidth="0.3" opacity="0.4"/>
      <line x1="85" y1="-85" x2="-85" y2="85" strokeWidth="0.3" opacity="0.4"/>
    </g>
    {/* sweep */}
    <g transform="translate(200 150)">
      <path d="M0 0 L120 -60 A120 120 0 0 1 100 80 Z" fill="#3a9b4a" opacity="0.15"/>
      <line x1="0" y1="0" x2="120" y2="-60" stroke="#3a9b4a" strokeWidth="1"/>
    </g>
    {/* blips */}
    <g fill="#e8a33d">
      <circle cx="260" cy="110" r="3"/><circle cx="150" cy="200" r="2"/><circle cx="220" cy="170" r="2.5"/>
    </g>
    <g fontFamily="Geist Mono" fontSize="9" fill="#3a9b4a" letterSpacing="0.2em" opacity="0.9">
      <text x="16" y="24">RADAR // SWEEP</text>
    </g>
  </svg>
);

const PillarTape = ({ className }) => (
  <svg className={className} viewBox="0 0 400 300">
    <rect width="400" height="300" fill="#1a1a1a"/>
    {/* two reels */}
    {[130, 270].map((cx,i) => (
      <g key={i} transform={`translate(${cx} 150)`}>
        <circle r="70" fill="#0a0a0a" stroke="#000" strokeWidth="2"/>
        <circle r="65" fill="none" stroke="#2a2a2a" strokeWidth="1"/>
        <g fill="#e8a33d" opacity="0.8">
          {Array.from({length:6}).map((_,j)=>{
            const a = (j*60)*Math.PI/180;
            const x1 = Math.cos(a)*20, y1 = Math.sin(a)*20;
            const x2 = Math.cos(a)*55, y2 = Math.sin(a)*55;
            return <line key={j} x1={x1} y1={y1} x2={x2} y2={y2} stroke="#e8a33d" strokeWidth="3"/>;
          })}
        </g>
        <circle r="12" fill="#2a2a2a" stroke="#000"/>
        <circle r="3" fill="#e8a33d"/>
      </g>
    ))}
    {/* tape between */}
    <path d="M195 150 Q200 230 205 150" fill="none" stroke="#3a2a10" strokeWidth="4"/>
    <rect x="60" y="225" width="280" height="40" fill="#0a0a0a" stroke="#000"/>
    <g fontFamily="Geist Mono" fontSize="9" fill="#e8a33d" letterSpacing="0.2em" opacity="0.8">
      <text x="16" y="24">TAPE // REC</text>
    </g>
  </svg>
);

// -------- Sparkline --------
const Sparkline = ({ data, color = "var(--raw-amber)", width = 120, height = 32 }) => {
  const max = Math.max(...data, 1);
  const pts = data.map((v,i) => `${(i/(data.length-1))*width},${height - (v/max)*height}`).join(" ");
  return (
    <svg width={width} height={height} viewBox={`0 0 ${width} ${height}`}>
      <polyline points={pts} fill="none" stroke={color} strokeWidth="1.5"/>
      {data.map((v,i) => <circle key={i} cx={(i/(data.length-1))*width} cy={height - (v/max)*height} r="1" fill={color}/>)}
    </svg>
  );
};

Object.assign(window, { Logomark, Wordmark, Lettermark, StraplineLockup, TIcon, HeroArt, HeroCrop, PillarPlanet, PillarSignal, PillarMixer, PillarRadar, PillarTape, Sparkline });
