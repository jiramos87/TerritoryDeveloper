// Panel components - one per surface, semantic data-panel-slug + data-slot attrs
const accepts5 = "knob fader vu-meter illuminated-button segmented-readout";

function PanelStrip({ slug }) {
  return (
    <div className="panel-strip">
      <span className="panel-strip__title">{slug}</span>
      <span className="panel-strip__id">PNL · {slug.toUpperCase()}</span>
    </div>
  );
}

// 1. HUD BAR — top of game, transport + readouts
function PanelHudBar() {
  return (
    <section data-panel-slug="hud-bar" className="rack-chrome rack-chrome--bottom-rivets" style={{ width: 1280, padding: "20px 28px" }}>
      <div className="rivet-bl" /><div className="rivet-br" />
      <PanelStrip slug="hud-bar" />
      <div style={{ display: "grid", gridTemplateColumns: "auto 1fr auto", gap: 32, alignItems: "center" }}>
        <div data-slot="left" data-accepts={accepts5} style={{ gap: 12 }}>
          <ILed size="sm" tone="primary" lit>R+</ILed>
          <ILed size="sm" tone="neutral" lit>C+</ILed>
          <ILed size="sm" tone="alert">I+</ILed>
        </div>
        <div data-slot="center" data-accepts={accepts5} style={{ justifyContent: "center", flexDirection: "column", gap: 4 }}>
          <div className="nameplate">MERIDIAN</div>
          <div className="silkscreen">CITY · NORTH SECTOR · YR 0042</div>
        </div>
        <div data-slot="right" data-accepts={accepts5} style={{ gap: 16 }}>
          <SegRead size="md" tone="primary" value="20,000" digits={9} />
          <VuMeter size="sm" tone="primary" segments={12} lit={8} label="HAPPY" />
          <div style={{ display: "flex", gap: 6 }}>
            <ILed size="sm">‖‖</ILed>
            <ILed size="sm" tone="primary" lit>▶</ILed>
            <ILed size="sm">▶▶</ILed>
            <ILed size="sm">▶▶▶</ILed>
          </div>
          <ILed size="sm" tone="alert">AUTO</ILed>
        </div>
      </div>
    </section>
  );
}

// 2. INFO PANEL — budget channel strip
function PanelInfoPanel() {
  return (
    <section data-panel-slug="info-panel" className="rack-chrome rack-chrome--bottom-rivets" style={{ width: 720, padding: "24px 28px" }}>
      <div className="rivet-bl" /><div className="rivet-br" />
      <PanelStrip slug="info-panel" />
      <div className="nameplate" style={{ fontSize: "var(--type-h3)", marginBottom: 12 }}>BUDGET BUS</div>
      <div className="silkscreen" style={{ marginBottom: 20 }}>EST. SURPLUS  <span style={{ fontFamily: "var(--font-face-segmented)", color: "var(--palette-led-grass-mid)" }}>+$0</span></div>

      <div style={{ display: "grid", gridTemplateColumns: "1fr 4fr", gap: 24 }}>
        <div data-slot="master" data-accepts={accepts5} style={{ flexDirection: "column", alignItems: "center", gap: 8 }}>
          <Fader size="md" tone="primary" value={45} label="GROWTH" />
          <SegRead size="sm" tone="primary" value="10%" digits={4} />
        </div>

        <div data-slot="bus-bay" data-accepts={accepts5} style={{ flexDirection: "row", justifyContent: "space-around", gap: 16 }}>
          <div style={{ display: "flex", flexDirection: "column", alignItems: "center", gap: 8 }}>
            <Fader size="sm" tone="neutral" value={25} label="ROAD" />
            <SegRead size="sm" tone="neutral" value="25%" digits={4} />
          </div>
          <div style={{ display: "flex", flexDirection: "column", alignItems: "center", gap: 8 }}>
            <Fader size="sm" tone="primary" value={25} label="ENERGY" />
            <SegRead size="sm" tone="primary" value="25%" digits={4} />
          </div>
          <div style={{ display: "flex", flexDirection: "column", alignItems: "center", gap: 8 }}>
            <Fader size="sm" tone="neutral" value={25} label="WATER" />
            <SegRead size="sm" tone="neutral" value="25%" digits={4} />
          </div>
          <div style={{ display: "flex", flexDirection: "column", alignItems: "center", gap: 8 }}>
            <Fader size="sm" tone="primary" value={25} label="ZONING" />
            <SegRead size="sm" tone="primary" value="25%" digits={4} />
          </div>
        </div>
      </div>

      <div className="rail" />

      <div data-slot="trim-bay" data-accepts={accepts5} style={{ display: "grid", gridTemplateColumns: "1fr 1fr 1fr", gap: 24 }}>
        <div style={{ display: "flex", flexDirection: "column", alignItems: "center", gap: 6 }}>
          <Knob size="md" tone="primary" rotation={-30} label="TAX · R" />
          <SegRead size="sm" tone="primary" value="10%" digits={4} />
        </div>
        <div style={{ display: "flex", flexDirection: "column", alignItems: "center", gap: 6 }}>
          <Knob size="md" tone="neutral" rotation={0} label="TAX · C" />
          <SegRead size="sm" tone="neutral" value="10%" digits={4} />
        </div>
        <div style={{ display: "flex", flexDirection: "column", alignItems: "center", gap: 6 }}>
          <Knob size="md" tone="alert" rotation={30} label="TAX · I" />
          <SegRead size="sm" tone="alert" value="10%" digits={4} />
        </div>
      </div>
    </section>
  );
}

// 3. PAUSE
function PanelPause() {
  return (
    <section data-panel-slug="pause" className="rack-chrome rack-chrome--bottom-rivets" style={{ width: 480, padding: "32px 36px" }}>
      <div className="rivet-bl" /><div className="rivet-br" />
      <PanelStrip slug="pause" />
      <div className="nameplate" style={{ textAlign: "center", marginBottom: 24 }}>STANDBY</div>
      <div className="silkscreen" style={{ textAlign: "center", marginBottom: 28 }}>SIGNAL HELD · CLOCK PAUSED</div>
      <div data-slot="actions" data-accepts={accepts5} style={{ flexDirection: "column", gap: 14 }}>
        <ILed size="lg" tone="primary" lit>RESUME</ILed>
        <ILed size="lg" tone="neutral">SAVE STATE</ILed>
        <ILed size="lg" tone="neutral">SETTINGS</ILed>
        <ILed size="lg" tone="alert">EXIT TO MENU</ILed>
      </div>
    </section>
  );
}

// 4. SETTINGS
function PanelSettings() {
  return (
    <section data-panel-slug="settings" className="rack-chrome rack-chrome--bottom-rivets" style={{ width: 640, padding: "28px 32px" }}>
      <div className="rivet-bl" /><div className="rivet-br" />
      <PanelStrip slug="settings" />
      <div className="nameplate" style={{ fontSize: "var(--type-h3)", marginBottom: 20 }}>OPTIONS · MAINTENANCE</div>

      <div data-slot="audio-rack" data-accepts={accepts5} style={{ flexDirection: "column", gap: 18, alignItems: "stretch" }}>
        <div style={{ display: "grid", gridTemplateColumns: "100px 1fr 80px", gap: 14, alignItems: "center" }}>
          <div className="silkscreen">SFX VOL</div>
          <Fader size="lg" tone="primary" value={70} orientation="horizontal" />
          <SegRead size="sm" tone="primary" value="70" digits={3} />
        </div>
        <div style={{ display: "grid", gridTemplateColumns: "100px 1fr 80px", gap: 14, alignItems: "center" }}>
          <div className="silkscreen">MUSIC VOL</div>
          <Fader size="lg" tone="primary" value={45} orientation="horizontal" />
          <SegRead size="sm" tone="primary" value="45" digits={3} />
        </div>
        <div style={{ display: "grid", gridTemplateColumns: "100px 1fr 80px", gap: 14, alignItems: "center" }}>
          <div className="silkscreen">UI VOL</div>
          <Fader size="lg" tone="neutral" value={60} orientation="horizontal" />
          <SegRead size="sm" tone="neutral" value="60" digits={3} />
        </div>
      </div>

      <div className="rail" />

      <div data-slot="toggles" data-accepts={accepts5} style={{ gap: 10, flexWrap: "wrap" }}>
        <ILed size="md" tone="alert" lit>MUTE SFX</ILed>
        <ILed size="md" tone="primary" lit>FULLSCREEN</ILed>
        <ILed size="md" tone="neutral">VSYNC</ILed>
        <ILed size="md" tone="neutral" lit>TOOLTIPS</ILed>
      </div>

      <div className="rail" />

      <div data-slot="footer" data-accepts={accepts5} style={{ justifyContent: "flex-end", gap: 10 }}>
        <ILed size="md" tone="alert">DISCARD</ILed>
        <ILed size="md" tone="primary" lit>APPLY</ILed>
        <ILed size="md" tone="neutral">BACK</ILed>
      </div>
    </section>
  );
}

// 5. SAVE-LOAD
function PanelSaveLoad() {
  const slots = [
    { id: "01", name: "MERIDIAN", year: "0042", lit: true, tone: "primary" },
    { id: "02", name: "VESTRA",   year: "0019", lit: false, tone: "neutral" },
    { id: "03", name: "AURORIN",  year: "0007", lit: false, tone: "neutral" },
    { id: "04", name: "—",        year: "----", lit: false, tone: "neutral", empty: true },
  ];
  return (
    <section data-panel-slug="save-load" className="rack-chrome rack-chrome--bottom-rivets" style={{ width: 720, padding: "28px 32px" }}>
      <div className="rivet-bl" /><div className="rivet-br" />
      <PanelStrip slug="save-load" />
      <div className="nameplate" style={{ fontSize: "var(--type-h3)", marginBottom: 20 }}>TAPE BAY · 04 SLOTS</div>

      <div data-slot="slots" data-accepts={accepts5} style={{ flexDirection: "column", gap: 10 }}>
        {slots.map(s => (
          <div key={s.id} style={{
            display: "grid", gridTemplateColumns: "auto 60px 1fr auto auto", gap: 14, alignItems: "center",
            padding: "10px 14px",
            background: "var(--palette-chassis-graphite-1)",
            border: "1px solid var(--palette-chassis-graphite-3)",
            borderRadius: "var(--frame-radius-rack-sm)",
            boxShadow: "var(--frame-shadow-recessed)"
          }}>
            <ILed size="sm" tone={s.tone} lit={s.lit}>{s.id}</ILed>
            <SegRead size="sm" tone={s.empty ? "neutral" : "primary"} state={s.empty ? "disabled" : null} value={s.id} digits={3} />
            <div>
              <div className="nameplate" style={{ fontSize: "var(--type-body)", color: s.empty ? "var(--palette-chassis-graphite-4)" : "var(--palette-silkscreen-engraved)" }}>{s.name}</div>
              <div className="silkscreen">YR {s.year} · POP 12,400</div>
            </div>
            <ILed size="sm" tone="primary">LOAD</ILed>
            <ILed size="sm" tone="alert">ERASE</ILed>
          </div>
        ))}
      </div>

      <div className="rail" />
      <div data-slot="footer" data-accepts={accepts5} style={{ justifyContent: "space-between" }}>
        <ILed size="md" tone="primary" lit>NEW SAVE</ILed>
        <ILed size="md" tone="neutral">BACK</ILed>
      </div>
    </section>
  );
}

// 6. NEW GAME (MainMenu)
function PanelNewGame() {
  return (
    <section data-panel-slug="new-game" className="rack-chrome rack-chrome--bottom-rivets" style={{ width: 520, padding: "36px 40px" }}>
      <div className="rivet-bl" /><div className="rivet-br" />
      <PanelStrip slug="new-game" />
      <div style={{ textAlign: "center", marginBottom: 28 }}>
        <div className="nameplate" style={{ fontSize: "var(--type-h2)", letterSpacing: "0.08em" }}>TERRITORY</div>
        <div className="silkscreen" style={{ marginTop: 6 }}>STUDIO RACK · MK II</div>
      </div>

      <div data-slot="power-stack" data-accepts={accepts5} style={{ flexDirection: "column", gap: 12 }}>
        <ILed size="lg" tone="primary" lit>CONTINUE</ILed>
        <ILed size="lg" tone="primary">NEW CITY</ILed>
        <ILed size="lg" tone="neutral">LOAD CITY</ILed>
        <ILed size="lg" tone="neutral">OPTIONS</ILed>
        <ILed size="lg" tone="alert">QUIT</ILed>
      </div>

      <div className="rail" />
      <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center" }}>
        <div className="silkscreen">PWR</div>
        <VuMeter size="sm" segments={10} lit={10} tone="primary" />
        <div className="silkscreen">RDY</div>
      </div>
    </section>
  );
}

// 7. TOOLTIP
function PanelTooltip() {
  return (
    <section data-panel-slug="tooltip" className="rack-chrome" style={{ width: 360, padding: "14px 18px", "--frame-rivet-size": "4px" }}>
      <div style={{ display: "flex", alignItems: "center", gap: 10, marginBottom: 8 }}>
        <span className="iled__lamp" style={{ background: "var(--illumination-amber-on)", boxShadow: "var(--illumination-amber-halo)" }} />
        <div className="nameplate" style={{ fontSize: "var(--type-body)", letterSpacing: "0.04em" }}>HEAVY INDUSTRY</div>
      </div>
      <div data-slot="body" data-accepts={accepts5} style={{ flexDirection: "column", alignItems: "stretch", gap: 8 }}>
        <div className="silkscreen" style={{ color: "var(--palette-silkscreen-primary)", letterSpacing: "0.04em", textTransform: "none", fontSize: "var(--type-caption)" }}>
          Generates 240 jobs. Drains 18 MW. Pollutes adjacent residential.
        </div>
        <div style={{ display: "grid", gridTemplateColumns: "1fr 1fr", gap: 8 }}>
          <SegRead size="sm" tone="primary" value="$8,400" digits={6} label="COST" />
          <SegRead size="sm" tone="alert" value="-3" digits={3} label="HAPPY" />
        </div>
        <VuMeter size="sm" tone="alert" segments={14} lit={11} label="POLLUTION" />
      </div>
    </section>
  );
}

// 8. TOOLBAR (BuildingSelector)
function PanelToolbar() {
  const tools = [
    { id: "RES", lit: true,  tone: "primary", label: "RES" },
    { id: "COM", lit: false, tone: "neutral", label: "COM" },
    { id: "IND", lit: false, tone: "alert",   label: "IND" },
    { id: "BLD", lit: false, tone: "neutral", label: "BLD" },
    { id: "ROD", lit: false, tone: "neutral", label: "ROD" },
    { id: "PRK", lit: false, tone: "primary", label: "PRK" },
    { id: "PWR", lit: false, tone: "alert",   label: "PWR" },
    { id: "WTR", lit: false, tone: "neutral", label: "WTR" },
    { id: "DEM", lit: false, tone: "alert",   label: "DEM" },
  ];
  return (
    <section data-panel-slug="toolbar" className="rack-chrome rack-chrome--bottom-rivets" style={{ width: 380, padding: "20px 22px" }}>
      <div className="rivet-bl" /><div className="rivet-br" />
      <PanelStrip slug="toolbar" />
      <div className="silkscreen" style={{ marginBottom: 12 }}>PATCHBAY · 3×3 + AUX</div>

      <div data-slot="tool-grid" data-accepts={accepts5} style={{ display: "grid", gridTemplateColumns: "repeat(3, 1fr)", gap: 10 }}>
        {tools.map(t => (
          <div key={t.id} style={{
            aspectRatio: "1",
            position: "relative",
            background: "linear-gradient(180deg, var(--palette-chassis-graphite-2), var(--palette-chassis-graphite-0))",
            borderRadius: "var(--frame-radius-rack-sm)",
            boxShadow: "var(--frame-shadow-recessed)",
            border: t.lit ? "1px solid var(--palette-led-amber-mid)" : "1px solid var(--palette-chassis-graphite-3)",
            display: "flex", alignItems: "center", justifyContent: "center", flexDirection: "column", gap: 4
          }}>
            <span className="iled__lamp" style={{
              position: "absolute", top: 6, right: 6,
              background: t.lit ? `var(--illumination-${t.tone === "primary" ? "amber" : t.tone === "alert" ? "ruby" : "cyan"}-on)` : `var(--illumination-${t.tone === "primary" ? "amber" : t.tone === "alert" ? "ruby" : "cyan"}-off)`,
              boxShadow: t.lit ? `var(--illumination-${t.tone === "primary" ? "amber" : t.tone === "alert" ? "ruby" : "cyan"}-halo)` : "inset 0 1px 2px rgba(0,0,0,0.7)"
            }} />
            <div style={{
              width: 28, height: 28, borderRadius: 4,
              background: "repeating-linear-gradient(45deg, var(--palette-chassis-graphite-3) 0 3px, var(--palette-chassis-graphite-1) 3px 6px)",
              border: "1px solid var(--palette-chassis-graphite-0)"
            }} />
            <div className="silkscreen" style={{ fontSize: "0.55rem" }}>{t.label}</div>
          </div>
        ))}
      </div>

      <div className="rail" />
      <div data-slot="subtype-row" data-accepts={accepts5} style={{ gap: 8 }}>
        <ILed size="sm" tone="primary" lit>MED · 3</ILed>
        <ILed size="sm" tone="primary">HVY · 2</ILed>
      </div>
    </section>
  );
}

// 9. CITY STATS
function PanelCityStats() {
  const rows = [
    { label: "POPULATION",      value: "12,408",      tone: "primary",  vu: null },
    { label: "MONEY",           value: "$20,000",     tone: "primary",  vu: null, delta: "+$0" },
    { label: "HAPPINESS",       value: "50/100",      tone: "neutral",  vu: { lit: 8, segments: 16, tone: "primary" } },
    { label: "POWER OUT",       value: "84 MW",       tone: "primary",  vu: { lit: 11, segments: 16, tone: "primary" } },
    { label: "POWER CONS",      value: "62 MW",       tone: "neutral",  vu: { lit: 8,  segments: 16, tone: "neutral" } },
    { label: "UNEMPLOYMENT",    value: "4.2%",        tone: "neutral",  vu: null },
    { label: "TOTAL JOBS",      value: "3,120",       tone: "neutral",  vu: null },
    { label: "RESID. DEMAND",   value: "OVERSUPPLY",  tone: "alert",    vu: { lit: 2,  segments: 16, tone: "alert" } },
    { label: "COMM. DEMAND",    value: "BALANCED",    tone: "neutral",  vu: { lit: 8,  segments: 16, tone: "neutral" } },
    { label: "INDUS. DEMAND",   value: "VERY HIGH",   tone: "primary",  vu: { lit: 15, segments: 16, tone: "primary" } },
    { label: "WATER OUT",       value: "1,200 KL",    tone: "primary",  vu: null },
    { label: "WATER CONS",      value: "  840 KL",    tone: "neutral",  vu: null },
  ];
  return (
    <section data-panel-slug="city-stats" className="rack-chrome rack-chrome--bottom-rivets" style={{ width: 560, padding: "24px 28px" }}>
      <div className="rivet-bl" /><div className="rivet-br" />
      <PanelStrip slug="city-stats" />
      <div className="nameplate" style={{ fontSize: "var(--type-h3)", marginBottom: 16 }}>CHANNEL STRIP · CITY STATISTICS</div>

      <div data-slot="row-list" data-accepts={accepts5} style={{ flexDirection: "column", gap: 6 }}>
        {rows.map((r, i) => (
          <div key={i} style={{
            display: "grid",
            gridTemplateColumns: "auto 1fr auto auto",
            gap: 12,
            alignItems: "center",
            padding: "6px 10px",
            background: "var(--palette-chassis-graphite-1)",
            borderLeft: `2px solid var(--palette-led-${r.tone === "primary" ? "amber" : r.tone === "alert" ? "ruby" : "cyan"}-low)`,
            borderRadius: "var(--frame-radius-rack-xs)"
          }}>
            <span className="iled__lamp" style={{
              background: `var(--illumination-${r.tone === "primary" ? "amber" : r.tone === "alert" ? "ruby" : "cyan"}-on)`,
              boxShadow: `var(--illumination-${r.tone === "primary" ? "amber" : r.tone === "alert" ? "ruby" : "cyan"}-halo)`
            }} />
            <div className="silkscreen" style={{ color: "var(--palette-silkscreen-primary)" }}>{r.label}</div>
            {r.vu ? <VuMeter size="sm" tone={r.vu.tone} segments={r.vu.segments} lit={r.vu.lit} /> : <div style={{ width: 140 }} />}
            <div className={cx("segread", "segread--sm", `segread--tone-${r.tone}`)} data-ghost="888888888" style={{ minWidth: 110, justifyContent: "flex-end" }}>
              {r.value}
            </div>
          </div>
        ))}
      </div>
    </section>
  );
}

// 10. ONBOARDING
function PanelOnboarding() {
  const steps = [
    { n: "01", label: "POWER ON", lit: true, done: true },
    { n: "02", label: "ZONE PATCH", lit: true, done: false },
    { n: "03", label: "FIRST BUILD", lit: false, done: false },
    { n: "04", label: "TAX BUS", lit: false, done: false },
    { n: "05", label: "GO LIVE", lit: false, done: false },
  ];
  return (
    <section data-panel-slug="onboarding" className="rack-chrome rack-chrome--bottom-rivets" style={{ width: 720, padding: "28px 32px" }}>
      <div className="rivet-bl" /><div className="rivet-br" />
      <PanelStrip slug="onboarding" />
      <div className="nameplate" style={{ fontSize: "var(--type-h3)", marginBottom: 8 }}>FIRST POWER-ON</div>
      <div className="silkscreen" style={{ marginBottom: 24 }}>STEP 02 · PATCH A ZONE INTO THE GRID</div>

      <div data-slot="step-rack" data-accepts={accepts5} style={{ gap: 8, marginBottom: 24 }}>
        {steps.map((s, i) => (
          <div key={s.n} style={{ flex: 1, display: "flex", flexDirection: "column", alignItems: "center", gap: 6 }}>
            <ILed size="sm" tone={s.done ? "primary" : s.lit ? "neutral" : "neutral"} lit={s.lit || s.done}>{s.n}</ILed>
            <div className="silkscreen" style={{ fontSize: "0.55rem", textAlign: "center" }}>{s.label}</div>
          </div>
        ))}
      </div>

      <div data-slot="meter-bay" data-accepts={accepts5} style={{ display: "grid", gridTemplateColumns: "1fr 1fr", gap: 16, marginBottom: 16 }}>
        <VuMeter size="md" tone="primary" segments={16} lit={6} label="PROGRESS" />
        <SegRead size="md" tone="primary" value="02 / 05" digits={7} label="STEP" />
      </div>

      <div className="rail" />
      <div data-slot="actions" data-accepts={accepts5} style={{ justifyContent: "space-between" }}>
        <ILed size="md" tone="neutral">SKIP TUTORIAL</ILed>
        <div style={{ display: "flex", gap: 8 }}>
          <ILed size="md" tone="neutral">PREV</ILed>
          <ILed size="md" tone="primary" lit>NEXT</ILed>
        </div>
      </div>
    </section>
  );
}

Object.assign(window, {
  PanelHudBar, PanelInfoPanel, PanelPause, PanelSettings, PanelSaveLoad,
  PanelNewGame, PanelTooltip, PanelToolbar, PanelCityStats, PanelOnboarding
});
