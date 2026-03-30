using UnityEngine;

[CreateAssetMenu(fileName = "Row Blast Pattern",menuName = "Blast Pattern/Row Blast Pattern")]
public class RowBlastPattern : BlastPattern
{
    public override void Blast(Vector2Int originPosition, Board board)
    {
        for (Vector2Int i = new Vector2Int(0,originPosition.y); i.x < board.Size.x; i.x++)
        {
            board.DamageTile(i);
        }
    }
}