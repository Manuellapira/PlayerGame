using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Client asks (ServerRpc) -> server validates -> server spawns Arrow NetworkObjects.
/// Put on the Player prefab and assign the Arrow prefab. (3D version: aim is a yaw angle around Y.)
/// </summary>
public class PlayerShooting : NetworkBehaviour
{
    [SerializeField] Arrow arrowPrefab;

    const float Cooldown = 0.5f;
    const float SpreadDegrees = 15f;

    PlayerStats stats;
    float lastRequestTime;   // owner client: stops spamming RPCs
    float lastShotTime;      // server: the real cooldown check

    void Awake() => stats = GetComponent<PlayerStats>();

    /// <summary>Called from PlayerController on the owner client.</summary>
    public void TryShoot(float yawDegrees)
    {
        if (Time.time - lastRequestTime < Cooldown) return;
        if (stats.Ammo.Value <= 0) return;   // must have ammo
        lastRequestTime = Time.time;
        ShootArrowServerRpc(yawDegrees);
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]   // RequireOwnership = true: only the owner can call this on their own player
    void ShootArrowServerRpc(float yawDegrees)
    {
        stats.Ammo.Value--;
        if (Time.time - lastShotTime < Cooldown * 0.9f) return;  // cooldown (small tolerance for network jitter)
        lastShotTime = Time.time;

        if (stats.ActivePowerUp.Value == PowerUpType.TripleShot)
        {
            SpawnArrow(yawDegrees - SpreadDegrees);
            SpawnArrow(yawDegrees);
            SpawnArrow(yawDegrees + SpreadDegrees);
        }
        else
        {
            SpawnArrow(yawDegrees);
        }
        ShootFxClientRpc();
    }

    void SpawnArrow(float yawDegrees)
    {
        Quaternion rot = Quaternion.Euler(0f, yawDegrees, 0f);
        Vector3 pos = transform.position + rot * Vector3.forward * 0.9f;

        Arrow arrow = Instantiate(arrowPrefab, pos, rot);
        arrow.ShooterId = OwnerClientId;
        arrow.NetworkObject.Spawn();
    }

    [ClientRpc]
    void ShootFxClientRpc()
    {
        // TODO: play a bow sound / muzzle effect here
    }
}