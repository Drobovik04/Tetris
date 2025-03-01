using Mirror;
using Steamworks;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class CustomNetworkManager : NetworkManager
{
    [SerializeField] private PlayerObjectController GamePlayerPrefab;
    [SerializeField] private GameObject gameManagerPrefab;

    public static CustomNetworkManager Instance;
    public List<PlayerObjectController> GamePlayers { get; } = new List<PlayerObjectController>();

    public int currentPlayerIndex = 0;

    public int winningScore = 100;
    public static GameManager GameManagerInstance;
    public override void Awake()
    {
        base.Awake();
        Instance = this;
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        // Спавним GameManager на сервере и сохраняем ссылку
        GameObject gm = Instantiate(gameManagerPrefab);
        DontDestroyOnLoad(gm);
        NetworkServer.Spawn(gm);
        GameManagerInstance = gm.GetComponent<GameManager>();
        //DontDestroyOnLoad(GameManagerInstance);
        SceneManager.sceneLoaded += OnSceneLoaded;
    }
    public override void OnServerChangeScene(string newSceneName)
    {
        if (newSceneName == "Lobby")
        {
            foreach (var player in GamePlayers)
            {
                DontDestroyOnLoad(player.gameObject);
            }
        }

        base.OnServerChangeScene(newSceneName);
    }
    public void OnSceneLoaded(Scene scene, LoadSceneMode loadSceneMode)
    {
        if (scene.name == "Game")
        {
            GameManagerInstance.Text = GameObject.FindGameObjectWithTag("WinText").GetComponent<TMP_Text>();
            GameObject button = GameObject.FindGameObjectWithTag("ReturnToLobbyButton");
            button.SetActive(false);
            button.GetComponent<Button>().onClick.AddListener(GameManagerInstance.OnBackToLobby);
            GameManagerInstance.Button = button;
            GameManagerInstance.NetworkManager = this;
        }
    }
    [Server]
    public void NextTurn()
    {
        currentPlayerIndex = (currentPlayerIndex + 1) % GamePlayers.Count;
        GameManagerInstance.RpcUpdateCurrentPlayer(currentPlayerIndex);
    }

    [Server]
    public void AddScore(PlayerObjectController playerFrom, int points)
    {
        PlayerObjectController player = GamePlayers.Find(p => p == playerFrom);
        if (player != null)
        {
            player.Score += points;
            if (player.Score >= winningScore)
            {
                GameManagerInstance.RpcAnnounceWinner(player.PlayerName);
            }
        }
    }

    void OnCurrentPlayerChanged(int oldIndex, int newIndex)
    {
        // Обновляем UI или логику, если необходимо
        Debug.Log("Ходит другой");
    }

    public override void OnServerAddPlayer(NetworkConnectionToClient conn)
    {
        if (SceneManager.GetActiveScene().name == "Lobby")
        {
            if (GamePlayers.Any(p => p.connectionToClient == conn))
            {
                Debug.LogWarning("Игрок уже существует, не создаем нового!");
                return;
            }
            PlayerObjectController GamePlayerInstance = Instantiate(GamePlayerPrefab);

            GamePlayerInstance.ConnectionID = conn.connectionId;
            GamePlayerInstance.PlayerIdNumber = GamePlayers.Count + 1;
            GamePlayerInstance.PlayerSteamID = (ulong)SteamMatchmaking.GetLobbyMemberByIndex((CSteamID)SteamLobby.Instance.CurrentLobbyId, GamePlayers.Count);

            NetworkServer.AddPlayerForConnection(conn, GamePlayerInstance.gameObject);
        }
    }

    public void StartGame(string sceneName)
    {
        ServerChangeScene(sceneName);
    }
    public void ChangeScene(string sceneName)
    {
        ServerChangeScene(sceneName);
    }
    public bool IsCurrentPlayer(PlayerObjectController player)
    {
        return true;
        return GamePlayers[currentPlayerIndex] == player;
    }
}
