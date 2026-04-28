// archetypes-extension.jsx — Package 1 components
const cx2 = (...parts) => parts.filter(Boolean).join(" ");

function Osc({ size = "md", tone = "primary", state, path, label }) {
  const stateClass = state ? `osc--${state}` : "";
  const dims = { sm: [96, 56], md: [192, 112], lg: [320, 180] }[size];
  const [w, h] = dims;
  const samples = {
    sm: "M0,28 Q12,0 24,28 T48,28 T72,28 T96,28",
    md: "M0,90 L40,80 L80,30 L100,15 L130,40 L192,75",
    lg: "M0,140 L60,120 L100,40 L160,60 L220,150 L320,90",
  };
  const d = path || samples[size];
  // graticule: 8x4 division lines
  const xLines = [];
  for (let i = 1; i < 8; i++) xLines.push(<line key={"x"+i} x1={(w*i)/8} y1={0} x2={(w*i)/8} y2={h} stroke="var(--frame-graticule-line)" strokeWidth="1"/>);
  for (let i = 1; i < 4; i++) xLines.push(<line key={"y"+i} x1={0} y1={(h*i)/4} x2={w} y2={(h*i)/4} stroke="var(--frame-graticule-line)" strokeWidth="1"/>);
  return (
    <div style={{ display: "flex", flexDirection: "column", alignItems: "flex-start", gap: 4 }}>
      {label && <div className="silkscreen">{label}</div>}
      <div className={cx2("osc", `osc--${size}`, `osc--tone-${tone}`, stateClass)}>
        <span className="osc__screw osc__screw--tl"/><span className="osc__screw osc__screw--tr"/>
        <span className="osc__screw osc__screw--bl"/><span className="osc__screw osc__screw--br"/>
        <div className="osc__well">
          <svg className="osc__graticule" viewBox={`0 0 ${w} ${h}`} preserveAspectRatio="none">{xLines}
            <line x1={0} y1={h/2} x2={w} y2={h/2} stroke="var(--frame-graticule-line-strong)" strokeWidth="1"/>
            <line x1={w/2} y1={0} x2={w/2} y2={h} stroke="var(--frame-graticule-line-strong)" strokeWidth="1"/>
          </svg>
          <svg className="osc__trace" viewBox={`0 0 ${w} ${h}`} preserveAspectRatio="none">
            <path d={d}/>
          </svg>
        </div>
      </div>
    </div>
  );
}

function DetentRing({ size = "md", tone = "primary", state, detents = 12, current = 7, label }) {
  const stateClass = state ? `detent-ring--${state}` : "";
  const dots = [];
  for (let i = 0; i < detents; i++) {
    const angle = (i / detents) * 360 - 90;
    const r = 50; // % from center
    const lit = i === current;
    const x = 50 + r * Math.cos((angle * Math.PI) / 180);
    const y = 50 + r * Math.sin((angle * Math.PI) / 180);
    dots.push(
      <span key={i} className={cx2("detent-ring__dot", lit && "detent-ring__dot--lit")}
        style={{ top: `${y}%`, left: `${x}%` }}/>
    );
  }
  const pointerAngle = (current / detents) * 360;
  return (
    <div style={{ display: "flex", flexDirection: "column", alignItems: "center", gap: 6 }}>
      <div className={cx2("detent-ring", `detent-ring--${size}`, `detent-ring--tone-${tone}`, stateClass)}>
        <div className="detent-ring__bezel"/>
        <div className="detent-ring__dots">{dots}</div>
        <div className="detent-ring__cap"/>
        <div className="detent-ring__pointer" style={{ transform: `translate(-50%, -100%) rotate(${pointerAngle}deg)`, transformOrigin: "50% 100%" }}/>
      </div>
      {label && <div className="silkscreen">{label}</div>}
    </div>
  );
}

function Led({ size = "md", tone = "primary", state, lit = true }) {
  const stateClass = state ? `led--${state}` : "";
  return <span className={cx2("led", `led--${size}`, `led--tone-${tone}`, lit && "led--lit", stateClass)}/>;
}

Object.assign(window, { Osc, DetentRing, Led });
