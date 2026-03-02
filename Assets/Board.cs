using UnityEngine;

public class Board : MonoBehaviour
{
    [field: SerializeField] public Vector2Int Size { get; private set; }
    private int[,] _cells;

    public enum MoveDirection
    {
        Directionless,
        Right,
        Left,
        Up,
        Down
    }
    public DoMove(Vector2Int position,)
}
