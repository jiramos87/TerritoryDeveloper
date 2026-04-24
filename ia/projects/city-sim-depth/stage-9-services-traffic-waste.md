### Stage 9 — New Simulation Signals / Services + Traffic + Waste

**Status:** Draft (tasks _pending_ — not yet filed)

**Objectives:** Ship `ServiceCoverageComputer` (5 service types), `TrafficFlowHeuristic`, `WasteSystem`; register all three with `SignalTickScheduler`; smoke-test each.

**Exit:**

- `ServicePolice/Fire/Education/Health/Parks` non-zero within coverage radius of placed service buildings.
- `TrafficLevel` non-zero on road-adjacent cells with RCI density.
- `WastePressure` non-zero in high-density zones without recycling coverage.
- All 3 MonoBehaviours in `SignalTickScheduler` producer list.
- EditMode smoke tests pass.
- Phase 1 — ServiceCoverageComputer + TrafficFlowHeuristic.
- Phase 2 — WasteSystem + scheduler registration + smoke tests.

**Tasks:**

| Task | Name | Issue | Status | Intent |
| --- | --- | --- | --- | --- |
| T9.1 | ServiceCoverageComputer MonoBehaviour | _pending_ | _pending_ | `ServiceCoverageComputer` MonoBehaviour implements `ISignalProducer` — for each of 5 service types, iterates placed service buildings; writes coverage signal via Manhattan-distance radius (per-service radius tunable per `SignalMetadataRegistry` entry); road-connectivity gated (service building unreachable by road → zero coverage emitted); `[SerializeField] GridManager` + `FindObjectOfType`. |
| T9.2 | TrafficFlowHeuristic MonoBehaviour | _pending_ | _pending_ | `TrafficFlowHeuristic` MonoBehaviour implements `ISignalProducer` — for each road cell: `TrafficLevel = rciNeighborhoodSum / roadTierCapacity`; `rciNeighborhoodSum` = Moore-sum of zone levels in radius 2 from `GridManager.GetCell`; `roadTierCapacity` from `RoadManager` tier lookup; non-road cells = 0; updates daily; `[SerializeField] RoadManager` + `FindObjectOfType`. |
| T9.3 | WasteSystem MonoBehaviour | _pending_ | _pending_ | `WasteSystem` MonoBehaviour implements `ISignalProducer` — `WastePressure.Add(x,y, zoneLevel * wasteRate)` per RCI cell; sinks: landfill buildings write negative emission over coverage radius; recycling center writes stronger negative emission; `[SerializeField] SignalFieldRegistry` + `FindObjectOfType`; updates monthly tick. |
| T9.4 | Scheduler registration + smoke tests | _pending_ | _pending_ | Register `ServiceCoverageComputer`, `TrafficFlowHeuristic`, `WasteSystem` in `SignalTickScheduler` — extend `[SerializeField] List<MonoBehaviour>` explicit producer slots + `FindObjectsOfType` fallback. EditMode smoke: police station placed → `ServicePolice.Get(stationX, stationY)` > 0; R zone + road → `TrafficLevel.Get` road cell > 0; R zone no landfill → `WastePressure` > 0. |

---
