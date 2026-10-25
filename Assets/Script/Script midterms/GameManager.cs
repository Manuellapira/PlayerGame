using System;
using System.Collections;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// One per scene (in-scene NetworkObject). Owns the match: countdown, timer, win condition.
/// Implements Step 4: Trigger -> State change -> Broadcast -> UI update.
/// </summary>
public class GameManager : NetworkBehaviour
{
    public static GameManager Instance { get; private set; }

    // STATE (server writes, everyone reads)
    public NetworkVariable<bool> IsMatchActive = new NetworkVariable<bool>(false);
    public NetworkVariable<bool> MatchOver = new NetworkVariable<bool>(false);
    public NetworkVariable<int> TimeLeft = new NetworkVariable<int>(0);      // whole seconds (cheaper than a float updated every frame)
    public NetworkVariable<int> Countdown = new NetworkVariable<int>(0);     // 3,2,1 then 0
    public NetworkVariable<ulong> WinnerId = new NetworkVariable<ulong>(ulong.MaxValue); // MaxValue = draw / none yet

    [Header("Rules")]
    [SerializeField] int scoreToWin = 5;
    [SerializeField] int matchSeconds = 90;
    [SerializeField] int minPlayers = 2;   // set to 1 if you want to test alone

    [Header("Spawn points: P1 top-left, P2 bottom-right, P3 top-right, P4 bottom-left")]
    [SerializeField] Transform[] spawnPoints;

    /// <summary>Raised on every client when the server broadcasts the end of the match (use for sound / effects).</summary>
    public static event Action<ulong> OnMatchEnded;

    Coroutine matchRoutine;

    void Awake() => Instance = this;

    public override void OnDestroy()
    {
        if (Instance == this) Instance = null;
        base.OnDestroy();
    }

    public Vector3 GetSpawnPoint(ulong clientId)
    {
        return spawnPoints[(int)(clientId % (ulong)spawnPoints.Length)].position;
    }

    // ---------------- START (host only) ----------------

    public void StartMatchRequest()
    {
        if (!IsServer || IsMatchActive.Value || matchRoutine != null) return;
        if (NetworkManager.Singleton.ConnectedClientsList.Count < minPlayers) return;
        matchRoutine = StartCoroutine(MatchRoutine());
    }

    IEnumerator MatchRoutine()
    {
        MatchOver.Value = false;
        WinnerId.Value = ulong.MaxValue;
        ResetPlayers();

        for (int i = 3; i > 0; i--)
        {
            Countdown.Value = i;
            yield return new WaitForSeconds(1f);
        }
        Countdown.Value = 0;

        TimeLeft.Value = matchSeconds;
        IsMatchActive.Value = true;

        while (TimeLeft.Value > 0)
        {
            yield return new WaitForSeconds(1f);
            TimeLeft.Value--;
        }

        // Trigger B: time ran out
        EndMatch(GetLeader());
    }

    void ResetPlayers()
    {
        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            if (client.PlayerObject == null) continue;
            client.PlayerObject.GetComponent<PlayerStats>()
                  .ResetForMatch(GetSpawnPoint(client.ClientId));
        }
    }

    // ---------------- WIN CONDITION ----------------

    /// <summary>Trigger A: called by PlayerStats on the server when someone's health hits 0.</summary>
    public void OnPlayerEliminated(PlayerStats victim, ulong killerId)
    {
        if (!IsServer || !IsMatchActive.Value) return;

        if (NetworkManager.Singleton.ConnectedClients.TryGetValue(killerId, out var killerClient) &&
            killerClient.PlayerObject != null)
        {
            var killer = killerClient.PlayerObject.GetComponent<PlayerStats>();
            killer.Score.Value++;

            if (killer.Score.Value >= scoreToWin)
                EndMatch(killerId);
        }
    }

    void EndMatch(ulong winnerId)
    {
        if (!IsServer || !IsMatchActive.Value) return;

        if (matchRoutine != null)
        {
            StopCoroutine(matchRoutine);
            matchRoutine = null;
        }

        // State change
        IsMatchActive.Value = false;
        MatchOver.Value = true;
        WinnerId.Value = winnerId;

        // Broadcast
        MatchEndedClientRpc(winnerId);
    }

    ulong GetLeader()
    {
        int best = -1;
        ulong leader = ulong.MaxValue;
        bool tie = false;

        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            if (client.PlayerObject == null) continue;
            int score = client.PlayerObject.GetComponent<PlayerStats>().Score.Value;

            if (score > best) { best = score; leader = client.ClientId; tie = false; }
            else if (score == best) tie = true;
        }
        return tie ? ulong.MaxValue : leader;   // tie = draw
    }

    [ClientRpc]
    void MatchEndedClientRpc(ulong winnerId)
    {
        // Players are frozen automatically because PlayerController checks IsMatchActive.
        OnMatchEnded?.Invoke(winnerId);
    }
}