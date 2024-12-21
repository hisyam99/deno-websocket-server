// File: JoinRoomManager.cs

using UnityEngine;
using UnityEngine.UIElements;

public class JoinRoomManager : MonoBehaviour
{
    [SerializeField] private UIDocument uiDocument;
    [SerializeField] private PageManager pageManager;
    private UIElements _ui;
    private string _username = string.Empty;
    private string _roomId = string.Empty;

    private class UIElements
    {
        public Label ConnectionStatus;
        public TextField UsernameInput;
        public TextField RoomIdInput;
        public Button JoinRoomButton;
    }

    private void Awake()
    {
        ValidateUIDocument();
        InitializeUIReferences();
        SetupUIEventHandlers();
    }

    private void Start()
    {
        WebSocketManager.Instance.OnJoinSuccess += OnJoinSuccess;
    }

    private void OnDestroy()
    {
        if (WebSocketManager.Instance != null)
        {
            WebSocketManager.Instance.OnJoinSuccess -= OnJoinSuccess;
        }
    }

    private void ValidateUIDocument()
    {
        if (uiDocument == null)
        {
            Debug.LogError("[JoinRoomManager] Dokumen UI tidak ditetapkan di Inspector!");
            enabled = false;
        }
    }

    private void InitializeUIReferences()
    {
        var root = uiDocument.rootVisualElement;
        _ui = new UIElements
        {
            ConnectionStatus = root.Q<Label>("ConnectionStatus"),
            UsernameInput = root.Q<TextField>("UsernameInput"),
            RoomIdInput = root.Q<TextField>("RoomIdInput"),
            JoinRoomButton = root.Q<Button>("JoinRoomButton")
        };
    }

    private void SetupUIEventHandlers()
    {
        _ui.UsernameInput.RegisterValueChangedCallback(evt => _username = evt.newValue);
        _ui.RoomIdInput.RegisterValueChangedCallback(evt => _roomId = evt.newValue);
        _ui.JoinRoomButton.clicked += JoinRoom;
    }

    public void JoinRoom()
    {
        if (string.IsNullOrWhiteSpace(_username) || string.IsNullOrWhiteSpace(_roomId))
        {
            UpdateConnectionStatus("Username dan Room ID harus diisi sebelum bergabung.", Color.red);
            return;
        }

        UpdateConnectionStatus("Mengirim permintaan bergabung...", Color.yellow);
        WebSocketManager.Instance.JoinRoom(_username, _roomId);
    }

    public void UpdateConnectionStatus(string status, Color color)
    {
        _ui.ConnectionStatus.text = status;
        _ui.ConnectionStatus.style.color = new StyleColor(color);
    }

    private void OnJoinSuccess()
    {
        Debug.Log("OnJoinSuccess triggered");
        OnJoinRoomSuccess();
    }

    public void OnJoinRoomSuccess()
    {
        Debug.Log("OnJoinRoomSuccess called");
        pageManager.OnJoinRoomSuccess(_username, _roomId);
    }
}