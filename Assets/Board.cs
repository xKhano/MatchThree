using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using UnityEngine;

public sealed class Board : MonoBehaviour
{
    [field: SerializeField] public Vector2Int Size { get; private set; }
    [SerializeField] private BoardConfig _config;
    private static uint[,] _cells;
    private static HashSet<Vector2Int> hitMask;
    private static HashSet<Vector2Int> blastMask;
    
    private const uint HEALTH_ONE = 0x1000;
    private const uint IS_RESIN = 0x0800;
    private const uint IS_BOX = 0x0400;
    //interactablesu
    private const uint RED = 0x0001;
    private const uint GREEN = 0x0002;
    private const uint BLUE = 0x0003;
    private const uint YELLOW = 0x0004;
    //
    private const uint ROCKET_H = 0x0005;
    private const uint ROCKET_V = 0x0006;
    private const uint BARREL = 0x0007;
    private const uint DISCO_BALL = 0x0008;

    public event Action<Vector2Int, MoveDirection> OnMove;

    public enum MoveDirection
    {
        Directionless,
        Right,
        Left,
        Up,
        Down
    }

    public void Awake()
    {
        hitMask = new HashSet<Vector2Int>(Size.x * Size.y);
        blastMask = new HashSet<Vector2Int>(Size.x * Size.y);
        _cells = new uint[Size.x, Size.y];
    }
    
    public void DoMove(Vector2Int position, MoveDirection direction)
    {
        hitMask.Clear();
        blastMask.Clear();
        switch (direction)
        {
            case MoveDirection.Directionless:
                if(IsCellInteractable(position)) BlastCell(position);
                break;
            case MoveDirection.Right:
                SwapCells(position,position + Vector2Int.right);
                if (!TryBlast(position) && !TryBlast(position + Vector2Int.right)) SwapCells(position,position + Vector2Int.right);
                break;
            case MoveDirection.Left:
                SwapCells(position,position + Vector2Int.left);
                if (!TryBlast(position) && !TryBlast(position + Vector2Int.left)) SwapCells(position, position + Vector2Int.left);
                break;
            case MoveDirection.Up:
                SwapCells(position,position + Vector2Int.up);
                if (!TryBlast(position) && !TryBlast(position + Vector2Int.up)) SwapCells(position, position + Vector2Int.up);
                break;
            case MoveDirection.Down:
                SwapCells(position,position + Vector2Int.down);
                if (!TryBlast(position) && !TryBlast(position + Vector2Int.down)) SwapCells(position, position + Vector2Int.down);
                break;
        }
    }

    private void BlastCell(Vector2Int position,uint damage = 0x1000)
    {
        if (hitMask.Contains(position)) return;
        if (ReduceCellHealth(position, damage) == 0) blastMask.Add(position);
        hitMask.Add(position);
        if ((_cells[position.x, position.y] & DISCO_BALL) == DISCO_BALL)
        {
            
        }
        else if ((_cells[position.x, position.y] & BARREL) == BARREL)
        {
            BlastCell(position+Vector2Int.right,damage);
            BlastCell(position+Vector2Int.left,damage);
            BlastCell(position+Vector2Int.up,damage);
            BlastCell(position+Vector2Int.down,damage);
            BlastCell(position+Vector2Int.right+Vector2Int.up,damage);
            BlastCell(position+Vector2Int.right+Vector2Int.down,damage);
            BlastCell(position+Vector2Int.left+Vector2Int.up,damage);
            BlastCell(position+Vector2Int.left+Vector2Int.down,damage);
        }
        else if ((_cells[position.x, position.y] & ROCKET_V) == ROCKET_V)
        {
            for (Vector2Int i = new Vector2Int(position.x,0); i.y < Size.y; i.y++)
            {
                BlastCell(i,damage);
            }
        }
        else if ((_cells[position.x, position.y] & ROCKET_H) == ROCKET_H)
        {
            for (Vector2Int i = new Vector2Int(0,position.y); i.x < Size.x; i.x++)
            {
                BlastCell(i,damage);
            }
        }
    }

    private void SwapCells(Vector2Int a, Vector2Int b)
    {
        // ReSharper disable once SwapViaDeconstruction
        uint temp = _cells[a.x, a.y];
        _cells[a.x, a.y] = _cells[b.x, b.y];
        _cells[b.x, b.y] = temp;
    }
    private bool TryBlast(Vector2Int pos)
    {
        if (IsCellInteractable(pos))
        {
            BlastCell(pos);
            return true;
        }
        else
        {
            bool blasted = false;
            uint type = GetCellType(pos);
            List<Vector2Int> horizontal = new List<Vector2Int>();
            List<Vector2Int> vertical = new List<Vector2Int>();
            for (int i = 0; i < Size.x; i++)
            {
                var iPos = new Vector2Int(i, pos.y);
                if(type == GetCellType(iPos)) horizontal.Add(iPos);
            }
            for (int i = 0; i < Size.y; i++)
            {
                var iPos = new Vector2Int(i, pos.y);
                if(type == GetCellType(iPos)) vertical.Add(iPos);
            }

            if (horizontal.Contains(pos))
            {
                foreach(var i in horizontal) BlastCell(i);
                blasted = true;
            }
            if (vertical.Contains(pos))
            {
                foreach(var i in vertical) BlastCell(i);
                blasted = true;
            }

            return blasted;
        }
    } 
    private bool IsCellInteractable(Vector2Int pos) => GetCellType(pos) > 4;
    private uint GetCellType(Vector2Int pos) => _cells[pos.x, pos.y] &= 0x000F;
    private uint GetCellHealth(Vector2Int pos) => (_cells[pos.x, pos.y] & 0xF000) >> 12;

    private uint ReduceCellHealth(Vector2Int pos, uint amount = 1)
    {
        uint temp = _cells[pos.x, pos.y];
        temp = temp & 0xF000;
        amount = (uint)Mathf.Clamp(amount, 0, temp);
        temp = temp - amount;
        _cells[pos.x, pos.y] &= 0x0FFF;
        _cells[pos.x, pos.y] ^= temp;
        return temp;
    }
}
