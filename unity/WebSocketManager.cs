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
    private WebSocket ws;
    private string authToken = "12345";
    private string clientId = string.Empty;

    // UI Elements
    private Label connectionStatusLabel;
    private TextField messageInputField;
    private Button broadcastButton;
    private Button privateMessageButton;
    private ScrollView messageLogView;

    private void Awake()
    {
        if (uiDocument == null)
        {
            Debug.LogError("UI Document is not assigned in the Inspector!");
            return;
        }
        SetupUI();
    }

    private void Start()
    {
        Application.runInBackground = true;
        ConnectToServer();
    }

    private void SetupUI()
    {
        var root = uiDocument.rootVisualElement;

        connectionStatusLabel = root.Q<Label>("ConnectionStatus");
        messageInputField = root.Q<TextField>("MessageInput");
        broadcastButton = root.Q<Button>("BroadcastButton");
        privateMessageButton = root.Q<Button>("PrivateMessageButton");
        messageLogView = root.Q<ScrollView>("MessageLog");

        broadcastButton.clicked += () => SendBroadcastMessage(messageInputField.value);
        privateMessageButton.clicked += () => SendPrivateMessage("some-target-id", messageInputField.value);
    }

    private void ConnectToServer()
    {
        ws = new WebSocket("wss://websockettest.deno.dev");

        // Configure SSL protocols to ensure compatibility
        ws.SslConfiguration.EnabledSslProtocols = System.Security.Authentication.SslProtocols.Tls12;

        ws.OnOpen += (sender, e) =>
        {
            UnityMainThreadDispatcher.Instance().Enqueue(() =>
            {
                UpdateConnectionStatus("Connected", Color.green);
                SendJoinRequest();
            });
        };

        ws.OnMessage += (sender, e) =>
        {
            UnityMainThreadDispatcher.Instance().Enqueue(() =>
            {
                HandleServerMessage(e.Data);
            });
        };

        ws.OnError += (sender, e) =>
        {
            UnityMainThreadDispatcher.Instance().Enqueue(() =>
            {
                UpdateConnectionStatus($"Error: {e.Message}", Color.red);
            });
        };

        ws.OnClose += (sender, e) =>
        {
            UnityMainThreadDispatcher.Instance().Enqueue(() =>
            {
                UpdateConnectionStatus("Disconnected", Color.yellow);
            });
        };

        try
        {
            ws.Connect();
        }
        catch (Exception ex)
        {
            UnityMainThreadDispatcher.Instance().Enqueue(() =>
            {
                UpdateConnectionStatus($"Connection failed: {ex.Message}", Color.red);
            });
        }
    }

    private void SendJoinRequest()
    {
        Invoke("join", new Dictionary<string, string>
        {
            { "roomId", "room1" },
            { "authToken", authToken }
        });
    }

    private void UpdateConnectionStatus(string status, Color color)
    {
        connectionStatusLabel.text = status;
        connectionStatusLabel.style.color = new StyleColor(color);
        AddMessageToLog($"[STATUS] {status}");
    }

    private void HandleServerMessage(string message)
    {
        try
        {
            var messageArray = JsonConvert.DeserializeObject<object[]>(message);
            if (messageArray != null && messageArray.Length >= 1)
            {
                string eventName = messageArray[0].ToString();
                object eventData = messageArray.Length > 1 ? messageArray[1] : null;

                switch (eventName)
                {
                    case "welcome":
                        var welcomeData = JObject.Parse(eventData.ToString());
                        clientId = welcomeData["welcome"].ToString();
                        AddMessageToLog($"[WELCOME] {clientId}");
                        break;

                    case "message": // Broadcast message
                        if (eventData is JObject broadcastData)
                        {
                            string from = broadcastData["from"]?.ToString();
                            string content = broadcastData["message"]?.ToString();
                            AddMessageToLog($"[BROADCAST] {from}: {content}");
                        }
                        break;

                    case "privateMessage": // Private message
                        if (eventData is JObject privateMessageData)
                        {
                            string from = privateMessageData["from"]?.ToString();
                            string content = privateMessageData["message"]?.ToString();
                            AddMessageToLog($"[PRIVATE] {from}: {content}");
                        }
                        break;

                    case "error":
                        AddMessageToLog($"[ERROR] {eventData}");
                        break;

                    default:
                        AddMessageToLog($"[UNHANDLED] Event: {eventName}, Data: {JsonConvert.SerializeObject(eventData)}");
                        break;
                }
            }
            else
            {
                AddMessageToLog("[ERROR] Invalid message format received.");
            }
        }
        catch (Exception ex)
        {
            // AddMessageToLog($"[ERROR] Failed to parse message: {ex.Message}");
        }
    }

    private void AddMessageToLog(string message)
    {
        var logEntry = new Label($"[{DateTime.Now:HH:mm:ss}] {message}")
        {
            style =
            {
                marginTop = 5,
                marginBottom = 5,
                color = new StyleColor(Color.white),
                unityFontStyleAndWeight = FontStyle.Normal
            }
        };

        messageLogView.contentContainer.Add(logEntry);
        messageLogView.ScrollTo(logEntry);
    }

    private void Invoke(string eventName, object data)
    {
        if (ws == null || ws.ReadyState != WebSocketState.Open)
        {
            AddMessageToLog("WebSocket not connected. Cannot send message.");
            return;
        }

        var message = new[] { eventName, data };
        string jsonMessage = JsonConvert.SerializeObject(message);
        ws.Send(jsonMessage);
    }

    private void SendBroadcastMessage(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return;

        Invoke("broadcast", new
        {
            message = message,
            authToken = authToken
        });

        messageInputField.value = string.Empty;
    }

    private void SendPrivateMessage(string targetId, string message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return;

        Invoke("privateMessage", new
        {
            targetId = targetId,
            message = message,
            authToken = authToken
        });

        AddMessageToLog($"[SENT] PrivateMessage to {targetId}: {message}");
        messageInputField.value = string.Empty;
    }

    private void OnApplicationQuit()
    {
        if (ws != null && ws.ReadyState == WebSocketState.Open)
        {
            ws.Close();
            AddMessageToLog("WebSocket closed.");
        }
    }
}

// Tambahkan kelas UnityMainThreadDispatcher untuk menangani threading
public class UnityMainThreadDispatcher : MonoBehaviour {
    private static UnityMainThreadDispatcher _instance;
    private Queue<Action> _executionQueue = new Queue<Action>();

    public static UnityMainThreadDispatcher Instance() {
        if (_instance == null) {
            GameObject go = new GameObject("UnityMainThreadDispatcher");
            _instance = go.AddComponent<UnityMainThreadDispatcher>();
            DontDestroyOnLoad(go);
        }
        return _instance;
    }

    private void Update() {
        while (_executionQueue.Count > 0) {
            _executionQueue.Dequeue().Invoke();
        }
    }

    public void Enqueue(Action action) {
        _executionQueue.Enqueue(action);
    }
}