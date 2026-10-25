using Unity.Netcode;
using Unity.Netcode.Transports.UTP;       // Required for UnityTransport
using Unity.Networking.Transport.Relay;   // Required for AllocationUtils
using Unity.Services.Relay;               // Required for Relay operations
using Unity.Services.Relay.Models;        // Required for Relay Allocations
using Unity.Services.Core;                // Required to initialize Unity Services
using Unity.Services.Authentication;      // Required for players to sign in anonymously
using System.Threading.Tasks;             // Required for asynchronous tasks (await)
using UnityEngine;
using TMPro;                              // Required to reference TextMeshPro UI elements

public class MultiplayerMenu : MonoBehaviour 
{
    // --- UI REFERENCES ---
    [SerializeField] private GameObject menuUI; // The panel we hide after connecting
    [SerializeField] private TMP_InputField joinCodeInput; // Where the Client types the code
    [SerializeField] private TMP_Text joinCodeText; // Where the Host sees their generated code
    [SerializeField] private TMP_Text statusText; // Shows connection messages

    // --- WEBGL CONFIGURATION ---
    // Tells Unity to use secure WebSockets (wss) for browser compatibility
    private const string WebGLConnectionType = "wss"; 

    // Runs automatically when the object loads in the scene
    private async void Start()
    {
        // 1. Initialize Unity Gaming Services
        await UnityServices.InitializeAsync();
        
        // 2. Sign the player in anonymously (Relay requires authentication)
        if (!AuthenticationService.Instance.IsSignedIn)
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
            statusText.text = "Signed in locally. Ready to connect.";
        }
    }

    // Must be 'async' because reaching out to Unity's internet servers takes time
    public async void StartHost()
    {
        try
        {
            statusText.text = "Creating Relay Session...";
            
            // 1. Ask Unity Relay for a server allocation (Max 4 players)
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(4);
            
            // 2. Ask Relay for the specific Join Code attached to our allocation
            string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
            
            // 3. Display the Join Code on the Host's screen so they can share it
            joinCodeText.text = "Join Code: " + joinCode;

            // 4. Grab the network transport component
            UnityTransport transport = NetworkManager.Singleton.GetComponent<UnityTransport>();

            // 5. Force WebSockets (vital for WebGL)
            transport.UseWebSockets = true;

            // 6. Pass the Relay allocation data into the transport
            transport.SetRelayServerData(AllocationUtils.ToRelayServerData(allocation, WebGLConnectionType));

            // 7. Finally, start the Host
            NetworkManager.Singleton.StartHost();
            
            statusText.text = "Host started successfully!";
            // Note: We do not hide the menuUI entirely for the Host yet, so they can still see the Join Code text on screen.
        }
        catch (RelayServiceException e)
        {
            Debug.LogError(e);
            statusText.text = "Failed to start Host.";
        }
    }

    // Must be 'async' because joining takes time
    public async void StartClient()
    {
        try
        {
            statusText.text = "Joining Session...";

            // 1. Get the text the Client typed and ask Relay to join that allocation
            JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCodeInput.text);
            
            // 2. Grab the network transport component
            UnityTransport transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            
            // 3. Force WebSockets for the Client
            transport.UseWebSockets = true;
            
            // 4. Pass the specific join allocation data into the transport
            transport.SetRelayServerData(AllocationUtils.ToRelayServerData(joinAllocation, WebGLConnectionType));

            // 5. Start the Client
            NetworkManager.Singleton.StartClient();
            
            // 6. Hide the main menu since the Client successfully joined
            if (menuUI != null) menuUI.SetActive(false);
        }
        catch (RelayServiceException e)
        {
            Debug.LogError(e);
            statusText.text = "Invalid Join Code.";
        }
    }
}