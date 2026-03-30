using UnityEngine;

[CreateAssetMenu(fileName = "Interactable Match Rule",menuName = "Match Rules/Interactable Match Rule")]
public class InteractableMatchRule : MatchRule
{
    public override bool HasMatch(Vector2Int position, Board board)
    {
        if (position == board.MovePositionA || position == board.MovePositionB)
        {
            Board.MatchMask.Add(position);
            return true;
        }
        return false;
    }
}