using System;
using UnityEngine;

// the colors each player sees their own side and the enemy's side in, picked on the options page;
// the two can't be the same
public static class SideColors
{
    public static readonly string[] names = { "White", "Red", "Orange", "Yellow", "Green", "Cyan", "Blue", "Magenta" };

    public static readonly Color[] colors = {
        new Color(0.95f, 0.95f, 0.95f),
        new Color(1.0f, 0.15f, 0.1f),
        new Color(1.0f, 0.5f, 0.05f),
        new Color(1.0f, 0.9f, 0.1f),
        new Color(0.2f, 0.9f, 0.2f),
        new Color(0.1f, 0.9f, 1.0f),
        new Color(0.0f, 0.35f, 1.0f),
        new Color(1.0f, 0.2f, 0.9f)
    };

    private const int defaultPlayer = 6;
    private const int defaultEnemy = 1;

    public static event Action changed;

    public static int player => Index(Constants.PLAYER_COLOR_PREF_KEY, defaultPlayer);
    public static int enemy => Index(Constants.ENEMY_COLOR_PREF_KEY, defaultEnemy);

    public static Color Player => colors[player];
    public static Color Enemy => colors[enemy];

    // false when the other side already has that color
    public static bool Set(bool isPlayer, int index) {
        if (index < 0 || index >= colors.Length) return false;
        if (index == (isPlayer ? enemy : player)) return false;

        PlayerPrefs.SetInt(isPlayer ? Constants.PLAYER_COLOR_PREF_KEY : Constants.ENEMY_COLOR_PREF_KEY, index);
        changed?.Invoke();
        return true;
    }

    private static int Index(string key, int fallback) {
        var index = PlayerPrefs.GetInt(key, fallback);
        return index >= 0 && index < colors.Length ? index : fallback;
    }
}
