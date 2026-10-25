using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Server-authoritative projectile (3D). The server moves it along its forward (+Z) and decides hits;
/// clients just see it through its NetworkTransform.
/// Arrow prefab: Cube mesh (scale 0.12, 0.12, 0.8), Rigidbody (Is Kinematic ON, Use Gravity OFF),
/// BoxCollider with Is Trigger ON, NetworkObject, NetworkTransform (server authority), this script.
/// </summary>
public class Arrow : NetworkBehaviour
{
    [SerializeField] float speed = 14f;
    [SerializeField] float lifetime = 3f;

    [HideInInspector] public ulong ShooterId;   // set by the server before Spawn()

    float spawnTime;

    public override void OnNetworkSpawn()
    {
        spawnTime = Time.time;
    }

    void Update()
    {
        if (!IsServer) return;

        transform.position += transform.forward * speed * Time.deltaTime;

        if (Time.time - spawnTime >= lifetime) DespawnSelf();
    }

    void OnTriggerEnter(Collider other)
    {
        if (!IsServer) return;

        PlayerStats target = other.GetComponentInParent<PlayerStats>();
        if (target != null)
        {
            if (target.OwnerClientId == ShooterId) return;   // can't hit yourself
            if (!target.IsAlive.Value) return;               // fly past dead players

            target.TakeDamage(1, ShooterId);
            DespawnSelf();
        }
        else if (other.CompareTag("Crate"))
        {
            DespawnSelf();
        }
    }

    void DespawnSelf()
    {
        if (NetworkObject.IsSpawned) NetworkObject.Despawn();
    }
}