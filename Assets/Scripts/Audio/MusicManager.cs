using UnityEngine;
using UnityEngine.SceneManagement;

public class MusicManager : MonoBehaviour
{
    private class Voice
    {
        public AudioSource source;
        public float level;        // fade level, 0 to 1
        public float target;       // where the fade is heading
        public float clipVolume = 1f;
    }

    private static MusicManager instance;
    private MusicLibrary library;
    private readonly Voice[] voices = new Voice[2];
    private int active = 0;
    private AudioClip currentClip;

    // set by a scene's Awake, used instead of the library for that scene load
    private static AudioClip pendingClip;
    private static float pendingVolume = 1f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Create()
    {
        if (instance != null) return;

        MusicLibrary lib = Resources.Load<MusicLibrary>("MusicLibrary");
        if (lib == null)
        {
            Debug.LogWarning("MusicManager: no MusicLibrary asset in Assets/Resources, so no music will play.");
            return;
        }

        GameObject go = new GameObject("MusicManager");
        DontDestroyOnLoad(go);
        instance = go.AddComponent<MusicManager>();
        instance.library = lib;
        instance.Setup();
    }

    // Fades the music out. Call it when the win/lose panel opens.
    public static void StopMusic()
    {
        if (instance == null) return;

        foreach (Voice v in instance.voices) v.target = 0f;
        instance.currentClip = null;   // so the next scene's song starts again, even if it's the same clip
    }

    // Call this in a scene's Awake(). This scene uses this song instead of its library track.
    // If the same song is already playing (like from the VN), it just keeps playing without restarting.
    public static void SetSceneOverride(AudioClip clip, float volume = 1f)
    {
        pendingClip = clip;
        pendingVolume = volume;
    }

    private void Setup()
    {
        for (int i = 0; i < voices.Length; i++)
        {
            AudioSource source = gameObject.AddComponent<AudioSource>();
            source.loop = true;
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            voices[i] = new Voice { source = source };
        }

        SceneManager.sceneLoaded += OnSceneLoaded;
        ApplySceneMusic(SceneManager.GetActiveScene().name);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ApplySceneMusic(scene.name);
    }

    private void ApplySceneMusic(string sceneName)
    {
        // the scene asked for its own song: use it and skip the library
        if (pendingClip != null)
        {
            AudioClip clip = pendingClip;
            float volume = pendingVolume;
            pendingClip = null;

            CrossfadeTo(clip, volume);
            return;
        }

        MusicLibrary.Track track = library.Find(sceneName);
        if (track == null) return;   // no entry for this scene: keep whatever is playing

        CrossfadeTo(track.clip, track.volume);
    }

    private void CrossfadeTo(AudioClip clip, float volume)
    {
        // same song as before: let it keep playing, no restart
        if (clip == currentClip)
        {
            voices[active].clipVolume = volume;
            return;
        }

        voices[active].target = 0f;   // fade the old song out

        currentClip = clip;
        active = 1 - active;
        Voice next = voices[active];
        next.clipVolume = volume;
        next.level = 0f;

        if (clip != null)
        {
            next.source.clip = clip;
            next.source.Play();
            next.target = 1f;
        }
        else
        {
            next.target = 0f;
        }
    }

    void Update()
    {
        float speed = library.fadeSeconds <= 0f ? 100f : 1f / library.fadeSeconds;

        foreach (Voice v in voices)
        {
            v.level = Mathf.MoveTowards(v.level, v.target, speed * Time.unscaledDeltaTime);
            v.source.volume = v.level * v.clipVolume * GameAudio.MusicVolume;   // follows the Music slider live

            if (v.level <= 0f && v.target <= 0f && v.source.isPlaying) v.source.Stop();
        }
    }
}