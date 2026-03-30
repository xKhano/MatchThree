using UnityEngine;

public abstract class BlastPattern : ScriptableObject
{
    public abstract void Blast(Vector2Int originPosition, Board board);
}