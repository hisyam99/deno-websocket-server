// File: ChatManager.cs

using UnityEngine;
using UnityEngine.UIElements;
using WebSocketSharp;
using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

public class ChatManager : MonoBehaviour
{
    [SerializeField] private UIDocument uiDocument;
    private UIElements _ui;
    private WebSocket _webSocket;
    private string _clientId = string.Empty;
    private string _currentRoomId = string.Empty;

    private class UIElements
    {
        public TextField MessageInput;
        public TextField TargetUserIdInput;
        public Button BroadcastButton;
        public Button PrivateMessageButton;
        public ScrollView MessageLog;
    }

    private void Awake()
    {
        ValidateUIDocument();
        if (uiDocument != null && uiDocument.rootVisualElement != null)
        {
            InitializeUIReferences();
            SetupUIEventHandlers();
        }
    }

    private void Start()
    {
        Application.runInBackground = true;
    }

    public void ConnectToWebSocket()
    {
        _webSocket = WebSocketManager.Instance.WebSocket;
        if (_webSocket != null)
        {
            ConfigureWebSocketEvents();
        }
        else
        {
            Debug.LogError("WebSocket is null. Cannot connect.");
        }
    }

    private void ValidateUIDocument()
    {
        if (uiDocument == null)
        {
            Debug.LogError("[ChatManager] Dokumen UI tidak ditetapkan di Inspector!");
            enabled = false;
        }
    }

    private void InitializeUIReferences()
    {
        var root = uiDocument.rootVisualElement;
        _ui = new UIElements
        {
            MessageInput = root.Q<TextField>("MessageInput"),
            TargetUserIdInput = root.Q<TextField>("TargetUserIdInput"),
            BroadcastButton = root.Q<Button>("BroadcastButton"),
            PrivateMessageButton = root.Q<Button>("PrivateMessageButton"),
            MessageLog = root.Q<ScrollView>("MessageLog")
        };
    }

    private void SetupUIEventHandlers()
    {
        _ui.BroadcastButton.clicked += () => SendBroadcastMessage(_ui.MessageInput.value);
        _ui.PrivateMessageButton.clicked += () => SendPrivateMessage(_ui.TargetUserIdInput.value, _ui.MessageInput.value);
    }

    private void ConfigureWebSocketEvents()
    {
        if (_webSocket != null)
        {
            _webSocket.OnOpen += HandleConnectionOpen;
            _webSocket.OnMessage += HandleIncomingMessage;
            _webSocket.OnError += HandleConnectionError;
            _webSocket.OnClose += HandleConnectionClosed;
        }
        else
        {
            Debug.LogError("WebSocket is null. Cannot configure events.");
        }
    }

    private void HandleConnectionOpen(object sender, EventArgs e)
    {
        UnityMainThreadDispatcher.Instance().Enqueue(() =>
        {
            LogMessage("Terhubung");
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
            LogMessage($"Kesalahan: {e.Message}");
        });
    }

    private void HandleConnectionClosed(object sender, CloseEventArgs e)
    {
        UnityMainThreadDispatcher.Instance().Enqueue(() =>
        {
            LogMessage("Terputus");
        });
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
        if (_ui == null || _ui.MessageLog == null)
        {
            Debug.LogWarning("UI elements not initialized. Cannot log message.");
            return;
        }

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

    private new void SendMessage(string eventName, object data)
    {
        if (_webSocket == null || _webSocket.ReadyState != WebSocketState.Open)
        {
            LogMessage("WebSocket tidak terhubung. Tidak dapat mengirim pesan.");
            return;
        }
        var message = new object[] { eventName, data };
        string jsonMessage = JsonConvert.SerializeObject(message);
        _webSocket.Send(jsonMessage);
    }

    private void SendBroadcastMessage(string message)
    {
        if (string.IsNullOrWhiteSpace(message)) return;
        SendMessage("broadcast", new
        {
            message = message,
            authToken = WebSocketManager.Instance.AuthToken,
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
            authToken = WebSocketManager.Instance.AuthToken
        });
        LogMessage($"Pesan pribadi terkirim ke {targetId}: {message}");
        _ui.MessageInput.value = string.Empty;
    }

    public void SetCurrentRoomId(string roomId)
    {
        _currentRoomId = roomId;
    }

    public void InitializeUI()
    {
        if (uiDocument != null && uiDocument.rootVisualElement != null)
        {
            InitializeUIReferences();
            SetupUIEventHandlers();
        }
    }
}