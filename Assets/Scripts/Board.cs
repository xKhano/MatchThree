using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BoardAnimator))]
public sealed class Board : MonoBehaviour
{
    public static Board Singleton { get; private set; }
    
    [SerializeField] private bool debugging = true;
    [SerializeField] private BoardAnimator _boardAnimator; 
    [SerializeField] private int _seed;
    [SerializeField] private Grid _grid;
    private Camera _cam;
    [field: SerializeField] public Vector2Int Size { get; private set; }
    public Vector2Int MovePositionA { get; private set; }
    public Vector2Int MovePositionB { get; private set; }
    public static uint[,] TileHealths;
    public static uint[,] TileIDs;
    public static bool[,] FallingTiles;
    //maybe add uint[,] _tileBoardRuleIDs to add tile rules all applying over the board.
    
    private static HashSet<Vector2Int> hitMask;
    private static HashSet<Vector2Int> blastMask;

    //Input Detection
    private Vector2Int selectedPos;
    private bool _isDragging;
    private Vector3 _offset;
    private Vector3 lastMouseDelta;
    
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
        TileIDs = new uint[Size.x, Size.y];
        Singleton = this;
        _cam = Camera.main;
        UnityEngine.Random.InitState(_seed);
    }
    private void Start()
    {
        RandomGenerateTiles();
        this.enabled = false;
    }
    public void SetActiveInversed(bool active)
    {
        this.enabled = !active;
    }
    private void Update()
    {
        CheckInput();
    }
    private void CheckInput()
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
            if(lastPos == selectedPos) DoMove(selectedPos,MoveDirection.Directionless);
            else if (lastPos == selectedPos + Vector2Int.up)  DoMove(selectedPos,MoveDirection.Up);
            else if (lastPos == selectedPos + Vector2Int.right) DoMove(selectedPos,MoveDirection.Right);
            else if (lastPos == selectedPos + Vector2Int.down) DoMove(selectedPos, MoveDirection.Down);
            else if (lastPos == selectedPos + Vector2Int.left) DoMove(selectedPos, MoveDirection.Left);
            selectedPos = Vector2Int.zero;
        }
    }
    private void RandomGenerateTiles()
    {
        for (int i = 0; i < Size.y; i++)
        {
            for (int j = 0; j < Size.x; j++)
            {
                PutRandomTile(new Vector2Int(j,i));
            }
        }
        _boardAnimator.Initialize(TileIDs);
    }
    public void DoMove(Vector2Int position, MoveDirection direction)
    {
        MovePositionA = position;
        MovePositionB = GetDirectionVector(direction) + position;
        if (ValidateMove() == false) return;
        StartCoroutine(DoMoveRoutine());
    }

    private bool ValidateMove()
    {
        if (MovePositionA.x < 0 || MovePositionA.x >= Size.x || MovePositionB.x < 0 || MovePositionB.x >= Size.x ||
            MovePositionA.y < 0 || MovePositionA.y >= Size.y || MovePositionB.y < 0 || MovePositionB.y > Size.y)
            return false;
        uint tileIdA = TileIDs[MovePositionA.x, MovePositionA.y];
        bool fallingA = FallingTiles[MovePositionA.x, MovePositionA.y];
        if (tileIdA == 0 || fallingA) return false;
        TileConfig configA = GameManager.Instance.GameConfig.TileDB.Get(tileIdA);
        if (configA.Interactable) return true;
        
        uint tileIdB = TileIDs[MovePositionB.x, MovePositionB.y];
        bool fallingB = FallingTiles[MovePositionB.x, MovePositionB.y];
        if (tileIdB == 0 || fallingB) return false;
        return true;
    }
    private IEnumerator DoMoveRoutine()
    {
        hitMask.Clear();
        blastMask.Clear();
        bool blastA;
        if (MovePositionA == MovePositionB) //Single
        {
            blastA = HasMatch(MovePositionA);
            if(IsCellInteractable(MovePositionA)) BlastCell(MovePositionA);
            _boardAnimator.BlastAnimation(blastMask);
        }
        else //Multiple
        {
            bool blastB;
            SwapCells(MovePositionA,MovePositionB);
            yield return _boardAnimator.SlideAnimation(MovePositionA,MovePositionB);
            blastA = HasMatch(MovePositionA);
            blastB = HasMatch(MovePositionB);
            if (!blastA && !blastB) //no blasts
            {
                SwapCells(MovePositionA, MovePositionB);
                yield return _boardAnimator.SlideAnimation(MovePositionA,MovePositionB);
                yield break;
            }
            _boardAnimator.BlastAnimation(blastMask);
        }
        
        foreach (var pos in blastMask)
        {
            TileIDs[pos.x, pos.y] = 0;
        }
        blastMask.Clear();
        hitMask.Clear();
        CollapseTiles();
        yield return _boardAnimator.CollapseAnimation(TileIDs);
    }
    private static Vector2Int GetDirectionVector(MoveDirection direction)
    {
        switch (direction)
        {
            case MoveDirection.Right:
                return Vector2Int.right;
            case MoveDirection.Left:
                return Vector2Int.left;
            case MoveDirection.Up:
                return Vector2Int.up;
            case MoveDirection.Down:
                return Vector2Int.down;
            default:
                return Vector2Int.zero;
        }
    }
    private void CollapseTiles()
    {
        for (int x = 0; x < Size.x; x++)
        {
            int writeY = 0; 
            for (int readY = 0; readY < Size.y; readY++)
            {
                if (GetCellHealth(new Vector2Int(x, readY)) > 0)
                {
                    if (readY != writeY)
                    {
                        TileIDs[x, writeY] = TileIDs[x, readY];
                        TileIDs[x, readY] = 0;
                    }

                    writeY++;
                }
            }
            for (int y = writeY; y < Size.y; y++)
            {
                PutRandomTile(new Vector2Int(x,y));
            }
        }
    }
    private void BlastCell(Vector2Int position,uint damage = 0x1000)
    {
        if (hitMask.Contains(position)) return;
        if (DamageTile(position, damage) == 0) blastMask.Add(position);
        hitMask.Add(position);
        // else if ((_cells[position.x, position.y] & ROCKET_V) == ROCKET_V)
        // {
        //     for (Vector2Int i = new Vector2Int(position.x,0); i.y < Size.y; i.y++)
        //     {
        //         BlastCell(i,damage);
        //     }
        // }
        // else if ((_cells[position.x, position.y] & ROCKET_H) == ROCKET_H)
        // {
        //     for (Vector2Int i = new Vector2Int(0,position.y); i.x < Size.x; i.x++)
        //     {
        //         BlastCell(i,damage);
        //     }
        // }
    }

    private void SwapCells(Vector2Int a, Vector2Int b)
    {
        // ReSharper disable once SwapViaDeconstruction
        uint tempID = TileIDs[a.x, a.y];
        TileIDs[a.x, a.y] = TileIDs[b.x, b.y];
        TileIDs[b.x, b.y] = tempID;
        
        // ReSharper disable once SwapViaDeconstruction
        uint tempHealth = TileHealths[a.x, a.y];
        TileHealths[a.x, a.y] = TileHealths[b.x, b.y];
        TileHealths[b.x, b.y] = tempHealth;
    }
    private bool HasMatch(Vector2Int pos)
    {
        TileConfig config = GetCellType(pos);
        if (config == null) return false;
        

        // foreach (var VARIABLE in config.matches)
        // {
        //     
        // }
        if (IsCellInteractable(pos))
        {
            BlastCell(pos);
            return true;
        }
        bool blasted = false;
        HashSet<Vector2Int> horizontal = new HashSet<Vector2Int>();
        HashSet<Vector2Int> vertical = new HashSet<Vector2Int>();
        for (int i = pos.x; i < Size.x; i++)
        {
            var iPos = new Vector2Int(i, pos.y);
            if (config == GetCellType(iPos)) horizontal.Add(iPos);
            else break;
        }
        for (int i = pos.x; i > -1; i--)
        {
            var iPos = new Vector2Int(i, pos.y);
            if (config == GetCellType(iPos)) horizontal.Add(iPos);
            else break;
        }
        for (int i = pos.y; i < Size.y; i++)
        {
            var iPos = new Vector2Int(pos.x, i);
            if(config == GetCellType(iPos)) vertical.Add(iPos);
            else break;
        }
        for (int i = pos.y; i > -1; i--)
        {
            var iPos = new Vector2Int(pos.x, i);
            if(config == GetCellType(iPos)) vertical.Add(iPos);
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
    private bool IsCellInteractable(Vector2Int pos) => GetCellType(pos).Interactable;
    private TileConfig GetCellType(Vector2Int pos) => GameManager.Instance.GameConfig.TileDB.Get(TileIDs[pos.x, pos.y]);
    private uint GetCellHealth(Vector2Int pos) => TileHealths[pos.x, pos.y];
    private uint DamageTile(Vector2Int pos, uint amount = 1)
    {
        int x = pos.x;
        int y = pos.y;
        uint health = TileHealths[x, y] -= amount;
        hitMask.Add(pos);
        if (health == 0)
        {
            TileIDs[x, y] = 0;
            FallingTiles[x, y] = false;
            blastMask.Add(pos);
        }
        return health;
    }
    private void PutRandomTile(Vector2Int pos)
    {
        uint id = GameManager.Instance.GameConfig.TileDB.GetRandomID();
        PutTile(pos, id);
    }
    private void PutTile(Vector2Int pos, uint tileID)
    {
        TileConfig config = GameManager.Instance.GameConfig.TileDB.Get(tileID);
        TileIDs[pos.x, pos.y] = tileID;
        TileHealths[pos.x, pos.y] = config.StartHealth;
        FallingTiles[pos.x, pos.y] = false;
    }
}
