// QUIVER CLASH 3D | STATUS: SAME - unchanged from the first version (works for both 2D and 3D)

using System;
using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Three buttons: Host, Join (client), Start Match (host only, also works as Rematch).
/// Connects on the default Unity Transport address 127.0.0.1:7777.
/// </summary>
public class ConnectionUI : MonoBehaviour
{
    [SerializeField] GameObject uiPanel;
    [SerializeField] Button hostButton;
    [SerializeField] Button clientButton;
    [SerializeField] Button startButton;
    
    GameManager _gameManager;
    NetworkManager _networkManager;


    private void Start()
    {
        _gameManager = GameManager.Instance;
        _networkManager = NetworkManager.Singleton;
        GameManager.OnMatchEnded += TogglePanel;
        
        Refresh();
    }

    private void OnEnable()
    {
        GameManager.OnMatchEnded += Refresh;
    }

    public void StartHost()
    {
        NetworkManager.Singleton.StartHost();
        Refresh();
    }
    public void StartClient()
    {
        NetworkManager.Singleton.StartClient();
        Refresh();
    }

     public void StartMatch()
     {
         if (_gameManager != null) {
             _gameManager.StartMatchRequest();
             TogglePanel();
         }
     }

     private void TogglePanel()
     {
         uiPanel.SetActive(!uiPanel.activeSelf);
     }

     private void Refresh()
    {
        bool running = _networkManager.IsListening;
        hostButton.gameObject.SetActive(!running);
        clientButton.gameObject.SetActive(!running);
        startButton.gameObject.SetActive(running && _networkManager.IsServer);
    }
}