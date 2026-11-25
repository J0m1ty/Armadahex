using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// a part of the campaign, numbered from 0: its missions sit above its line on the map, and every mission in it
// has to be cleared before the next chapter opens. a new chapter is a new file in Resources/Chapters
[CreateAssetMenu(menuName = "Armadahex/Chapter")]
public class Chapter : ScriptableObject
{
    public string id;
    public int number;
}

public static class Chapters
{
    private static List<Chapter> all;

    public static IReadOnlyList<Chapter> All {
        get {
            if (all == null) {
                all = Resources.LoadAll<Chapter>("Chapters").OrderBy(c => c.number).ToList();
            }
            return all;
        }
    }
}
