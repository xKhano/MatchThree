using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BoardAnimator))]
public sealed class Board : MonoBehaviour
{
    public static Board Singleton { get; private set; }
    
    public uint Moves { get; private set; }
    [field:SerializeField] public LevelConfig CurrentLevel { get; private set; }
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

    public event Action OnLevelSetup;
    public event Func<TileConfig> OnTileBlast;
    public event Func<Vector2Int> OnTileCollapseDone;
    
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
        OnLevelSetup?.Invoke();
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
    public void RandomGenerateTiles()
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

    public bool IsMoveableToCell(Vector2Int pos)
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
            GetMatchesWholeBoard();
            if (MatchMask.Count < 1) yield break;
        }
        else //Slide
        {
            SwapCells(MovePositionA,MovePositionB);
            GetMatchesWholeBoard();
            yield return _boardAnimator.SlideAnimation(MovePositionA,MovePositionB);
            if (MatchMask.Count < 1) //no blasts
            {
                SwapCells(MovePositionA, MovePositionB);
                yield return _boardAnimator.SlideAnimation(MovePositionA,MovePositionB);
                yield break;
            }
        }
        do
        {
            foreach (var position in MatchMask)
            {
                DamageTile(position);
            }
            MatchMask.Clear();
            if (BlastMask.Count < 1) break;
            while (BlastMask.Count > 0)
            {
                var enumerator = BlastMask.GetEnumerator();
                enumerator.MoveNext();
                var pos = enumerator.Current;
                BlastTile(pos);
            }
            BlastMask.Clear();
            CollapseTiles();
            while (HasFallingTiles()) yield return null;
            GetMatchesWholeBoard();
        } while (MatchMask.Count > 0);
        MatchMask.Clear();
        BlastMask.Clear();
    }

    private void GetMatchesWholeBoard()
    {
        for (Vector2Int pos = Vector2Int.zero; pos.y < Size.y; pos.y++)
        {
            for (pos.x = 0; pos.x < Size.x; pos.x++)
            {
                TryMatch(pos);
            }
        }
    }
    private void CollapseTiles()
    {
        for (Vector2Int i = Vector2Int.zero; i.x < Size.x; i.x++)
        {
            i.y = 0;
            Queue<Vector2Int> fallingTilesStartPos = new Queue<Vector2Int>(Size.y);
            Queue<uint> fallingTilesID = new Queue<uint>(Size.y);
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
                        fallingTilesStartPos.Enqueue(i);
                        fallingTilesID.Enqueue(TileIDs[i.x,i.y]);
                        TileIDs[i.x, i.y] = 0;
                    }
                }
                if(firstEmptyIndex != -1) FallingLockMask[i.x, i.y] = true;
            }

            if (firstEmptyIndex == -1) continue;
            i.y = firstEmptyIndex;
            int generations = 0;
            for (; i.y < Size.y; i.y++)
            {
                if (fallingTilesStartPos.Count > 0)
                {
                    Vector2Int startPos = fallingTilesStartPos.Dequeue();
                    uint id = fallingTilesID.Dequeue();
                    TileIDs[i.x, i.y] = id;
                    _boardAnimator.CollapseAnimation(startPos,i);
                }// you are here
                else
                {
                    uint id = PutRandomTile(i);
                    _boardAnimator.CollapseNewTileAnimation(id,new Vector2Int(i.x,Size.y+1+generations),i);
                    generations++;
                }
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
    public void DamageTile(Vector2Int pos, uint amount = 1)
    {
        int x = pos.x;
        int y = pos.y;
        if (!IsPositionInBounds(pos) || TileIDs[x, y] == 0) return;
        amount = (uint)Mathf.Clamp(amount, 0, TileHealths[x, y]);
        uint health = TileHealths[x, y] -= amount;
        if (health == 0)
        {
            BlastMask.Add(pos);
        }
    }
    private uint PutRandomTile(Vector2Int pos)
    {
        uint id = GameManager.Instance.GameConfig.TileDB.GetRandomID();
        PutTile(pos, id);
        return id;
    }
    private void PutTile(Vector2Int pos, uint tileID)
    {
        if (IsPositionInBounds(pos) == false) return;
        TileConfig config = GameManager.Instance.GameConfig.TileDB.Get(tileID);
        TileIDs[pos.x, pos.y] = tileID;
        TileHealths[pos.x, pos.y] = config.StartHealth;
        FallingLockMask[pos.x, pos.y] = false;
    }

    private bool HasFallingTiles()
    {
        for (int x = 0; x < Size.x; x++)
            for (int y = 0; y < Size.y; y++)
                if (FallingLockMask[x, y]) return true;
        return false;
    }

    public bool IsPositionInBounds(Vector2Int pos)
    {
        if (pos.x < 0 || pos.x >= Size.x || pos.y < 0 || pos.y >= Size.y) return false;
        return true;
    }
    public enum MoveDirection
    {
        Directionless,
        Right,
        Left,
        Up,
        Down
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
}
[CreateAssetMenu(fileName = "LevelConfig",menuName = "Config/Level Config")]
public class LevelConfig : ScriptableObject
{
    [field:SerializeField] public uint ID { get; private set; }
    [field:SerializeField] public Vector2Int BoardSize { get; private set; }
    
}
