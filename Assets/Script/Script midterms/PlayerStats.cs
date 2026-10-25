using System.Collections;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

/// <summary>
/// STATE side of the T-chart: Health, Score, Ammo, IsAlive, ActivePowerUp.
/// Every NetworkVariable here is written by the SERVER only (default permission).
/// Put on the Player prefab (3D version).
/// </summary>
public class PlayerStats : NetworkBehaviour
{
    public const int MaxHealth = 3;
    public const int StartAmmo = 5;
    public const int MaxAmmo = 10;
    const float RespawnDelay = 3f;
    const float SpawnProtection = 2f;

    public NetworkVariable<int> Health = new NetworkVariable<int>(MaxHealth);
    public NetworkVariable<int> Score = new NetworkVariable<int>(0);
    public NetworkVariable<int> Ammo = new NetworkVariable<int>(StartAmmo);
    public NetworkVariable<bool> IsAlive = new NetworkVariable<bool>(true);
    public NetworkVariable<PowerUpType> ActivePowerUp = new NetworkVariable<PowerUpType>(PowerUpType.None);

    [Tooltip("The Capsule's MeshRenderer - gets tinted with the player's color")]
    [SerializeField] Renderer body;

    static readonly Color[] Palette =
    {
        new Color(0.35f, 0.55f, 1f),   // P1
        new Color(1f, 0.5f, 0.4f),     // P2
        new Color(0.4f, 0.85f, 0.5f),  // P3
        new Color(1f, 0.8f, 0.3f)      // P4
    };

    Color baseColor = Color.white;
    Renderer[] allRenderers;
    NetworkTransform netTransform;
    Rigidbody rb;

    // server-only bookkeeping (not synced)
    float powerUpEndTime;
    float invulnerableUntil;

    void Awake()
    {
        allRenderers = GetComponentsInChildren<Renderer>();
        netTransform = GetComponent<NetworkTransform>();
        rb = GetComponent<Rigidbody>();
    }

    public override void OnNetworkSpawn()
    {
        baseColor = Palette[(int)(OwnerClientId % (ulong)Palette.Length)];
        SetBodyColor(baseColor);
        ApplyAliveVisuals(IsAlive.Value);
        IsAlive.OnValueChanged += OnAliveChanged;

        if (IsServer) StartCoroutine(PlaceAtSpawn());
    }

    public override void OnNetworkDespawn()
    {
        IsAlive.OnValueChanged -= OnAliveChanged;
    }

    void OnAliveChanged(bool previous, bool current) => ApplyAliveVisuals(current);

    void ApplyAliveVisuals(bool alive)
    {
        foreach (var r in allRenderers) r.enabled = alive;
    }

    void SetBodyColor(Color c)
    {
        if (body != null) body.material.color = c;
    }

    void Update()
    {
        if (!IsServer) return;
        if (ActivePowerUp.Value != PowerUpType.None && Time.time >= powerUpEndTime)
            ActivePowerUp.Value = PowerUpType.None;
    }

    // ---------------- SERVER LOGIC ----------------

    public bool TryConsumeAmmo()
    {
        if (!IsServer || Ammo.Value <= 0) return false;
        Ammo.Value--;
        return true;
    }

    public void TakeDamage(int amount, ulong attackerId)
    {
        if (!IsServer || !IsAlive.Value || Time.time < invulnerableUntil) return;
        if (GameManager.Instance == null || !GameManager.Instance.IsMatchActive.Value) return;

        if (ActivePowerUp.Value == PowerUpType.Shield)
        {
            ActivePowerUp.Value = PowerUpType.None;   // shield blocks one arrow
            HitFxClientRpc();
            return;
        }

        Health.Value = Mathf.Max(0, Health.Value - amount);
        HitFxClientRpc();

        if (Health.Value == 0)
        {
            IsAlive.Value = false;
            // park the (invisible) body in its spawn corner so it doesn't block anyone
            TeleportTo(GameManager.Instance.GetSpawnPoint(OwnerClientId));
            GameManager.Instance.OnPlayerEliminated(this, attackerId);
            StartCoroutine(RespawnRoutine());
        }
    }

    public void ApplyPowerUp(PowerUpType type)
    {
        if (!IsServer) return;

        if (type == PowerUpType.AmmoRefill)
        {
            Ammo.Value = Mathf.Min(MaxAmmo, Ammo.Value + 5);
            return;
        }

        ActivePowerUp.Value = type;
        powerUpEndTime = Time.time + DurationFor(type);
    }

    static float DurationFor(PowerUpType type)
    {
        switch (type)
        {
            case PowerUpType.TripleShot: return 8f;
            case PowerUpType.SpeedBoost: return 6f;
            case PowerUpType.Shield: return 10f;
            default: return 0f;
        }
    }

    IEnumerator RespawnRoutine()
    {
        yield return new WaitForSeconds(RespawnDelay);
        if (GameManager.Instance.MatchOver.Value) yield break;

        Health.Value = MaxHealth;
        Ammo.Value = StartAmmo;
        ActivePowerUp.Value = PowerUpType.None;
        IsAlive.Value = true;
        invulnerableUntil = Time.time + SpawnProtection;
    }

    IEnumerator PlaceAtSpawn()
    {
        // wait until the scene's GameManager exists, then move to our corner
        while (GameManager.Instance == null) yield return null;
        yield return null;
        TeleportTo(GameManager.Instance.GetSpawnPoint(OwnerClientId));
    }

    /// <summary>Called by the server at the start of every match / rematch.</summary>
    public void ResetForMatch(Vector3 spawnPos)
    {
        if (!IsServer) return;
        StopAllCoroutines();
        SetBodyColor(baseColor);
        Health.Value = MaxHealth;
        Score.Value = 0;
        Ammo.Value = StartAmmo;
        ActivePowerUp.Value = PowerUpType.None;
        IsAlive.Value = true;
        invulnerableUntil = Time.time + SpawnProtection;
        TeleportTo(spawnPos);
    }

    // ---------------- EVENTS (RPCs) ----------------

    /// <summary>Respawn teleport: server tells the OWNER client to move (owner has authority over position).</summary>
    public void TeleportTo(Vector3 position)
    {
        var target = new ClientRpcParams
        {
            Send = new ClientRpcSendParams { TargetClientIds = new[] { OwnerClientId } }
        };
        RespawnClientRpc(position, target);
    }

    [ClientRpc]
    void RespawnClientRpc(Vector3 position, ClientRpcParams rpcParams = default)
    {
        netTransform.Teleport(position, Quaternion.identity, transform.localScale);
        rb.position = position;
        rb.linearVelocity = Vector3.zero;
    }

    [ClientRpc]
    void HitFxClientRpc()
    {
        StartCoroutine(Flash());
    }

    IEnumerator Flash()
    {
        SetBodyColor(Color.red);
        yield return new WaitForSeconds(0.1f);
        SetBodyColor(baseColor);
    }

    [ClientRpc]
    public void PickupFxClientRpc()
    {
        // TODO: play a pickup sound / particle here
    }
}