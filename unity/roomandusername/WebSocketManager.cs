using UnityEngine;
using UnityEngine.UIElements;
using WebSocketSharp;
using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

public class WebSocketManager : MonoBehaviour
{
    [SerializeField] private UIDocument uiDocument;
    [SerializeField] private string serverUrl = "wss://hisyam99-websockettest.deno.dev";
    [SerializeField] private string authToken = "12345";
    private UIElements _ui;
    private WebSocket _webSocket;
    private string _clientId = string.Empty;
    private string _currentRoomId = "room1"; // Default room ID
    private string _username = "User"; // Default username

    private class UIElements
    {
        public Label ConnectionStatus;
        public TextField MessageInput;
        public TextField TargetUserIdInput;
        public TextField RoomIdInput; // New input for room ID
        public TextField UsernameInput; // New input for username
        public Button BroadcastButton;
        public Button PrivateMessageButton;
        public Button JoinRoomButton; // New button to join a room
        public ScrollView MessageLog;
    }

    private void Awake()
    {
        ValidateUIDocument();
        InitializeUIReferences();
        SetupUIEventHandlers();
    }

    private void Start()
    {
        Application.runInBackground = true;
        ConnectToWebSocket();
    }

    private void ValidateUIDocument()
    {
        if (uiDocument == null)
        {
            Debug.LogError("[WebSocketManager] Dokumen UI tidak ditetapkan di Inspector!");
            enabled = false;
        }
    }

    private void InitializeUIReferences()
    {
        var root = uiDocument.rootVisualElement;
        _ui = new UIElements
        {
            ConnectionStatus = root.Q<Label>("ConnectionStatus"),
            MessageInput = root.Q<TextField>("MessageInput"),
            TargetUserIdInput = root.Q<TextField>("TargetUserIdInput"),
            RoomIdInput = root.Q<TextField>("RoomIdInput"), // New input for room ID
            UsernameInput = root.Q<TextField>("UsernameInput"), // New input for username
            BroadcastButton = root.Q<Button>("BroadcastButton"),
            PrivateMessageButton = root.Q<Button>("PrivateMessageButton"),
            JoinRoomButton = root.Q<Button>("JoinRoomButton"), // New button to join a room
            MessageLog = root.Q<ScrollView>("MessageLog")
        };
    }

    private void SetupUIEventHandlers()
    {
        _ui.BroadcastButton.clicked += () => SendBroadcastMessage(_ui.MessageInput.value);
        _ui.PrivateMessageButton.clicked += () => SendPrivateMessage(_ui.TargetUserIdInput.value, _ui.MessageInput.value);
        _ui.JoinRoomButton.clicked += () => JoinRoom(_ui.RoomIdInput.value); // New handler for joining a room
        _ui.UsernameInput.RegisterValueChangedCallback(evt => _username = evt.newValue);
    }

    private void ConnectToWebSocket()
    {
        _webSocket = new WebSocket(serverUrl);
        ConfigureWebSocketEvents();
        EstablishConnection();
    }

    private void ConfigureWebSocketEvents()
    {
        _webSocket.SslConfiguration.EnabledSslProtocols = System.Security.Authentication.SslProtocols.Tls12;
        _webSocket.OnOpen += HandleConnectionOpen;
        _webSocket.OnMessage += HandleIncomingMessage;
        _webSocket.OnError += HandleConnectionError;
        _webSocket.OnClose += HandleConnectionClosed;
    }

    private void EstablishConnection()
    {
        try
        {
            _webSocket.Connect();
        }
        catch (Exception ex)
        {
            UpdateConnectionStatus($"Koneksi gagal: {ex.Message}", Color.red);
        }
    }

    private void HandleConnectionOpen(object sender, EventArgs e)
    {
        UnityMainThreadDispatcher.Instance().Enqueue(() =>
        {
            UpdateConnectionStatus("Terhubung", Color.green);
            SendJoinRequest();
        });
    }

    private void HandleIncomingMessage(object sender, MessageEventArgs e)
    {
        UnityMainThreadDispatcher.Instance().Enqueue(() =>
        {
            ProcessServerMessage(e.Data);
        });
    }

    private void HandleConnectionError(object sender, ErrorEventArgs e)
    {
        UnityMainThreadDispatcher.Instance().Enqueue(() =>
        {
            UpdateConnectionStatus($"Kesalahan: {e.Message}", Color.red);
        });
    }

    private void HandleConnectionClosed(object sender, CloseEventArgs e)
    {
        UnityMainThreadDispatcher.Instance().Enqueue(() =>
        {
            UpdateConnectionStatus("Terputus", Color.yellow);
        });
    }

    private void SendJoinRequest()
    {
        SendMessage("join", new
        {
            roomId = _currentRoomId,
            authToken = authToken,
            username = _username
        });
    }

    private void UpdateConnectionStatus(string status, Color color)
    {
        _ui.ConnectionStatus.text = status;
        _ui.ConnectionStatus.style.color = new StyleColor(color);
        LogMessage($"[STATUS] {status}");
    }

    private void ProcessServerMessage(string message)
    {
        try
        {
            var messageArray = JsonConvert.DeserializeObject<object[]>(message);
            if (messageArray == null || messageArray.Length < 1)
            {
                LogMessage("[KESALAHAN] Format pesan tidak valid.");
                return;
            }

            string eventName = messageArray[0].ToString();
            object eventData = messageArray.Length > 1 ? messageArray[1] : null;

            HandleSpecificServerEvent(eventName, eventData);
        }
        catch (Exception ex)
        {
            Debug.LogError($"Kesalahan pemrosesan pesan: {ex.Message}");
        }
    }

    private void HandleSpecificServerEvent(string eventName, object eventData)
    {
        switch (eventName)
        {
            case "welcome":
                HandleWelcomeEvent(eventData);
                break;
            case "message":
                HandleBroadcastMessage(eventData);
                break;
            case "privateMessage":
                HandlePrivateMessage(eventData);
                break;
            case "error":
                LogMessage($"[KESALAHAN] {eventData}");
                break;
            default:
                // Ignore other events
                break;
        }
    }

    private void HandleWelcomeEvent(object eventData)
    {
        if (eventData is JObject welcomeData)
        {
            _clientId = welcomeData["id"]?.ToString();
            string message = welcomeData["message"]?.ToString();
            LogMessage($"Selamat datang! ID Anda: {_clientId}. Pesan: {message}");
        }
    }

    private void HandleBroadcastMessage(object eventData)
    {
        if (eventData is JObject broadcastData)
        {
            string from = broadcastData["from"]?.ToString();
            string username = broadcastData["username"]?.ToString();
            string content = broadcastData["message"]?.ToString();
            LogMessage($"[BROADCAST] {username} ({from}): {content}");
        }
    }

    private void HandlePrivateMessage(object eventData)
    {
        if (eventData is JObject privateMessageData)
        {
            string from = privateMessageData["from"]?.ToString();
            string username = privateMessageData["username"]?.ToString();
            string content = privateMessageData["message"]?.ToString();
            LogMessage($"[PRIBADI] {username} ({from}): {content}");
        }
    }

    private void LogMessage(string message)
    {
        var logEntry = new Label($"[{DateTime.Now:HH:mm:ss}] {message}")
        {
            style =
            {
                marginTop = 5,
                marginBottom = 5,
                color = new StyleColor(Color.white),
                unityFontStyleAndWeight = FontStyle.Normal
            },
            pickingMode = PickingMode.Position
        };
        _ui.MessageLog.contentContainer.Add(logEntry);
        _ui.MessageLog.ScrollTo(logEntry);
        _ui.MessageLog.horizontalScroller.value = _ui.MessageLog.horizontalScroller.highValue;
    }

    private void SendMessage(string eventName, object data)
    {
        if (_webSocket == null || _webSocket.ReadyState != WebSocketState.Open)
        {
            LogMessage("WebSocket tidak terhubung. Tidak dapat mengirim pesan.");
            return;
        }
        var message = new[] { eventName, data };
        string jsonMessage = JsonConvert.SerializeObject(message);
        _webSocket.Send(jsonMessage);
    }

    private void SendBroadcastMessage(string message)
    {
        if (string.IsNullOrWhiteSpace(message)) return;
        SendMessage("broadcast", new
        {
            message = message,
            authToken = authToken,
            roomId = _currentRoomId
        });
        _ui.MessageInput.value = string.Empty;
    }

    private void SendPrivateMessage(string targetId, string message)
    {
        if (string.IsNullOrWhiteSpace(message) || string.IsNullOrWhiteSpace(targetId)) return;
        SendMessage("privateMessage", new
        {
            targetId = targetId,
            message = message,
            authToken = authToken
        });
        LogMessage($"Pesan pribadi terkirim ke {targetId}: {message}");
        _ui.MessageInput.value = string.Empty;
    }

    private void JoinRoom(string roomId)
    {
        if (string.IsNullOrWhiteSpace(roomId)) return;
        _currentRoomId = roomId;
        SendJoinRequest();
        LogMessage($"Bergabung ke ruang: {roomId}");
    }

    private void OnApplicationQuit()
    {
        if (_webSocket != null && _webSocket.ReadyState == WebSocketState.Open)
        {
            _webSocket.Close();
            LogMessage("WebSocket ditutup.");
        }
    }
}