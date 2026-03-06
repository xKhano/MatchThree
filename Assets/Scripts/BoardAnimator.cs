using System;
using System.Collections;
using System.Collections.Generic;
using DefaultNamespace;
using PrimeTween;
using Unity.VisualScripting;
using UnityEngine;

public class BoardAnimator : MonoBehaviour
{
    private Transform[,] _elements;
    [SerializeField] private Board _board;
    [SerializeField] private Grid _grid;
    [SerializeField] private BoardAnimatorConfig _boardAnimatorConfig;
    [SerializeField] private ObjectPooler _tileObjectPooler;
    [SerializeField] private ObjectPooler _vfxObjectPooler;
    private Dictionary<Vector2Int, Tween> tileToFallingTween;

    public void Initialize(uint[,] cells)
    {
        Clear();
        _elements = new Transform[cells.GetLength(0), cells.GetLength(1)];
        for (int y = 0; y < cells.GetLength(1); y++)
        {
            for (int x = 0; x < cells.GetLength(0); x++)
            {
                Transform newObject = GetNewTile(cells[x, y]);
                newObject.position = GetCellWorldPosition(new Vector2Int(x,y));
                _elements[x, y] = newObject;
            }
        }
    }

    private Transform GetNewTile(uint id)
    {
        GameObject gamObject = _tileObjectPooler.Get();
        gamObject.transform.SetParent(transform);
        gamObject.GetComponent<SpriteRenderer>().sprite = GameManager.Instance.GameConfig.TileDB.Get(id).Sprite;
        return gamObject.transform;
    }
    public void Clear()
    {
        if (_elements == null) return;
        foreach (var VARIABLE in _elements)
        {
            if(VARIABLE != null) _tileObjectPooler.Release(VARIABLE.gameObject);
        }
    }

    public void BlastAnimation(HashSet<Vector2Int> blastIndexes)
    {
        foreach (var VARIABLE in blastIndexes)
        {
            BlastAnimation(VARIABLE);
        }
    }

    public void BlastAnimation(Vector2Int pos)
    {
        var worldPos = GetCellWorldPosition(pos);
        GameObject vfx = _vfxObjectPooler.Get();
        vfx.transform.position = worldPos;
        vfx.GetComponent<ObjectPoolVFX>().Pool = _vfxObjectPooler;
        _tileObjectPooler.Release(_elements[pos.x, pos.y].gameObject);
        _elements[pos.x, pos.y] = null;
    }
    
    public IEnumerator SlideAnimation(Vector2Int posA, Vector2Int posB)
    {
        Vector3 worldPosA = GetCellWorldPosition(posA);
        Vector3 worldPosB = GetCellWorldPosition(posB);
        Tween.Position(_elements[posB.x, posB.y], worldPosA, _boardAnimatorConfig.CellSlideDuration);
        Tween.Position(_elements[posA.x, posA.y],worldPosB,_boardAnimatorConfig.CellSlideDuration);
        SwitchReferences(posA,posB);
        yield return new WaitForSeconds(_boardAnimatorConfig.CellSlideDuration);
    }

    private void SwitchReferences(Vector2Int a, Vector2Int b)
    {
        // ReSharper disable once SwapViaDeconstruction
        var temp =_elements[b.x, b.y];
        _elements[b.x, b.y] = _elements[a.x, a.y];
        _elements[a.x, a.y] = temp;
    }

    private Vector3 GetCellWorldPosition(Vector2Int position) => _grid.GetCellCenterWorld((Vector3Int)position);

    public IEnumerator CollapseAnimation(uint[,] cells)
    {
        int width = cells.GetLength(0);
        int height = cells.GetLength(1);

        float duration = _boardAnimatorConfig.CellFallDuration;

        for (int x = 0; x < width; x++)
        {
            int writeY = 0;

            for (int readY = 0; readY < height; readY++)
            {
                if (_elements[x, readY] != null)
                {
                    if (readY != writeY)
                    {
                        Transform falling = _elements[x, readY];
                        _elements[x, writeY] = falling;
                        _elements[x, readY] = null;

                        Vector3 targetPos = GetCellWorldPosition(new Vector2Int(x, writeY));
                        Tween.Position(falling, targetPos, duration);
                    }
                    
                    var sr = _elements[x, writeY].GetComponent<SpriteRenderer>();
                    sr.sprite = GameManager.Instance.GameConfig.TileDB.Get(cells[x, writeY]).Sprite;

                    writeY++;
                }
            }

            for (int y = writeY; y < height; y++)
            {
                SpriteRenderer newTile = _tileObjectPooler.Get().GetComponent<SpriteRenderer>();
                newTile.transform.SetParent(transform);

                _elements[x, y] = newTile.transform;

                newTile.sprite = GameManager.Instance.GameConfig.TileDB.Get(cells[x,y]).Sprite;
                
                Vector3 spawnPos = GetCellWorldPosition(new Vector2Int(x, height + (y - writeY)));
                newTile.transform.position = spawnPos;

                Vector3 targetPos = GetCellWorldPosition(new Vector2Int(x, y));
                Tween.Position(newTile.transform, targetPos, duration);
            }
        }

        yield return new WaitForSeconds(duration);
    }

    public void CollapseAnimation(Vector2Int start, Vector2Int end)
    {
        StartCoroutine(TileCollapseRoutine(start, end));
    }
    public void CollapseNewTileAnimation(uint id,Vector2Int start, Vector2Int end)
    {
        StartCoroutine(NewTileCollapseRoutine(id,start, end));
    }

    private IEnumerator TileCollapseRoutine(Vector2Int start,Vector2Int end)
    {
        Transform element = _elements[start.x, start.y];
        _elements[start.x, start.y] = null;
        _elements[end.x, end.y] = element;
        yield return Tween.Position(element, GetCellWorldPosition(end), _boardAnimatorConfig.CellFallDuration).ToYieldInstruction();
        Board.FallingLockMask[end.x, end.y] = false;
    }
    private IEnumerator NewTileCollapseRoutine(uint id,Vector2Int startTilePosition,Vector2Int end)
    {
        Transform element = GetNewTile(id);
        element.position = GetCellWorldPosition(startTilePosition);
        yield return Tween.Position(element, GetCellWorldPosition(end), _boardAnimatorConfig.CellFallDuration).ToYieldInstruction();
        Board.FallingLockMask[end.x, end.y] = false;
    }
}
