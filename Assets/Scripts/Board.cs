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
    public static bool[,] FallingLockMask;
    //maybe add uint[,] _tileBoardRuleIDs to add tile rules all applying over the board.
    
    public static HashSet<Vector2Int> MatchMask;
    public static HashSet<Vector2Int> BlastMask;

    //Input Detection
    private Vector2Int selectedPos;
    private bool _isDragging;
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
        Singleton = this;
        _cam = Camera.main;
        UnityEngine.Random.InitState(_seed);
    }
    private void Start()
    {
        SetupLevel();
        this.enabled = false;
    }

    private void SetupLevel()
    {
        MatchMask = new HashSet<Vector2Int>(Size.x * Size.y);
        BlastMask = new HashSet<Vector2Int>(Size.x * Size.y);
        TileHealths = new uint[Size.x, Size.y];
        FallingLockMask = new bool[Size.x, Size.y];
        TileIDs = new uint[Size.x, Size.y];
        RandomGenerateTiles();
    }
    public void SetActiveInversed(bool active)
    {
        this.enabled = !active;
    }
    private void Update()
    {
        CheckInput();
        if(Input.GetKeyDown(KeyCode.A)) _boardAnimator.Initialize(TileIDs);
    }
    private void CheckInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Vector2 mouseWorld = _cam.ScreenToWorldPoint(Input.mousePosition);
            selectedPos = (Vector2Int)_grid.WorldToCell(mouseWorld);

            if (IsPositionInBounds(selectedPos))
            {
                _isDragging = true;
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
    private void DoMove(Vector2Int position, MoveDirection direction)
    {
        MovePositionA = position;
        MovePositionB = GetDirectionVector(direction) + position;
        if (ValidateMove() == false) return;
        StartCoroutine(DoMoveRoutine());
    }
    private bool ValidateMove()
    {
        if (IsPositionInBounds(MovePositionA) == false ||
            IsPositionInBounds(MovePositionB) == false ||
            IsMoveableToCell(MovePositionA) == false || 
            ((MovePositionA != MovePositionB) && (IsMoveableToCell(MovePositionB) == false)))
            return false;
        return true;
    }

    private bool IsMoveableToCell(Vector2Int pos)
    {
        uint tileId = TileIDs[pos.x, pos.y];
        TileConfig config = GameManager.Instance.GameConfig.TileDB.Get(tileId);
        bool fallLock = FallingLockMask[pos.x, pos.y];
        bool moveable = config.Moveable;
        if (tileId == 0 || fallLock || !moveable) return false;
        return true;
    }
    private IEnumerator DoMoveRoutine()
    {
        if (MovePositionA == MovePositionB) //Tap
        {
            SearchMatchesWholeBoard();
            if (MatchMask.Count < 1) yield break;
        }
        else //Slide
        {
            SwapCells(MovePositionA,MovePositionB);
            SearchMatchesWholeBoard();
            yield return _boardAnimator.SlideAnimation(MovePositionA,MovePositionB);
            if (MatchMask.Count < 1) //no blasts
            {
                SwapCells(MovePositionA, MovePositionB);
                yield return _boardAnimator.SlideAnimation(MovePositionA,MovePositionB);
                yield break;
            }
        }
        foreach (var position in MatchMask)
        {
            DamageTile(position);
        }
        if (BlastMask.Count < 1) yield break;
        while (BlastMask.Count > 0)
        {
            var enumerator = BlastMask.GetEnumerator();
            enumerator.MoveNext();
            var pos = enumerator.Current;
            BlastTile(pos);
        }
        CollapseTiles();
        // yield return _boardAnimator.CollapseAnimation(TileIDs);
        MatchMask.Clear();
        BlastMask.Clear();
    }

    private void SearchMatchesWholeBoard()
    {
        for (Vector2Int pos = Vector2Int.zero; pos.y < Size.y; pos.y++)
        {
            for (;pos.x < Size.x; pos.x++)
            {
                TryMatch(pos);
            }
        }
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
        for (Vector2Int i = Vector2Int.zero; i.x < Size.x; i.x++)
        {
            i.y = 0;
            Dictionary<Vector2Int,uint> fallingTilesInColumn = new Dictionary<Vector2Int, uint>(Size.y);
            int firstEmptyIndex = -1;
            int zeroCount = 0;
            for (; i.y < Size.y; i.y++)
            {
                if (TileIDs[i.x, i.y] == 0)
                {
                    if (firstEmptyIndex == -1) firstEmptyIndex = i.y;
                    zeroCount++;
                }
                else
                {
                    if(firstEmptyIndex != -1) //will fall
                    {
                        fallingTilesInColumn.Add(i,TileIDs[i.x, i.y]);
                        TileIDs[i.x, i.y] = 0;
                    }
                }
                if(firstEmptyIndex != -1) FallingLockMask[i.x, i.y] = true;
            }

            for (int y = firstEmptyIndex; y < Size.y; y++)
            {
                if (fallingTilesInColumn.Count > 0)
                {
                    var enumerator = fallingTilesInColumn.GetEnumerator();
                    enumerator.MoveNext();
                    TileIDs[i.x, y] = enumerator.Current.Value;
                    _boardAnimator.CollapseAnimation(enumerator.Current.Key,new Vector2Int(i.x,y));
                }// you are here
                else PutRandomTile(new Vector2Int(i.x,y));
            }
        }
    }
    private void BlastTile(Vector2Int position,uint damage = 0x1000)
    {
        if (!BlastMask.Contains(position))
            throw new Exception("cell needs to be marked from BlastMask to be blastable.");
        uint id =  TileIDs[position.x, position.y];
        TileConfig config = GameManager.Instance.GameConfig.TileDB.Get(id);
        foreach (var pattern in config.BlastPatterns)
        {
            pattern.Blast(position,this);
        }
        _boardAnimator.BlastAnimation(position);
        TileIDs[position.x, position.y] = 0;
        FallingLockMask[position.x, position.y] = false;
        TileHealths[position.x, position.y] = 0;
        BlastMask.Remove(position);
    }

    private void SwapCells(Vector2Int a, Vector2Int b)
    {
        if (IsPositionInBounds(a) == false || IsPositionInBounds(b) == false) throw new IndexOutOfRangeException();
        // ReSharper disable once SwapViaDeconstruction
        uint tempID = TileIDs[a.x, a.y];
        TileIDs[a.x, a.y] = TileIDs[b.x, b.y];
        TileIDs[b.x, b.y] = tempID;
        // ReSharper disable once SwapViaDeconstruction
        uint tempHealth = TileHealths[a.x, a.y];
        TileHealths[a.x, a.y] = TileHealths[b.x, b.y];
        TileHealths[b.x, b.y] = tempHealth;
    }
    private bool TryMatch(Vector2Int pos)
    {
        if (MatchMask.Contains(pos)) return true;
        TileConfig config = GetTileType(pos);
        if (config == null) throw new Exception("Invalid TileID for Config Query.");
        bool match = false;
        foreach (var rule in config.MatchRules)
        {
            if (rule.HasMatch(pos, this)) match = true;
        }
        return match;
    }
    
    private TileConfig GetTileType(Vector2Int pos) => GameManager.Instance.GameConfig.TileDB.Get(TileIDs[pos.x, pos.y]);
    private void DamageTile(Vector2Int pos, uint amount = 1)
    {
        int x = pos.x;
        int y = pos.y;
        amount = (uint)Mathf.Clamp(amount, 0, TileHealths[x, y]);
        uint health = TileHealths[x, y] -= amount;
        if (health == 0)
        {
            BlastMask.Add(pos);
        }
    }
    private void PutRandomTile(Vector2Int pos)
    {
        uint id = GameManager.Instance.GameConfig.TileDB.GetRandomID();
        PutTile(pos, id);
    }
    private void PutTile(Vector2Int pos, uint tileID)
    {
        if (IsPositionInBounds(pos) == false) return;
        TileConfig config = GameManager.Instance.GameConfig.TileDB.Get(tileID);
        TileIDs[pos.x, pos.y] = tileID;
        TileHealths[pos.x, pos.y] = config.StartHealth;
        FallingLockMask[pos.x, pos.y] = false;
    }

    public bool IsPositionInBounds(Vector2Int pos)
    {
        if (pos.x < 0 || pos.x >= Size.x || pos.y < 0 || pos.y >= Size.y) return false;
        return true;
    }
}
