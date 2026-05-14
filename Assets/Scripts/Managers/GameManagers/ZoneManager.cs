using System;
using System.Collections.Generic;
using UnityEngine;
using Territory.Core;
using Territory.Roads;
using Territory.Economy;
using Territory.UI;
using Territory.Terrain;
using Territory.Buildings;
using Territory.Simulation;
using Domains.Registry;
using Domains.Zones.Services;

namespace Territory.Zones
{
/// <summary>THIN hub — delegates to ZonePrefabRegistry/ZonePlacementService/ZoneSectionService/ZonesService. [SerializeField] set UNCHANGED (locked #3).</summary>
public class ZoneManager : MonoBehaviour, IZoneManager
{
    #region Fields (SerializeField UNCHANGED — locked #3)
    public GridManager gridManager; public PowerPlant powerPlantManager; public WaterPlant waterPlantManager;
    public RoadManager roadManager; public CityStats cityStats; public UIManager uiManager;
    public GameNotificationManager gameNotificationManager; public DemandManager demandManager;
    public WaterManager waterManager; public InterstateManager interstateManager;
    public SlopePrefabRegistry slopePrefabRegistry;
    [SerializeField] private ZoneSubTypeRegistry zoneSubTypeRegistry;
    [SerializeField] private PlacementValidator placementValidator;
    public List<GameObject> lightResidential1x1Prefabs,lightResidential2x2Prefabs,lightResidential3x3Prefabs;
    public List<GameObject> mediumResidential1x1Prefabs,mediumResidential2x2Prefabs,mediumResidential3x3Prefabs;
    public List<GameObject> heavyResidential1x1Prefabs,heavyResidential2x2Prefabs,heavyResidential3x3Prefabs;
    public List<GameObject> lightCommercial1x1Prefabs,lightCommercial2x2Prefabs,lightCommercial3x3Prefabs;
    public List<GameObject> mediumCommercial1x1Prefabs,mediumCommercial2x2Prefabs,mediumCommercial3x3Prefabs;
    public List<GameObject> heavyCommercial1x1Prefabs,heavyCommercial2x2Prefabs,heavyCommercial3x3Prefabs;
    public List<GameObject> lightIndustrial1x1Prefabs,lightIndustrial2x2Prefabs,lightIndustrial3x3Prefabs;
    public List<GameObject> mediumIndustrial1x1Prefabs,mediumIndustrial2x2Prefabs,mediumIndustrial3x3Prefabs;
    public List<GameObject> heavyIndustrial1x1Prefabs,heavyIndustrial2x2Prefabs,heavyIndustrial3x3Prefabs;
    public List<GameObject> residentialLightZoningPrefabs,residentialMediumZoningPrefabs,residentialHeavyZoningPrefabs;
    public List<GameObject> commercialLightZoningPrefabs,commercialMediumZoningPrefabs,commercialHeavyZoningPrefabs;
    public List<GameObject> industrialLightZoningPrefabs,industrialMediumZoningPrefabs,industrialHeavyZoningPrefabs;
    public List<GameObject> roadPrefabs,grassPrefabs,waterPrefabs;
    [Header("Desirability (FEAT-26)")] [SerializeField] private float baseSpawnWeight=1f,minDesirabilityThreshold=2f,lowDesirabilityPenalty=0.1f;
    [Header("FEAT-43")] [SerializeField] private AutoZoningManager autoZoningManager;
    [SerializeField] private DesirabilityComposer desirabilityComposer;
    [Header("Stage 10")] [SerializeField] private ConstructionStageController constructionStageController;
    #endregion
    #region Services + state
    private ZonePrefabRegistry _prefabReg; private ZonePlacementService _plac; private ZoneSectionService _sec; private ZonesService _zones; private ServiceRegistry _registry;
    private bool isZoning,isInitialized; private Vector2 zoningStart,zoningEnd;
    private List<GameObject> previewTiles=new List<GameObject>();
    private Dictionary<Zone.ZoneType,List<List<Vector2>>> availSections=new Dictionary<Zone.ZoneType,List<List<Vector2>>>();
    private bool sectionsDirty=true; private const int MaxRoadDist=3;
    public System.Action<Vector2,bool> onUrbanCellChanged;
    #endregion
    #region Init
    void Awake()
    {
        _registry=FindObjectOfType<ServiceRegistry>(); _plac=new ZonePlacementService(); _sec=new ZoneSectionService(); _zones=new ZonesService();
        InitializeZonePrefabs();
        if(autoZoningManager==null)autoZoningManager=FindObjectOfType<AutoZoningManager>();
        if(desirabilityComposer==null)desirabilityComposer=FindObjectOfType<DesirabilityComposer>();
        if(constructionStageController==null)constructionStageController=FindObjectOfType<ConstructionStageController>();
    }
    public void InitializeZonePrefabs()
    {
        if(isInitialized)return;
        L(Zone.ZoneType.ResidentialLightBuilding,lightResidential1x1Prefabs,lightResidential2x2Prefabs,lightResidential3x3Prefabs);
        L(Zone.ZoneType.ResidentialMediumBuilding,mediumResidential1x1Prefabs,mediumResidential2x2Prefabs,mediumResidential3x3Prefabs);
        L(Zone.ZoneType.ResidentialHeavyBuilding,heavyResidential1x1Prefabs,heavyResidential2x2Prefabs,heavyResidential3x3Prefabs);
        L(Zone.ZoneType.CommercialLightBuilding,lightCommercial1x1Prefabs,lightCommercial2x2Prefabs,lightCommercial3x3Prefabs);
        L(Zone.ZoneType.CommercialMediumBuilding,mediumCommercial1x1Prefabs,mediumCommercial2x2Prefabs,mediumCommercial3x3Prefabs);
        L(Zone.ZoneType.CommercialHeavyBuilding,heavyCommercial1x1Prefabs,heavyCommercial2x2Prefabs,heavyCommercial3x3Prefabs);
        L(Zone.ZoneType.IndustrialLightBuilding,lightIndustrial1x1Prefabs,lightIndustrial2x2Prefabs,lightIndustrial3x3Prefabs);
        L(Zone.ZoneType.IndustrialMediumBuilding,mediumIndustrial1x1Prefabs,mediumIndustrial2x2Prefabs,mediumIndustrial3x3Prefabs);
        L(Zone.ZoneType.IndustrialHeavyBuilding,heavyIndustrial1x1Prefabs,heavyIndustrial2x2Prefabs,heavyIndustrial3x3Prefabs);
        var d=new Dictionary<(Zone.ZoneType,int),List<GameObject>>(_prefabDict);
        foreach(var zt in new[]{(Zone.ZoneType.ResidentialLightZoning,residentialLightZoningPrefabs),(Zone.ZoneType.ResidentialMediumZoning,residentialMediumZoningPrefabs),(Zone.ZoneType.ResidentialHeavyZoning,residentialHeavyZoningPrefabs),(Zone.ZoneType.CommercialLightZoning,commercialLightZoningPrefabs),(Zone.ZoneType.CommercialMediumZoning,commercialMediumZoningPrefabs),(Zone.ZoneType.CommercialHeavyZoning,commercialHeavyZoningPrefabs),(Zone.ZoneType.IndustrialLightZoning,industrialLightZoningPrefabs),(Zone.ZoneType.IndustrialMediumZoning,industrialMediumZoningPrefabs),(Zone.ZoneType.IndustrialHeavyZoning,industrialHeavyZoningPrefabs)})
            d[(zt.Item1,1)]=zt.Item2??new List<GameObject>();
        d[(Zone.ZoneType.Grass,1)]=grassPrefabs??new List<GameObject>(); d[(Zone.ZoneType.Road,1)]=roadPrefabs??new List<GameObject>(); d[(Zone.ZoneType.Water,1)]=waterPrefabs??new List<GameObject>();
        _prefabReg=new ZonePrefabRegistry(d); isInitialized=true;
    }
    private Dictionary<(Zone.ZoneType,int),List<GameObject>> _prefabDict=new Dictionary<(Zone.ZoneType,int),List<GameObject>>();
    private void L(Zone.ZoneType zt,List<GameObject> p1,List<GameObject> p2,List<GameObject> p3)
    { _prefabDict[(zt,1)]=p1??new List<GameObject>(); _prefabDict[(zt,2)]=p2??new List<GameObject>(); _prefabDict[(zt,3)]=p3??new List<GameObject>(); }
    void Start()
    {
        if(!isInitialized)InitializeZonePrefabs();
        _zones.WireDependencies(_registry?.Resolve<Domains.Grid.IGrid>(),_registry?.Resolve<Domains.Water.IWater>(),_registry?.Resolve<Domains.Roads.IRoads>());
    }
    #endregion
    #region Public API
    public ZoneAttributes GetZoneAttributes(Zone.ZoneType t)=>_plac.GetZoneAttributes(t);
    public Zone.ZoneType GetBuildingZoneType(Zone.ZoneType t)=>_plac.GetBuildingZoneType(t);
    public bool IsResidentialBuilding(Zone.ZoneType t)=>_plac.IsResidentialBuilding(t);
    public bool IsCommercialOrIndustrialBuilding(Zone.ZoneType t)=>_plac.IsCommercialOrIndustrialBuilding(t);
    public static bool IsZoningType(Zone.ZoneType t)=>new ZonePlacementService().IsZoningType(t);
    public static bool IsStateServiceZoneType(Zone.ZoneType t)=>new ZonePlacementService().IsStateServiceZoneType(t);
    public Zone.ZoneType GetZoneTypeFromZoneTypeString(string s)=>_plac.ParseZoneType(s);
    public GameObject GetRandomZonePrefab(Zone.ZoneType t,int size=1){if(!isInitialized)InitializeZonePrefabs();return _prefabReg.GetRandom(t,size);}
    public GameObject GetGrassPrefab()=>grassPrefabs[0];
    public GameObject GetWaterPrefab()=>waterPrefabs[0];
    public GameObject FindPrefabByName(string n)
    {
        if(string.IsNullOrEmpty(n))return null; n=n.Replace("(Clone)","");
        foreach(var l in _prefabReg.AllLists()){if(l==null)continue;foreach(var p in l){if(p!=null&&p.name==n)return p;}}
        if(roadManager!=null){var rp=roadManager.GetRoadPrefabs();if(rp!=null)foreach(var p in rp){if(p!=null&&p.name==n)return p;}}
        if(powerPlantManager!=null){var pp=powerPlantManager.GetPowerPlantPrefabs();if(pp!=null)foreach(var p in pp){if(p!=null&&p.name==n)return p;}}
        if(waterPlantManager!=null){var wp=waterPlantManager.GetWaterPlantPrefabs();if(wp!=null)foreach(var p in wp){if(p!=null&&p.name==n)return p;}}
        if(slopePrefabRegistry!=null){var sp=slopePrefabRegistry.FindByName(n);if(sp!=null)return sp;}
        return null;
    }
    public void addZonedTileToList(Vector2 pos,Zone.ZoneType t){sectionsDirty=true;onUrbanCellChanged?.Invoke(pos,true);_zones.AddPosition(pos,t);}
    public void removeZonedPositionFromList(Vector2 pos,Zone.ZoneType t,bool isConversion=false){sectionsDirty=true;if(!isConversion)onUrbanCellChanged?.Invoke(pos,false);_zones.RemovePosition(pos,t);}
    public void ClearZonedPositions(){_zones.ClearAll();availSections.Clear();sectionsDirty=true;}
    public void CalculateAvailableSquareZonedSections()
    {
        if(!sectionsDirty)return; sectionsDirty=false; availSections.Clear();
        foreach(var zt in ZoneSectionService.GetValidZoneTypes()) availSections[zt]=_sec.CalculateSections(_zones.GetZonedPositions(zt));
    }
    public bool IsZoning()=>isZoning;
    public int GetPreviewZoneCellCount(){if(!isZoning)return 0;var s=Vector2Int.FloorToInt(zoningStart);var e=Vector2Int.FloorToInt(zoningEnd);return(Mathf.Abs(e.x-s.x)+1)*(Mathf.Abs(e.y-s.y)+1);}
    public void HandleZoning(Vector2 pos)
    {
        if(Input.GetMouseButtonDown(0)&&!isZoning){isZoning=true;zoningStart=pos;zoningEnd=pos;ClearPreviews();uiManager?.HideGhostPreview();}
        else if(Input.GetMouseButton(0)&&isZoning){zoningEnd=pos;ClearPreviews();var tl=new Vector2Int(Mathf.Min((int)zoningStart.x,(int)pos.x),Mathf.Max((int)zoningStart.y,(int)pos.y));var br=new Vector2Int(Mathf.Max((int)zoningStart.x,(int)pos.x),Mathf.Min((int)zoningStart.y,(int)pos.y));for(int x=tl.x;x<=br.x;x++)for(int y=br.y;y<=tl.y;y++){if(CanZone(_plac.GetZoneAttributes(uiManager.GetSelectedZoneType()),new Vector2(x,y))){CityCell c=gridManager.GetCell(x,y);GameObject t=Instantiate(SlopeAware(GetRandomZonePrefab(uiManager.GetSelectedZoneType()),x,y),c.transformPosition,Quaternion.identity);t.GetComponent<SpriteRenderer>().color=new Color(1,1,1,0.5f);previewTiles.Add(t);}}}
        else if(Input.GetMouseButtonUp(0)&&isZoning){isZoning=false;ClearPreviews();var tl=new Vector2Int(Mathf.Min((int)zoningStart.x,(int)pos.x),Mathf.Max((int)zoningStart.y,(int)pos.y));var br=new Vector2Int(Mathf.Max((int)zoningStart.x,(int)pos.x),Mathf.Min((int)zoningStart.y,(int)pos.y));for(int x=tl.x;x<=br.x;x++)for(int y=br.y;y<=tl.y;y++){if(CanZone(_plac.GetZoneAttributes(uiManager.GetSelectedZoneType()),new Vector2(x,y)))PlaceZone(new Vector2(x,y));}CalculateAvailableSquareZonedSections();uiManager?.RestoreGhostPreview();}
    }
    // iter-19 — throttle Debug.Log spawn diagnostics so the building-growth gates surface
    // why a zoned area isn't producing buildings. One log per zone type per 2 seconds.
    static readonly System.Collections.Generic.Dictionary<Zone.ZoneType, float> _lastSpawnLogTime = new System.Collections.Generic.Dictionary<Zone.ZoneType, float>();
    static void LogSpawnGate(Zone.ZoneType zt, string reason)
    {
        if (_lastSpawnLogTime.TryGetValue(zt, out var t) && Time.unscaledTime - t < 2f) return;
        _lastSpawnLogTime[zt] = Time.unscaledTime;
        Debug.Log($"[ZoneManager] PlaceZonedBuildings({zt}) → {reason}");
    }

    public void PlaceZonedBuildings(Zone.ZoneType zt)
    {
        if(_plac.IsStateServiceZoneType(zt)) return;
        if(availSections.Count == 0) { LogSpawnGate(zt, "no available sections (zone the cells first)"); return; }
        var s=RandSection(zt);
        if(!s.HasValue||s.Value.size==0) { LogSpawnGate(zt, $"RandSection returned null — zoned cells not within {MaxRoadDist} of a road or no 1x1/2x2/3x3 square (availSections[{zt}]={(availSections.ContainsKey(zt) ? availSections[zt].Count : 0)})"); return; }
        var bt=_plac.GetBuildingZoneType(zt);
        if(_plac.IsResidentialBuilding(bt))
        {
            if(!CanPlaceRes()) { LogSpawnGate(zt, "CanPlaceRes false (residential needs jobs — no commercial/industrial buildings yet)"); return; }
            if(demandManager!=null&&!demandManager.GetResidentialDemand().canGrow) { LogSpawnGate(zt, "ResidentialDemand.canGrow false"); return; }
        }
        else
        {
            if(!CanPlaceCI(bt)) { LogSpawnGate(zt, $"CanPlaceCI false for {bt} (C/I needs residential population)"); return; }
            if(demandManager!=null&&UnityEngine.Random.value>demandManager.GetDemandSpawnFactor(zt)) { LogSpawnGate(zt, $"demand-roll missed (spawnFactor={demandManager.GetDemandSpawnFactor(zt):F2})"); return; }
        }
        if(!cityStats.GetCityPowerAvailability()) { LogSpawnGate(zt, $"power output {cityStats.cityPowerOutput} ≤ consumption {cityStats.cityPowerConsumption}"); return; }
        if(waterManager!=null&&!waterManager.GetCityWaterAvailability()) { LogSpawnGate(zt, $"water output {cityStats.cityWaterOutput} ≤ consumption {cityStats.cityWaterConsumption}"); return; }
        Debug.Log($"[ZoneManager] PlaceZonedBuildings({zt}) → SPAWNING building (size={s.Value.size}, section.length={s.Value.section.Length})");
        DoBuildingPlace(s.Value.section,bt,_plac.GetZoneAttributes(bt),zt,(int)System.Math.Sqrt(s.Value.section.Length));
    }
    public bool PlaceZoneAt(Vector2 pos,Zone.ZoneType zt)
    {
        var a=_plac.GetZoneAttributes(zt);if(a==null||!cityStats.CanAfford(a.ConstructionCost)||!CanZone(a,pos,false))return false;
        CityCell cell=gridManager.GetCell((int)pos.x,(int)pos.y);if(cell==null)return false;
        gridManager.DestroyCellChildrenExceptForest(cell.gameObject,pos);
        GameObject pf=SlopeAware(GetRandomZonePrefab(zt,1),(int)pos.x,(int)pos.y);if(pf==null)return false;
        GameObject tile=Instantiate(pf,cell.transformPosition,Quaternion.identity);Zone z=tile.AddComponent<Zone>();z.zoneType=zt;z.zoneCategory=Zone.ZoneCategory.Zoning;
        SetCellAttrs(cell,zt,pf,a);gridManager.SetZoningTileSortingOrder(tile,(int)pos.x,(int)pos.y);addZonedTileToList(pos,zt);cityStats.AddZoneBuildingCount(zt);return true;
    }
    public bool PlaceStateServiceZoneAt(int cx,int cy,Zone.ZoneType zt,int subId)
    {
        CityCell cell=gridManager.GetCell(cx,cy);if(cell==null)return false;
        if(placementValidator!=null){if(zoneSubTypeRegistry==null)zoneSubTypeRegistry=FindObjectOfType<ZoneSubTypeRegistry>();if(zoneSubTypeRegistry!=null&&zoneSubTypeRegistry.TryGetAssetIdForSubType(subId,out int aid)){var pr=placementValidator.CanPlace(aid,cx,cy,0,zt);if(!pr.IsAllowed)return false;}}
        cell.zoneType=zt;Zone ex=cell.gameObject.GetComponentInChildren<Zone>();
        if(ex!=null){ex.zoneType=zt;ex.SubTypeId=subId;}else{Zone nz=cell.gameObject.AddComponent<Zone>();nz.zoneType=zt;nz.SubTypeId=subId;}
        addZonedTileToList(new Vector2(cx,cy),zt);cityStats.AddZoneBuildingCount(zt);return true;
    }
    public void RestoreZoneTile(GameObject pf,GameObject gridCell,Zone.ZoneType zt)
    {
        CityCell cell=gridCell.GetComponent<CityCell>();if(cell==null)return;
        gridManager.DestroyCellChildrenExceptForest(gridCell,new Vector2(cell.x,cell.y));
        GameObject tile=Instantiate(pf,cell.transformPosition,Quaternion.identity);tile.transform.SetParent(gridCell.transform);
        Zone zone=tile.GetComponent<Zone>()??tile.AddComponent<Zone>();zone.zoneType=zt;zone.zoneCategory=Zone.ZoneCategory.Zoning;
        var a=_plac.GetZoneAttributes(zt);if(a!=null)SetCellAttrs(cell,zt,pf,a);gridManager.SetZoningTileSortingOrder(tile,cell.x,cell.y);
    }
    public void RemoveZonedSectionFromList(Vector2[] zp,Zone.ZoneType zt)
    {
        if(!availSections.ContainsKey(zt))return;var ss=availSections[zt];
        for(int i=0;i<ss.Count;i++){if(ss[i].Count!=zp.Length)continue;bool m=true;for(int j=0;j<ss[i].Count;j++)if(ss[i][j]!=zp[j]){m=false;break;}if(m){ss.RemoveAt(i);return;}}
    }
    public void PlaceZoneBuildingTile(GameObject pf,GameObject gridCell,int size=1)
    {
        CityCell cell=gridCell.GetComponent<CityCell>();if(size>1&&!cell.isPivot)return;
        Vector3 wp=cell.transformPosition;if(size>1&&cell.zoneType!=Zone.ZoneType.Building)wp-=new Vector3(0,-(size-1)*gridManager.tileHeight/2,0);
        gridManager.DestroyCellChildren(gridCell,new Vector2(cell.x,cell.y),null,destroyFlatGrass:true);
        GameObject tile=Instantiate(pf,wp,Quaternion.identity);cell.isPivot=true;gridManager.SetZoneBuildingSortingOrder(tile,(int)cell.x,(int)cell.y,size);
    }
    #endregion
    #region Private helpers
    private GameObject SlopeAware(GameObject flat,int x,int y){if(slopePrefabRegistry==null||flat==null||gridManager==null||gridManager.terrainManager==null)return flat;TerrainSlopeType st=gridManager.terrainManager.GetTerrainSlopeTypeAt(x,y);if(st==TerrainSlopeType.Flat)return flat;return slopePrefabRegistry.GetSlopeVariant(flat,st)??flat;}
    private bool CanZone(ZoneAttributes a,Vector3 pos,bool ri=true){if(a==null)return false;if(ri&&interstateManager!=null){interstateManager.CheckInterstateConnectivity();if(!interstateManager.IsConnectedToInterstate){gameNotificationManager?.PostWarning("Connect a road to the Interstate Highway before zoning.");return false;}}if(!cityStats.CanAfford(a.ConstructionCost))return false;if(!gridManager.canPlaceBuilding(pos,1))return false;return true;}
    private void ClearPreviews(){foreach(var t in previewTiles)Destroy(t);previewTiles.Clear();}
    private void PlaceZone(Vector3 pos){Zone.ZoneType zt=uiManager.GetSelectedZoneType();var a=_plac.GetZoneAttributes(zt);if(a==null)return;if(!cityStats.CanAfford(a.ConstructionCost)){uiManager.ShowInsufficientFundsTooltip(zt.ToString(),a.ConstructionCost);return;}if(!CanZone(a,pos)){gameNotificationManager.PostError("Cannot place zone here.");return;}CityCell cell=gridManager.GetCell((int)pos.x,(int)pos.y);gridManager.DestroyCellChildrenExceptForest(cell.gameObject,pos);GameObject pf=SlopeAware(GetRandomZonePrefab(zt),(int)pos.x,(int)pos.y);if(pf==null)return;GameObject tile=Instantiate(pf,cell.transformPosition,Quaternion.identity);Zone zone=tile.AddComponent<Zone>();zone.zoneType=zt;zone.zoneCategory=Zone.ZoneCategory.Zoning;SetCellAttrs(cell,zt,pf,a);gridManager.SetZoningTileSortingOrder(tile,(int)pos.x,(int)pos.y);addZonedTileToList(pos,zt);cityStats.AddZoneBuildingCount(zt);cityStats.RemoveMoney(a.ConstructionCost);}
    private (int size,Vector2[] section)? RandSection(Zone.ZoneType zt){if(!availSections.ContainsKey(zt)||availSections[zt].Count==0)return null;var all=new List<(int,List<Vector2>)>();foreach(var sec in availSections[zt]){int sz=(int)System.Math.Sqrt(sec.Count);if(sz>=1&&sz<=3&&sec.Count==sz*sz&&IsSectionNearRoad(sec))all.Add((sz,sec));}if(all.Count==0)return null;Func<int,int,float> desir=(sx,sy)=>{if(autoZoningManager!=null&&autoZoningManager.IsSignalDesirabilityEnabled)return autoZoningManager.DesirabilityComposer.CellValue(sx,sy);CityCell c=gridManager.GetCell(sx,sy);return c!=null?c.desirability:0f;};var sel=_sec.GetWeightedSection(zt,all,desir,baseSpawnWeight,minDesirabilityThreshold,lowDesirabilityPenalty);if(sel.section==null)return null;var arr=new Vector2[sel.section.Count];for(int i=0;i<sel.section.Count;i++)arr[i]=sel.section[i];return(sel.size,arr);}
    private bool IsSectionNearRoad(List<Vector2> sec){if(sec==null||gridManager==null)return false;for(int i=0;i<sec.Count;i++){if(gridManager.IsWithinDistanceOfRoad((int)sec[i].x,(int)sec[i].y,MaxRoadDist))return true;}return false;}
    private bool CanPlaceRes()=>demandManager==null||demandManager.CanPlaceResidentialBuilding();
    private bool CanPlaceCI(Zone.ZoneType t)=>demandManager==null||demandManager.CanPlaceCommercialOrIndustrialBuilding(t);
    private void DoBuildingPlace(Vector2[] section,Zone.ZoneType bt,ZoneAttributes a,Zone.ZoneType zt,int size){Vector2 piv=section[0];GameObject pf=SlopeAware(GetRandomZonePrefab(bt,size),(int)piv.x,(int)piv.y);if(pf==null)return;foreach(Vector2 pos in section){GameObject cgo=gridManager.GetGridCell(pos);CityCell cc=gridManager.GetCell((int)pos.x,(int)pos.y);cc?.RemoveForestForBuilding();gridManager.DestroyCellChildren(cgo,pos,null,destroyFlatGrass:true);gridManager.UpdateCellAttributes(cc,bt,a,pf,size);removeZonedPositionFromList(pos,zt,isConversion:true);cityStats.RemoveZoneBuildingCount(zt);}gridManager.GetCell((int)piv.x,(int)piv.y).isPivot=true;constructionStageController?.BeginConstruction(gridManager.GetCell((int)piv.x,(int)piv.y),bt);PlaceZoneBuildingTile(pf,gridManager.GetGridCell(piv),size);cityStats.HandleZoneBuildingPlacement(bt,a);cityStats.AddPowerConsumption(a.PowerConsumption);Territory.Audio.BlipEngine.Play(Territory.Audio.BlipId.ToolBuildingPlace);}
    private void SetCellAttrs(CityCell c,Zone.ZoneType zt,GameObject pf,ZoneAttributes a){c.zoneType=zt;c.population=a.Population;c.powerConsumption=a.PowerConsumption;c.waterConsumption=a.WaterConsumption;c.happiness=a.Happiness;c.prefab=pf;c.prefabName=pf.name;c.buildingType=null;c.buildingSize=1;c.powerPlant=null;c.occupiedBuilding=null;c.isPivot=false;}
    #endregion
}
}
