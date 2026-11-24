using System.Collections.Generic;
using System.Linq;
using UnityEngine;

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
    public bool unlockedByDefault;
    // how a locked captain is found, e.g. "CAMPAIGN" or "ACHIEVEMENT"
    public string unlockedBy;

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

    // found through the campaign or achievements, which mark them in PlayerPrefs
    public static bool IsUnlocked(Captain captain) {
        return captain.unlockedByDefault || PlayerPrefs.GetInt(Constants.CAPTAIN_UNLOCKED_PREF_PREFIX + captain.id, 0) == 1;
    }

    public static int unlockedCount => All.Count(IsUnlocked);

    // the captain taken into matches, the first unlocked one until the player picks
    public static Captain chosen {
        get {
            var picked = Find(PlayerPrefs.GetString(Constants.CAPTAIN_PREF_KEY, ""));
            return picked != null && IsUnlocked(picked) ? picked : All.FirstOrDefault(IsUnlocked);
        }
    }

    public static void Choose(Captain captain) {
        if (captain == null || !IsUnlocked(captain)) return;
        PlayerPrefs.SetString(Constants.CAPTAIN_PREF_KEY, captain.id);
    }
}
