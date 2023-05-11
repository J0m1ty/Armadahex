using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class ButtonClick : MonoBehaviour
{
    public static ButtonClick instance;

    public AudioClip click;
    public AudioClip clickLight;
    public AudioClip clickHeavy;

    private AudioSource audioSource;

    void Awake() {
        if (instance == null) {
            instance = this;
        }
        else {
            Destroy(gameObject);
        }

        DontDestroyOnLoad(gameObject);

        audioSource = GetComponent<AudioSource>();

        VolumeController.UpdateVolume();
        VolumeController.OnMainVolumeChange += OnMainVolumeChange;
    }

    private void OnMainVolumeChange(float volume) {
        audioSource.volume = volume;
    }

    public void PlayClick() {
        audioSource.PlayOneShot(click);
    }

    public void PlayClickLight() {
        audioSource.PlayOneShot(clickLight);
    }

    public void PlayClickHeavy() {
        audioSource.PlayOneShot(clickHeavy);
    }
}
