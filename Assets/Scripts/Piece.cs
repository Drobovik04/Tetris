using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Mirror;
using Unity.VisualScripting;

public class Piece : NetworkBehaviour
{
    public PlayerInput input {get; private set;}
    public Board board { get; private set; }
    public TetrominoData data { get; private set; }
    public Vector3Int[] cells { get; private set; }

    public CustomNetworkManager manager;

    [SyncVar]
    public Vector3Int position;
    public Ghost ghost { get; private set; }

    [SyncVar]
    public int rotationIndex;
    public float stepDelay = 1f;
    public float lockDelay = 0.5f;
    private float stepTime;
    private float lockTime;
    public void Initialize(Board board, Vector3Int position, TetrominoData data, Ghost ghost)
    {
        this.board = board;
        this.position = position;
        this.data = data;
        rotationIndex = 0;
        stepTime = Time.time + stepDelay;
        lockTime = 0f;
        this.ghost = ghost;
        if (input == null)
        {
            //input = GetComponent<PlayerInput>();
            //foreach (InputAction a in input.actions)
            //    a.started += /*Move*/OnInput;
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
    private void OnInput(InputAction.CallbackContext context)
    {
        //if (!authority)
        //    return;
        //if (manager.GamePlayers[manager.currentPlayerIndex] == board.player)
        // Отправляем команду на сервер с названием действия
        CmdProcessInput(context);
    }

    [Server]
    public void ProcessInput(string actionName)
    {
        board.Clear(this);
        if (actionName == "LeftRotation")
        {
            Rotate(-1);
        }
        else if (actionName == "RightRotation")
        {
            Rotate(1);
        }
        else if (actionName == "Left")
        {
            Vector3Int newPos = position + Vector3Int.left;
            if (board.IsValidPosition(this, newPos))
            {
                position = newPos;
                lockTime = 0f;
            }
        }
        else if (actionName == "Right")
        {
            Vector3Int newPos = position + Vector3Int.right;
            if (board.IsValidPosition(this, newPos))
            {
                position = newPos;
            }
        }
        else if (actionName == "Down")
        {
            Vector3Int newPos = position + Vector3Int.down;
            if (board.IsValidPosition(this, newPos))
            {
                position = newPos;
            }
        }
        else if (actionName == "FastDown")
        {
            Vector3Int newPos = position + Vector3Int.down;
            while (board.IsValidPosition(this, newPos))
            {
                position = newPos;
                newPos = position + Vector3Int.down;
            }
            Lock();
        }
        board.Set(this);
        RpcUpdateGhost();
    }

    [Command]
    void CmdProcessInput(InputAction.CallbackContext context)
    {
        if (!CustomNetworkManager.Instance.IsCurrentPlayer(board.player))
        {
            // Можно отправить сообщение или просто игнорировать
            return;
        }
        board.Clear(this);
        if (context.action == input.actions["LeftRotation"])
        {
            Rotate(-1);
        }
        else if (context.action == input.actions["RightRotation"])
        {
            Rotate(1);
        }
        else if (context.action == input.actions["Left"])
        {
            Vector3Int newPos = position + Vector3Int.left;
            if (board.IsValidPosition(this, newPos))
            {
                position = newPos;
                lockTime = 0f;
            }
        }
        else if (context.action == input.actions["Right"])
        {
            Vector3Int newPos = position + Vector3Int.right;
            if (board.IsValidPosition(this, newPos))
            {
                position = newPos;
            }
        }
        else if (context.action == input.actions["Down"])
        {
            Vector3Int newPos = position + Vector3Int.down;
            if (board.IsValidPosition(this, newPos))
            {
                position = newPos;
            }
        }
        else if (context.action == input.actions["FastDown"])
        {
            Vector3Int newPos = position + Vector3Int.down;
            while (board.IsValidPosition(this, newPos))
            {
                position = newPos;
                newPos = position + Vector3Int.down;
            }
            Lock();
        }
        board.Set(this);
        RpcUpdateGhost();
    }
    public void Move(InputAction.CallbackContext context)
    {
        board.Clear(this);
        if (context.action == input.actions["LeftRotation"])
        {
            Rotate(-1);
        }
        else
        if (context.action == input.actions["RightRotation"])
        {
            Rotate(1);
        }
        else
        if (context.action == input.actions["Left"])
        {
            Vector3Int newPosition = position + Vector3Int.left;
            bool valid = board.IsValidPosition(this, newPosition);
            if (valid)
            {
                position = newPosition;
                lockTime = 0f;
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
            Lock();
        }
        board.Set(this);
        ghost.AfterAll();
    }
    private void Rotate(int direction)
    {
        int originalRotation = rotationIndex;
        rotationIndex = Wrap(rotationIndex + direction, 0, 4);
        ApplyRotationMatrix(direction);
        if (!TestWallKicks(rotationIndex, direction))
        {
            rotationIndex = originalRotation;
            ApplyRotationMatrix(-direction);
        }
    }
    private void ApplyRotationMatrix(int direction)
    {
        for (int i = 0; i < cells.Length; i++)
        {
            Vector3 cell = cells[i];
            int x, y;
            switch (data.tetromino)
            {
                case Tetromino.I:
                case Tetromino.O:
                        cell.x -= 0.5f;
                        cell.y -= 0.5f;
                        x = Mathf.CeilToInt((cell.x * Data.RotationMatrix[0] * direction) + (cell.y * Data.RotationMatrix[1] * direction));
                        y = Mathf.CeilToInt((cell.x * Data.RotationMatrix[2] * direction) + (cell.y * Data.RotationMatrix[3] * direction));
                        break;
                    
                default:
                        x = Mathf.RoundToInt((cell.x * Data.RotationMatrix[0] * direction) + (cell.y * Data.RotationMatrix[1] * direction));
                        y = Mathf.RoundToInt((cell.x * Data.RotationMatrix[2] * direction) + (cell.y * Data.RotationMatrix[3] * direction));
                        break;
            }
            cells[i] = new Vector3Int(x, y, 0);
        }
    }
    private bool TestWallKicks(int rotationIndex, int rotationDirection)
    {
        int wallKickIndex = GetWallKickIndex(rotationIndex, rotationDirection);
        for (int i=0;i<data.wallKicks.GetLength(1);i++)
        {
            Vector2Int translation = data.wallKicks[wallKickIndex, i];
            Vector3Int valid = position + (Vector3Int)translation;
            if (board.IsValidPosition(this, valid)) {board.Clear(this); position = valid; lockTime = 0f; board.Set(this) ; return true; }
        }
        return false;
    }
    private int GetWallKickIndex(int rotationIndex, int rotationDirection)
    {
        int wallKickIndex = rotationIndex * 2;
        if (rotationDirection < 0)
        {
            wallKickIndex--;
        }
        return Wrap(wallKickIndex, 0, data.wallKicks.GetLength(0));
    }

    private int Wrap(int input, int min, int max)
    {
        if (input < min)
        {
            return max - (min - input) % (max - min);
        }
        else
        {
            return min + (input - min) % (max - min);
        }
    }
    private void Update()
    {
        //lockTime += Time.deltaTime;
        //if (Time.time>=stepTime)
        //{
        //    Step();
        //}
        //ghost.AfterAll();
        if (!isServer)
            return; // Движение управляется сервером
        lockTime += Time.deltaTime;
        if (Time.time >= stepTime)
        {
            Step();
        }
    }
    private void Step()
    {
        board.Clear(this);
        stepTime = Time.time + stepDelay;
        Vector3Int newPosition = position + Vector3Int.down;
        bool valid = board.IsValidPosition(this, newPosition);
        if (valid)
        {
            position = newPosition;
        }
        if (lockTime >= lockDelay && !valid)
        {
            Lock();
        }
        board.Set(this);
    }
    private void Lock()
    {
        board.Set(this);
        board.ClearLines();
        CustomNetworkManager.Instance.NextTurn();
        board.SpawnPiece();
    }

    [ClientRpc]
    void RpcUpdateGhost()
    {
        ghost.AfterAll();
    }
}
