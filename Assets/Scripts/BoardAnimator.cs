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
    [SerializeField] private Grid _grid;
    [SerializeField] private BoardAnimatorConfig _boardAnimatorConfig;
    [SerializeField] private ObjectPooler _tileObjectPooler;
    [SerializeField] private ObjectPooler _vfxObjectPooler;


    public void Initialize(uint[,] cells)
    {
        Clear();
        _elements = new Transform[cells.GetLength(0), cells.GetLength(1)];
        for (int y = 0; y < cells.GetLength(1); y++)
        {
            for (int x = 0; x < cells.GetLength(0); x++)
            {
                SpriteRenderer newCell = _tileObjectPooler.Get().GetComponent<SpriteRenderer>();
                _elements[x, y] = newCell.transform;
                newCell.transform.SetParent(transform);
                newCell.transform.position = GetCellWorldPosition(new Vector2Int(x,y));
                newCell.sprite = GameManager.Instance.GameConfig.TileDB.Get(cells[x,y]).Sprite;
            }
        }
    }

    public void Clear()
    {
        if (_elements == null) return;
        foreach (var VARIABLE in _elements)
        {
            _tileObjectPooler.Release(VARIABLE.gameObject);
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
}
