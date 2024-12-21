// File: WebSocketManager.cs

using UnityEngine;
using WebSocketSharp;
using System;
using Newtonsoft.Json;

public class WebSocketManager : MonoBehaviour
{
    public static WebSocketManager Instance { get; private set; }

    [SerializeField] private string serverUrl = "wss://hisyam99-websockettest.deno.dev";
    [SerializeField] private string authToken = "12345";
    private WebSocket _webSocket;
    private bool _isJoiningRoom = false; // Flag to track if we are joining a room

    public string AuthToken => authToken;
    public WebSocket WebSocket => _webSocket;

    public event Action OnJoinSuccess;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void JoinRoom(string username, string roomId)
    {
        if (_webSocket == null || _webSocket.ReadyState != WebSocketState.Open)
        {
            ConnectToWebSocket();
        }

        _isJoiningRoom = true; // Set the flag to true before sending the join request
        var message = new object[]
        {
            "join",
            new
            {
                roomId = roomId,
                authToken = authToken,
                username = username
            }
        };
        string jsonMessage = JsonConvert.SerializeObject(message);
        _webSocket.Send(jsonMessage);
    }

    private void ConnectToWebSocket()
    {
        _webSocket = new WebSocket(serverUrl);
        _webSocket.SslConfiguration.EnabledSslProtocols = System.Security.Authentication.SslProtocols.Tls12;
        _webSocket.OnOpen += HandleConnectionOpen;
        _webSocket.OnMessage += HandleIncomingMessage;
        _webSocket.OnError += HandleConnectionError;
        _webSocket.OnClose += HandleConnectionClosed;

        try
        {
            _webSocket.Connect();
        }
        catch (Exception ex)
        {
            Debug.LogError($"Koneksi gagal: {ex.Message}");
        }
    }

    private void HandleConnectionOpen(object sender, EventArgs e)
    {
        Debug.Log("WebSocket terhubung.");
    }

    private void HandleIncomingMessage(object sender, MessageEventArgs e)
    {
        Debug.Log($"Received message: {e.Data}");
        var messageArray = JsonConvert.DeserializeObject<object[]>(e.Data);
        if (messageArray != null && messageArray.Length > 0)
        {
            string eventName = messageArray[0].ToString();
            Debug.Log($"Event name: {eventName}");
            if (eventName == "welcome")
            {
                if (_isJoiningRoom)
                {
                    Debug.Log("Triggering OnJoinSuccess");
                    _isJoiningRoom = false;
                    OnJoinSuccess?.Invoke();
                }
                else
                {
                    Debug.Log("Welcome message received but not joining a room");
                }
            }
        }
    }

    private void HandleConnectionError(object sender, ErrorEventArgs e)
    {
        Debug.LogError($"Kesalahan WebSocket: {e.Message}");
    }

    private void HandleConnectionClosed(object sender, CloseEventArgs e)
    {
        Debug.Log("WebSocket terputus.");
    }

    private void OnApplicationQuit()
    {
        if (_webSocket != null && _webSocket.ReadyState == WebSocketState.Open)
        {
            _webSocket.Close();
            Debug.Log("WebSocket ditutup.");
        }
    }
}