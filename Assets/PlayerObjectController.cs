using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using Steamworks;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using System.Threading;

public class PlayerObjectController : NetworkBehaviour
{
    //Player Data
    [SyncVar] public int ConnectionID;
    [SyncVar] public int PlayerIdNumber;
    [SyncVar] public ulong PlayerSteamID;
    [SyncVar(hook = nameof(PlayerNameUpdate))] public string PlayerName;
    [SyncVar(hook = nameof(PlayerReadyUpdate))] public bool Ready;
    [SyncVar(hook = nameof(PlayerChangeScore))] public int Score;
    [SerializeField] private GameObject boardPrefab;
    public GameObject BoardInstance { get; private set; }
    private PlayerInput playerInput;

    private CustomNetworkManager manager;

    public CustomNetworkManager Manager
    {
        get
        {
            if (manager != null)
            {
                return manager;
            }
            return manager = CustomNetworkManager.singleton as CustomNetworkManager;
        }
    }

    public Piece CurrentPiece { get; private set; }


    void Awake()
    {
        if (FindObjectsOfType<PlayerObjectController>().Length > 1)
        {
            Destroy(gameObject);
            return;
        }

        DontDestroyOnLoad(gameObject);
    }
    public void Start()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    public void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != "Game")
            return;
        //BoardInstance = GameObject.FindGameObjectWithTag("TilemapBoard").GetComponent<Board>();
        //BoardInstance.GetComponentInChildren<Board>().player = this;
        StartCoroutine(WaitForClientReadyAndSpawn());
        //LobbyController.Instance.FindLocalPlayer();

    }

    private IEnumerator WaitForClientReadyAndSpawn()
    {
        // Ждем, пока клиент подключен и помечен как ready.
        while (!NetworkClient.isConnected || !NetworkClient.ready)
        {
            yield return null;
        }
        // Теперь можно безопасно вызвать [Command]
        CmdSpawnBoard();
    }

    public void PlayerReadyUpdate(bool oldValue, bool newValue)
    {
        if (isServer)
        {
            this.Ready = newValue;
        }
        if (isClient)
        {
            LobbyController.Instance.UpdatePlayerList();
        }
    }

    [Command]
    private void CmdSetPlayerReady()
    {
        this.PlayerReadyUpdate(this.Ready, !this.Ready);
    }

    public void ChangeReady()
    {
        if (authority)
        {
            CmdSetPlayerReady();
        }
    }

    public override void OnStartAuthority()
    {
        CmdSetPlayerName(SteamFriends.GetPersonaName().ToString());
        gameObject.name = "LocalGamePlayer";
        LobbyController.Instance.FindLocalPlayer();
        LobbyController.Instance.UpdateLobbyName();
        playerInput = GetComponent<PlayerInput>();

        playerInput.actions["Left"].performed += ctx => OnInput(ctx, "Left");
        playerInput.actions["Right"].performed += ctx => OnInput(ctx, "Right");
        playerInput.actions["Down"].performed += ctx => OnInput(ctx, "Down");
        playerInput.actions["FastDown"].performed += ctx => OnInput(ctx, "FastDown");
        playerInput.actions["LeftRotation"].performed += ctx => OnInput(ctx, "LeftRotation");
        playerInput.actions["RightRotation"].performed += ctx => OnInput(ctx, "RightRotation");

        //CmdSpawnBoard();
    }
    [Command(requiresAuthority = false)]
    void CmdSpawnBoard()
    {
        if (BoardInstance == null)
        {
            BoardInstance = Instantiate(boardPrefab);
            NetworkServer.Spawn(BoardInstance.gameObject/*, connectionToClient*/);
        }
        CurrentPiece = BoardInstance.GetComponentInChildren<Board>().activePiece;
    }
    void OnInput(InputAction.CallbackContext context, string actionName)
    {
        if (!authority) return;

        CmdProcessPieceInput(actionName);
    }
    [Command]
    void CmdProcessPieceInput(string actionName)
    {
        if (CurrentPiece != null)
        {
            CurrentPiece.ProcessInput(actionName);
        }
    }
    public override void OnStartClient()
    {
        Manager.GamePlayers.Add(this);
        LobbyController.Instance.UpdateLobbyName();
        LobbyController.Instance.UpdatePlayerList();
    }

    public override void OnStopClient()
    {
        Manager.GamePlayers.Remove(this);
        LobbyController.Instance.UpdatePlayerList();
    }

    [Command]
    private void CmdSetPlayerName(string PlayerName)
    {
        this.PlayerNameUpdate(this.PlayerName, PlayerName);
    }

    public void PlayerNameUpdate(string oldValue, string newValue)
    {
        if (isServer)
        {
            this.PlayerName = newValue;
        }

        if (isClient)
        {
            LobbyController.Instance.UpdatePlayerList();
        }
    }

    public void CanStartGame(string sceneName)
    {
        if (authority)
        {
            CmdCanStartGame(sceneName);
        }
    }

    [Command]
    public void CmdCanStartGame(string sceneName)
    {
        manager.StartGame(sceneName);
    }

    public void PlayerChangeScore(int oldValue, int newValue)
    {
        if (isServer)
        {
            this.Score = newValue;
        }

        if (isClient)
        {
            LobbyController.Instance.UpdatePlayerList();
        }
    }
}
