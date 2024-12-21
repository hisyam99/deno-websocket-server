// File: PageManager.cs

using UnityEngine;

public class PageManager : MonoBehaviour
{
    [SerializeField] private GameObject joinRoomPageUI;
    [SerializeField] private GameObject chatPageUI;
    [SerializeField] private JoinRoomManager joinRoomManager;
    [SerializeField] private ChatManager chatManager;

    private void Start()
    {
        ShowJoinRoomPage();
    }

    public void ShowJoinRoomPage()
    {
        joinRoomPageUI.SetActive(true);
        chatPageUI.SetActive(false);
    }

    public void ShowChatPage()
    {
        Debug.Log("Showing ChatPage");
        joinRoomPageUI.SetActive(false);
        chatPageUI.SetActive(true);
        chatManager.InitializeUI();
        chatManager.ConnectToWebSocket();
    }

    public void OnJoinRoomSuccess(string username, string roomId)
    {
        Debug.Log("PageManager.OnJoinRoomSuccess called");
        chatManager.SetCurrentRoomId(roomId);
        ShowChatPage();
    }
}