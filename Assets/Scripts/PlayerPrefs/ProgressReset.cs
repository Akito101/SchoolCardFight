using UnityEngine;

public static class ProgressReset
{
    public static void ResetAll()
    {
        float music = GameAudio.MusicVolume;
        float sfx = GameAudio.SfxVolume;

        PlayerPrefs.DeleteAll();
        if (typeof(PlayerCollection) != null) PlayerCollection.Clear();
        if (typeof(PlayerDeck) != null) PlayerDeck.Clear();

        GameAudio.MusicVolume = music;
        GameAudio.SfxVolume = sfx;
        PlayerPrefs.Save();
    }
}