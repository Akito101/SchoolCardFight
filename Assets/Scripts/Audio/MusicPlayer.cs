using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class MusicPlayer : MonoBehaviour
{
    public AudioClip music;
    [Range(0f, 1f)] public float baseVolume = 1f;
    public bool keepBetweenScenes = true;

    private static MusicPlayer instance;
    private AudioSource source;

    void Awake()
    {
        if (keepBetweenScenes)
        {
            // only one music player at a time, extra copies in other scenes remove themselves
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;
            DontDestroyOnLoad(gameObject);
        }

        source = GetComponent<AudioSource>();
        source.clip = music;
        source.loop = true;
        source.playOnAwake = false;
        source.spatialBlend = 0f;

        ApplyVolume();
        GameAudio.OnChanged += ApplyVolume;
    }

    void Start()
    {
        if (source == null) return;   // this was a duplicate
        if (source.clip != null && !source.isPlaying) source.Play();
    }

    void OnDestroy()
    {
        GameAudio.OnChanged -= ApplyVolume;
        if (instance == this) instance = null;
    }

    private void ApplyVolume()
    {
        if (source != null) source.volume = baseVolume * GameAudio.MusicVolume;
    }
}