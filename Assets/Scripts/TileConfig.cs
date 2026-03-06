using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

[CreateAssetMenu(fileName = "TileConfig", menuName = "Scriptable Objects/Tile Config")]
public class TileConfig : SerializedScriptableObject
{
    [field: SerializeField,PreviewField(80),HorizontalGroup("SpriteRow",.2f),HideLabel] public Sprite Sprite { get; private set; } = null;
    [field: SerializeField,VerticalGroup("SpriteRow/RightColumn")] public string Name { get; private set; } = "Tile";
    [field: SerializeField,VerticalGroup("SpriteRow/RightColumn")] public RuntimeAnimatorController AnimatorController { get; private set; } = null;
    [field: SerializeField,VerticalGroup("SpriteRow/RightColumn")] public uint StartHealth { get; private set; } = 1;
    [field: SerializeField,VerticalGroup("SpriteRow/RightColumn")] public bool Fallable { get; private set; } = false;
    [field: SerializeField,VerticalGroup("SpriteRow/RightColumn")] public bool Interactable { get; private set; } = false;
    [field: SerializeField] public GameObject BlastVFXPrefab { get; private set; } = null;
    
    [Title("Blast Patterns")]
    [ListDrawerSettings(DraggableItems = true, ShowPaging = false, Expanded = true)]
    [field:SerializeField] public BlastPattern[] BlastPatterns { get; private set; }= null;
    [field:SerializeField] public MatchRule[] MatchRules { get; private set; }
}

public abstract class BlastPattern : ScriptableObject
{
    public abstract void Blast(Vector2Int originPosition, Board board);
}
public abstract class MatchRule : ScriptableObject
{
    public abstract bool HasMatch(Vector2Int position,Board board);
}

[CreateAssetMenu(fileName = "Row Blast Pattern",menuName = "Blast Pattern/Row Blast Pattern")]
public class RowBlastPattern : BlastPattern
{
    public override void Blast(Vector2Int originPosition, Board board)
    {
        
    }
}
[CreateAssetMenu(fileName = "Column Blast Pattern",menuName = "Blast Pattern/Column Blast Pattern")]
public class ColumnBlastPattern : BlastPattern
{
    public override void Blast(Vector2Int originPosition, Board board)
    {
        
    }
}
[CreateAssetMenu(fileName = "Standard Blast Pattern",menuName = "Blast Pattern/Standard Blast Pattern")]
public class StandardBlastPattern : BlastPattern
{
    public override void Blast(Vector2Int originPosition, Board board)
    {
        
    }
}
[CreateAssetMenu(fileName = "Standard Match Rule",menuName = "Match Rules/Standard Match Rule")]
public class StandardMatchRule : MatchRule
{ //match 3 rule
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
        return blasted;
    }
}
[CreateAssetMenu(fileName = "Interactable Match Rule",menuName = "Match Rules/Interactable Match Rule")]
public class InteractableMatchRule : MatchRule
{
    public override bool HasMatch(Vector2Int position, Board board)
    {
        return position == board.MovePositionA || position == board.MovePositionB;
    }
}