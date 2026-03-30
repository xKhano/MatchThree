using UnityEngine;

public abstract class MatchRule : ScriptableObject
{
    public abstract bool HasMatch(Vector2Int position,Board board);
}