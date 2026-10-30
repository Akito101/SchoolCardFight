using UnityEngine;

public class ButtonClickSound : MonoBehaviour
{
    public AudioClip clickSound;
    [Range(0f, 1f)] public float volume = 1f;

    // one hidden audio source shared by every scene, created the first time it's needed
    private static AudioSource sharedSource;

    // Buttons call this from OnClick
    public void PlayClick()
    {
        if (clickSound == null) return;

        if (sharedSource == null)
        {
            GameObject go = new GameObject("ClickAudio");
            DontDestroyOnLoad(go);
            sharedSource = go.AddComponent<AudioSource>();
            sharedSource.playOnAwake = false;
            sharedSource.loop = false;
            sharedSource.spatialBlend = 0f;
        }

        // the SFX slider scales the click, read fresh on every press
        sharedSource.PlayOneShot(clickSound, volume * GameAudio.SfxVolume);
    }
}