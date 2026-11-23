using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;

public class VolumeController : MonoBehaviour {
    [SerializeField] private UIDocument document;

    private Slider mainVolumeSlider;
    private Slider musicVolumeSlider;

    void Start() {
        mainVolumeSlider = document.rootVisualElement.Q<Slider>("mainVolume");
        musicVolumeSlider = document.rootVisualElement.Q<Slider>("musicVolume");

        UpdateVolume();
        
        mainVolumeSlider.value = GetMainVolume();
        musicVolumeSlider.value = GetMusicVolume(false);

        mainVolumeSlider.RegisterValueChangedCallback(e => SetMainVolume(e.newValue));
        musicVolumeSlider.RegisterValueChangedCallback(e => SetMusicVolume(e.newValue));
    }

    public static void UpdateVolume() {
        OnMainVolumeChange?.Invoke(GetMainVolume());
        OnMusicVolumeChange?.Invoke(GetMusicVolume());
    }

    public static void SetMainVolume(float volume) {
        volume = Mathf.Clamp01(volume);

        PlayerPrefs.SetFloat(Constants.MAIN_VOLUME_PREF_KEY, volume);
        
        UpdateVolume();
    }

    public static void SetMusicVolume(float volume) {
        volume = Mathf.Clamp01(volume);

        PlayerPrefs.SetFloat(Constants.MUSIC_VOLUME_PREF_KEY, volume);
        
        UpdateVolume();
    }

    public static float GetMainVolume() {
        return PlayerPrefs.GetFloat(Constants.MAIN_VOLUME_PREF_KEY, 1f);
    }

    public static float GetMusicVolume(bool includeMainVolume = true) {
        float volume = PlayerPrefs.GetFloat(Constants.MUSIC_VOLUME_PREF_KEY, 1f);

        if (includeMainVolume) {
            volume *= GetMainVolume();
        }

        return volume;
    }
    
    public delegate void MainVolumeChange(float volume);
    public static event MainVolumeChange OnMainVolumeChange;

    public delegate void MusicVolumeChange(float volume);
    public static event MusicVolumeChange OnMusicVolumeChange;
}
