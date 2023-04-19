using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(AudioSource))]
public class JukeBox : MonoBehaviour
{
    private AudioSource musicSource;

    public bool allowPlaying;

    public AudioInfo[] musicClips;

    public int currentClip;
    
    public bool inBetweenPieces;

    public float timeInbetween;
    public float timeBeforeNext;

    private void Awake() {
        musicSource = GetComponent<AudioSource>();
        allowPlaying = false;
    }

    public void StartMusic(AudioInfo[] musicClips) {
        this.musicClips = musicClips;
        currentClip = Random.Range(0, musicClips.Length);
        allowPlaying = true;
        inBetweenPieces = true;
        timeBeforeNext = 0;
    }

    public void StopMusic() {
        allowPlaying = false;
        musicSource.Stop();
    }

    void FixedUpdate() {
        if (!allowPlaying) return;

        if (!inBetweenPieces) {
            if (!musicSource.isPlaying) {
                inBetweenPieces = true;
                timeBeforeNext = timeInbetween;
            }
        }
        else  {
            timeBeforeNext -= Time.deltaTime;
            if (timeBeforeNext <= 0) {
                inBetweenPieces = false;
                PlayNext();
            }
        }
    }
    
    private void PlayNext() {
        currentClip++;
        if (currentClip >= musicClips.Length) {
            currentClip = 0;
        }
        PlayMusic(currentClip);
    }

    private void PlayMusic(int clip) {
        if (clip >= musicClips.Length) return;

        musicSource.clip = musicClips[clip].clip;
        musicSource.volume = musicClips[clip].volume;
        musicSource.Play();
    }
}
