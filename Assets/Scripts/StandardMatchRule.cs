using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Standard Match Rule",menuName = "Match Rules/Standard Match Rule")]
public class StandardMatchRule : MatchRule
{ //match 3 rule
    [field: SerializeField] public TileGenerationRule[] GenerationRules { get; private set; }
    
    public override bool HasMatch(Vector2Int position, Board board)
    {
        if (Board.MatchMask.Contains(position)) return true;
        uint id = Board.TileIDs[position.x, position.y];
        bool blasted = false;
        HashSet<Vector2Int> horizontal = new HashSet<Vector2Int>();
        HashSet<Vector2Int> vertical = new HashSet<Vector2Int>();
        for (Vector2Int i = position; i.x < board.Size.x; i.x++)
        {
            if (id == Board.TileIDs[i.x,i.y]) horizontal.Add(i);
            else break;
        }
        for (Vector2Int i = position; i.x > -1; i.x--)
        {
            if (id == Board.TileIDs[i.x,i.y]) horizontal.Add(i);
            else break;
        }
        for (Vector2Int i = position; i.y < board.Size.y; i.y++)
        {
            if(id == Board.TileIDs[i.x,i.y]) vertical.Add(i);
            else break;
        }
        for (Vector2Int i = position; i.y > -1; i.y--)
        {
            if(id == Board.TileIDs[i.x,i.y]) vertical.Add(i);
            else break;
        }

        if (horizontal.Count >= 3)
        {
            //add to a match mask
            foreach (var VARIABLE in horizontal)
            {
                Board.MatchMask.Add(VARIABLE);
            }
            blasted = true;
        }
        if (vertical.Count >= 3)
        {
            foreach (var VARIABLE in vertical)
            {
                Board.MatchMask.Add(VARIABLE);
            }
            blasted = true;
        }

        // for (int i = 0; i < UPPER; i++)
        // {
        //     if
        // }
        return blasted;
    }
}
[CreateAssetMenu(fileName = "TileGenerationRule",menuName = "Generation/Tile Generation Rule")]
public class TileGenerationRule : ScriptableObject
{
    public int RequiredCount { get; private set; }= 4;
    public TileConfig GeneratedTile { get; private set; }= null;
}