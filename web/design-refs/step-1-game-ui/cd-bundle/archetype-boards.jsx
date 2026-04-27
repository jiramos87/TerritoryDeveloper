// Archetype Boards — every archetype × every state × every variant
function StateCell({ label, children, w = 120 }) {
  return (
    <div style={{
      display: "flex", flexDirection: "column", alignItems: "center", justifyContent: "center",
      gap: 10, padding: 12, minWidth: w, minHeight: 110,
      background: "var(--palette-chassis-graphite-1)",
      border: "1px solid var(--palette-chassis-graphite-3)",
      borderRadius: "var(--frame-radius-rack-xs)"
    }}>
      <div style={{ flex: 1, display: "flex", alignItems: "center", justifyContent: "center" }}>
        {children}
      </div>
      <div style={{
        fontFamily: "var(--font-face-segmented)",
        fontSize: "var(--type-mono-meta)",
        color: "var(--palette-led-cyan-mid)",
        letterSpacing: "0.18em", textTransform: "uppercase"
      }}>{label}</div>
    </div>
  );
}

function ArchetypeBoard({ slug, title, children, width = 1280 }) {
  return (
    <section className="rack-chrome rack-chrome--bottom-rivets" style={{ width, padding: "24px 28px" }}>
      <div className="rivet-bl" /><div className="rivet-br" />
      <div className="panel-strip">
        <span className="panel-strip__title">archetype · {slug}</span>
        <span className="panel-strip__id">ARCH · {slug.toUpperCase()}</span>
      </div>
      <div className="nameplate" style={{ fontSize: "var(--type-h3)", marginBottom: 16 }}>{title}</div>
      {children}
    </section>
  );
}

function GridRow({ label, children }) {
  return (
    <div style={{ display: "grid", gridTemplateColumns: "120px 1fr", gap: 16, alignItems: "center", marginBottom: 12 }}>
      <div className="silkscreen" style={{ textAlign: "right" }}>{label}</div>
      <div style={{ display: "flex", gap: 10, flexWrap: "wrap" }}>{children}</div>
    </div>
  );
}

function BoardKnob() {
  const states = ["default", "hover", "focus", "pressed", "disabled"];
  return (
    <ArchetypeBoard slug="knob" title="ROTARY · DETENT KNOB">
      <GridRow label="STATES · MD · PRIMARY">
        {states.map(s => (
          <StateCell key={s} label={s}>
            <Knob size="md" tone="primary" rotation={-30} state={s === "default" ? null : s} />
          </StateCell>
        ))}
      </GridRow>
      <GridRow label="SIZE · PRIMARY">
        <StateCell label="sm"><Knob size="sm" tone="primary" rotation={-60} /></StateCell>
        <StateCell label="md"><Knob size="md" tone="primary" rotation={-30} /></StateCell>
        <StateCell label="lg"><Knob size="lg" tone="primary" rotation={20} /></StateCell>
      </GridRow>
      <GridRow label="TONE · MD">
        <StateCell label="primary"><Knob size="md" tone="primary" rotation={-30} /></StateCell>
        <StateCell label="neutral"><Knob size="md" tone="neutral" rotation={0} /></StateCell>
        <StateCell label="alert"><Knob size="md" tone="alert" rotation={45} /></StateCell>
      </GridRow>
    </ArchetypeBoard>
  );
}

function BoardFader() {
  const states = ["default", "hover", "focus", "pressed", "disabled"];
  return (
    <ArchetypeBoard slug="fader" title="LINEAR · CONSOLE FADER">
      <GridRow label="STATES · MD · PRIMARY">
        {states.map(s => (
          <StateCell key={s} label={s} w={70}>
            <Fader size="md" tone="primary" value={55} state={s === "default" ? null : s} />
          </StateCell>
        ))}
      </GridRow>
      <GridRow label="SIZE · VERTICAL">
        <StateCell label="sm"><Fader size="sm" tone="primary" value={40} /></StateCell>
        <StateCell label="md"><Fader size="md" tone="primary" value={55} /></StateCell>
        <StateCell label="lg" w={140}><Fader size="lg" tone="primary" value={70} /></StateCell>
      </GridRow>
      <GridRow label="TONE · HORIZONTAL">
        <StateCell label="primary" w={260}><Fader size="md" tone="primary" value={60} orientation="horizontal" /></StateCell>
        <StateCell label="neutral" w={260}><Fader size="md" tone="neutral" value={40} orientation="horizontal" /></StateCell>
        <StateCell label="alert" w={260}><Fader size="md" tone="alert" value={85} orientation="horizontal" /></StateCell>
      </GridRow>
    </ArchetypeBoard>
  );
}

function BoardVu() {
  const states = ["default", "hover", "focus", "pressed", "disabled"];
  return (
    <ArchetypeBoard slug="vu-meter" title="LADDER · VU METER">
      <GridRow label="STATES · MD · PRIMARY">
        {states.map(s => (
          <StateCell key={s} label={s} w={250}>
            <VuMeter size="md" tone="primary" segments={16} lit={11} state={s === "default" ? null : s} />
          </StateCell>
        ))}
      </GridRow>
      <GridRow label="SIZE · HORIZONTAL">
        <StateCell label="sm" w={170}><VuMeter size="sm" tone="primary" segments={12} lit={8} /></StateCell>
        <StateCell label="md" w={250}><VuMeter size="md" tone="primary" segments={16} lit={11} /></StateCell>
        <StateCell label="lg" w={350}><VuMeter size="lg" tone="primary" segments={20} lit={14} /></StateCell>
      </GridRow>
      <GridRow label="TONE · MD">
        <StateCell label="primary" w={250}><VuMeter size="md" tone="primary" segments={16} lit={12} /></StateCell>
        <StateCell label="neutral" w={250}><VuMeter size="md" tone="neutral" segments={16} lit={8} /></StateCell>
        <StateCell label="alert" w={250}><VuMeter size="md" tone="alert" segments={16} lit={15} /></StateCell>
      </GridRow>
      <GridRow label="VERTICAL">
        <StateCell label="vert · md" w={120}><VuMeter size="md" tone="primary" segments={16} lit={11} orientation="vertical" /></StateCell>
        <StateCell label="vert · alert" w={120}><VuMeter size="md" tone="alert" segments={16} lit={14} orientation="vertical" /></StateCell>
      </GridRow>
    </ArchetypeBoard>
  );
}

function BoardILed() {
  const states = ["default", "hover", "focus", "pressed", "disabled"];
  return (
    <ArchetypeBoard slug="illuminated-button" title="LATCHING · ILLUMINATED BUTTON">
      <GridRow label="STATES · UNLIT · MD">
        {states.map(s => (
          <StateCell key={s} label={s}>
            <ILed size="md" tone="primary" state={s === "default" ? null : s}>REC</ILed>
          </StateCell>
        ))}
      </GridRow>
      <GridRow label="STATES · LIT · MD">
        {states.map(s => (
          <StateCell key={s + "-lit"} label={s + " · lit"}>
            <ILed size="md" tone="primary" lit state={s === "default" ? null : s}>REC</ILed>
          </StateCell>
        ))}
      </GridRow>
      <GridRow label="SIZE">
        <StateCell label="sm"><ILed size="sm" tone="primary" lit>SM</ILed></StateCell>
        <StateCell label="md"><ILed size="md" tone="primary" lit>MEDIUM</ILed></StateCell>
        <StateCell label="lg"><ILed size="lg" tone="primary" lit>LARGE BUTTON</ILed></StateCell>
      </GridRow>
      <GridRow label="TONE · LIT">
        <StateCell label="primary"><ILed size="md" tone="primary" lit>PLAY</ILed></StateCell>
        <StateCell label="neutral"><ILed size="md" tone="neutral" lit>SYNC</ILed></StateCell>
        <StateCell label="alert"><ILed size="md" tone="alert" lit>MUTE</ILed></StateCell>
      </GridRow>
    </ArchetypeBoard>
  );
}

function BoardSegRead() {
  const states = ["default", "hover", "focus", "pressed", "disabled"];
  return (
    <ArchetypeBoard slug="segmented-readout" title="LCD · SEGMENTED READOUT">
      <GridRow label="STATES · MD · PRIMARY">
        {states.map(s => (
          <StateCell key={s} label={s} w={150}>
            <SegRead size="md" tone="primary" value="20,000" digits={7} state={s === "default" ? null : s} />
          </StateCell>
        ))}
      </GridRow>
      <GridRow label="SIZE">
        <StateCell label="sm"><SegRead size="sm" tone="primary" value="50" digits={3} /></StateCell>
        <StateCell label="md" w={170}><SegRead size="md" tone="primary" value="20,000" digits={7} /></StateCell>
        <StateCell label="lg" w={240}><SegRead size="lg" tone="primary" value="1,200,000" digits={9} /></StateCell>
      </GridRow>
      <GridRow label="TONE">
        <StateCell label="primary" w={170}><SegRead size="md" tone="primary" value="$20,000" digits={8} /></StateCell>
        <StateCell label="neutral" w={170}><SegRead size="md" tone="neutral" value="50/100" digits={7} /></StateCell>
        <StateCell label="alert" w={170}><SegRead size="md" tone="alert" value="-3,420" digits={7} /></StateCell>
      </GridRow>
    </ArchetypeBoard>
  );
}

Object.assign(window, { BoardKnob, BoardFader, BoardVu, BoardILed, BoardSegRead });
