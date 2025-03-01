using UnityEngine;
using UnityEngine.Tilemaps;
using Mirror;
using Telepathy;
using System.Linq;

public class Board : NetworkBehaviour
{
    public PlayerObjectController player;
    public Tilemap tilemap { get; private set; }
    public Piece activePiece { get; private set; }
    public TetrominoData[] tetrominoes;
    public Vector3Int spawnPosition;
    public Ghost ghost;
    public Vector2Int boardSize = new Vector2Int(10, 20);
    public RectInt Bounds
    {
        get
        {
            Vector2Int position = new Vector2Int(-boardSize.x / 2, -boardSize.y / 2);
            return new RectInt(position, boardSize);
        }
    }
    private void Awake()
    {
        tilemap = GetComponentInChildren<Tilemap>();
        activePiece = GetComponentInChildren<Piece>();
        for (int i = 0; i < tetrominoes.Length; i++)
        {
            tetrominoes[i].Initialize();
        }
        //SpawnPiece();
    }
    private void Start()
    {
        SpawnPiece();
    }

    [Server]
    public void SpawnPiece()
    {
        int random = Random.Range(0, tetrominoes.Length);
        TetrominoData data = tetrominoes[random];
        activePiece.Initialize(this, spawnPosition, data, ghost);
        if (IsValidPosition(activePiece, activePiece.position))
        {
            Set(activePiece);
        }
        else
        {
            tilemap.ClearAllTiles();
            Destroy(activePiece);
            
            CustomNetworkManager.GameManagerInstance.RpcAnnounceWinner(CustomNetworkManager.Instance.GamePlayers.OrderByDescending(x => x.Score).First().PlayerName);
        }
        RpcRefreshBoard();
    }

    [Server]
    public void Set(Piece piece)
    {
        for (int i = 0;i< piece.cells.Length; i++)
        {
            Vector3Int tilePosition = piece.cells[i] + piece.position;
            tilemap.SetTile(tilePosition, piece.data.tile);
        }
       
    }

    [Server]
    public void Clear(Piece piece)
    {
        for (int i = 0; i < piece.cells.Length; i++)
        {
            Vector3Int tilePosition = piece.cells[i] + piece.position;
            Vector3 s = tilemap.cellBounds.center;
            tilemap.SetTile(tilePosition,  null);
        }
    }
    public bool IsValidPosition(Piece piece, Vector3Int position)
    {
        RectInt bounds = Bounds;
        for (int i=0;i<piece.cells.Length;i++)
        {
            Vector3Int tilePosition = piece.cells[i] + position;
            if (!bounds.Contains((Vector2Int)tilePosition))
            {
                return false;
            }
            if (tilemap.HasTile(tilePosition))
            {
                return false;
            }
        }
        return true;
    }

    [Server]
    public void ClearLines()
    {
        int linesCleared = 0;
        int row = Bounds.yMin;
        while (row < Bounds.yMax)
        {
            if (IsLineFull(row))
            {
                LineClear(row);
                linesCleared++;
            }
            else row++;
        }

        if (linesCleared > 0)
        {
            // Например, начисляем 10 очков за каждую линию
            CustomNetworkManager.Instance.AddScore(LobbyController.Instance.LocalPlayerController, linesCleared * 10);
            if (LobbyController.Instance.LocalPlayerController.Score >= CustomNetworkManager.Instance.winningScore)
            {
                tilemap.ClearAllTiles();
                Destroy(activePiece);
                CustomNetworkManager.GameManagerInstance.RpcAnnounceWinner(LobbyController.Instance.LocalPlayerController.PlayerName);
            }
        }
    }
    private bool IsLineFull(int row)
    {
        for (int col = Bounds.xMin; col < Bounds.xMax; col++)
        {
            Vector3Int position = new Vector3Int(col, row, 0);
            if (!tilemap.HasTile(position))
            {
                return false;
            }
        }
        return true;
    }

    [Server]
    private void LineClear(int row)
    {
        for (int col = Bounds.xMin; col < Bounds.xMax; col++)
        {
            Vector3Int position = new Vector3Int(col, row, 0);
            tilemap.SetTile(position,null);
        }
        while (row < Bounds.yMax)
        {
            for (int col = Bounds.xMin; col < Bounds.xMax; col++)
            {
                Vector3Int position = new Vector3Int(col, row + 1, 0);
                TileBase above = tilemap.GetTile(position);
                position = new Vector3Int(col, row, 0);
                tilemap.SetTile(position, above);
            }
            row++;
        }
    }

    [ClientRpc]
    void RpcRefreshBoard()
    {
        // Клиентская логика обновления доски, например, перерисовка Tilemap.
        // Если ваша визуальная часть доски завязана на tilemap, можно просто не делать ничего,
        // так как изменения tilemap происходят автоматически.
    }
}
