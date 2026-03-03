using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class CommuneData
{
    public string communeName;
    public int anchorX;
    public int anchorY;
    public List<SerializableVector2Int> cellPositions = new List<SerializableVector2Int>();

    public Vector2Int Anchor
    {
        get => new Vector2Int(anchorX, anchorY);
        set { anchorX = value.x; anchorY = value.y; }
    }

    public List<Vector2Int> GetCellPositions()
    {
        var list = new List<Vector2Int>();
        foreach (var s in cellPositions)
            list.Add(s.ToVector2Int());
        return list;
    }

    public void SetCellPositions(List<Vector2Int> positions)
    {
        cellPositions.Clear();
        foreach (var p in positions)
            cellPositions.Add(SerializableVector2Int.From(p));
    }
}

[System.Serializable]
public struct SerializableVector2Int
{
    public int x;
    public int y;

    public Vector2Int ToVector2Int() => new Vector2Int(x, y);
    public static SerializableVector2Int From(Vector2Int v) => new SerializableVector2Int { x = v.x, y = v.y };
}
