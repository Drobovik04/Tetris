using UnityEngine.Tilemaps;
using System;
using UnityEngine;

public enum Tetromino
{
    I,
    O,
    T,
    J,
    L,
    S,
    Z
}
[Serializable]
public struct TetrominoData
{
    public Tetromino tetromino;
    public Tile tile;
    public Vector2Int[] cells { get; private set; }
    public void Initialize()
    {
        cells = Data.Cells[this.tetromino];
    }
}