using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

// the training page: pick the rules (a playlist's or your own) and the bot, then start a match against it.
// bots play on their own schedule, so there's no turn time
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
    private VisualElement unlimitedAmmoRow;
    private SliderInt difficultySlider;
    private Label difficultyText;

    // every listed playlist played without a code or captains is a preset, the last choice is custom
    private List<Playlist> presets;
    // null when the rules are the player's own
    private Playlist preset;

    void Start() {
        root = document.rootVisualElement.Q(page);
        presetsDropdown = root.Q<ListDropdown>("trainingPresets");
        isAdvancedAttacks = root.Q<Switch>("trainingAdvanced");
        isUnlimitedAmmo = root.Q<Switch>("trainingUnlimited");
        isSalvo = root.Q<Switch>("trainingSalvo");
        isBonus = root.Q<Switch>("trainingBonus");
        unlimitedAmmoRow = root.Q("trainingUnlimitedRow");
        difficultySlider = root.Q<SliderInt>("trainingDifficulty");
        difficultyText = root.Q<Label>("trainingDifficultyText");

        foreach (var rule in new[] { isAdvancedAttacks, isUnlimitedAmmo, isSalvo, isBonus }) {
            rule.changed += _ => RulesChanged();
        }
        presetsDropdown.changed += PickPreset;

        difficultySlider.SetValueWithoutNotify((int)BotAI.SavedDifficulty);
        difficultySlider.RegisterValueChangedCallback(e => {
            PlayerPrefs.SetInt(Constants.BOT_DIFFICULTY_PREF_KEY, e.newValue);
            ShowDifficulty();
        });
        ShowDifficulty();

        UIKit.OnClick(root.Q("trainingBack"), () => pages.Show(-1));
        UIKit.OnClick(root.Q("trainingStart"), StartGame);

        presets = Playlists.All.Where(p => p.listed && !p.usesCode && !p.captains).ToList();
        var names = presets.Select(p => p.title.ToUpper()).ToList();
        names.Add("CUSTOM");
        presetsDropdown.SetChoices(names);

        PickPreset(0);
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
            isBonus.value == playlist.bonus;
    }

    private void RefreshRules() {
        // unlimited ammo is only for advanced attacks
        unlimitedAmmoRow.SetEnabled(isAdvancedAttacks.value);
        unlimitedAmmoRow.EnableInClassList("unavailable", !isAdvancedAttacks.value);
    }

    public void StartGame() {
        Debug.Log("Starting game");
        PlayerPrefs.SetString(Constants.PLAYLIST_PREF_KEY, Playlists.Custom);

        // a bot match always plays the custom rules, set to the ones picked here; no turn time or chat with a bot
        GameModeInfo.SetCustomPrefs(true, isAdvancedAttacks.value, isSalvo.value, isBonus.value, isAdvancedAttacks.value && isUnlimitedAmmo.value, 0f, false);
        if (preset != null) {
            PlayerPrefs.SetString(Constants.CUSTOMS_NAME_KEY, preset.title);
        }
        else {
            PlayerPrefs.DeleteKey(Constants.CUSTOMS_NAME_KEY);
        }

        SceneManager.LoadScene(gameScene.SceneName);
    }
}
