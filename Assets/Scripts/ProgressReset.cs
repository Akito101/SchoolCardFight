using UnityEngine;

public static class ProgressReset
{
    public static void ResetAll()
    {
        float music = GameAudio.MusicVolume;
        float sfx = GameAudio.SfxVolume;

        PlayerPrefs.DeleteAll();
        PlayerCollection.Clear();
        PlayerDeck.Clear();

        GameAudio.MusicVolume = music;
        GameAudio.SfxVolume = sfx;
        PlayerPrefs.Save();
    }
}