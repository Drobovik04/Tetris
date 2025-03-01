using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using Steamworks;
using System.Linq;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class LobbyController : MonoBehaviour
{
    public static LobbyController Instance;
    private bool FirstLoad = true;

    //UI Elements
    public TMP_Text LobbyNameText;

    //Player Data
    public GameObject PlayerListViewContent;
    public GameObject PlayerListItemPrefab;
    public GameObject LocalPlayerObject;

    //Other Data
    public ulong CurrentLobbyID;
    public bool PlayerItemCreated = false;
    private List<PlayerListItem> PlayerListItems = new List<PlayerListItem>();
    public PlayerObjectController LocalPlayerController;

    //Ready
    public Button StartGameButton;
    public TMP_Text ReadyButtonText;

    //Manager
    private CustomNetworkManager manager;

    private CustomNetworkManager Manager
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

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("Удаляем дубликат LobbyController");
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(Instance);
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "Lobby")
        {
            if(FirstLoad)
            {
                FirstLoad = false;
                return;
            }
            FindLobbyUI();
            UpdateLobbyName();
            PlayerItemCreated = false;
            PlayerListItems.Clear();
            UpdatePlayerList();
            //FindLocalPlayer();
            StartCoroutine(WaitForLocalPlayer());
            //DrawPlayerList();
        }
    }

    public void ReadyPlayer()
    {
        LocalPlayerController.ChangeReady();
    }

    public void UpdateButton()
    {
        if (LocalPlayerController.Ready)
        {
            ReadyButtonText.text = "Unready";
        }
        else
        {
            ReadyButtonText.text = "Ready";
        }
    }

    public void FindLocalPlayerLobby()
    {
        LocalPlayerController = NetworkClient.localPlayer?.GetComponent<PlayerObjectController>();

        if (LocalPlayerController == null)
        {
            StartCoroutine(WaitForLocalPlayer());
        }
        else
        {
            Debug.Log("Локальный игрок найден!");
        }
    }

    private IEnumerator WaitForLocalPlayer()
    {
        while (LocalPlayerController == null)
        {
            yield return new WaitForSeconds(0.5f);
            LocalPlayerController = NetworkClient.localPlayer?.GetComponent<PlayerObjectController>();
        }

        UpdateButton();
    }


    public void CheckIfAllReady()
    {
        bool AllReady = false;

        foreach (PlayerObjectController player in Manager.GamePlayers)
        {
            if (player.Ready)
            {
                AllReady = true;
            }
            else
            {
                AllReady = false;
                break;
            }
        }

        if (AllReady)
        {
            if (LocalPlayerController.PlayerIdNumber == 1)
            {
                StartGameButton.interactable = true;
            }
            else
            {
                StartGameButton.interactable = false;
            }
        }
        else
        {
            StartGameButton.interactable = false;
        }
    }

    public void UpdateLobbyName()
    {
        CurrentLobbyID = Manager.GetComponent<SteamLobby>().CurrentLobbyId;
        LobbyNameText.text = SteamMatchmaking.GetLobbyData(new CSteamID(CurrentLobbyID), "name");
    }
    public void FindLobbyUI()
    {

        LobbyNameText = GameObject.Find("LobbyNameText")?.GetComponent<TMP_Text>();
        PlayerListViewContent = GameObject.Find("Content");
        StartGameButton = GameObject.Find("StartGameButton")?.GetComponent<Button>();
        GameObject ReadyButton = GameObject.Find("ReadyButton");
        ReadyButtonText = ReadyButton?.GetComponentInChildren<TMP_Text>();

        if (StartGameButton != null)
        {
            StartGameButton.onClick.RemoveAllListeners();
            StartGameButton.onClick.AddListener(() => StartGame("Game"));
        }

        Button ReadyButtonButton = ReadyButton.GetComponent<Button>();

        if (ReadyButtonButton)
        {
            ReadyButtonButton.onClick.AddListener(ReadyPlayer);
        }
    }
    public void UpdatePlayerList()
    {
        if (!PlayerItemCreated)
        {
            CreateHostPlayerItem();
        }

        if (PlayerListItems.Count < Manager.GamePlayers.Count)
        {
            CreateClientPlayerItem();
        }

        if (PlayerListItems.Count > Manager.GamePlayers.Count)
        {
            RemovePlayerItem();
        }

        if (PlayerListItems.Count == Manager.GamePlayers.Count)
        {
            UpdatePlayerItem();
        }
    }

    public void FindLocalPlayer()
    {
        LocalPlayerObject = GameObject.Find("LocalGamePlayer");
        LocalPlayerController = LocalPlayerObject.GetComponent<PlayerObjectController>();
    }

    public void CreateHostPlayerItem()
    {
        foreach(PlayerObjectController player in Manager.GamePlayers)
        {
            GameObject NewPlayerItem = Instantiate(PlayerListItemPrefab) as GameObject;
            PlayerListItem NewPlayerItemScript = NewPlayerItem.GetComponent<PlayerListItem>();

            NewPlayerItemScript.PlayerName = player.PlayerName;
            NewPlayerItemScript.ConnectionID = player.ConnectionID;
            NewPlayerItemScript.PlayerSteamID = player.PlayerSteamID;
            NewPlayerItemScript.Ready = player.Ready;
            NewPlayerItemScript.SetPlayerValues();

            NewPlayerItem.transform.SetParent(PlayerListViewContent.transform, false);
            NewPlayerItem.transform.localScale = Vector3.one;
            //NewPlayerItem.transform.localPosition = Vector3.zero + new Vector3(0, (PlayerListItems.Count + 1) * 30, 0);
            //NewPlayerItem.GetComponent<RectTransform>().position = Vector3.zero + new Vector3(0, (PlayerListItems.Count + 1) * 30, 0);

            PlayerListItems.Add(NewPlayerItemScript);
        }
        PlayerItemCreated = true;
    }

    public void CreateClientPlayerItem()
    {
        foreach (PlayerObjectController player in Manager.GamePlayers)
        {
            if (!PlayerListItems.Any(x => x.ConnectionID == player.ConnectionID))
            {
                GameObject NewPlayerItem = Instantiate(PlayerListItemPrefab) as GameObject;
                PlayerListItem NewPlayerItemScript = NewPlayerItem.GetComponent<PlayerListItem>();

                NewPlayerItemScript.PlayerName = player.PlayerName;
                NewPlayerItemScript.ConnectionID = player.ConnectionID;
                NewPlayerItemScript.PlayerSteamID = player.PlayerSteamID;
                NewPlayerItemScript.Ready = player.Ready;
                NewPlayerItemScript.SetPlayerValues();

                NewPlayerItem.transform.SetParent(PlayerListViewContent.transform, false);
                NewPlayerItem.transform.localScale = Vector3.one;
                //NewPlayerItem.transform.localPosition = Vector3.zero + new Vector3(0, (PlayerListItems.Count + 1) * 30, 0);

                PlayerListItems.Add(NewPlayerItemScript);
            }
        }
    }

    public void UpdatePlayerItem()
    {
        List<PlayerListItem> itemsToRemove = PlayerListItems.Where(item => item == null || item.gameObject == null).ToList();
        foreach (var item in itemsToRemove)
        {
            if (item != null)
            {
                PlayerListItems.Remove(item);
                Destroy(item.gameObject);
            }
        }
        foreach (PlayerObjectController player in Manager.GamePlayers)
        {
            foreach (PlayerListItem PlayerListItemScript in PlayerListItems)
            {
                if (PlayerListItemScript.ConnectionID == player.ConnectionID)
                {
                    if (PlayerListItemScript != null)
                    {
                        PlayerListItemScript.PlayerName = player.PlayerName;
                        PlayerListItemScript.Ready = player.Ready;
                        PlayerListItemScript.PlayerReadyText = PlayerListItemScript.gameObject?.transform.Find("PlayerReadyText").GetComponent<TMP_Text>();
                        PlayerListItemScript.SetPlayerValues();
                        if (player == LocalPlayerController)
                        {
                            UpdateButton();
                        }
                    }
                }
            }
        }
        CheckIfAllReady();
    }

    public void RemovePlayerItem()
    {
        List<PlayerListItem> PlayerListItemToRemove = new List<PlayerListItem>();

        foreach(PlayerListItem playerlistItem in PlayerListItems)
        {
            if (!Manager.GamePlayers.Any(x => x.ConnectionID == playerlistItem.ConnectionID))
            {
                PlayerListItemToRemove.Add(playerlistItem);
            }
        }

        if (PlayerListItemToRemove.Count > 0)
        {
            foreach(PlayerListItem playerlistItemToRemove in PlayerListItemToRemove)
            {
                if (playerlistItemToRemove != null)
                {
                    GameObject ObjectToRemove = playerlistItemToRemove.gameObject;
                    PlayerListItems.Remove(playerlistItemToRemove);
                    Destroy(ObjectToRemove);
                    ObjectToRemove = null;
                }
            }
        }
    }

    public void DrawPlayerList()
    {
        foreach (PlayerObjectController player in Manager.GamePlayers)
        {
            GameObject NewPlayerItem = Instantiate(PlayerListItemPrefab) as GameObject;
            PlayerListItem NewPlayerItemScript = NewPlayerItem.GetComponent<PlayerListItem>();

            NewPlayerItemScript.PlayerName = player.PlayerName;
            NewPlayerItemScript.ConnectionID = player.ConnectionID;
            NewPlayerItemScript.PlayerSteamID = player.PlayerSteamID;
            NewPlayerItemScript.Ready = player.Ready;
            NewPlayerItemScript.SetPlayerValues();

            NewPlayerItem.transform.SetParent(PlayerListViewContent.transform, false);
            NewPlayerItem.transform.localScale = Vector3.one;
            //NewPlayerItem.transform.localPosition = Vector3.zero + new Vector3(0, (PlayerListItems.Count + 1) * 30, 0);
        }
    }

    public void StartGame(string sceneName)
    {
        LocalPlayerController.CanStartGame(sceneName);
    }

}
