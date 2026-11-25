using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

// the campaign: the map on the left, starting from the bottom, and the picked mission on the right with play
public class CampaignScreen : MonoBehaviour
{
    [SerializeField] private UIDocument document;
    [SerializeField] private string gameScene = "6 Game";

    private VisualElement root;
    private ScrollView scroll;
    private CampaignMap map;
    private Button play;
    private Mission selected;
    private bool scrolled;

    void Start() {
        root = document.rootVisualElement;
        scroll = root.Q<ScrollView>("campaignScroll");
        map = root.Q<CampaignMap>("campaignMap");
        play = root.Q<Button>("missionPlay");

        map.Fill(Missions.All);
        map.picked += Select;
        UIKit.OnClick(play, Play);

        root.Q<Label>("campaignProgress").text = Mathf.RoundToInt(Missions.progress * 100) + "% COMPLETE";

        // back from a mission picks it again, otherwise the furthest one still to play
        var start = Missions.returning ?? Missions.All.LastOrDefault(m => Missions.State(m) == MissionState.Available) ?? Missions.All.FirstOrDefault();
        Missions.returning = null;
        if (start != null) Select(start);

        // the map starts at the bottom once it has a size, then makes sure the picked mission is in view
        // (a frame later, once the scroll view has caught up with the map's height)
        map.RegisterCallback<GeometryChangedEvent>(_ => {
            if (scrolled) return;
            scrolled = true;
            scroll.schedule.Execute(() => {
                scroll.scrollOffset = new Vector2(0, map.layout.height - scroll.contentViewport.layout.height);
                var node = map.NodeOf(selected);
                if (node != null) scroll.ScrollTo(node);
            });
        });
    }

    void Update() {
        var submit = GameInput.Get("UI/Submit");
        if (submit != null && submit.WasPressedThisFrame() && play.enabledInHierarchy) Play();
    }

    private void Select(Mission mission) {
        selected = mission;
        map.Refresh(mission);

        var state = Missions.State(mission);
        root.Q<Label>("missionNumber").text = "MISSION " + mission.number.ToString("D2");
        var tag = root.Q<Label>("missionState");
        tag.text = state == MissionState.Cleared ? "CLEARED" : state == MissionState.Locked ? "LOCKED" : "";
        tag.EnableInClassList("dim", state == MissionState.Locked);

        root.Q<Label>("missionName").text = mission.title.ToUpper();
        root.Q<Label>("missionDescription").text = mission.description;
        root.Q<Label>("missionRules").text = Rules(mission);
        root.Q<Label>("missionOpponent").text = mission.difficulty.ToString().ToUpper() + " BOT";
        root.Q<Label>("missionReward").text = mission.reward != null ? "UNLOCKS " + mission.reward.captainName.ToUpper() : "-";

        play.SetEnabled(state != MissionState.Locked);
        play.text = state == MissionState.Cleared ? "PLAY AGAIN" : "PLAY";
    }

    private static string Rules(Mission mission) {
        var rules = new List<string>();
        if (mission.advancedCombat) rules.Add(mission.unlimitedAmmo ? "UNLIMITED" : "ADVANCED");
        if (mission.salvo) rules.Add("SALVO");
        if (mission.bonus) rules.Add("BONUS");
        return rules.Count == 0 ? "CLASSIC" : string.Join("  ·  ", rules);
    }

    private void Play() {
        if (selected == null || Missions.State(selected) == MissionState.Locked) return;
        Missions.Play(selected, gameScene);
    }
}
