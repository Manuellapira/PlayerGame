// QUIVER CLASH 3D | STATUS: SAME - unchanged from the first version (works for both 2D and 3D)
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Three buttons: Host, Join (client), Start Match (host only, also works as Rematch).
/// Connects on the default Unity Transport address 127.0.0.1:7777.
/// </summary>
public class ConnectionUI : MonoBehaviour
{
    [SerializeField] Button hostButton;
    [SerializeField] Button clientButton;
    [SerializeField] Button startButton;

    void Start()
    {
        hostButton.onClick.AddListener(() => { NetworkManager.Singleton.StartHost(); Refresh(); });
        clientButton.onClick.AddListener(() => { NetworkManager.Singleton.StartClient(); Refresh(); });
        startButton.onClick.AddListener(() =>
        {
            if (GameManager.Instance != null) GameManager.Instance.StartMatchRequest();
        });
        Refresh();
    }

    void Refresh()
    {
        bool running = NetworkManager.Singleton.IsListening;
        hostButton.gameObject.SetActive(!running);
        clientButton.gameObject.SetActive(!running);
        startButton.gameObject.SetActive(running && NetworkManager.Singleton.IsServer);
    }
}