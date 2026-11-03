// QUIVER CLASH 3D | STATUS: NEW (v4) - one shared color per player, used by the player body, scoreboard and winner banner
using UnityEngine;

/// <summary>
/// P1 = Red, P2 = Blue, P3 = Green, P4 = Yellow.
/// Not attached to anything - other scripts just call PlayerColors.Get(...).
/// P1 is the host, then players are numbered in the order they join.
/// </summary>
public static class PlayerColors
{
    static readonly Color[] colors =
    {
        new Color(0.90f, 0.20f, 0.20f),   // P1 red
        new Color(0.25f, 0.50f, 1.00f),   // P2 blue
        new Color(0.30f, 0.80f, 0.40f),   // P3 green
        new Color(1.00f, 0.85f, 0.20f)    // P4 yellow
    };

    static readonly string[] names = { "Red", "Blue", "Green", "Yellow" };

    public static Color Get(ulong clientId) => colors[(int)(clientId % (ulong)colors.Length)];

    public static string Name(ulong clientId) => names[(int)(clientId % (ulong)names.Length)];

    /// <summary>Hex like "E63333", for TextMeshPro rich text: &lt;color=#E63333&gt;</summary>
    public static string Hex(ulong clientId) => ColorUtility.ToHtmlStringRGB(Get(clientId));
}