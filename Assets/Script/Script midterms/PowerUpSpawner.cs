using System.Collections;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Static spawn point that creates a random power-up, waits for pickup, then respawns after a delay.
/// Place 3 of these in the scene (left, center, right). Each needs a NetworkObject component.
/// </summary>
public class PowerUpSpawner : NetworkBehaviour
{
    [SerializeField] PowerUpPickup pickupPrefab;
    [SerializeField] float respawnDelay = 10f;

    public NetworkVariable<bool> HasPowerUp = new NetworkVariable<bool>(false);

    public override void OnNetworkSpawn()
    {
        if (IsServer) SpawnPickup();
    }

    void SpawnPickup()
    {
        // enum values 1..4 (skip None)
        PowerUpType type = (PowerUpType)Random.Range(1, 5);

        PowerUpPickup pickup = Instantiate(pickupPrefab, transform.position, Quaternion.identity);
        pickup.NetworkObject.Spawn();
        pickup.ServerInit(this, type);

        HasPowerUp.Value = true;
    }

    public void OnPickedUp()
    {
        HasPowerUp.Value = false;
        StartCoroutine(RespawnAfterDelay());
    }

    IEnumerator RespawnAfterDelay()
    {
        yield return new WaitForSeconds(respawnDelay);
        SpawnPickup();
    }
}