using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager instance;

    public AudioSource musicSource;

    public AudioClip[] musicClips;

    public int currentClip = 0;

    private void Awake()
    {
        if (musicClips.Length == 0) {
            Debug.Log("No music clips found!");
            Destroy(gameObject);
            return;
        }

        if (instance == null) {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else {
            Destroy(gameObject);
        }

        musicSource = GetComponent<AudioSource>();
        
        currentClip = Random.Range(0, musicClips.Length);

        PlayMusic(currentClip);
    }

    public void PlayMusic(int clip)
    {
        musicSource.clip = musicClips[clip];
        musicSource.Play();
    }

    private void Update() {
        if (!musicSource.isPlaying) {
            currentClip++;
            if (currentClip >= musicClips.Length) {
                currentClip = 0;
            }
            PlayMusic(currentClip);
        }
    }
}
