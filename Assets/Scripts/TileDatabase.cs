using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Tilemaps;

[CreateAssetMenu(fileName = "TileDatabase", menuName = "Scriptable Objects/Tile Database")]
public class TileDatabase : SerializedScriptableObject
{
    [SerializeField] private Dictionary<uint,TileConfig> Tiles;

    public TileConfig Get(uint id)
    {
        if (id == 0) return null;
        return Tiles[id];
    }

    public uint GetID(TileConfig config)
    {
        foreach (var VARIABLE in Tiles)
        {
            if (VARIABLE.Value == config) return VARIABLE.Key;
        }
        return 0;
    }

    public TileConfig GetRandom()
    {
        Tiles.TryGetValue((uint)Random.Range(1, Tiles.Count), out TileConfig config);
        return config;
    }

    public uint GetRandomID() => (uint)Random.Range(1, Tiles.Count);
}