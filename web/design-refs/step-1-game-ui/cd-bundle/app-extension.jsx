function App2() {
  const allIcons = [
    "select","road","zone-residential","zone-commercial","zone-industrial","bulldoze",
    "power","water","services","landmark",
    "desirability","pollution","land-value","heat",
    "pause","play","fast-forward","step",
    "alert","info","success","autosave"
  ];

  function StateCell2({ label, w = 150, h = 130, children }) {
    return (
      <div style={{ display: "flex", flexDirection: "column", alignItems: "center", justifyContent: "center", gap: 10, padding: 12, minWidth: w, minHeight: h, background: "var(--palette-chassis-graphite-1)", border: "1px solid var(--palette-chassis-graphite-3)", borderRadius: "var(--frame-radius-rack-xs)" }}>
        <div style={{ flex: 1, display: "flex", alignItems: "center", justifyContent: "center" }}>{children}</div>
        <div style={{ fontFamily: "var(--font-face-segmented)", fontSize: "var(--type-mono-meta)", color: "var(--palette-led-cyan-mid)", letterSpacing: "0.18em", textTransform: "uppercase" }}>{label}</div>
      </div>
    );
  }

  function ArchBoard2({ slug, title, children, width = 1340 }) {
    return (
      <section className="rack-chrome rack-chrome--bottom-rivets" style={{ width, padding: "24px 28px" }}>
        <div className="rivet-bl"/><div className="rivet-br"/>
        <div className="panel-strip"><span className="panel-strip__title">archetype · {slug}</span><span className="panel-strip__id">ARCH · {slug.toUpperCase()}</span></div>
        <div className="nameplate" style={{ fontSize: "var(--type-h3)", marginBottom: 16 }}>{title}</div>
        {children}
      </section>
    );
  }

  function GridRow2({ label, children }) {
    return (
      <div style={{ display: "grid", gridTemplateColumns: "140px 1fr", gap: 16, alignItems: "center", marginBottom: 12 }}>
        <div className="silkscreen" style={{ textAlign: "right" }}>{label}</div>
        <div style={{ display: "flex", gap: 10, flexWrap: "wrap" }}>{children}</div>
      </div>
    );
  }

  const states = ["default", "hover", "focus", "pressed", "disabled"];

  return (
    <DesignCanvas title="Territory · v2 Extension" subtitle="3 new archetypes · 5 game-domain panels · 22-icon sprite">

      <DCSection id="ext-archetypes" title="04 · Extension Archetypes — Oscilloscope · Detent-Ring · Led">

        <DCArtboard id="osc-board" label="oscilloscope — states + sizes + tones" width={1340} height={780}>
          <div className="studio-rack" style={{ padding: 24, height: "100%", boxSizing: "border-box" }}>
            <ArchBoard2 slug="oscilloscope" title="CRT · OSCILLOSCOPE">
              <GridRow2 label="STATES · MD · PRIMARY">
                {states.map(s => <StateCell2 key={s} label={s} w={220} h={150}><Osc size="md" tone="primary" state={s === "default" ? null : s}/></StateCell2>)}
              </GridRow2>
              <GridRow2 label="SIZE · TRACE SAMPLES">
                <StateCell2 label="sm · sine" w={130} h={100}><Osc size="sm" tone="primary"/></StateCell2>
                <StateCell2 label="md · pollution" w={220} h={150}><Osc size="md" tone="primary"/></StateCell2>
                <StateCell2 label="lg · economy" w={350} h={220}><Osc size="lg" tone="primary"/></StateCell2>
              </GridRow2>
              <GridRow2 label="TONE · MD">
                <StateCell2 label="primary · cyan" w={220} h={150}><Osc size="md" tone="primary"/></StateCell2>
                <StateCell2 label="neutral · cream" w={220} h={150}><Osc size="md" tone="neutral"/></StateCell2>
                <StateCell2 label="alert · ruby" w={220} h={150}><Osc size="md" tone="alert"/></StateCell2>
              </GridRow2>
            </ArchBoard2>
          </div>
        </DCArtboard>

        <DCArtboard id="detent-board" label="detent-ring — states + sizes + tones" width={1340} height={760}>
          <div className="studio-rack" style={{ padding: 24, height: "100%", boxSizing: "border-box" }}>
            <ArchBoard2 slug="detent-ring" title="ROTARY SELECTOR · DETENT RING">
              <GridRow2 label="STATES · MD · PRIMARY">
                {states.map(s => <StateCell2 key={s} label={s} w={140} h={140}><DetentRing size="md" tone="primary" detents={12} current={7} state={s === "default" ? null : s}/></StateCell2>)}
              </GridRow2>
              <GridRow2 label="SIZE · DETENTS">
                <StateCell2 label="sm · 8 · pos 3" w={120} h={120}><DetentRing size="sm" tone="primary" detents={8} current={3}/></StateCell2>
                <StateCell2 label="md · 12 · pos 7" w={140} h={140}><DetentRing size="md" tone="primary" detents={12} current={7}/></StateCell2>
                <StateCell2 label="lg · 16 · pos 11" w={180} h={180}><DetentRing size="lg" tone="primary" detents={16} current={11}/></StateCell2>
              </GridRow2>
              <GridRow2 label="TONE · MD">
                <StateCell2 label="primary" w={140} h={140}><DetentRing size="md" tone="primary" detents={12} current={4}/></StateCell2>
                <StateCell2 label="neutral" w={140} h={140}><DetentRing size="md" tone="neutral" detents={12} current={6}/></StateCell2>
                <StateCell2 label="alert" w={140} h={140}><DetentRing size="md" tone="alert" detents={12} current={10}/></StateCell2>
              </GridRow2>
            </ArchBoard2>
          </div>
        </DCArtboard>

        <DCArtboard id="led-board" label="led — states + sizes + tones × lit/unlit" width={1340} height={620}>
          <div className="studio-rack" style={{ padding: 24, height: "100%", boxSizing: "border-box" }}>
            <ArchBoard2 slug="led" title="STATUS PIP · LED">
              <GridRow2 label="STATES · LIT · MD · PRIMARY">
                {states.map(s => <StateCell2 key={s} label={s} w={100} h={80}><Led size="md" tone="primary" lit state={s === "default" ? null : s}/></StateCell2>)}
              </GridRow2>
              <GridRow2 label="SIZE · LIT">
                <StateCell2 label="sm · 8px"><Led size="sm" tone="primary" lit/></StateCell2>
                <StateCell2 label="md · 12px"><Led size="md" tone="primary" lit/></StateCell2>
                <StateCell2 label="lg · 18px"><Led size="lg" tone="primary" lit/></StateCell2>
              </GridRow2>
              <GridRow2 label="TONE · LIT / UNLIT">
                <StateCell2 label="primary · cyan"><div style={{display:"flex",gap:10}}><Led tone="primary"/><Led tone="primary" lit/></div></StateCell2>
                <StateCell2 label="neutral · cream"><div style={{display:"flex",gap:10}}><Led tone="neutral"/><Led tone="neutral" lit/></div></StateCell2>
                <StateCell2 label="alert · ruby"><div style={{display:"flex",gap:10}}><Led tone="alert"/><Led tone="alert" lit/></div></StateCell2>
                <StateCell2 label="success · grass"><div style={{display:"flex",gap:10}}><Led tone="success"/><Led tone="success" lit/></div></StateCell2>
              </GridRow2>
            </ArchBoard2>
          </div>
        </DCArtboard>

      </DCSection>

      <DCSection id="ext-panels" title="05 · Game-Domain Panels">
        <DCArtboard id="building-info" label="building-info · Power Plant" width={580} height={680}>
          <div className="studio-rack" style={{ padding: 24, height: "100%", boxSizing: "border-box" }}><PanelBuildingInfo/></div>
        </DCArtboard>
        <DCArtboard id="zone-overlay" label="zone-overlay · heatmap matrix" width={580} height={580}>
          <div className="studio-rack" style={{ padding: 24, height: "100%", boxSizing: "border-box" }}><PanelZoneOverlay/></div>
        </DCArtboard>
        <DCArtboard id="time-controls" label="time-controls · 2024-03-15" width={780} height={260}>
          <div className="studio-rack" style={{ padding: 24, height: "100%", boxSizing: "border-box" }}><PanelTimeControls/></div>
        </DCArtboard>
        <DCArtboard id="alerts-panel" label="alerts-panel · annunciator feed" width={680} height={620}>
          <div className="studio-rack" style={{ padding: 24, height: "100%", boxSizing: "border-box" }}><PanelAlerts/></div>
        </DCArtboard>
        <DCArtboard id="mini-map" label="mini-map · St/Zn/Fr/De/Ct" width={460} height={440}>
          <div className="studio-rack" style={{ padding: 24, height: "100%", boxSizing: "border-box" }}><PanelMiniMap/></div>
        </DCArtboard>
      </DCSection>

      <DCSection id="ext-icons" title="06 · Icon Sprite — 22 Icons × 3 Sizes">
        <DCArtboard id="icon-grid" label="icon ring — 16 / 24 / 32 px" width={1340} height={680}>
          <div className="studio-rack" style={{ padding: 24, height: "100%", boxSizing: "border-box" }}>
            <section className="rack-chrome rack-chrome--bottom-rivets" style={{ padding: "24px 28px" }}>
              <div className="rivet-bl"/><div className="rivet-br"/>
              <div className="panel-strip"><span className="panel-strip__title">sprite · icons</span><span className="panel-strip__id">SPRITE · 22</span></div>
              <div className="nameplate" style={{ fontSize: "var(--type-h3)", marginBottom: 16 }}>STROKE · CURRENTCOLOR · 24×24</div>
              <div style={{ display: "grid", gridTemplateColumns: "repeat(6, 1fr)", gap: 10 }}>
                {allIcons.map(slug => (
                  <div key={slug} style={{ display: "flex", flexDirection: "column", alignItems: "center", gap: 8, padding: 14, background: "var(--palette-chassis-graphite-1)", border: "1px solid var(--palette-chassis-graphite-3)", borderRadius: "var(--frame-radius-rack-xs)" }}>
                    <div style={{ display: "flex", gap: 12, alignItems: "center", color: "var(--palette-led-amber-hot)" }}>
                      <Icon slug={slug} size={16}/>
                      <Icon slug={slug} size={24}/>
                      <Icon slug={slug} size={32}/>
                    </div>
                    <div className="silkscreen" style={{ fontSize: "0.55rem" }}>{slug}</div>
                  </div>
                ))}
              </div>
            </section>
          </div>
        </DCArtboard>
      </DCSection>

    </DesignCanvas>
  );
}

ReactDOM.createRoot(document.getElementById("root2")).render(<App2/>);
