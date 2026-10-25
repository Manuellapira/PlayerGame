// QUIVER CLASH 3D | STATUS: SAME - unchanged from the first version (works for both 2D and 3D)
using System;
using System.Text;
using TMPro;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Reads the NetworkVariables and shows them. It never changes game state.
/// Needs three TextMeshPro texts: scoreboard (top corners / left), timer (top center), banner (middle).
/// </summary>
public class HUDController : MonoBehaviour
{
    [SerializeField] TMP_Text scoreboardText;
    [SerializeField] TMP_Text timerText;
    [SerializeField] TMP_Text bannerText;

    float nextScoreboardRefresh;

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
            bannerText.text = w == ulong.MaxValue ? "Draw!" : $"Player {w + 1} Wins!";
        }
        else if (gm.Countdown.Value > 0)
        {
            bannerText.text = gm.Countdown.Value.ToString();
        }
        else if (!gm.IsMatchActive.Value)
        {
            bannerText.text = NetworkManager.Singleton.IsServer
                ? "Press START when everyone has joined"
                : "Waiting for host to start...";
        }
        else
        {
            bannerText.text = "";
        }

        // Scoreboard (4x per second is plenty)
        if (Time.time < nextScoreboardRefresh) return;
        nextScoreboardRefresh = Time.time + 0.25f;

        PlayerStats[] players = FindObjectsByType<PlayerStats>(FindObjectsSortMode.None);
        Array.Sort(players, (a, b) => a.OwnerClientId.CompareTo(b.OwnerClientId));

        var sb = new StringBuilder();
        foreach (var p in players)
        {
            string power = p.ActivePowerUp.Value == PowerUpType.None ? "" : $"  [{p.ActivePowerUp.Value}]";
            sb.AppendLine($"P{p.OwnerClientId + 1}   HP {p.Health.Value}   Ammo {p.Ammo.Value}   Score {p.Score.Value}{power}");
        }
        scoreboardText.text = sb.ToString();
    }
}