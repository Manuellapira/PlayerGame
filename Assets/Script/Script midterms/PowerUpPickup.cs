using Unity.Netcode;
using UnityEngine;

/// <summary>
/// A spawned pickup (3D). Server detects the touch, applies the effect, despawns it.
/// Prefab: Sphere mesh (scale 0.6), SphereCollider with Is Trigger ON, NetworkObject, this script.
/// </summary>
public class PowerUpPickup : NetworkBehaviour
{
    public NetworkVariable<PowerUpType> Type = new NetworkVariable<PowerUpType>(PowerUpType.AmmoRefill);

    [SerializeField] Renderer rend;

    PowerUpSpawner spawner;   // server only

    public override void OnNetworkSpawn()
    {
        Type.OnValueChanged += OnTypeChanged;
        Tint(Type.Value);
    }

    public override void OnNetworkDespawn()
    {
        Type.OnValueChanged -= OnTypeChanged;
    }

    void Update()
    {
        transform.Rotate(0f, 90f * Time.deltaTime, 0f);   // cosmetic spin (local only, not synced)
    }

    void OnTypeChanged(PowerUpType previous, PowerUpType current) => Tint(current);

    void Tint(PowerUpType type)
    {
        if (rend == null) return;
        switch (type)
        {
            case PowerUpType.AmmoRefill: rend.material.color = new Color(0.95f, 0.85f, 0.3f); break;  // yellow
            case PowerUpType.TripleShot: rend.material.color = new Color(0.8f, 0.4f, 1f); break;      // purple
            case PowerUpType.SpeedBoost: rend.material.color = new Color(0.3f, 0.9f, 0.9f); break;    // cyan
            case PowerUpType.Shield:     rend.material.color = new Color(0.4f, 0.6f, 1f); break;      // blue
        }
    }

    /// <summary>Server calls this right after Spawn().</summary>
    public void ServerInit(PowerUpSpawner owner, PowerUpType type)
    {
        spawner = owner;
        Type.Value = type;
    }

    void OnTriggerEnter(Collider other)
    {
        if (!IsServer) return;

        PlayerStats player = other.GetComponentInParent<PlayerStats>();
        if (player == null || !player.IsAlive.Value) return;

        player.ApplyPowerUp(Type.Value);
        player.PickupFxClientRpc();

        if (spawner != null) spawner.OnPickedUp();
        NetworkObject.Despawn();
    }
}