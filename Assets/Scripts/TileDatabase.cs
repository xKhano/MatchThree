using UnityEngine;

[CreateAssetMenu(fileName = "TileDatabase", menuName = "Scriptable Objects/Tile Database")]
public class TileDatabase : ScriptableObject
{
    [field:SerializeField] public TileConfig[] Tiles { get; private set; }

    public TileConfig Get(uint id)
    {
        if (id < 1 || id > Tiles.Length) return null;
        return Tiles[id-1];
    }

    public uint GetID(TileConfig config)
    {
        for (uint i = 0; i < Tiles.Length; i++)
        {
            if (Tiles[0] == config) return i+1;
        }
        return 0;
    }
    public TileConfig GetRandom() => Tiles[(uint)Random.Range(1, Tiles.Length + 1)];
    public uint GetRandomID() => (uint)Random.Range(1, Tiles.Length+1);
}