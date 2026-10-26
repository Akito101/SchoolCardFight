using System;
using UnityEngine;

public static class GameAudio
{
    private const string MusicKey = "MusicVolume";
    private const string SfxKey = "SfxVolume";

    public static event Action OnChanged;

    public static float MusicVolume
    {
        get => PlayerPrefs.GetFloat(MusicKey, 0.8f);
        set { PlayerPrefs.SetFloat(MusicKey, Mathf.Clamp01(value)); OnChanged?.Invoke(); }
    }

    public static float SfxVolume
    {
        get => PlayerPrefs.GetFloat(SfxKey, 1f);
        set { PlayerPrefs.SetFloat(SfxKey, Mathf.Clamp01(value)); OnChanged?.Invoke(); }
    }

    public static void Save()
    {
        PlayerPrefs.Save();
    }

    // Play any sound effect with: GameAudio.PlaySfx(myClip);
    private static AudioSource sfxSource;

    public static void PlaySfx(AudioClip clip, float volumeScale = 1f)
    {
        if (clip == null) return;

        if (sfxSource == null)
        {
            GameObject go = new GameObject("SfxPlayer");
            UnityEngine.Object.DontDestroyOnLoad(go);
            sfxSource = go.AddComponent<AudioSource>();
            sfxSource.playOnAwake = false;
            sfxSource.spatialBlend = 0f;   // plain 2D sound
        }

        sfxSource.PlayOneShot(clip, SfxVolume * volumeScale);
    }
}