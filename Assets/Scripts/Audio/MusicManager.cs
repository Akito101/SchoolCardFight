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
        PlayForScene(SceneManager.GetActiveScene().name);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        PlayForScene(scene.name);
    }

    private void PlayForScene(string sceneName)
    {
        MusicLibrary.Track track = library.Find(sceneName);
        if (track == null) return;   // no entry for this scene: keep whatever is playing

        // same song as before (like the intro going into the battle): let it keep playing
        if (track.clip == currentClip)
        {
            voices[active].clipVolume = track.volume;
            return;
        }

        voices[active].target = 0f;   // fade the old song out

        currentClip = track.clip;
        active = 1 - active;
        Voice next = voices[active];
        next.clipVolume = track.volume;
        next.level = 0f;

        if (track.clip != null)
        {
            next.source.clip = track.clip;
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