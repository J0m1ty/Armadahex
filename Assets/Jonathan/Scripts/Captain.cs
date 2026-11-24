using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// where a captain comes from
public enum CaptainSource {
    Starter,
    Campaign,
    Achievement
}

// locked until its requirement is met, then unlockable until the player claims it
public enum CaptainState {
    Locked,
    Unlockable,
    Unlocked
}

// one captain: who they are and the passive and special they bring to a match that uses captains.
// a new captain is a new file in Resources/Captains. captains aren't played yet, only picked
[CreateAssetMenu(menuName = "Armadahex/Captain")]
public class Captain : ScriptableObject
{
    public string id;
    public string captainName;
    // a short title under the name, e.g. "THE SPOTTER"
    public string epithet;
    // where it sits in the library
    public int order;
    // optional, the card shows initials without it
    public Texture2D portrait;

    [Header("Passive")]
    public string passiveName;
    [TextArea(2, 5)]
    public string passive;

    [Header("Special")]
    public string specialName;
    [TextArea(2, 5)]
    public string special;

    [Header("Unlock")]
    public CaptainSource source;
    // exactly what unlocks them, e.g. "Clear Mission 4: Iron Strait" or "Play 30 multiplayer games"
    public string requirement;

    public string initials {
        get {
            var parts = captainName.Split(' ');
            return parts.Length > 1 ? "" + parts[0][0] + parts[parts.Length - 1][0] : captainName.Substring(0, Mathf.Min(2, captainName.Length));
        }
    }
}

public static class Captains
{
    private static List<Captain> all;

    public static IReadOnlyList<Captain> All {
        get {
            if (all == null) {
                all = Resources.LoadAll<Captain>("Captains").OrderBy(c => c.order).ToList();
            }
            return all;
        }
    }

    public static Captain Find(string id) {
        return All.FirstOrDefault(c => c.id == id);
    }

    // starters are always unlocked; the campaign and achievements mark the rest unlockable, the player claims them
    public static CaptainState State(Captain captain) {
        if (captain.source == CaptainSource.Starter) return CaptainState.Unlocked;
        return (CaptainState)PlayerPrefs.GetInt(Constants.CAPTAIN_STATE_PREF_PREFIX + captain.id, (int)CaptainState.Locked);
    }

    public static bool IsUnlocked(Captain captain) {
        return State(captain) == CaptainState.Unlocked;
    }

    // what the campaign or an achievement calls when the requirement is met
    public static void MakeUnlockable(Captain captain) {
        if (State(captain) == CaptainState.Locked) PlayerPrefs.SetInt(Constants.CAPTAIN_STATE_PREF_PREFIX + captain.id, (int)CaptainState.Unlockable);
    }

    // the player claiming an unlockable captain
    public static void Unlock(Captain captain) {
        if (State(captain) == CaptainState.Unlockable) PlayerPrefs.SetInt(Constants.CAPTAIN_STATE_PREF_PREFIX + captain.id, (int)CaptainState.Unlocked);
    }

    public static int unlockedCount => All.Count(IsUnlocked);

    // the active captain, taken into matches; the first unlocked one until the player picks
    public static Captain active {
        get {
            var picked = Find(PlayerPrefs.GetString(Constants.CAPTAIN_PREF_KEY, ""));
            return picked != null && IsUnlocked(picked) ? picked : All.FirstOrDefault(IsUnlocked);
        }
    }

    public static void MakeActive(Captain captain) {
        if (captain == null || !IsUnlocked(captain)) return;
        PlayerPrefs.SetString(Constants.CAPTAIN_PREF_KEY, captain.id);
    }
}
