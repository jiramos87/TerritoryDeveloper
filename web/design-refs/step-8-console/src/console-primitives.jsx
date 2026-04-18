// =============================================================
// Territory Developer — Console Primitives
// Reskinned: Button, BadgeChip, StatBar, DataTable, FilterChips,
// HeatmapCell — names + roles preserved from the flat kit.
// =============================================================

const { useState, useMemo, useEffect } = React;

// ---- Chrome wrappers ----
const Rack = ({ label, children, className = "" }) => (
  <div className={`metal rack ${className}`}>
    <span className="screw tl"/><span className="screw tr"/>
    <span className="screw bl"/><span className="screw br"/>
    {label && <span className="rack-label">{label}</span>}
    {children}
  </div>
);

const Bezel = ({ children, className = "", thin = false }) => (
  <div className={`bezel ${thin ? "thin" : ""} ${className}`}>{children}</div>
);

const Screen = ({ children, color, className = "", sweep = true }) => (
  <div className={`screen ${color || ""} ${className}`}>
    {sweep && <div className="sweep" aria-hidden="true"/>}
    {children}
  </div>
);

const LED = ({ tone = "g", blink = false, title }) => (
  <span className={`led ${tone} ${blink ? "blink" : ""}`} title={title} aria-label={title}/>
);

// ---- Button ----
const Button = ({ variant = "secondary", size, icon, children, ...rest }) => (
  <button className={`btn btn-${variant} ${size === "lg" ? "btn-lg" : ""}`} {...rest}>
    {icon}
    {children && <span>{children}</span>}
  </button>
);

// ---- BadgeChip (status) ----
const StatusChip = ({ status, children }) => (
  <span className={`chip s-${status}`}>{children || statusLabel(status)}</span>
);
const IdChip = ({ children }) => <span className="id-chip mono">{children}</span>;

// ---- StatBar ----
const StatBar = ({ done, total, tone = "progress", label, showNums = true }) => {
  const pct = total > 0 ? Math.round((done / total) * 100) : 0;
  return (
    <div className="statbar">
      {(label || showNums) && (
        <div className="row">
          {label && <span>{label}</span>}
          {showNums && <span className="num">{done}/{total} · {pct}%</span>}
        </div>
      )}
      <div className="track"><div className={`fill ${tone}`} style={{width: `${pct}%`}}/></div>
    </div>
  );
};

// ---- FilterChip ----
const FilterChip = ({ active, status, onClick, children, count }) => (
  <button className={`fchip ${active ? "on" : ""}`} onClick={onClick} aria-pressed={active}>
    {status && <span className={`dot s-${status}`}/>}
    <span>{children}</span>
    {count !== undefined && <span style={{opacity:.6, fontSize:10}}>{count}</span>}
  </button>
);

// ---- HeatmapCell ----
const HeatCell = ({ n, label }) => (
  <div className={`hcell ${densityBucket(n)}`} title={label || (n + " tasks")} aria-label={label}/>
);

// ---- Status legend strip ----
const Legend = () => (
  <div className="legend" role="list" aria-label="Status legend">
    <span className="item"><span className="sw done"/> Done</span>
    <span className="item"><span className="sw prog"/> In Progress</span>
    <span className="item"><span className="sw pend"/> Pending</span>
    <span className="item"><span className="sw block"/> Blocked</span>
  </div>
);

// ---- Density toggle ----
const DensityToggle = ({ mode, setMode }) => (
  <div className="seg" role="radiogroup" aria-label="Density">
    {["comfortable","compact","ultra"].map(m => (
      <button key={m} className={mode===m?"on":""} onClick={() => setMode(m)} aria-checked={mode===m} role="radio">{m.slice(0,4)}</button>
    ))}
  </div>
);

// ---- VU meter strip (used in landing + screens) ----
const VuStrip = ({ level = 0.7, segments = 24 }) => {
  const lit = Math.floor(level * segments);
  return (
    <div className="vu" role="meter" aria-label={`Signal level ${Math.round(level*100)}%`}>
      {Array.from({length: segments}).map((_, i) => {
        const tone = i < segments*0.7 ? "g" : i < segments*0.9 ? "a" : "r";
        return <div key={i} className={`seg ${i < lit ? "lit "+tone : ""}`}/>;
      })}
    </div>
  );
};

// ---- Tape reel ----
const TapeReel = ({ size = 22 }) => (
  <svg className="reel" width={size} height={size} viewBox="0 0 22 22" aria-hidden="true">
    <circle cx="11" cy="11" r="10" fill="#0a0a0a" stroke="#000"/>
    <g stroke="#e8a33d" strokeWidth="1" opacity="0.9">
      {[0,60,120,180,240,300].map(a => <line key={a} x1="11" y1="11" x2={11 + Math.cos(a*Math.PI/180)*8} y2={11 + Math.sin(a*Math.PI/180)*8}/>)}
    </g>
    <circle cx="11" cy="11" r="2.5" fill="#2a2a2a" stroke="#000"/>
    <circle cx="11" cy="11" r="1" fill="#e8a33d"/>
  </svg>
);

// ---- Transport strip ----
const TransportStrip = ({ state = "play", onSetState }) => (
  <Rack label="TRANSPORT">
    <div className="transport">
      <button className="tbtn" onClick={() => onSetState && onSetState("rwe")} aria-label="Rewind to start"><TIcon.RewindEnd/></button>
      <button className="tbtn" onClick={() => onSetState && onSetState("rw")} aria-label="Rewind"><TIcon.Rewind/></button>
      <button className={`tbtn lg ${state==="pause"?"active a":""}`} onClick={() => onSetState && onSetState("pause")} aria-label="Pause"><TIcon.Pause/></button>
      <button className={`tbtn lg ${state==="play"?"active g":""}`} onClick={() => onSetState && onSetState("play")} aria-label="Play"><TIcon.Play/></button>
      <button className={`tbtn ${state==="stop"?"active":""}`} onClick={() => onSetState && onSetState("stop")} aria-label="Stop"><TIcon.Stop/></button>
      <button className={`tbtn ${state==="rec"?"active r":""}`} onClick={() => onSetState && onSetState("rec")} aria-label="Record"><TIcon.Record/></button>
      <button className="tbtn" onClick={() => onSetState && onSetState("ff")} aria-label="Fast forward"><TIcon.FastForward/></button>
      <button className="tbtn" onClick={() => onSetState && onSetState("ffe")} aria-label="Forward to end"><TIcon.FastForwardEnd/></button>
      <span style={{flex:1}}/>
      <button className="tbtn" aria-label="Loop"><TIcon.Loop/></button>
      <button className="tbtn" aria-label="Shuffle"><TIcon.Shuffle/></button>
    </div>
  </Rack>
);

// ---- State cards ----
const EmptyState = ({ title, body, cta }) => (
  <Rack>
    <div className="state-card">
      <Screen color="amber" className="lcd" sweep={false}><span className="lcd-big">— NO SIGNAL —</span></Screen>
      <div className="title">{title}</div>
      <div className="body">{body}</div>
      {cta}
    </div>
  </Rack>
);

const LoadingSkeleton = ({ rows = 4 }) => (
  <Rack>
    <div style={{padding: "12px", display:"flex", flexDirection:"column", gap:8}}>
      {Array.from({length: rows}).map((_,i)=>(
        <div key={i} style={{display:"flex", gap:8, alignItems:"center"}}>
          <div className="skel" style={{width:60, height:16}}/>
          <div className="skel" style={{flex:1, height:16}}/>
          <div className="skel" style={{width:100, height:10}}/>
        </div>
      ))}
    </div>
  </Rack>
);

const ErrorState = ({ title, body, cta }) => (
  <Rack>
    <div className="state-card">
      <Screen color="red" className="lcd" sweep={false}>
        <span className="lcd-big">ERR · 502</span>
      </Screen>
      <div className="title" style={{color: "var(--raw-red)"}}>{title}</div>
      <div className="body">{body}</div>
      {cta}
    </div>
  </Rack>
);

const StaleDataBanner = ({ when = "4m 12s ago" }) => (
  <span className="stale">
    <LED tone="a" blink/>
    <span>STALE · LAST SYNC {when}</span>
    <Button variant="ghost" style={{padding: "2px 8px", fontSize: 9}}>REFRESH</Button>
  </span>
);

Object.assign(window, {
  Rack, Bezel, Screen, LED, Button, StatusChip, IdChip, StatBar, FilterChip, HeatCell,
  Legend, DensityToggle, VuStrip, TapeReel, TransportStrip,
  EmptyState, LoadingSkeleton, ErrorState, StaleDataBanner,
});
