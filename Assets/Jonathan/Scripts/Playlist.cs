using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// one playlist: how it shows in the multiplayer strip and the rules a match in it plays by.
// a new playlist is a new file in Resources/Playlists, nothing else.
// its id is what matchmaking and the room use, so it never changes once players have it
[CreateAssetMenu(menuName = "Armadahex/Playlist")]
public class Playlist : ScriptableObject
{
    public string id;
    public string title;
    [TextArea(3, 10)]
    public string description;

    // left to right in the strip
    public int order;
    // false keeps it out of the strip (the custom game rules live here but are reached by code)
    public bool listed = true;
    // joined with a 4 character code instead of matchmaking
    public bool usesCode;
    // optional, the card is plain without it
    public Texture2D art;

    [Header("Rules")]
    public bool singleplayer;
    public bool advancedCombat;
    public bool salvo;
    public bool bonus;
    public bool unlimitedAmmo;
    public float turnTimeLimit = 25;
    public bool allowChat = true;
    // captains with their passives and specials, not played yet
    public bool captains;

    // a copy to play by, so a custom game's changes never touch this file
    public GameModeData Rules() {
        return new GameModeData {
            id = id,
            name = title,
            isSingleplayer = singleplayer,
            isAdvancedCombat = advancedCombat,
            isSalvo = salvo,
            isBonus = bonus,
            isUnlimitedAmmo = unlimitedAmmo,
            turnTimeLimit = turnTimeLimit,
            allowChat = allowChat,
            captains = captains
        };
    }
}

public static class Playlists
{
    // the custom game's id, the same as the old GameMode.Customs so custom rooms keep working
    public const string Custom = "Customs";

    private static List<Playlist> all;

    public static IReadOnlyList<Playlist> All {
        get {
            if (all == null) {
                all = Resources.LoadAll<Playlist>("Playlists").OrderBy(p => p.order).ToList();
            }
            return all;
        }
    }

    public static Playlist Find(string id) {
        return All.FirstOrDefault(p => p.id == id);
    }
}
