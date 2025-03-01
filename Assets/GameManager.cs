using Mirror;
using TMPro;
using UnityEngine;

public class GameManager : NetworkBehaviour
{
    public TMP_Text Text;
    public GameObject Button;
    public CustomNetworkManager NetworkManager;
    [ClientRpc]
    public void RpcUpdateCurrentPlayer(int index)
    {
        Debug.Log("Сейчас ходит игрок с индексом: " + index);
        // Обновление UI
    }

    [ClientRpc]
    public void RpcAnnounceWinner(string winnerName)
    {
        Debug.Log("Победитель: " + winnerName);
        // Оповещение о победе
        ShowVictory(winnerName);
    }
    public void ShowVictory(string winnerName)
    {
        Text.text = "Победитель: " + winnerName;
        Text.gameObject.SetActive(true);
        Button.SetActive(true);
    }
    // Можно добавить другие методы, управляющие игрой
    public void OnBackToLobby()
    {
        NetworkManager.ChangeScene("Lobby");
    }
}
