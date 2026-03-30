using UnityEngine;

[CreateAssetMenu(fileName = "Column Blast Pattern",menuName = "Blast Pattern/Column Blast Pattern")]
public class ColumnBlastPattern : BlastPattern
{
    public override void Blast(Vector2Int originPosition, Board board)
    {
        for (Vector2Int i = new Vector2Int(originPosition.x,0); i.y < board.Size.y; i.y++)
        {
            board.DamageTile(i);
        }
    }
}