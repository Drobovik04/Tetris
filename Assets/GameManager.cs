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
        Debug.Log("������ ����� ����� � ��������: " + index);
        // ���������� UI
    }

    [ClientRpc]
    public void RpcAnnounceWinner(string winnerName)
    {
        Debug.Log("����������: " + winnerName);
        // ���������� � ������
        ShowVictory(winnerName);
    }
    public void ShowVictory(string winnerName)
    {
        Text.text = "����������: " + winnerName;
        Text.gameObject.SetActive(true);
        Button.SetActive(true);
    }
    // ����� �������� ������ ������, ����������� �����
    public void OnBackToLobby()
    {
        NetworkManager.ChangeScene("Lobby");
    }
}
