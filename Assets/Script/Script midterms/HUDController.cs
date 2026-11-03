// QUIVER CLASH 3D | STATUS: UPDATED (v4) - each player's scoreboard line and the winner banner use that player's color
using System;
using System.Text;
using TMPro;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Reads the NetworkVariables and shows them. It never changes game state.
/// Needs three TextMeshPro texts: scoreboard (top-left), timer (top-center), banner (middle).
/// Make sure "Rich Text" is ticked on the scoreboard and banner texts (it is by default).
/// </summary>
public class HUDController : MonoBehaviour
{
    [SerializeField] TMP_Text scoreboardText;
    [SerializeField] TMP_Text timerText;
    [SerializeField] TMP_Text bannerText;

    float nextScoreboardRefresh;
    int playerCount;

    void Update()
    {
        var gm = GameManager.Instance;
        if (gm == null) return;

        // Timer
        int t = gm.TimeLeft.Value;
        timerText.text = $"{t / 60:00}:{t % 60:00}";

        // Banner: waiting / countdown / winner
        if (gm.MatchOver.Value)
        {
            ulong w = gm.WinnerId.Value;
            bannerText.text = w == ulong.MaxValue
                ? "Draw!"
                : $"<color=#{PlayerColors.Hex(w)}>Player {w + 1} ({PlayerColors.Name(w)}) Wins!</color>";
        }
        else if (gm.Countdown.Value > 0)
        {
            bannerText.text = gm.Countdown.Value.ToString();
        }
        else if (!gm.IsMatchActive.Value)
        {
            if (playerCount < gm.MinPlayers)
                bannerText.text = $"Waiting for players ({playerCount}/{gm.MinPlayers})";
            else
                bannerText.text = NetworkManager.Singleton.IsServer
                    ? "Press START"
                    : "Waiting for host to start...";
        }
        else
        {
            bannerText.text = "";
        }

        // Scoreboard + player count (4x per second is plenty)
        if (Time.time < nextScoreboardRefresh) return;
        nextScoreboardRefresh = Time.time + 0.25f;

        PlayerStats[] players = FindObjectsByType<PlayerStats>(FindObjectsSortMode.None);
        playerCount = players.Length;
        Array.Sort(players, (a, b) => a.OwnerClientId.CompareTo(b.OwnerClientId));

        var sb = new StringBuilder();
        foreach (var p in players)
        {
            string hex = PlayerColors.Hex(p.OwnerClientId);
            string power = p.ActivePowerUp.Value == PowerUpType.None ? "" : $"  [{p.ActivePowerUp.Value}]";
            string you = p.IsOwner ? "   (YOU)" : "";

            sb.AppendLine(
                $"<color=#{hex}><b>P{p.OwnerClientId + 1} {PlayerColors.Name(p.OwnerClientId)}</b>   " +
                $"HP {p.Health.Value}   Ammo {p.Ammo.Value}   Score {p.Score.Value}{power}{you}</color>");
        }
        scoreboardText.text = sb.ToString();
    }
}