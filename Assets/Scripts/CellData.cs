
[System.Serializable]
public class CellData
{
    public bool hasRoadAtLeft;
    public bool hasRoadAtTop;
    public bool hasRoadAtRight;
    public bool hasRoadAtBottom;
    public int population;
    public PowerPlant powerPlant;
    public WaterPlant waterPlant; // Added water plant
    public int powerOutput;
    public int powerConsumption;
    public int waterConsumption; // Added water consumption
    public string buildingType;
    public int buildingSize;
    public int x;
    public int y;
    public int happiness;
    public string prefabName;
    public string zoneType; // Store the enum as a string for easy serialization
    public string occupiedBuildingName;
    public bool isPivot;
    public int sortingOrder;
    public int height;
}