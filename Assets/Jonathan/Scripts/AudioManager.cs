using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using MyBox;
using UnityEngine.SceneManagement;

[Serializable]
public class AudioInfo {
    public AudioClip clip;
    public float volume;
}

[Serializable]
public class ResultAudio {
    public AudioInfo[] win;
    public AudioInfo[] lose;
}

[Serializable]
public enum InteractionType {
    Click,
    Error,
    VoiceConfirm,
    VoiceDeny
}

[Serializable]
public class InteractionAudio {
    public InteractionType type;
    public AudioInfo[] audio;
}

[Serializable]
public enum ActionType {
    Fire,
    Explosion
}

[Serializable]
public class ActionAudio {
    public ActionType type;
    public AudioInfo[] audio;
}

[Serializable]
public class ShipAudio {
    public ShipType type;
    public AudioInfo playerDestroyed;
    public AudioInfo enemyDestroyed;
}

[Serializable]
public class GameModeAudio {
    public GameMode mode;
    public AudioInfo start;
}

[Serializable]
public class MusicAudio {
    public SceneReference[] scenesToPlay;
    public AudioInfo[] clips;
    public int id;
}

[Serializable]
public class HitAudio {
    public AudioInfo hitVoice;
    public AudioInfo missVoice;
}

[RequireComponent(typeof(AudioSource))]
public class AudioManager : MonoBehaviour {
    public static AudioManager instance;

    private AudioSource mainSource;

    public ResultAudio resultAudio;
    public List<InteractionAudio> interactionAudio;
    public List<ActionAudio> actionAudio;
    public List<ShipAudio> shipAudio;
    public List<GameModeAudio> gameModeAudio;
    public List<MusicAudio> musicAudio;
    public HitAudio hitAudio;
    public JukeBox jukeBox; //for playing music, feed a list of songs
    public int id;
    
    void Awake() {
        if (instance == null) {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else {
            Destroy(gameObject);
        }

        mainSource = GetComponent<AudioSource>();

        SceneManager.sceneLoaded += OnSceneLoaded;
        
        VolumeController.UpdateVolume();
        VolumeController.OnMainVolumeChange += OnMainVolumeChange;
    }

    private void OnMainVolumeChange(float volume) {
        mainSource.volume = volume;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode) {
        var music = musicAudio.Find(x => Array.Exists(x.scenesToPlay, y => y.SceneName == scene.name));
        if (music != null) {
            if (id != music.id) {
                jukeBox.StartMusic(music.clips);
            }

            id = music.id;
        }
        else {
            jukeBox.StopMusic();
        }
    }
    
    public void PlayResultSound(bool win) {
        Debug.Log("Playing result sound " + (win ? "win" : "lose"));

        // stop all other sounds
        mainSource.Stop();

        var audioInfo = win ? resultAudio.win : resultAudio.lose;
        var audio = audioInfo[UnityEngine.Random.Range(0, audioInfo.Length)];
        PlaySound(audio.clip, audio.volume);
    }
    
    public void PlayInteractionSound(InteractionType type) {
        Debug.Log("Playing interaction sound " + type);
        var audioInfo = interactionAudio.Find(x => x.type == type).audio;
        var audio = audioInfo[UnityEngine.Random.Range(0, audioInfo.Length)];
        PlaySound(audio.clip, audio.volume);
    }
    
    public void PlayActionSound(ActionType type) {
        Debug.Log("Playing action sound " + type);
        var audioInfo = actionAudio.Find(x => x.type == type).audio;
        var audio = audioInfo[UnityEngine.Random.Range(0, audioInfo.Length)];
        PlaySound(audio.clip, audio.volume);
    }

    public void PlayShipSound(ShipType type, bool isPlayer, float delay = 0) {
        if (delay > 0) {
            StartCoroutine(PlayShipSoundDelayed(type, isPlayer, delay));
            return;
        }

        Debug.Log("Playing ship sound " + type + " " + (isPlayer ? "player" : "enemy"));
        var audioInfo = isPlayer ? shipAudio.Find(x => x.type == type).playerDestroyed : shipAudio.Find(x => x.type == type).enemyDestroyed;
        PlaySound(audioInfo.clip, audioInfo.volume);
    }

    private IEnumerator PlayShipSoundDelayed(ShipType type, bool isPlayer, float delay) {
        yield return new WaitForSeconds(delay);
        PlayShipSound(type, isPlayer);
    }

    public void PlayGameModeSound(GameMode mode) {
        Debug.Log("Playing game mode sound " + mode);
        // check if mode is in list
        if (!gameModeAudio.Exists(x => x.mode == mode)) {
            Debug.Log("No audio for game mode " + mode);
            return;
        }
        var audioInfo = gameModeAudio.Find(x => x.mode == mode).start;
        PlaySound(audioInfo.clip, audioInfo.volume);
    }

    // play hit sound after delay
    // then play destroyed sound if destroyed after 3f
    public void PlayHitSound(bool hit, float delay) {
        StartCoroutine(PlayHitSoundDelayed(hit, delay));
    }

    private IEnumerator PlayHitSoundDelayed(bool hit, float delay) {
        yield return new WaitForSeconds(delay);

        if (hit) {
            PlaySound(hitAudio.hitVoice.clip, hitAudio.hitVoice.volume);
        }
        else {
            PlaySound(hitAudio.missVoice.clip, hitAudio.missVoice.volume);
        }
    }

    public void PlaySound(AudioClip clip, float volume) {
        if (clip != null) mainSource.PlayOneShot(clip, volume);
    }
}
