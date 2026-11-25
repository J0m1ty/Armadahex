using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// locked until a mission before it is cleared, then available until the player wins it
public enum MissionState {
    Locked,
    Available,
    Cleared
}

// one battle on the campaign map: where it sits, what it opens up, and the rules and bot it's played with.
// a new mission is a new file in Resources/Missions. missions play as bot matches for now, with no logic of their own
[CreateAssetMenu(menuName = "Armadahex/Mission")]
public class Mission : ScriptableObject
{
    public string id;
    public int number;
    public string title;
    [TextArea(2, 4)]
    public string description;

    [Header("Map")]
    // 0 is the bottom row, the map climbs from there
    public int row;
    // in columns from the middle, -1 one to the left, 1 one to the right
    public float column;
    // the missions clearing this one opens up
    public List<Mission> unlocks = new List<Mission>();

    [Header("Battle")]
    public BotDifficulty difficulty;
    public bool advancedCombat;
    public bool salvo;
    public bool bonus;
    public bool unlimitedAmmo;

    [Header("Reward")]
    // a campaign captain that becomes unlockable when this is cleared
    public Captain reward;

    public string fullTitle => "Mission " + number + ": " + title;
}

public static class Missions
{
    private static List<Mission> all;

    // the mission being played; set when one starts, read by the bot and by game over
    public static Mission playing { get; private set; }
    // the mission played last, so continue on game over comes back to the map
    public static Mission returning { get; set; }

    public static IReadOnlyList<Mission> All {
        get {
            if (all == null) {
                all = Resources.LoadAll<Mission>("Missions").OrderBy(m => m.number).ToList();
            }
            return all;
        }
    }

    public static Mission Find(string id) {
        return All.FirstOrDefault(m => m.id == id);
    }

    // only cleared is saved; a mission with nothing before it, or with any mission before it cleared, is available
    public static MissionState State(Mission mission) {
        if (IsCleared(mission)) return MissionState.Cleared;

        var before = All.Where(m => m.unlocks.Contains(mission)).ToList();
        if (before.Count == 0 || before.Any(IsCleared)) return MissionState.Available;
        return MissionState.Locked;
    }

    private static bool IsCleared(Mission mission) {
        return PlayerPrefs.GetInt(Constants.MISSION_STATE_PREF_PREFIX + mission.id, 0) == (int)MissionState.Cleared;
    }

    public static int clearedCount => All.Count(IsCleared);

    // 0 to 1
    public static float progress => All.Count == 0 ? 0 : (float)clearedCount / All.Count;

    // a win: saves it, makes its captain unlockable and updates the progress stat
    public static void Clear(Mission mission) {
        PlayerPrefs.SetInt(Constants.MISSION_STATE_PREF_PREFIX + mission.id, (int)MissionState.Cleared);
        if (mission.reward != null && mission.reward.source == CaptainSource.Campaign) {
            Captains.MakeUnlockable(mission.reward);
        }
        PlayerPrefs.SetFloat(Constants.STAT_CAMPAIGN_PROGRESS, progress);
    }

    // a bot match with the mission's rules, through the same path as training
    public static void Play(Mission mission, string gameScene) {
        PlayerPrefs.SetString(Constants.PLAYLIST_PREF_KEY, Playlists.Custom);
        GameModeInfo.SetCustomPrefs(true, mission.advancedCombat, mission.salvo, mission.bonus, mission.advancedCombat && mission.unlimitedAmmo, 0f, false);
        PlayerPrefs.SetString(Constants.CUSTOMS_NAME_KEY, mission.fullTitle);
        PlayerPrefs.SetString(Constants.CAMPAIGN_MISSION_PREF_KEY, mission.id);
        UnityEngine.SceneManagement.SceneManager.LoadScene(gameScene);
    }

    // the game scene takes the handed over mission once, so a later training match can't clear it
    public static void TakeHandover() {
        playing = Find(PlayerPrefs.GetString(Constants.CAMPAIGN_MISSION_PREF_KEY, ""));
        PlayerPrefs.DeleteKey(Constants.CAMPAIGN_MISSION_PREF_KEY);
    }

    // the match is over: a win clears the mission
    public static void Finish(bool won) {
        if (playing == null) return;
        if (won) Clear(playing);
        returning = playing;
        playing = null;
    }
}
