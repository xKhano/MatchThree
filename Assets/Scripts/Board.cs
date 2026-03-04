using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = System.Random;

[RequireComponent(typeof(BoardAnimator))]
public sealed class Board : MonoBehaviour
{
    public static Board Singleton { get; private set; }
    [SerializeField] private bool debugging = true;
    [SerializeField] private BoardAnimator _boardAnimator; 
    [field: SerializeField] public Vector2Int Size { get; private set; }
    [SerializeField] private int _seed;
    [SerializeField] private BoardConfig _config;
    [SerializeField] private Grid _grid;
    private Camera _cam;
    private static uint[,] _cells;
    private static HashSet<Vector2Int> hitMask;
    private static HashSet<Vector2Int> blastMask;
    private Coroutine currentCoroutine;

    private Vector2Int selectedPos;
    private bool _isDragging;
    private Vector3 _offset;
    private Vector3 lastMouseDelta;

    private const uint HEALTH_ONE = 0x1000;
    private const uint IS_RESIN = 0x0800;
    private const uint IS_BOX = 0x0400;
    //interactables
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
    public event Action<List<Vector2Int>> OnBlast;

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
        Singleton = this;
        _cam = Camera.main;
        UnityEngine.Random.InitState(_seed);
    }

    private void Start()
    {
        GenerateCells();
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Vector2 mouseWorld = _cam.ScreenToWorldPoint(Input.mousePosition);
            selectedPos = (Vector2Int)_grid.WorldToCell(mouseWorld);

            if (selectedPos.x > -1 && selectedPos.x < Size.x && selectedPos.y > -1 && selectedPos.y < Size.y)
            {
                _isDragging = true;
                _offset = transform.position - (Vector3)mouseWorld;
            }
        }
        if (_isDragging && Input.GetMouseButton(0))
        {
            //ondrag
        }
        if (_isDragging && Input.GetMouseButtonUp(0))
        {
            _isDragging = false; 
            Vector2 mouseUpWorldPos = _cam.ScreenToWorldPoint(Input.mousePosition);
            
            var lastPos = (Vector2Int)_grid.WorldToCell(mouseUpWorldPos);
            if(debugging) Debug.Log("startCell: "+selectedPos + " endCell: "+ lastPos);
            if(lastPos == selectedPos) DoMove(selectedPos,MoveDirection.Directionless);
            else if (lastPos == selectedPos + Vector2Int.up)  DoMove(selectedPos,MoveDirection.Up);
            else if (lastPos == selectedPos + Vector2Int.right) DoMove(selectedPos,MoveDirection.Right);
            else if (lastPos == selectedPos + Vector2Int.down) DoMove(selectedPos, MoveDirection.Down);
            else if (lastPos == selectedPos + Vector2Int.left) DoMove(selectedPos, MoveDirection.Left);
            selectedPos = Vector2Int.zero;
        }
    }
    private void OnDropped()
    {
        Debug.Log("Dropped: " + gameObject.name);
    }

    private void GenerateCells()
    {
        for (int i = 0; i < Size.y; i++)
        {
            for (int j = 0; j < Size.x; j++)
            {
                _cells[j, i] = GenerateRandomCell();
            }
        }
        _boardAnimator.Initialize(_cells);
    }
    public void DoMove(Vector2Int position, MoveDirection direction)
    {
        if (currentCoroutine != null) return;
        currentCoroutine = StartCoroutine(DoMoveRoutine(position, direction));
    }
    private IEnumerator DoMoveRoutine(Vector2Int position, MoveDirection direction)
    {
        hitMask.Clear();
        blastMask.Clear();
        Vector2Int posB;
        switch (direction)
        {
            case MoveDirection.Directionless:
                if(IsCellInteractable(position)) BlastCell(position);
                break;
            case MoveDirection.Right:
                posB = position + Vector2Int.right;
                SwapCells(position,posB);
                yield return _boardAnimator.SlideAnimation(position,direction);
                if (!TryBlast(position) && !TryBlast(posB))
                {
                    SwapCells(position, posB);
                    yield return _boardAnimator.SlideAnimation(position,direction);
                }
                break;
            case MoveDirection.Left:
                posB = position + Vector2Int.left;
                SwapCells(position,posB);
                yield return _boardAnimator.SlideAnimation(position,direction);
                if (!TryBlast(position) && !TryBlast(posB))
                {
                    SwapCells(position, posB);
                    yield return _boardAnimator.SlideAnimation(position,direction);
                }
                break;
            case MoveDirection.Up:
                posB = position + Vector2Int.up;
                SwapCells(position,posB);
                yield return _boardAnimator.SlideAnimation(position,direction);
                if (!TryBlast(position) && !TryBlast(posB))
                {
                    SwapCells(position, posB);
                    yield return _boardAnimator.SlideAnimation(position,MoveDirection.Down); 
                }
                break;
            case MoveDirection.Down:
                posB = position + Vector2Int.down;
                SwapCells(position,posB);
                yield return _boardAnimator.SlideAnimation(position,direction);
                if (!TryBlast(position) && !TryBlast(posB))
                {
                    SwapCells(position, posB);
                    yield return _boardAnimator.SlideAnimation(position,MoveDirection.Up); 
                }
                break;
        }

        yield return _boardAnimator.BlastAnimation(blastMask);
        currentCoroutine = null;
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
        bool blasted = false;
        uint type = GetCellType(pos);
        HashSet<Vector2Int> horizontal = new HashSet<Vector2Int>();
        HashSet<Vector2Int> vertical = new HashSet<Vector2Int>();
        for (int i = pos.x; i < Size.x; i++)
        {
            var iPos = new Vector2Int(i, pos.y);
            if (type == GetCellType(iPos)) horizontal.Add(iPos);
            else break;
        }
        for (int i = pos.x; i > -1; i--)
        {
            var iPos = new Vector2Int(i, pos.y);
            if (type == GetCellType(iPos)) horizontal.Add(iPos);
            else break;
        }
        for (int i = pos.y; i < Size.y; i++)
        {
            var iPos = new Vector2Int(i, pos.y);
            if(type == GetCellType(iPos)) vertical.Add(iPos);
            else break;
        }
        for (int i = pos.y; i > -1; i--)
        {
            var iPos = new Vector2Int(i, pos.y);
            if(type == GetCellType(iPos)) vertical.Add(iPos);
            else break;
        }

        if (horizontal.Contains(pos) && horizontal.Count >= 3)
        {
            foreach(var i in horizontal) BlastCell(i);
            blasted = true;
        }
        if (vertical.Contains(pos) && vertical.Count >= 3)
        {
            foreach(var i in vertical) BlastCell(i);
            blasted = true;
        }

        return blasted;
        
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

    private static uint GenerateRandomCell()
    {
        uint output = HEALTH_ONE;
        output += (uint)UnityEngine.Random.Range(1, 5);
        return output;
    }
}
