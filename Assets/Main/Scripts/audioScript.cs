using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class audioScript : MonoBehaviour
{
    public int maxNumOfShips = 5;
    public int numOfShips;
    public float maxVolume;
    private AudioSource backgroundPlayer;
    private AudioSource layer1Player;
    private AudioSource layer2Player;
    public AudioClip backgroundLayer;
    public AudioClip layer1;
    public AudioClip layer2;
    private float layer1Volume = 0;
    private float layer2Volume = 0;
    private float lastLayer1Volume = 0;
    private float lastLayer2Volume = 0;
    private bool is1Stabilizing = false;
    private float stabilizingAmount1;
    private float targetVolume1;
    private bool is2Stabilizing = false;
    private float stabilizingAmount2;
    private float targetVolume2;
    public float fadeTime = 50;

    // Start is called before the first frame update
    void Start()
    {
        VolumeController.UpdateVolume();
        VolumeController.OnMusicVolumeChange += OnMusicVolumeChange;

        numOfShips = maxNumOfShips;
        backgroundPlayer = GetComponent<AudioSource>();
        layer1Player = gameObject.transform.Find("layer1").GetComponent<AudioSource>();
        layer2Player = gameObject.transform.Find("layer2").GetComponent<AudioSource>();
        playMusic(backgroundPlayer, backgroundLayer, maxVolume);
        playMusic(layer1Player, layer1, layer1Volume);
        playMusic(layer2Player, layer2, layer2Volume);
        UpdateShips();

        SetVolumes();
    }

    void SetVolumes() {
        backgroundPlayer.volume = maxVolume * VolumeController.GetMusicVolume();
        layer1Player.volume = layer1Volume * VolumeController.GetMusicVolume();;
        layer2Player.volume = layer2Volume * VolumeController.GetMusicVolume();;
    }

    // Update is called once per frame
    void playMusic(AudioSource player, AudioClip music, float volume)
    {
        Debug.Log("playing music at volume " + volume + " and clip " + music.name);

        player.volume = volume;
        player.clip = music;
        player.Stop();
        player.Play();
    }

    public void RemainingShips(int remainingShips) {
        numOfShips = remainingShips;
        UpdateShips();
    }

    public void DestroyShip(int numOfDestroyed = 1)
    {
        numOfShips -= numOfDestroyed;
        UpdateShips();
    }

    void UpdateShips()
    {
        float currentcompletion = (((float)maxNumOfShips - (float)numOfShips) / (float)maxNumOfShips);
        //controlling layer1 volume
        if (currentcompletion >= .5f)
        {
            layer1Player.volume = maxVolume;
        }
        else
        {
            is1Stabilizing = true;
            float currVolume = layer1Player.volume;
            targetVolume1 = (currentcompletion / .5f) * maxVolume;
            is1Stabilizing = true;
            stabilizingAmount1 = (targetVolume1 - currVolume) / (fadeTime / Time.deltaTime);
        }
        //controlling layer2 volume
        if (currentcompletion <= .5f)
        {
            layer2Player.volume = 0;
        }
        else
        {
            is2Stabilizing = true;
            float currVolume = layer2Player.volume;
            targetVolume2 = ((currentcompletion - .5f) / .5f) * maxVolume;
            is2Stabilizing = true;
            stabilizingAmount2 = (targetVolume1 - currVolume) / (fadeTime / Time.deltaTime);
        }
    }
    private void FixedUpdate()
    {
        if (layer1Player.volume != lastLayer1Volume || layer2Player.volume != lastLayer2Volume) {
            SetVolumes();
        }

        if (is1Stabilizing)
        {
            if (stabilizingAmount1 + layer1Player.volume >= targetVolume1)
            {
                layer1Player.volume = targetVolume1;
                is1Stabilizing = false;
            }
            else
            {
                layer1Player.volume += stabilizingAmount1;
            }
        }
        if (is2Stabilizing)
        {
            if (stabilizingAmount2 + layer2Player.volume >= targetVolume2)
            {
                layer2Player.volume = targetVolume2;
                is1Stabilizing = false;
            }
            else
            {
                layer2Player.volume += stabilizingAmount2;
            }
        }

        lastLayer1Volume = layer1Player.volume;
        lastLayer2Volume = layer2Player.volume;
    }

    private void OnMusicVolumeChange(float volume)
    {
        maxVolume = volume * 0.5f;
    }
}
