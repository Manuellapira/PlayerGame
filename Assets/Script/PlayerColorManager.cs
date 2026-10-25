using Unity.Netcode; // Required for network variables
using UnityEngine;
public class PlayerColorManager : NetworkBehaviour
{
// The component that actually draws the player on the screen
[SerializeField] private Renderer playerRenderer;
// An array to hold our different colored materials
[SerializeField] private Material[] playerMaterials;
// A synchronized variable that automatically updates across all clients when changed by the server
private NetworkVariable<int> colorIndex = new NetworkVariable<int>();
public override void OnNetworkSpawn()
{
// Subscribe to the event so we know when the color index changes
colorIndex.OnValueChanged += HandleColorChanged;
// Only the Server is allowed to assign the color
if (IsServer && playerMaterials.Length > 0)
{
// Pick a color based on the connecting client's ID so players get unique colors
colorIndex.Value = (int)(OwnerClientId % (ulong)playerMaterials.Length);
}
// Apply the color immediately for the local client
ApplyColor(colorIndex.Value);
}
public override void OnNetworkDespawn()
{
// Unsubscribe from the event when the object is destroyed to prevent memory leaks
colorIndex.OnValueChanged -= HandleColorChanged;
}
// This method is triggered automatically whenever colorIndex changes
private void HandleColorChanged(int oldColorIndex, int newColorIndex)
{
ApplyColor(newColorIndex);
}
// Applies the material to the renderer
private void ApplyColor(int index)
{
if (playerRenderer == null || playerMaterials.Length == 0) return;
// Ensure the index doesn't go out of bounds of our array
int safeIndex = index % playerMaterials.Length;
playerRenderer.material = playerMaterials[safeIndex];
}
}