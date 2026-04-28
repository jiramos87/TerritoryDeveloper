// panels-extension.jsx — Package 2 game-domain panels
const accepts8 = "knob fader vu-meter illuminated-button segmented-readout oscilloscope detent-ring led";

function Icon({ slug, size = 24, color }) {
  const cls = size === 16 ? "icon icon--16" : size === 32 ? "icon icon--32" : "icon icon--24";
  return (
    <svg className={cls} style={color ? { color } : null}>
      <use href={`#icon-${slug}`}/>
    </svg>
  );
}

function PanelStrip2({ slug }) {
  return (
    <div className="panel-strip">
      <span className="panel-strip__title">{slug}</span>
      <span className="panel-strip__id">PNL · {slug.toUpperCase()}</span>
    </div>
  );
}

// 2A — building-info
function PanelBuildingInfo() {
  return (
    <section data-panel-slug="building-info" className="rack-chrome rack-chrome--bottom-rivets" style={{ width: 540, padding: "24px 28px" }}>
      <div className="rivet-bl"/><div className="rivet-br"/>
      <PanelStrip2 slug="building-info"/>

      <div data-slot="header" data-accepts={accepts8} style={{ display: "flex", alignItems: "center", gap: 12, marginBottom: 16 }}>
        <Icon slug="power" size={32} color="var(--palette-led-grass-hot)"/>
        <div style={{ flex: 1 }}>
          <div className="nameplate" style={{ fontSize: "var(--type-h3)" }}>POWER PLANT</div>
          <div className="silkscreen">DENSITY · HEAVY · CELL (12,34)</div>
        </div>
        <Led size="lg" tone="success" lit/>
      </div>

      <div className="rail"/>

      <div data-slot="vitals" data-accepts={accepts8} style={{ display: "flex", flexDirection: "column", gap: 10, marginBottom: 16 }}>
        <div style={{ display: "grid", gridTemplateColumns: "100px 1fr 90px", gap: 12, alignItems: "center" }}>
          <div className="silkscreen">CAPACITY</div>
          <VuMeter size="sm" tone="primary" segments={16} lit={13}/>
          <SegRead size="sm" tone="primary" value="84 MW" digits={6}/>
        </div>
        <div style={{ display: "grid", gridTemplateColumns: "100px 1fr 90px", gap: 12, alignItems: "center" }}>
          <div className="silkscreen">EFFICIENCY</div>
          <VuMeter size="sm" tone="success" segments={16} lit={11}/>
          <SegRead size="sm" tone="primary" value="93%" digits={5}/>
        </div>
        <div style={{ display: "grid", gridTemplateColumns: "100px 1fr 90px", gap: 12, alignItems: "center" }}>
          <div className="silkscreen">UPKEEP</div>
          <VuMeter size="sm" tone="alert" segments={16} lit={5}/>
          <SegRead size="sm" tone="alert" value="-$320" digits={6}/>
        </div>
      </div>

      <div className="rail"/>
      <div className="silkscreen" style={{ marginBottom: 8 }}>OUTPUT TRACE · LAST 60 TICKS</div>
      <div data-slot="trend" data-accepts={accepts8} style={{ marginBottom: 16 }}>
        <Osc size="md" tone="primary"/>
      </div>

      <div className="rail"/>
      <div data-slot="controls" data-accepts={accepts8} style={{ display: "flex", gap: 10, alignItems: "center", justifyContent: "space-between" }}>
        <ILed size="md" tone="success" lit>OPERATE</ILed>
        <DetentRing size="sm" tone="primary" detents={4} current={2} label="TIER"/>
        <ILed size="md" tone="primary">UPGRADE</ILed>
        <ILed size="md" tone="alert">DEMOLISH</ILed>
      </div>
    </section>
  );
}

// 2B — zone-overlay
function PanelZoneOverlay() {
  const overlays = [
    { slug: "desirability", label: "DESIRABILITY", icon: "desirability", lit: true },
    { slug: "pollution",    label: "POLLUTION",    icon: "pollution",    lit: false },
    { slug: "land-value",   label: "LAND VALUE",   icon: "land-value",   lit: false },
    { slug: "heat",         label: "HEAT",         icon: "heat",         lit: false },
    { slug: "power",        label: "POWER",        icon: "power",        lit: false },
    { slug: "water",        label: "WATER",        icon: "water",        lit: false },
  ];
  return (
    <section data-panel-slug="zone-overlay" className="rack-chrome rack-chrome--bottom-rivets" style={{ width: 540, padding: "24px 28px" }}>
      <div className="rivet-bl"/><div className="rivet-br"/>
      <PanelStrip2 slug="zone-overlay"/>
      <div className="nameplate" style={{ fontSize: "var(--type-h3)", marginBottom: 14 }}>OVERLAY MATRIX</div>

      <div data-slot="overlay-select" data-accepts={accepts8} style={{ display: "grid", gridTemplateColumns: "repeat(3, 1fr)", gap: 8, marginBottom: 16 }}>
        {overlays.map(o => (
          <button key={o.slug} className={cx("iled", "iled--sm", "iled--tone-primary", o.lit && "iled--lit")} style={{ minWidth: 0 }}>
            <Icon slug={o.icon} size={16}/>
            <span>{o.label}</span>
          </button>
        ))}
      </div>

      <div className="rail"/>
      <div data-slot="legend" data-accepts={accepts8} style={{ marginBottom: 16 }}>
        <div className="silkscreen" style={{ marginBottom: 6 }}>LEGEND · LOW → HIGH</div>
        <div style={{ display: "grid", gridTemplateColumns: "60px 1fr 60px", gap: 10, alignItems: "center" }}>
          <SegRead size="sm" tone="neutral" value="0.12" digits={5}/>
          <VuMeter size="md" tone="primary" segments={20} lit={14}/>
          <SegRead size="sm" tone="primary" value="0.87" digits={5}/>
        </div>
      </div>

      <div className="rail"/>
      <div data-slot="tools" data-accepts={accepts8} style={{ display: "flex", gap: 8, marginBottom: 16 }}>
        <ILed size="sm" tone="primary" lit><Icon slug="zone-residential" size={16}/><span>PAINT</span></ILed>
        <ILed size="sm" tone="alert"><Icon slug="bulldoze" size={16}/><span>ERASE</span></ILed>
        <ILed size="sm" tone="neutral"><Icon slug="select" size={16}/><span>SAMPLE</span></ILed>
      </div>

      <div className="rail"/>
      <div data-slot="stats" data-accepts={accepts8} style={{ display: "grid", gridTemplateColumns: "auto 1fr auto", gap: 10, alignItems: "center" }}>
        <Led size="md" tone="success" lit/>
        <div className="silkscreen">CELL · DESIRABILITY</div>
        <SegRead size="sm" tone="success" value="0.74" digits={5}/>
      </div>
    </section>
  );
}

// 2C — time-controls
function PanelTimeControls() {
  return (
    <section data-panel-slug="time-controls" className="rack-chrome rack-chrome--bottom-rivets" style={{ width: 720, padding: "20px 24px" }}>
      <div className="rivet-bl"/><div className="rivet-br"/>
      <PanelStrip2 slug="time-controls"/>

      <div style={{ display: "grid", gridTemplateColumns: "auto auto 1fr auto", gap: 24, alignItems: "center" }}>
        <div data-slot="clock" data-accepts={accepts8}>
          <SegRead size="lg" tone="primary" value="2024-03-15" digits={10} label="CLOCK"/>
        </div>

        <div data-slot="speed-select" data-accepts={accepts8}>
          <DetentRing size="md" tone="primary" detents={4} current={1} label="SPEED · 2×"/>
        </div>

        <div data-slot="transport" data-accepts={accepts8} style={{ display: "flex", gap: 8, justifyContent: "center" }}>
          <ILed size="md" tone="neutral"><Icon slug="pause" size={16}/></ILed>
          <ILed size="md" tone="primary" lit><Icon slug="play" size={16}/></ILed>
          <ILed size="md" tone="primary"><Icon slug="fast-forward" size={16}/></ILed>
          <ILed size="md" tone="neutral"><Icon slug="step" size={16}/></ILed>
        </div>

        <div data-slot="status" data-accepts={accepts8} style={{ display: "flex", flexDirection: "column", gap: 6 }}>
          <div style={{ display: "flex", alignItems: "center", gap: 8 }}>
            <Led size="md" tone="success" lit/>
            <div className="silkscreen">AUTOSAVE</div>
          </div>
          <div style={{ display: "flex", alignItems: "center", gap: 8 }}>
            <Led size="md" tone="primary" lit/>
            <div className="silkscreen">FOCUSED</div>
          </div>
        </div>
      </div>
    </section>
  );
}

// 2D — alerts-panel
function PanelAlerts() {
  const feed = [
    { sev: "info",    tone: "primary", msg: "Built Whitmore Heights",                                              ts: "00:14:22" },
    { sev: "success", tone: "success", msg: "Bond issued.",                                                         ts: "00:13:55" },
    { sev: "info",    tone: "primary", msg: "Auto-built Power Plant at (12, 34)",                                  ts: "00:13:10" },
    { sev: "warning", tone: "alert",   msg: "Connect a road to the Interstate Highway before building.",           ts: "00:11:48" },
    { sev: "error",   tone: "alert",   msg: "Cannot place zone here.",                                              ts: "00:10:02" },
    { sev: "info",    tone: "primary", msg: "Residential tax raised to 8%",                                         ts: "00:08:31" },
  ];
  return (
    <section data-panel-slug="alerts-panel" className="rack-chrome rack-chrome--bottom-rivets" style={{ width: 620, padding: "24px 28px" }}>
      <div className="rivet-bl"/><div className="rivet-br"/>
      <PanelStrip2 slug="alerts-panel"/>
      <div className="nameplate" style={{ fontSize: "var(--type-h3)", marginBottom: 14 }}>ANNUNCIATOR FEED</div>

      <div data-slot="summary" data-accepts={accepts8} style={{ display: "flex", gap: 12, marginBottom: 14, alignItems: "center" }}>
        <div style={{ display: "flex", alignItems: "center", gap: 6 }}><Led size="md" tone="primary" lit/><SegRead size="sm" tone="primary" value="3" digits={2} label="INFO"/></div>
        <div style={{ display: "flex", alignItems: "center", gap: 6 }}><Led size="md" tone="success" lit/><SegRead size="sm" tone="success" value="1" digits={2} label="OK"/></div>
        <div style={{ display: "flex", alignItems: "center", gap: 6 }}><Led size="md" tone="alert" lit/><SegRead size="sm" tone="alert" value="2" digits={2} label="WARN"/></div>
      </div>

      <div className="rail"/>
      <div data-slot="filters" data-accepts={accepts8} style={{ display: "flex", gap: 6, marginBottom: 12, flexWrap: "wrap" }}>
        <ILed size="sm" tone="primary" lit>INFO</ILed>
        <ILed size="sm" tone="success" lit>SUCCESS</ILed>
        <ILed size="sm" tone="alert" lit>WARNING</ILed>
        <ILed size="sm" tone="alert" lit>ERROR</ILed>
      </div>

      <div className="rail"/>
      <div data-slot="feed" data-accepts={accepts8} style={{ display: "flex", flexDirection: "column", gap: 6 }}>
        {feed.map((row, i) => (
          <div key={i} className={`alert-row alert-row--${row.sev}`}>
            <Led size="md" tone={row.tone} lit/>
            <div className="alert-row__msg">{row.msg}</div>
            <div className={cx("segread", "segread--sm", "segread--tone-neutral")} data-ghost="88:88:88" style={{ minWidth: 90 }}>{row.ts}</div>
          </div>
        ))}
      </div>
    </section>
  );
}

// 2E — mini-map
function PanelMiniMap() {
  return (
    <section data-panel-slug="mini-map" className="rack-chrome rack-chrome--bottom-rivets" style={{ width: 420, padding: "20px 22px" }}>
      <div className="rivet-bl"/><div className="rivet-br"/>
      <PanelStrip2 slug="mini-map"/>

      <div style={{ display: "flex", gap: 6, marginBottom: 10 }}>
        {["St","Zn","Fr","De","Ct"].map((p, i) => (
          <ILed key={p} size="sm" tone={i < 2 ? "primary" : "neutral"} lit={i < 2}>{p}</ILed>
        ))}
      </div>

      <div data-slot="viewport" data-accepts="" style={{
        position: "relative",
        height: 220,
        background:
          "repeating-linear-gradient(45deg, var(--palette-chassis-graphite-2) 0 6px, var(--palette-chassis-graphite-1) 6px 12px)",
        border: "1px solid var(--palette-chassis-graphite-0)",
        borderRadius: "var(--frame-radius-rack-xs)",
        boxShadow: "var(--frame-shadow-recessed)",
        marginBottom: 12,
        overflow: "hidden"
      }}>
        <div style={{ position: "absolute", inset: "20% 25%", background: "rgba(46,184,200,0.12)", border: "1px solid var(--palette-led-cyan-low)" }}/>
        <div style={{ position: "absolute", left: "50%", top: "50%", width: 8, height: 8, marginLeft: -4, marginTop: -4, borderRadius: "50%", background: "var(--palette-led-amber-hot)", boxShadow: "0 0 8px var(--palette-led-amber-mid)" }}/>
      </div>

      <div style={{ display: "grid", gridTemplateColumns: "auto 1fr auto", gap: 12, alignItems: "center" }}>
        <div data-slot="controls" data-accepts={accepts8} style={{ display: "flex", gap: 4 }}>
          <ILed size="sm" tone="neutral">+</ILed>
          <ILed size="sm" tone="neutral">−</ILed>
          <ILed size="sm" tone="primary" lit>◎</ILed>
        </div>
        <div data-slot="scale-select" data-accepts={accepts8} style={{ display: "flex", justifyContent: "center" }}>
          <DetentRing size="sm" tone="primary" detents={3} current={0} label="CITY"/>
        </div>
        <div data-slot="coords" data-accepts={accepts8}>
          <SegRead size="sm" tone="primary" value="067,127" digits={7} label="X·Y"/>
        </div>
      </div>
    </section>
  );
}

Object.assign(window, { PanelBuildingInfo, PanelZoneOverlay, PanelTimeControls, PanelAlerts, PanelMiniMap, Icon });
