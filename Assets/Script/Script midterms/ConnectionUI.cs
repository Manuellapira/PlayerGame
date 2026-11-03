// QUIVER CLASH 3D | STATUS: NEW (v3) - join codes through Unity Relay (Multiplayer Services sessions)
using System;
using System.Threading.Tasks;
using TMPro;
using Unity.Netcode;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Multiplayer;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Connection screen with JOIN CODES.
///  HOST  : click Host -> a join code appears on screen -> tell it to the other player.
///  JOIN  : type the host's code into the input box -> click Join.
///  START : host only. Hidden during the countdown and the match, back after the match ends (rematch).
///          Greyed out until enough players have joined.
/// Set "Use Relay" to OFF to fall back to plain local Host / Client (no internet or Unity account needed).
/// </summary>
public class ConnectionUI : MonoBehaviour
{
    [Header("Connect panel (hidden once connected)")]
    [SerializeField] GameObject connectPanel;
    [SerializeField] Button hostButton;
    [SerializeField] Button joinButton;
    [SerializeField] TMP_InputField joinCodeInput;
    [SerializeField] TMP_Text statusText;

    [Header("Lobby (host only)")]
    [SerializeField] TMP_Text joinCodeText;
    [SerializeField] Button startButton;

    [Header("Options")]
    [SerializeField] bool useRelay = true;
    [SerializeField] int maxPlayers = 4;

    string currentJoinCode = "";
    bool connecting;
    float nextPlayerCountCheck;
    int playerCount;

    void Start()
    {
        hostButton.onClick.AddListener(OnHostClicked);
        joinButton.onClick.AddListener(OnJoinClicked);
        startButton.onClick.AddListener(() =>
        {
            if (GameManager.Instance != null) GameManager.Instance.StartMatchRequest();
        });

        joinCodeInput.characterLimit = 6;
        joinCodeInput.onValidateInput += (text, index, ch) => char.ToUpperInvariant(ch);
        joinCodeInput.gameObject.SetActive(useRelay);   // no code needed for local mode

        SetStatus(useRelay ? "Host a game, or type a join code." : "Local mode: Host or Join on this network.");
    }

    // ---------------- HOST ----------------

    async void OnHostClicked()
    {
        if (connecting) return;
        SetConnecting(true, "Starting game...");

        try
        {
            if (useRelay)
            {
                await SignInAsync();

                // Creates the session + Relay allocation and starts the NetworkManager as HOST for us.
                var options = new SessionOptions { MaxPlayers = maxPlayers }.WithRelayNetwork();
                ISession session = await MultiplayerService.Instance.CreateSessionAsync(options);
                currentJoinCode = session.Code;
            }
            else
            {
                NetworkManager.Singleton.StartHost();
            }

            SetConnecting(false, "");
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            SetConnecting(false, "Could not host: " + e.Message);
        }
    }

    // ---------------- JOIN ----------------

    async void OnJoinClicked()
    {
        if (connecting) return;

        if (!useRelay)
        {
            NetworkManager.Singleton.StartClient();
            return;
        }

        string code = joinCodeInput.text.Trim().ToUpperInvariant();
        if (code.Length == 0)
        {
            SetStatus("Type the host's join code first.");
            return;
        }

        SetConnecting(true, "Joining...");

        try
        {
            await SignInAsync();

            // Joins the session and starts the NetworkManager as CLIENT for us.
            await MultiplayerService.Instance.JoinSessionByCodeAsync(code);
            SetConnecting(false, "");
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            SetConnecting(false, "Could not join: " + e.Message);
        }
    }

    // ---------------- UNITY SERVICES ----------------

    static async Task SignInAsync()
    {
        if (UnityServices.State != ServicesInitializationState.Initialized)
        {
            // A random profile gives every game window its own anonymous player ID,
            // so two windows on the same computer don't count as the same player.
            var init = new InitializationOptions();
            init.SetProfile(Guid.NewGuid().ToString("N").Substring(0, 24));
            await UnityServices.InitializeAsync(init);
        }

        if (!AuthenticationService.Instance.IsSignedIn)
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
    }

    // ---------------- UI STATE ----------------

    void SetConnecting(bool value, string message)
    {
        connecting = value;
        hostButton.interactable = !value;
        joinButton.interactable = !value;
        SetStatus(message);
    }

    void SetStatus(string message)
    {
        if (statusText != null) statusText.text = message;
    }

    void Update()
    {
        var nm = NetworkManager.Singleton;
        if (nm == null) return;

        bool running = nm.IsListening;
        connectPanel.SetActive(!running);

        var gm = GameManager.Instance;
        bool isHost = running && nm.IsServer;
        bool lobby = gm != null && !gm.IsMatchActive.Value && gm.Countdown.Value == 0;
        bool showStart = isHost && lobby;

        // Start button
        startButton.gameObject.SetActive(showStart);
        if (showStart)
        {
            if (Time.time >= nextPlayerCountCheck)
            {
                nextPlayerCountCheck = Time.time + 0.25f;
                playerCount = FindObjectsByType<PlayerStats>(FindObjectsSortMode.None).Length;
            }
            startButton.interactable = playerCount >= gm.MinPlayers;
        }

        // Join code shown to the host while waiting in the lobby
        bool showCode = showStart && useRelay && currentJoinCode.Length > 0;
        joinCodeText.gameObject.SetActive(showCode);
        if (showCode) joinCodeText.text = "JOIN CODE:  " + currentJoinCode;
    }
}