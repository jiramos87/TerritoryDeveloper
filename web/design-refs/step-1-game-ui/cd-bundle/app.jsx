function App() {
  return (
    <DesignCanvas title="Territory · Studio Rack Game UI MVP" subtitle="audio-rack faceplates · 5 archetypes · 10 panels · all states">

      <DCSection id="overview" title="01 · System Overview">
        <DCArtboard id="legend" label="Studio Rack — Visual Language" width={1280} height={420}>
          <div className="studio-rack" style={{ width: "100%", height: "100%", padding: 32, boxSizing: "border-box", display: "grid", gridTemplateColumns: "1.2fr 1fr", gap: 24 }}>
            <div className="rack-chrome" style={{ padding: 28 }}>
              <div className="panel-strip">
                <span className="panel-strip__title">SYSTEM · LEGEND</span>
                <span className="panel-strip__id">SYS · 001</span>
              </div>
              <div className="nameplate" style={{ fontSize: "var(--type-h3)", marginBottom: 12 }}>FIVE ARCHETYPES</div>
              <div style={{ display: "grid", gridTemplateColumns: "repeat(5, 1fr)", gap: 16, alignItems: "end", marginTop: 16 }}>
                <div style={{ display: "flex", flexDirection: "column", alignItems: "center", gap: 8 }}>
                  <Knob size="md" tone="primary" rotation={-30} />
                  <div className="silkscreen">KNOB</div>
                </div>
                <div style={{ display: "flex", flexDirection: "column", alignItems: "center", gap: 8 }}>
                  <Fader size="sm" tone="primary" value={55} />
                  <div className="silkscreen">FADER</div>
                </div>
                <div style={{ display: "flex", flexDirection: "column", alignItems: "center", gap: 8 }}>
                  <VuMeter size="sm" tone="primary" segments={12} lit={8} />
                  <div className="silkscreen">VU METER</div>
                </div>
                <div style={{ display: "flex", flexDirection: "column", alignItems: "center", gap: 8 }}>
                  <ILed size="md" tone="primary" lit>LIVE</ILed>
                  <div className="silkscreen">ILLUM. BTN</div>
                </div>
                <div style={{ display: "flex", flexDirection: "column", alignItems: "center", gap: 8 }}>
                  <SegRead size="sm" tone="primary" value="2,025" digits={5} />
                  <div className="silkscreen">SEG. READ</div>
                </div>
              </div>
            </div>
            <div className="rack-chrome" style={{ padding: 28 }}>
              <div className="panel-strip">
                <span className="panel-strip__title">PALETTE · ILLUMINATION</span>
                <span className="panel-strip__id">SYS · 002</span>
              </div>
              <div style={{ display: "grid", gridTemplateColumns: "repeat(4, 1fr)", gap: 12, marginTop: 12 }}>
                {[
                  ["AMBER", "amber"], ["CYAN", "cyan"], ["GRASS", "grass"], ["RUBY", "ruby"]
                ].map(([name, slug]) => (
                  <div key={slug} style={{ display: "flex", flexDirection: "column", alignItems: "center", gap: 6, padding: 10, background: "var(--palette-chassis-graphite-1)", borderRadius: 4 }}>
                    <div style={{ display: "flex", gap: 4, alignItems: "center" }}>
                      <span className="iled__lamp" style={{ background: `var(--illumination-${slug}-off)` }} />
                      <span className="iled__lamp" style={{ background: `var(--illumination-${slug}-on)`, boxShadow: `var(--illumination-${slug}-halo)` }} />
                    </div>
                    <div className="silkscreen" style={{ fontSize: "0.55rem" }}>{name}</div>
                  </div>
                ))}
              </div>
              <div className="silkscreen" style={{ marginTop: 16, marginBottom: 6 }}>FONT FACES</div>
              <div style={{ display: "flex", gap: 16, flexWrap: "wrap" }}>
                <div className="nameplate" style={{ fontSize: "var(--type-body-lg)" }}>FACEPLATE</div>
                <div className="silkscreen">SILKSCREEN</div>
                <div style={{ fontFamily: "var(--font-face-segmented)", color: "var(--palette-led-amber-hot)", textShadow: "0 0 8px var(--palette-led-amber-low)" }}>SEG · 88:88</div>
              </div>
            </div>
          </div>
        </DCArtboard>
      </DCSection>

      <DCSection id="panels" title="02 · Panels — All 10 Surfaces">
        <DCArtboard id="hud-bar" label="hud-bar" width={1340} height={220}>
          <div className="studio-rack" style={{ padding: 24, height: "100%", boxSizing: "border-box" }}><PanelHudBar /></div>
        </DCArtboard>
        <DCArtboard id="info-panel" label="info-panel · budget" width={780} height={520}>
          <div className="studio-rack" style={{ padding: 24, height: "100%", boxSizing: "border-box" }}><PanelInfoPanel /></div>
        </DCArtboard>
        <DCArtboard id="city-stats" label="city-stats" width={620} height={760}>
          <div className="studio-rack" style={{ padding: 24, height: "100%", boxSizing: "border-box" }}><PanelCityStats /></div>
        </DCArtboard>
        <DCArtboard id="toolbar" label="toolbar · patchbay" width={440} height={560}>
          <div className="studio-rack" style={{ padding: 24, height: "100%", boxSizing: "border-box" }}><PanelToolbar /></div>
        </DCArtboard>
        <DCArtboard id="settings" label="settings" width={700} height={520}>
          <div className="studio-rack" style={{ padding: 24, height: "100%", boxSizing: "border-box" }}><PanelSettings /></div>
        </DCArtboard>
        <DCArtboard id="new-game" label="new-game · mainmenu" width={580} height={620}>
          <div className="studio-rack" style={{ padding: 24, height: "100%", boxSizing: "border-box" }}><PanelNewGame /></div>
        </DCArtboard>
        <DCArtboard id="pause" label="pause · standby" width={540} height={500}>
          <div className="studio-rack" style={{ padding: 24, height: "100%", boxSizing: "border-box" }}><PanelPause /></div>
        </DCArtboard>
        <DCArtboard id="save-load" label="save-load · tape bay" width={780} height={500}>
          <div className="studio-rack" style={{ padding: 24, height: "100%", boxSizing: "border-box" }}><PanelSaveLoad /></div>
        </DCArtboard>
        <DCArtboard id="tooltip" label="tooltip" width={420} height={300}>
          <div className="studio-rack" style={{ padding: 24, height: "100%", boxSizing: "border-box" }}><PanelTooltip /></div>
        </DCArtboard>
        <DCArtboard id="onboarding" label="onboarding · power-on" width={780} height={460}>
          <div className="studio-rack" style={{ padding: 24, height: "100%", boxSizing: "border-box" }}><PanelOnboarding /></div>
        </DCArtboard>
      </DCSection>

      <DCSection id="archetypes" title="03 · Archetype × State × Variant">
        <DCArtboard id="arch-knob" label="knob — states + sizes + tones" width={1340} height={580}>
          <div className="studio-rack" style={{ padding: 24, height: "100%", boxSizing: "border-box" }}><BoardKnob /></div>
        </DCArtboard>
        <DCArtboard id="arch-fader" label="fader — states + sizes + tones" width={1340} height={620}>
          <div className="studio-rack" style={{ padding: 24, height: "100%", boxSizing: "border-box" }}><BoardFader /></div>
        </DCArtboard>
        <DCArtboard id="arch-vu" label="vu-meter — states + sizes + tones + orientation" width={1340} height={780}>
          <div className="studio-rack" style={{ padding: 24, height: "100%", boxSizing: "border-box" }}><BoardVu /></div>
        </DCArtboard>
        <DCArtboard id="arch-iled" label="illuminated-button — lit/unlit × states × sizes × tones" width={1340} height={760}>
          <div className="studio-rack" style={{ padding: 24, height: "100%", boxSizing: "border-box" }}><BoardILed /></div>
        </DCArtboard>
        <DCArtboard id="arch-segread" label="segmented-readout — states + sizes + tones" width={1340} height={580}>
          <div className="studio-rack" style={{ padding: 24, height: "100%", boxSizing: "border-box" }}><BoardSegRead /></div>
        </DCArtboard>
      </DCSection>

    </DesignCanvas>
  );
}

ReactDOM.createRoot(document.getElementById("root")).render(<App />);
