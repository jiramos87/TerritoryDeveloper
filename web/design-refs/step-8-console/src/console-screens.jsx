// =============================================================
// Territory Developer — Console Screens
// Landing, Dashboard, Releases, Detail, Design (asset sheets)
// =============================================================

const { useState: useS, useMemo: useM, useEffect: useE } = React;

// ---------- LANDING ----------
const ScreenLanding = ({ go }) => {
  const [tState, setTState] = useS("play");
  return (
    <div>
      <div className="crumbs-row"><span className="current">TERRITORY DEVELOPER // CONSOLE</span></div>

      <div className="landing-hero">
        <Rack className="hero-left" label="STUDIO A">
          <span className="hero-eyebrow">
            <LED tone="a" blink/> ON AIR // INTERNAL ENGINEERING
          </span>
          <h1 className="hero-h1">Plan the territory. Ship the signal.</h1>
          <p className="hero-lede">
            An internal console for the Territory Developer pilot. Every step, stage, phase and task
            routed through one mixing desk — with release telemetry, density heatmaps, and a
            drill-down that goes all the way to the task row.
          </p>
          <div className="hero-ctas">
            <Button variant="primary" size="lg" onClick={() => go("dashboard")}>ENTER DASHBOARD →</Button>
            <Button variant="secondary" size="lg" onClick={() => go("design")}>DESIGN GUIDE</Button>
          </div>
          <div className="hstack" style={{gap:16, paddingTop:12, borderTop:"1px solid #000", boxShadow:"0 1px 0 rgba(255,255,255,.04)"}}>
            <div className="hstack" style={{gap:6}}><LED tone="g"/><span className="mono" style={{fontSize:10, color:"var(--text-muted)", letterSpacing:".1em"}}>BUILD v0.4.0-α</span></div>
            <div className="hstack" style={{gap:6}}><TapeReel size={16}/><span className="mono" style={{fontSize:10, color:"var(--text-muted)", letterSpacing:".1em"}}>7/12 NODES</span></div>
            <div className="hstack" style={{gap:6}}><LED tone="b"/><span className="mono" style={{fontSize:10, color:"var(--text-muted)", letterSpacing:".1em"}}>SIGNAL NOMINAL</span></div>
          </div>
        </Rack>
        <Rack className="hero-right" label="VIEWPORT // LIVE">
          <HeroArt/>
        </Rack>
      </div>

      {/* Now playing — an LCD + VU strip tying landing to the console metaphor */}
      <Rack className="np" label="NOW PLAYING">
        <Bezel><Screen color="amber" className="lcd"><div className="lcd-label">CURRENT RELEASE</div><div className="lcd-big v">v0.4.0-α</div></Screen></Bezel>
        <VuStrip level={0.62}/>
        <div className="indicators">
          <span className="cell"><LED tone="g"/> CI</span>
          <span className="cell"><LED tone="g"/> DB</span>
          <span className="cell"><LED tone="a" blink/> MIG</span>
          <span className="cell"><LED tone="r"/> BLK</span>
          <span className="cell"><LED tone="b"/> INFO</span>
        </div>
      </Rack>

      {/* Feature pillars — 8c */}
      <div style={{marginTop:24}}>
        <div className="eyebrow" style={{marginBottom:12}}>// FEATURE PILLARS</div>
        <div className="pillars">
          <div className="pillar"><PillarPlanet/><div className="caption"><div className="k">01 · SIM</div><div className="t">A living territory that ships with the code.</div></div></div>
          <div className="pillar"><PillarSignal/><div className="caption"><div className="k">02 · TELEMETRY</div><div className="t">Every commit graphed like a waveform.</div></div></div>
          <div className="pillar"><PillarMixer/><div className="caption"><div className="k">03 · MIX</div><div className="t">Pick the channels that matter this week.</div></div></div>
          <div className="pillar"><PillarRadar/><div className="caption"><div className="k">04 · SWEEP</div><div className="t">Release radar — what's landing, what's blocked.</div></div></div>
          <div className="pillar"><PillarTape/><div className="caption"><div className="k">05 · TAPE</div><div className="t">Full project history, persistent and rewindable.</div></div></div>
        </div>
      </div>

      {/* Transport strip */}
      <div style={{marginTop:24}}>
        <TransportStrip state={tState} onSetState={setTState}/>
      </div>
    </div>
  );
};

// ---------- DASHBOARD ----------
const ScreenDashboard = ({ narrow }) => {
  const [statusFilter, setStatusFilter] = useS(new Set());
  const [expanded, setExpanded] = useS(new Set(["STEP-5","STAGE-5.2"]));
  const [selId, setSelId] = useS(null);
  const [state, setState] = useS("ready"); // ready|loading|empty|error
  const toggle = (id) => { const s=new Set(expanded); s.has(id)?s.delete(id):s.add(id); setExpanded(s); };
  const toggleStatus = (k) => { const s=new Set(statusFilter); s.has(k)?s.delete(k):s.add(k); setStatusFilter(s); };

  const allTasks = useM(() => flattenTasks(MASTER_PLAN), []);
  const counts = useM(() => ({
    done: allTasks.filter(t=>t.status==="done").length,
    progress: allTasks.filter(t=>t.status==="progress").length,
    pending: allTasks.filter(t=>t.status==="pending").length,
    blocked: allTasks.filter(t=>t.status==="blocked").length,
    total: allTasks.length
  }), [allTasks]);
  const matchesFilter = (s) => statusFilter.size === 0 || statusFilter.has(s);

  const renderTask = (t) => (
    <li key={t.id}><div className={`row leaf ${selId===t.id?"selected":""}`} onClick={()=>setSelId(t.id)}>
      <span className="chev"/><span className={`dot s-${t.status}`}/>
      <IdChip>{t.id}</IdChip>
      <span className="title">{t.title}</span>
      <StatusChip status={t.status}/>
    </div></li>
  );
  const renderPhase = (p) => {
    const r = rollup(p); const on = expanded.has(p.id);
    return <li key={p.id}>
      <div className="row" onClick={()=>toggle(p.id)} aria-expanded={on}>
        <span className="chev">▶</span><span className={`dot s-${p.status}`}/>
        <IdChip>{p.id}</IdChip><span className="title">{p.title}</span>
        <span className="stat"><StatBar done={r.done} total={r.total} tone={p.status==="blocked"?"blocked":p.status==="done"?"done":"progress"} showNums={!narrow}/></span>
      </div>
      {on && <ul>{p.tasks.filter(t=>matchesFilter(t.status)).map(renderTask)}</ul>}
    </li>;
  };
  const renderStage = (s) => {
    const r = rollup(s); const on = expanded.has(s.id);
    return <li key={s.id}>
      <div className="row" onClick={()=>toggle(s.id)} aria-expanded={on}>
        <span className="chev">▶</span><span className={`dot s-${s.status}`}/>
        <IdChip>{s.id}</IdChip><span className="title">{s.title}</span>
        <span className="stat"><StatBar done={r.done} total={r.total} tone={s.status==="blocked"?"blocked":s.status==="done"?"done":"progress"} showNums={!narrow}/></span>
      </div>
      {on && <ul>{s.phases.map(renderPhase)}</ul>}
    </li>;
  };
  const renderStep = (step) => {
    const r = rollup(step); const on = expanded.has(step.id);
    return <li key={step.id}>
      <div className="row" onClick={()=>toggle(step.id)} aria-expanded={on}>
        <span className="chev">▶</span><span className={`dot s-${step.status}`}/>
        <IdChip>{step.id}</IdChip><span className="title" style={{fontWeight:500}}>{step.title}</span>
        <span className="stat"><StatBar done={r.done} total={r.total} tone={step.status==="blocked"?"blocked":step.status==="done"?"done":"progress"} showNums={!narrow}/></span>
      </div>
      {on && <ul>{step.stages.map(renderStage)}</ul>}
    </li>;
  };

  return (
    <div>
      <div className="crumbs-row"><a>TERRITORY</a><span className="sep">//</span><span className="current">DASHBOARD</span></div>

      {/* summary strip: 4 LCD bezels + density sparkline */}
      <div className="grid-3" style={{gridTemplateColumns: narrow ? "1fr" : "repeat(4, 1fr) 1.3fr", marginBottom:16}}>
        <Rack label="DONE"><Bezel><Screen color="green" className="lcd"><div className="lcd-label">COMPLETED</div><div className="lcd-big" style={{fontSize:26}}>{String(counts.done).padStart(3,"0")}</div></Screen></Bezel></Rack>
        <Rack label="PROG"><Bezel><Screen color="amber" className="lcd"><div className="lcd-label">IN PROGRESS</div><div className="lcd-big" style={{fontSize:26}}>{String(counts.progress).padStart(3,"0")}</div></Screen></Bezel></Rack>
        <Rack label="PEND"><Bezel><Screen className="lcd"><div className="lcd-label">PENDING</div><div className="lcd-big" style={{fontSize:26, color:"var(--text-muted)"}}>{String(counts.pending).padStart(3,"0")}</div></Screen></Bezel></Rack>
        <Rack label="BLKD"><Bezel><Screen color="red" className="lcd"><div className="lcd-label">BLOCKED</div><div className="lcd-big" style={{fontSize:26}}>{String(counts.blocked).padStart(3,"0")}</div></Screen></Bezel></Rack>
        <Rack label="VELOCITY">
          <div style={{padding:"6px 10px"}}>
            <div className="hstack" style={{justifyContent:"space-between"}}>
              <span className="lcd-label">TASKS/WEEK</span>
              <span className="mono" style={{color:"var(--raw-amber)", fontSize:14}}>+6.2</span>
            </div>
            <Sparkline data={[2,3,5,4,6,8,7,9,11,8,10,12]} width={220} height={40}/>
          </div>
        </Rack>
      </div>

      {/* Heatmap */}
      <Rack label="DENSITY // 7 STAGES × 12 WEEKS" className="hm-wrap" style={{marginBottom:16}}>
        <Bezel>
          <div style={{padding:"6px 4px"}}>
            {WEEK_DENSITY.map(row => (
              <div key={row.stage} style={{display:"grid", gridTemplateColumns: narrow ? "100px repeat(12, 1fr)" : "220px repeat(12, 1fr)", gap:4, alignItems:"center", marginBottom:2}}>
                <span className="mono" style={{fontSize:10, color:"var(--text-muted)", letterSpacing:".05em", overflow:"hidden", textOverflow:"ellipsis", whiteSpace:"nowrap"}}>{narrow ? row.stage.split(" ")[0] : row.stage}</span>
                {row.cells.map((n,i) => <HeatCell key={i} n={n} label={`${row.stage} · wk ${i+1} · ${n} tasks`}/>)}
              </div>
            ))}
            <div style={{display:"grid", gridTemplateColumns: narrow ? "100px repeat(12, 1fr)" : "220px repeat(12, 1fr)", gap:4, marginTop:6}}>
              <span/>
              {Array.from({length:12}).map((_,i)=><span key={i} className="mono" style={{fontSize:9, color:"var(--text-muted)", textAlign:"center", letterSpacing:".05em"}}>W{String(i+1).padStart(2,"0")}</span>)}
            </div>
          </div>
        </Bezel>
      </Rack>

      {/* Filters */}
      <div className="hstack" style={{flexWrap:"wrap", gap:8, marginBottom:16}}>
        <span className="mono" style={{fontSize:10, color:"var(--text-muted)", letterSpacing:".1em", textTransform:"uppercase"}}>FILTER //</span>
        {["done","progress","pending","blocked"].map(s => (
          <FilterChip key={s} active={statusFilter.has(s)} status={s} onClick={()=>toggleStatus(s)} count={counts[s]}>{statusLabel(s)}</FilterChip>
        ))}
        <span className="spacer"/>
        <FilterChip>OWNER: ALL</FilterChip>
        <FilterChip>STEP: ALL</FilterChip>
      </div>

      {/* Tree or state */}
      {state === "ready" && (
        <Rack label="MASTER PLAN">
          <div className="tree" style={{padding: "4px 8px"}}>
            <ul>{MASTER_PLAN.map(renderStep)}</ul>
          </div>
        </Rack>
      )}
      {state === "loading" && <LoadingSkeleton rows={6}/>}
      {state === "empty" && <EmptyState title="NO MATCHING TASKS" body="Relax your filters or try a different owner." cta={<Button variant="secondary" onClick={()=>{setStatusFilter(new Set()); setState("ready");}}>CLEAR FILTERS</Button>}/>}
      {state === "error" && <ErrorState title="CANNOT REACH NEON" body="Upstream database timed out. This view shows the last good snapshot." cta={<Button variant="primary" onClick={()=>setState("ready")}>RETRY</Button>}/>}

      {/* State tweaker — kept visible so reviewers can see all four */}
      <div className="hstack" style={{marginTop:16, gap:8, flexWrap:"wrap"}}>
        <span className="mono" style={{fontSize:10, color:"var(--text-muted)", letterSpacing:".1em"}}>STATE //</span>
        {["ready","loading","empty","error"].map(s => (
          <FilterChip key={s} active={state===s} onClick={()=>setState(s)}>{s.toUpperCase()}</FilterChip>
        ))}
      </div>
    </div>
  );
};

// ---------- RELEASES ----------
const ScreenReleases = ({ go, narrow }) => {
  const [q, setQ] = useS("");
  const [sort, setSort] = useS("date");
  const [filter, setFilter] = useS(new Set());
  const toggle = (k) => { const s=new Set(filter); s.has(k)?s.delete(k):s.add(k); setFilter(s); };
  const rows = useM(() => {
    let r = RELEASES.filter(x =>
      (filter.size===0 || filter.has(x.status)) &&
      (!q || x.id.toLowerCase().includes(q.toLowerCase()) || x.title.toLowerCase().includes(q.toLowerCase()))
    );
    r = [...r].sort((a,b) => sort==="date" ? (b.date||"").localeCompare(a.date||"") : sort==="progress" ? (b.done/b.total)-(a.done/a.total) : a.id.localeCompare(b.id));
    return r;
  }, [q, sort, filter]);

  return (
    <div>
      <div className="crumbs-row"><a>TERRITORY</a><span className="sep">//</span><a>DASHBOARD</a><span className="sep">//</span><span className="current">RELEASES</span></div>
      <div className="page-head">
        <div>
          <div className="eyebrow">// RELEASES</div>
          <h1>Release ledger</h1>
          <div className="sub">Signed tags, drafts, and hotfix candidates.</div>
        </div>
        <div className="right"><Button variant="primary">NEW RELEASE</Button></div>
      </div>

      <div className="hstack" style={{gap:8, marginBottom:12, flexWrap:"wrap"}}>
        <input className="input" placeholder="search: tag / title" value={q} onChange={e=>setQ(e.target.value)}/>
        {["done","progress","blocked","pending"].map(s => <FilterChip key={s} active={filter.has(s)} status={s} onClick={()=>toggle(s)}>{statusLabel(s)}</FilterChip>)}
        <span className="spacer"/>
        <FilterChip>LAST 90d</FilterChip>
      </div>

      <Rack>
        {rows.length === 0 ? (
          <div className="state-card">
            <Screen color="amber" className="lcd" sweep={false}><span className="lcd-big">— NO MATCHES —</span></Screen>
            <div className="title">NO RELEASES MATCH</div>
            <div className="body">Try clearing the search or filter chips above.</div>
            <Button variant="secondary" onClick={()=>{setQ(""); setFilter(new Set());}}>CLEAR</Button>
          </div>
        ) : (
          <table className="table">
            <thead>
              <tr>
                <th className={sort==="id"?"sorted":""} onClick={()=>setSort("id")}>TAG</th>
                <th>TITLE</th>
                <th>STATUS</th>
                {!narrow && <th className={sort==="progress"?"sorted":""} onClick={()=>setSort("progress")}>PROGRESS</th>}
                {!narrow && <th>OWNER</th>}
                <th className={sort==="date"?"sorted":""} onClick={()=>setSort("date")}>DATE</th>
              </tr>
            </thead>
            <tbody>
              {rows.map(r => (
                <tr key={r.id} onClick={()=>go("detail", r.id)}>
                  <td className="mono"><span style={{color:"var(--raw-amber)"}}>{r.id}</span></td>
                  <td>{r.title}</td>
                  <td><StatusChip status={r.status}/></td>
                  {!narrow && <td style={{minWidth:160}}><StatBar done={r.done} total={r.total} tone={r.status==="blocked"?"blocked":r.status==="done"?"done":"progress"}/></td>}
                  {!narrow && <td className="mono" style={{color:"var(--text-muted)"}}>{r.owner}</td>}
                  <td className="mono" style={{color:"var(--text-muted)", fontSize:11}}>{r.date}</td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </Rack>
    </div>
  );
};

// ---------- DETAIL ----------
const ScreenDetail = ({ releaseId, go, narrow }) => {
  const rel = RELEASES.find(r => r.id === releaseId) || RELEASES[0];
  const [selStage, setSelStage] = useS("STAGE-5.2");
  const step = MASTER_PLAN.find(s => s.id === "STEP-5");
  const stage = step?.stages.find(s => s.id === selStage) || step?.stages[1];

  return (
    <div>
      <div className="crumbs-row">
        <a>TERRITORY</a><span className="sep">//</span>
        <a onClick={()=>go("releases")} style={{cursor:"pointer"}}>RELEASES</a><span className="sep">//</span>
        <span className="current">{rel.id}</span>
      </div>
      <div className="page-head">
        <div>
          <div className="eyebrow">// RELEASE DETAIL</div>
          <h1>{rel.id} — {rel.title}</h1>
          <div className="sub">Owner {rel.owner} · target {rel.date} · <StatusChip status={rel.status}/></div>
        </div>
        <div className="right"><Button variant="secondary">EXPORT</Button><Button variant="primary">SHIP</Button></div>
      </div>

      {/* Stage × week heatmap scoped to release */}
      <Rack label="STAGES × 12 WEEKS" style={{marginBottom:16}}>
        <Bezel>
          <div style={{padding:"6px 4px"}}>
            {WEEK_DENSITY.map(row => {
              const id = row.stage.split(" ")[0];
              const sel = id === selStage;
              return (
                <div key={row.stage} style={{display:"grid", gridTemplateColumns: narrow ? "100px repeat(12, 1fr)" : "220px repeat(12, 1fr)", gap:4, alignItems:"center", marginBottom:2, padding:"2px 4px", borderRadius:2, background: sel ? "rgba(74,123,200,.08)" : "transparent", cursor:"pointer"}} onClick={()=>setSelStage(id)}>
                  <span className="mono" style={{fontSize:10, color: sel ? "var(--raw-blue)" : "var(--text-muted)", letterSpacing:".05em", overflow:"hidden", textOverflow:"ellipsis", whiteSpace:"nowrap"}}>{narrow ? id : row.stage}</span>
                  {row.cells.map((n,i) => <HeatCell key={i} n={n}/>)}
                </div>
              );
            })}
          </div>
        </Bezel>
      </Rack>

      <div className="grid-2" style={{gridTemplateColumns: narrow ? "1fr" : "1.4fr 1fr"}}>
        <Rack label={`SCOPE // ${selStage}`}>
          <div className="tree" style={{padding:"8px"}}>
            <ul>
              {stage?.phases.map(p => (
                <li key={p.id}>
                  <div className="row" aria-expanded="true">
                    <span className="chev">▼</span>
                    <span className={`dot s-${p.status}`}/>
                    <IdChip>{p.id}</IdChip>
                    <span className="title">{p.title}</span>
                    <span className="stat"><StatBar {...rollup(p)} tone={p.status==="blocked"?"blocked":"progress"}/></span>
                  </div>
                  <ul>
                    {p.tasks.map(t => (
                      <li key={t.id}>
                        <div className="row leaf">
                          <span className="chev"/><span className={`dot s-${t.status}`}/>
                          <IdChip>{t.id}</IdChip>
                          <span className="title">{t.title}</span>
                          <StatusChip status={t.status}/>
                        </div>
                      </li>
                    ))}
                  </ul>
                </li>
              ))}
            </ul>
          </div>
        </Rack>

        <div className="stack">
          <Rack label="STAGE // READOUT">
            <div style={{padding:"10px 12px", display:"flex", flexDirection:"column", gap:10}}>
              <Bezel><Screen color="amber" className="lcd"><div className="lcd-label">STAGE</div><div className="lcd-big" style={{fontSize:20}}>{stage?.id}</div></Screen></Bezel>
              <div className="hstack" style={{justifyContent:"space-between"}}>
                <span className="mono" style={{fontSize:11, color:"var(--text-muted)", letterSpacing:".1em"}}>STATUS</span>
                <StatusChip status={stage?.status}/>
              </div>
              <StatBar {...rollup(stage||{phases:[]})} label="COMPLETION" tone="progress"/>
              {stage?.status === "blocked" && (
                <div style={{padding:10, background:"rgba(214,56,56,.08)", borderLeft:"2px solid var(--raw-red)", borderRadius:2}}>
                  <div className="mono" style={{fontSize:10, color:"var(--raw-red)", letterSpacing:".1em", marginBottom:4}}>BLOCK REASON</div>
                  <div style={{fontSize:13}}>Vendor decision pending on payment gateway selection.</div>
                </div>
              )}
              <div className="hstack" style={{gap:6}}>
                <Button variant="primary" style={{flex:1}}>OPEN IN EDITOR</Button>
                <Button variant="secondary">COPY IDs</Button>
              </div>
            </div>
          </Rack>

          <Rack label="CHANNELS">
            <div className="channels">
              {["AUTH","DB","MIG","E2E","MDX","CI"].map((ch,i) => {
                const h = [85, 60, 45, 20, 30, 70][i];
                return (
                  <div className="lane" key={ch}>
                    <div className="label">{ch}</div>
                    <div className="meter">
                      {Array.from({length:16}).map((_,j) => {
                        const lit = j >= (16 - Math.round(h/6));
                        const tone = j > 13 ? "r" : j > 10 ? "a" : "g";
                        return <div key={j} className="seg" style={{background: lit ? (tone==="r"?"var(--raw-red)":tone==="a"?"var(--raw-amber)":"var(--raw-green)") : undefined, boxShadow: lit ? `0 0 3px ${tone==="r"?"var(--raw-red)":tone==="a"?"var(--raw-amber)":"var(--raw-green)"}` : undefined}}/>;
                      })}
                    </div>
                    <div className="ms">
                      <button className={`b solo ${i===0?"on":""}`}>S</button>
                      <button className={`b mute ${i===3?"on":""}`}>M</button>
                    </div>
                  </div>
                );
              })}
            </div>
          </Rack>
        </div>
      </div>
    </div>
  );
};

// ---------- DESIGN / ASSET SHEETS ----------
const ScreenDesign = () => {
  return (
    <div>
      <div className="crumbs-row"><a>TERRITORY</a><span className="sep">//</span><span className="current">DESIGN GUIDE</span></div>
      <div className="page-head">
        <div><div className="eyebrow">// DESIGN GUIDE</div><h1>Console aesthetic — asset sheets</h1><div className="sub">Logo suite · media icons · hero art · feature pillars · primitive states.</div></div>
      </div>

      <div className="asset-sheet">
        {/* Logo suite */}
        <Rack label="8d · LOGO SUITE">
          <section>
            <h2>Primary lockups</h2>
            <div className="asset-grid">
              <div className="asset-cell on-canvas"><Wordmark height={36} variant="amber-on-black"/><span className="label">WORDMARK · AMBER/BLACK · PRIMARY</span></div>
              <div className="asset-cell on-canvas"><Wordmark height={36} variant="white-on-black"/><span className="label">WORDMARK · WHITE/BLACK</span></div>
              <div className="asset-cell on-panel"><Wordmark height={36} variant="black-on-amber"/><span className="label">REVERSIBLE · BLACK/AMBER</span></div>
            </div>
            <h2 style={{marginTop:24}}>Logomark · Lettermark</h2>
            <div className="asset-grid">
              <div className="asset-cell on-canvas"><Logomark size={96} variant="amber-on-black"/><span className="label">LOGOMARK 96</span></div>
              <div className="asset-cell on-canvas"><Logomark size={48} variant="amber-on-black"/><span className="label">LOGOMARK 48</span></div>
              <div className="asset-cell on-canvas"><Logomark size={24} variant="amber-on-black"/><span className="label">LOGOMARK 24 FAVICON</span></div>
              <div className="asset-cell on-canvas"><Lettermark size={64} variant="amber-on-black"/><span className="label">LETTERMARK</span></div>
              <div className="asset-cell on-panel"><Lettermark size={64} variant="black-on-amber"/><span className="label">LETTERMARK · REVERSED</span></div>
            </div>
            <h2 style={{marginTop:24}}>Strapline lockup</h2>
            <div className="asset-grid" style={{gridTemplateColumns:"1fr"}}>
              <div className="asset-cell on-canvas"><StraplineLockup height={56}/><span className="label">FOOTER / CO-BRAND LOCKUP</span></div>
            </div>
          </section>
        </Rack>

        {/* Media transport icons */}
        <Rack label="8b · MEDIA TRANSPORT ICON FAMILY">
          <section>
            <h2>Tactile glyph set — 24px, outlined+filled</h2>
            <div className="icon-grid">
              {Object.entries(TIcon).map(([name, Icon]) => (
                <div key={name} className="icon-tile"><Icon style={{color:"var(--raw-amber)"}}/><span className="name">{name.toUpperCase()}</span></div>
              ))}
            </div>
            <h2 style={{marginTop:24}}>On a live transport strip</h2>
            <TransportStrip state="play"/>
          </section>
        </Rack>

        {/* Hero crops */}
        <Rack label="8a · HERO ART">
          <section>
            <h2>Primary hero</h2>
            <div className="asset-grid" style={{gridTemplateColumns:"1fr"}}>
              <div className="pillar" style={{aspectRatio:"16/9"}}><HeroArt/></div>
            </div>
            <h2 style={{marginTop:24}}>Social crop (16:8)</h2>
            <div className="asset-grid" style={{gridTemplateColumns:"1fr"}}>
              <div className="pillar" style={{aspectRatio:"16/8"}}><HeroCrop/></div>
            </div>
          </section>
        </Rack>

        {/* Feature pillars */}
        <Rack label="8c · FEATURE PILLARS">
          <section>
            <div className="pillars">
              <div className="pillar"><PillarPlanet/><div className="caption"><div className="k">01 · SIM</div><div className="t">Territory as a living simulation.</div></div></div>
              <div className="pillar"><PillarSignal/><div className="caption"><div className="k">02 · TELEMETRY</div><div className="t">Commits as waveforms.</div></div></div>
              <div className="pillar"><PillarMixer/><div className="caption"><div className="k">03 · MIX</div><div className="t">Channel-strip focus.</div></div></div>
              <div className="pillar"><PillarRadar/><div className="caption"><div className="k">04 · SWEEP</div><div className="t">Release radar.</div></div></div>
              <div className="pillar"><PillarTape/><div className="caption"><div className="k">05 · TAPE</div><div className="t">Rewindable history.</div></div></div>
            </div>
          </section>
        </Rack>

        {/* Primitives */}
        <Rack label="PRIMITIVES · STATES">
          <section>
            <h2>Button</h2>
            <div className="hstack" style={{gap:8, flexWrap:"wrap"}}>
              <Button variant="primary">PRIMARY</Button>
              <Button variant="primary" size="lg">PRIMARY LG</Button>
              <Button variant="secondary">SECONDARY</Button>
              <Button variant="ghost">GHOST</Button>
              <Button variant="primary" disabled>DISABLED</Button>
            </div>
            <h2 style={{marginTop:24}}>StatusChip · IdChip</h2>
            <div className="hstack" style={{gap:8, flexWrap:"wrap"}}>
              <StatusChip status="done"/><StatusChip status="progress"/><StatusChip status="pending"/><StatusChip status="blocked"/>
              <IdChip>STEP-5</IdChip><IdChip>STAGE-5.2</IdChip><IdChip>T-254</IdChip>
            </div>
            <h2 style={{marginTop:24}}>StatBar · tones</h2>
            <div className="stack" style={{maxWidth:420}}>
              <StatBar done={14} total={14} tone="done" label="DONE"/>
              <StatBar done={7} total={12} tone="progress" label="IN PROGRESS"/>
              <StatBar done={0} total={8} tone="pending" label="PENDING"/>
              <StatBar done={1} total={4} tone="blocked" label="BLOCKED"/>
              <StatBar done={9} total={12} tone="info" label="INFO / SELECTED"/>
            </div>
            <h2 style={{marginTop:24}}>FilterChip</h2>
            <div className="hstack" style={{gap:8, flexWrap:"wrap"}}>
              <FilterChip status="done">DONE</FilterChip>
              <FilterChip status="progress" active>IN PROGRESS</FilterChip>
              <FilterChip status="blocked">BLOCKED</FilterChip>
              <FilterChip active>OWNER: AGENT</FilterChip>
            </div>
            <h2 style={{marginTop:24}}>HeatmapCell scale</h2>
            <div className="hstack" style={{gap:4}}>
              {[0,1,2,4,6,8].map(n => <HeatCell key={n} n={n}/>)}
              <span className="mono" style={{fontSize:10, color:"var(--text-muted)", marginLeft:12}}>NULL → PEAK</span>
            </div>
            <h2 style={{marginTop:24}}>Empty · Loading · Error</h2>
            <div className="grid-3">
              <EmptyState title="NO RESULTS" body="No tasks match the current filter."/>
              <LoadingSkeleton rows={4}/>
              <ErrorState title="UPSTREAM TIMEOUT" body="Showing last good snapshot."/>
            </div>
          </section>
        </Rack>

        {/* Type */}
        <Rack label="TYPE · 3 FAMILIES">
          <section>
            <div style={{fontFamily:"var(--font-sans)", fontSize:48, fontWeight:700, letterSpacing:"-0.02em"}}>Geist — display</div>
            <div style={{fontFamily:"var(--font-sans)", fontSize:18}}>Geist — body. The portal ships with Geist variable as the sans surface.</div>
            <div style={{fontFamily:"var(--font-mono)", fontSize:14, color:"var(--text-muted)", letterSpacing:".05em", marginTop:12}}>Geist Mono — IDs, chip captions, telemetry labels. 0/O · 1/l/I.</div>
            <div style={{fontFamily:"var(--font-lcd)", fontSize:28, color:"var(--raw-amber)", letterSpacing:".02em", marginTop:12}}>Azeret Mono — LCD readout · 0123456789</div>
          </section>
        </Rack>
      </div>
    </div>
  );
};

Object.assign(window, { ScreenLanding, ScreenDashboard, ScreenReleases, ScreenDetail, ScreenDesign });
