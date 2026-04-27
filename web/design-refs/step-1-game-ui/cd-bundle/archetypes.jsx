// Studio Rack archetype components
const cx = (...parts) => parts.filter(Boolean).join(" ");

function Knob({ size = "md", tone = "primary", state, rotation = 0, label }) {
  const stateClass = state ? `knob--${state}` : "";
  return (
    <div className="knob-wrap" style={{ display: "flex", flexDirection: "column", alignItems: "center", gap: 6 }}>
      <div
        className={cx("knob", `knob--${size}`, `knob--tone-${tone}`, stateClass)}
        style={{ "--knob-rot": `${rotation}deg` }}
      >
        <div className="knob__detents" />
      </div>
      {label && <div className="silkscreen">{label}</div>}
    </div>
  );
}

function Fader({ size = "md", tone = "primary", state, value = 60, orientation = "vertical", label, ticks = true }) {
  const stateClass = state ? `fader--${state}` : "";
  return (
    <div style={{ display: "flex", flexDirection: "column", alignItems: "center", gap: 6 }}>
      <div
        className={cx("fader", `fader--${size}`, `fader--tone-${tone}`, orientation === "horizontal" && "fader--horizontal", stateClass)}
        style={{ "--fader-pos": `${value}%` }}
      >
        <div className="fader__track">
          <div className="fader__fill" />
        </div>
        <div className="fader__cap" />
        {ticks && orientation === "vertical" && <div className="fader__ticks" />}
      </div>
      {label && <div className="silkscreen">{label}</div>}
    </div>
  );
}

function VuMeter({ size = "md", tone = "primary", state, segments = 16, lit = 10, orientation = "horizontal", label }) {
  const stateClass = state ? `vu--${state}` : "";
  const segs = [];
  for (let i = 0; i < segments; i++) {
    let cls = "vu__seg";
    if (i < lit) {
      if (i >= segments * 0.85) cls += " vu__seg--peak";
      else if (i >= segments * 0.65) cls += " vu__seg--warn";
      else cls += " vu__seg--lit";
    }
    segs.push(<div key={i} className={cls} />);
  }
  return (
    <div style={{ display: "flex", flexDirection: "column", alignItems: "flex-start", gap: 4 }}>
      {label && <div className="silkscreen">{label}</div>}
      <div className={cx("vu", `vu--${size}`, `vu--tone-${tone}`, orientation === "vertical" && "vu--vertical", stateClass)}>
        {segs}
      </div>
    </div>
  );
}

function ILed({ size = "md", tone = "primary", state, lit = false, children }) {
  const stateClass = state ? `iled--${state}` : "";
  return (
    <button className={cx("iled", `iled--${size}`, `iled--tone-${tone}`, lit && "iled--lit", stateClass)}>
      <span className="iled__lamp" />
      <span>{children}</span>
    </button>
  );
}

function SegRead({ size = "md", tone = "primary", state, value = "0000000", digits = 7, label }) {
  const stateClass = state ? `segread--${state}` : "";
  const ghost = "8".repeat(digits);
  const padded = String(value).padStart(digits, " ");
  return (
    <div style={{ display: "flex", flexDirection: "column", alignItems: "flex-start", gap: 4 }}>
      {label && <div className="silkscreen">{label}</div>}
      <div className={cx("segread", `segread--${size}`, `segread--tone-${tone}`, stateClass)} data-ghost={ghost}>
        {padded}
      </div>
    </div>
  );
}

// State display helper
function StateLabel({ children }) {
  return (
    <div style={{
      fontFamily: "var(--font-face-segmented)",
      fontSize: "var(--type-mono-meta)",
      color: "var(--palette-led-cyan-mid)",
      letterSpacing: "0.15em",
      textTransform: "uppercase",
      marginTop: 4
    }}>{children}</div>
  );
}

Object.assign(window, { Knob, Fader, VuMeter, ILed, SegRead, StateLabel, cx });
