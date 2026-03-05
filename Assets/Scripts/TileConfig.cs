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
    [field:SerializeField] public IBlastPattern[] BlastPatterns { get; private set; }= null;
    [field:SerializeField] public IMatchRule[] MatchRules { get; private set; }
}

public interface IBlastPattern
{
    public void Blast(Vector2Int originPosition, Board board);
}
public interface IMatchRule
{
    public bool HasMatch(Vector2Int position,Board board);
}

[CreateAssetMenu(fileName = "Row Blast Pattern",menuName = "Blast Pattern/Row Blast Pattern")]
public class RowBlastPattern : ScriptableObject,IBlastPattern
{
    public void Blast(Vector2Int originPosition, Board board)
    {
        
    }
}
[CreateAssetMenu(fileName = "Column Blast Pattern",menuName = "Blast Pattern/Column Blast Pattern")]
public class ColumnBlastPattern : ScriptableObject,IBlastPattern
{
    public void Blast(Vector2Int originPosition, Board board)
    {
        
    }
}
[CreateAssetMenu(fileName = "Standard Match Rule",menuName = "Match Rules/Standard Match Rule")]
public class StandardMatchRule : ScriptableObject,IMatchRule
{
    public bool HasMatch(Vector2Int position, Board board)
    {
        return false;
    }
}
[CreateAssetMenu(fileName = "Interactable Match Rule",menuName = "Match Rules/Interactable Match Rule")]
public class InteractableMatchRule : ScriptableObject,IMatchRule
{
    public bool HasMatch(Vector2Int position, Board board)
    {
        return position == board.MovePositionA || position == board.MovePositionB;
    }
}