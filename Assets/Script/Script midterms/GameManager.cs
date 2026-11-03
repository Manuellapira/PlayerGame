// QUIVER CLASH 3D | STATUS: UPDATED (v5) - random SAFE corner spawns (no spawn camping, no overlapping)
using System;
using System.Collections;
using System.Collections.Generic;
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
    [SerializeField] float safeSpawnDistance = 12f;   // a respawn corner must be at least this far from every other alive player
    [SerializeField] float spawnHeight = 1f;   // players always spawn at this height, so a wrong Y on a spawn point can't drop them through the floor

    [Header("Spawn points: the 4 arena corners, in any order. Players get a random SAFE corner")]
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

    public int MinPlayers => minPlayers;

    /// <summary>
    /// Picks a random corner that is far from every OTHER alive player (no spawn camping, no spawning on top of someone).
    /// If no corner is far enough, it uses the corner that is farthest from the nearest enemy.
    /// </summary>
    public Vector3 GetRandomSpawnPoint(ulong forClientId)
    {
        var others = new List<Vector3>();
        foreach (var p in FindObjectsByType<PlayerStats>(FindObjectsSortMode.None))
        {
            if (p.OwnerClientId != forClientId && p.IsAlive.Value)
                others.Add(Flat(p.transform.position));
        }

        var safe = new List<Transform>();
        Transform farthest = spawnPoints[0];
        float farthestDistance = -1f;

        foreach (var sp in spawnPoints)
        {
            float nearest = float.MaxValue;
            foreach (var o in others)
                nearest = Mathf.Min(nearest, Vector3.Distance(Flat(sp.position), o));

            if (nearest >= safeSpawnDistance) safe.Add(sp);
            if (nearest > farthestDistance) { farthestDistance = nearest; farthest = sp; }
        }

        Transform pick = safe.Count > 0 ? safe[UnityEngine.Random.Range(0, safe.Count)] : farthest;
        Vector3 pos = pick.position;
        pos.y = spawnHeight;
        return pos;
    }

    static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);

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
        // shuffle the corners so everybody starts in a different, random corner
        var order = new List<int>();
        for (int i = 0; i < spawnPoints.Length; i++) order.Add(i);
        for (int i = order.Count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            int tmp = order[i]; order[i] = order[j]; order[j] = tmp;
        }

        int n = 0;
        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            if (client.PlayerObject == null) continue;

            Vector3 pos = spawnPoints[order[n % order.Count]].position;
            pos.y = spawnHeight;
            n++;

            client.PlayerObject.GetComponent<PlayerStats>().ResetForMatch(pos);
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