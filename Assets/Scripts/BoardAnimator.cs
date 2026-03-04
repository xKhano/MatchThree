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
    [SerializeField] private BoardConfig _boardConfig;
    [SerializeField] private ObjectPooler _tileObjectPooler;
    [SerializeField] private ObjectPooler _vfxObjectPooler;


    public void Initialize(uint[,] cells)
    {
        _elements = new Transform[cells.GetLength(0), cells.GetLength(1)];
        for (int y = 0; y < cells.GetLength(1); y++)
        {
            for (int x = 0; x < cells.GetLength(0); x++)
            {
                SpriteRenderer newCell = _tileObjectPooler.Get().GetComponent<SpriteRenderer>();
                _elements[x, y] = newCell.transform;
                newCell.transform.SetParent(transform);
                newCell.transform.position = GetCellWorldPosition(new Vector2Int(x,y));
                newCell.sprite = _boardConfig.GetSprite(cells[x,y]);
            }
        }
    }

    public IEnumerator BlastAnimation(HashSet<Vector2Int> blastIndexes)
    {
        foreach (var VARIABLE in blastIndexes)
        {
            var worldPos = GetCellWorldPosition(VARIABLE);
            GameObject vfx = _vfxObjectPooler.Get();
            vfx.transform.position = worldPos;
            vfx.GetComponent<ObjectPoolVFX>().Pool = _vfxObjectPooler;
            _tileObjectPooler.Release(_elements[VARIABLE.x, VARIABLE.y].gameObject);
        }
        yield return new WaitForSeconds(_boardAnimatorConfig.CellBlastDuration);
    }
    
    public IEnumerator SlideAnimation(Vector2Int posA, Board.MoveDirection direction)
    {
        Vector2Int posB = posA;
        Vector3 worldPosA = GetCellWorldPosition(posA);
        Vector3 worldPosB;
        switch (direction)
        {
            case Board.MoveDirection.Directionless:
                yield return null;
                break;
            case Board.MoveDirection.Right:
                posB += Vector2Int.right;
                worldPosB = GetCellWorldPosition(posB);
                Tween.PositionX(_elements[posB.x, posB.y], worldPosA.x, _boardAnimatorConfig.CellSlideDuration);
                Tween.PositionX(_elements[posA.x, posA.y],worldPosB.x,_boardAnimatorConfig.CellSlideDuration);
                SwitchReferences(posA,posB);
                yield return new WaitForSeconds(_boardAnimatorConfig.CellSlideDuration);
                break;
            case Board.MoveDirection.Left:
                posB += Vector2Int.left;
                worldPosB = GetCellWorldPosition(posB);
                Tween.PositionX(_elements[posB.x, posB.y], worldPosA.x, _boardAnimatorConfig.CellSlideDuration);
                Tween.PositionX(_elements[posA.x, posA.y],worldPosB.x,_boardAnimatorConfig.CellSlideDuration);
                SwitchReferences(posA,posB);
                yield return new WaitForSeconds(_boardAnimatorConfig.CellSlideDuration);
                break;
            case Board.MoveDirection.Up:
                posB += Vector2Int.up;
                worldPosB = GetCellWorldPosition(posB);
                Tween.PositionY(_elements[posB.x, posB.y], worldPosA.y, _boardAnimatorConfig.CellSlideDuration);
                Tween.PositionY(_elements[posA.x, posA.y],worldPosB.y,_boardAnimatorConfig.CellSlideDuration);
                SwitchReferences(posA,posB);
                yield return new WaitForSeconds(_boardAnimatorConfig.CellSlideDuration);
                break;
            case Board.MoveDirection.Down:
                posB += Vector2Int.down;
                worldPosB = GetCellWorldPosition(posB);
                Debug.Log(worldPosB);
                Tween.PositionY(_elements[posB.x, posB.y], worldPosA.y, _boardAnimatorConfig.CellSlideDuration);
                Tween.PositionY(_elements[posA.x, posA.y],worldPosB.y,_boardAnimatorConfig.CellSlideDuration);
                SwitchReferences(posA,posB);
                yield return new WaitForSeconds(_boardAnimatorConfig.CellSlideDuration);
                break;
        }

        Debug.Log("A: " + posA + " B: " + posB);
    }

    private void SwitchReferences(Vector2Int a, Vector2Int b)
    {
        // ReSharper disable once SwapViaDeconstruction
        var temp =_elements[b.x, b.y];
        _elements[b.x, b.y] = _elements[a.x, a.y];
        _elements[a.x, a.y] = temp;
    }

    private Vector3 GetCellWorldPosition(Vector2Int position) => _grid.GetCellCenterWorld((Vector3Int)position);
}
