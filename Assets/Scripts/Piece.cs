using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class Piece : MonoBehaviour
{
    public PlayerInput input {get; private set;}
    public Board board { get; private set; }
    public TetrominoData data { get; private set; }
    public Vector3Int[] cells { get; private set; }
    public Vector3Int position { get; private set; }
    public void Initialize(Board board, Vector3Int position, TetrominoData data)
    {
        this.board = board;
        this.position = position;
        this.data = data;
        if (input == null)
        {
            input = GetComponent<PlayerInput>();
            foreach (InputAction a in input.actions)
                a.started += Move;
        }
        if (cells == null)
        {
            cells = new Vector3Int[data.cells.Length];
        }
        for (int i = 0; i < data.cells.Length; i++)
        {
            cells[i] = (Vector3Int)data.cells[i];
        }

    }
    public void Move(InputAction.CallbackContext context)
    {
        board.Clear(this);
        if (context.action == input.actions["Left"])
        {
            Vector3Int newPosition = position + Vector3Int.left;
            bool valid = board.IsValidPosition(this, newPosition);
            if (valid)
            {
                position = newPosition;
            }
        }
        else
        if (context.action == input.actions["Right"])
        {
            Vector3Int newPosition = position + Vector3Int.right;
            bool valid = board.IsValidPosition(this, newPosition);
            if (valid)
            {
                position = newPosition;
            }
        }
        else
        if (context.action == input.actions["Down"])
        {
            Vector3Int newPosition = position + Vector3Int.down;
            bool valid = board.IsValidPosition(this, newPosition);
            if (valid)
            {
                position = newPosition;
            }
        }
        else
        if(context.action == input.actions["FastDown"])
        {
            Vector3Int newPosition = position + Vector3Int.down;
            bool valid = board.IsValidPosition(this, newPosition);
            while (valid)
            if (valid)
            {
                position = newPosition;
                newPosition = position + Vector3Int.down;
                valid = board.IsValidPosition(this, newPosition);
            }
        }
        board.Set(this);
    }
}
