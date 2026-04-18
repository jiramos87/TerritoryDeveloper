// =============================================================
// Territory Developer — Console App Shell
// Sidebar + viewport. Density + narrow mode persisted.
// =============================================================

const { useState: uS, useEffect: uE } = React;

const App = () => {
  const [route, setRoute] = uS(() => localStorage.getItem("td.route") || "landing");
  const [releaseId, setReleaseId] = uS(() => localStorage.getItem("td.rel") || "v0.4.0-alpha");
  const [mode, setMode] = uS(() => localStorage.getItem("td.mode") || "comfortable");
  const [narrow, setNarrow] = uS(() => localStorage.getItem("td.narrow") === "1");
  const [sbOpen, setSbOpen] = uS(false);

  uE(() => localStorage.setItem("td.route", route), [route]);
  uE(() => localStorage.setItem("td.rel", releaseId), [releaseId]);
  uE(() => localStorage.setItem("td.mode", mode), [mode]);
  uE(() => localStorage.setItem("td.narrow", narrow ? "1" : "0"), [narrow]);

  const go = (r, id) => { setRoute(r); if (id) setReleaseId(id); setSbOpen(false); window.scrollTo({top:0}); };
  const backTo = { dashboard: "landing", releases: "dashboard", detail: "releases", design: "landing" }[route];

  const crumbsNarrow = {
    landing: [{k:"TERRITORY", current:true}],
    dashboard: [{k:"TERRITORY"}, {k:"DASHBOARD", current:true}],
    releases: [{k:"TERRITORY"}, {k:"DASHBOARD"}, {k:"RELEASES", current:true}],
    detail: [{k:"RELEASES"}, {k:releaseId, current:true}],
    design: [{k:"TERRITORY"}, {k:"DESIGN", current:true}],
  }[route];

  return (
    <div className={`shell mode-${mode} ${narrow ? "narrow" : ""}`}>
      <button className="hamburger" onClick={()=>setSbOpen(!sbOpen)} aria-label="Toggle nav">
        <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2"><path d="M4 6h16M4 12h16M4 18h16"/></svg>
      </button>

      <aside className={`sidebar ${sbOpen ? "open" : ""}`}>
        <div className="brand" onClick={()=>go("landing")} style={{cursor:"pointer"}}>
          <Logomark size={28}/>
          <div className="stack">
            <div className="top">Internal</div>
            <div className="name">Territory Developer</div>
          </div>
        </div>
        <div className="section">CONSOLE</div>
        <a className={`nav ${route==="landing"?"active":""}`} onClick={()=>go("landing")}>Landing</a>
        <a className={`nav ${route==="dashboard"?"active":""}`} onClick={()=>go("dashboard")}>Dashboard</a>
        <a className={`nav ${route==="releases"||route==="detail"?"active":""}`} onClick={()=>go("releases")}>Releases</a>
        <div className="section">SYSTEM</div>
        <a className={`nav ${route==="design"?"active":""}`} onClick={()=>go("design")}>Design guide</a>
        <div className="foot">
          <div className="hstack" style={{gap:6}}><LED tone="g"/><span>CONNECTED</span></div>
          <div className="hstack" style={{gap:6}}><TapeReel size={14}/><span>BUILD 042</span></div>
          <label className="hstack" style={{gap:6, cursor:"pointer"}}>
            <input type="checkbox" checked={narrow} onChange={e=>setNarrow(e.target.checked)} style={{accentColor:"var(--raw-amber)"}}/>
            <span>NARROW VIEWPORT</span>
          </label>
        </div>
      </aside>

      <main className="main">
        {narrow && backTo && route !== "landing" && (
          <div className="backbar">
            <button onClick={()=>go(backTo === "landing" ? "landing" : backTo)} aria-label="Back">◀ BACK</button>
            <div className="crumbs">
              {crumbsNarrow.map((c,i) => (
                <span key={i} className={c.current ? "current" : ""}>{c.k}{i < crumbsNarrow.length-1 ? " //" : ""}</span>
              ))}
            </div>
          </div>
        )}

        {route !== "landing" && (
          <div className="topbar">
            <Legend/>
            <span className="spacer"/>
            <DensityToggle mode={mode} setMode={setMode}/>
            <StaleDataBanner when="4m 12s ago"/>
          </div>
        )}

        {route === "landing" && <ScreenLanding go={go}/>}
        {route === "dashboard" && <ScreenDashboard narrow={narrow}/>}
        {route === "releases" && <ScreenReleases go={go} narrow={narrow}/>}
        {route === "detail" && <ScreenDetail releaseId={releaseId} go={go} narrow={narrow}/>}
        {route === "design" && <ScreenDesign/>}
      </main>
    </div>
  );
};

ReactDOM.createRoot(document.getElementById("root")).render(<App/>);
