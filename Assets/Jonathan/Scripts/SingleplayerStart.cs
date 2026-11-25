using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

// the training page: pick the rules (a playlist's or your own) and the bot, then start a match against it.
// the bot's power scaling and story sliders are shown but do nothing yet
public class SingleplayerStart : MonoBehaviour
{
    [SerializeField] private UIDocument document;
    [SerializeField] private MenuPages pages;
    [SerializeField] private string page = "trainingPage";

    public MyBox.SceneReference gameScene;

    private VisualElement root;
    private ListDropdown presetsDropdown;
    private Switch isAdvancedAttacks;
    private Switch isUnlimitedAmmo;
    private Switch isSalvo;
    private Switch isBonus;
    private SliderInt turnTimeSlider;
    private Label turnTimeText;
    private VisualElement unlimitedAmmoRow;
    private SliderInt difficultySlider;
    private Label difficultyText;

    // every listed playlist played without a code or captains is a preset, the last choice is custom
    private List<Playlist> presets;
    // null when the rules are the player's own
    private Playlist preset;

    // the slider's last step is no time limit
    private const int unlimitedTime = 95;

    void Start() {
        root = document.rootVisualElement.Q(page);
        presetsDropdown = root.Q<ListDropdown>("trainingPresets");
        isAdvancedAttacks = root.Q<Switch>("trainingAdvanced");
        isUnlimitedAmmo = root.Q<Switch>("trainingUnlimited");
        isSalvo = root.Q<Switch>("trainingSalvo");
        isBonus = root.Q<Switch>("trainingBonus");
        turnTimeSlider = root.Q<SliderInt>("trainingTurnTime");
        turnTimeText = root.Q<Label>("trainingTurnTimeText");
        unlimitedAmmoRow = root.Q("trainingUnlimitedRow");
        difficultySlider = root.Q<SliderInt>("trainingDifficulty");
        difficultyText = root.Q<Label>("trainingDifficultyText");

        foreach (var rule in new[] { isAdvancedAttacks, isUnlimitedAmmo, isSalvo, isBonus }) {
            rule.changed += _ => RulesChanged();
        }
        turnTimeSlider.RegisterValueChangedCallback(e => {
            // whole steps of 5 seconds
            var snapped = Mathf.RoundToInt(e.newValue / 5f) * 5;
            if (snapped != e.newValue) {
                turnTimeSlider.SetValueWithoutNotify(snapped);
            }
            RulesChanged();
        });
        presetsDropdown.changed += PickPreset;

        difficultySlider.SetValueWithoutNotify((int)BotAI.SavedDifficulty);
        difficultySlider.RegisterValueChangedCallback(e => {
            PlayerPrefs.SetInt(Constants.BOT_DIFFICULTY_PREF_KEY, e.newValue);
            ShowDifficulty();
        });
        ShowDifficulty();

        UIKit.OnClick(root.Q("trainingBack"), () => pages.Show(-1));
        UIKit.OnClick(root.Q("trainingStart"), StartGame);
        UIKit.OnClick(root.Q("trainingRulesTab"), () => SetTab(false), ClickSound.Light);
        UIKit.OnClick(root.Q("trainingBotTab"), () => SetTab(true), ClickSound.Light);

        presets = Playlists.All.Where(p => p.listed && !p.usesCode && !p.captains).ToList();
        var names = presets.Select(p => p.title.ToUpper()).ToList();
        names.Add("CUSTOM");
        presetsDropdown.SetChoices(names);

        PickPreset(0);
        SetTab(false);
    }

    void Update() {
        var cancel = GameInput.Get("UI/Cancel");
        if (cancel != null && cancel.WasPressedThisFrame() && UIKit.IsShown(root)) {
            pages.Show(-1);
        }
    }

    public void Open() {
        pages.ShowPage(page);
    }

    // only the tabbed look shows the tabs, one card at a time
    public void SetTab(bool bot) {
        UIKit.SetLit(root.Q("trainingRulesTab"), !bot);
        UIKit.SetLit(root.Q("trainingBotTab"), bot);
        root.Q("trainingRules").EnableInClassList("off", bot);
        root.Q("trainingBot").EnableInClassList("off", !bot);
    }

    private void ShowDifficulty() {
        difficultyText.text = ((BotDifficulty)difficultySlider.value).ToString().ToUpper();
    }

    private void PickPreset(int index) {
        // the custom choice keeps the rules as they are
        if (index >= presets.Count) {
            preset = null;
            presetsDropdown.index = presets.Count;
            return;
        }

        preset = presets[index];
        presetsDropdown.index = index;

        isAdvancedAttacks.value = preset.advancedCombat;
        isUnlimitedAmmo.value = preset.unlimitedAmmo;
        isSalvo.value = preset.salvo;
        isBonus.value = preset.bonus;
        turnTimeSlider.SetValueWithoutNotify(preset.turnTimeLimit <= 0 ? unlimitedTime : (int)preset.turnTimeLimit);

        RefreshRules();
    }

    // any change away from the preset makes it custom
    private void RulesChanged() {
        if (preset != null && !Matches(preset)) {
            preset = null;
            presetsDropdown.index = presets.Count;
        }

        RefreshRules();
    }

    private bool Matches(Playlist playlist) {
        return isAdvancedAttacks.value == playlist.advancedCombat &&
            isUnlimitedAmmo.value == playlist.unlimitedAmmo &&
            isSalvo.value == playlist.salvo &&
            isBonus.value == playlist.bonus &&
            turnTimeLimit == playlist.turnTimeLimit;
    }

    private float turnTimeLimit => turnTimeSlider.value >= unlimitedTime ? 0f : turnTimeSlider.value;

    private void RefreshRules() {
        // unlimited ammo is only for advanced attacks
        unlimitedAmmoRow.SetEnabled(isAdvancedAttacks.value);
        unlimitedAmmoRow.EnableInClassList("unavailable", !isAdvancedAttacks.value);
        turnTimeText.text = turnTimeLimit > 0 ? turnTimeLimit + " SECONDS" : "UNLIMITED";
    }

    public void StartGame() {
        Debug.Log("Starting game");
        PlayerPrefs.SetString(Constants.PLAYLIST_PREF_KEY, Playlists.Custom);

        // a bot match always plays the custom rules, set to the ones picked here; no chat with a bot
        GameModeInfo.SetCustomPrefs(true, isAdvancedAttacks.value, isSalvo.value, isBonus.value, isAdvancedAttacks.value && isUnlimitedAmmo.value, turnTimeLimit, false);

        SceneManager.LoadScene(gameScene.SceneName);
    }
}
