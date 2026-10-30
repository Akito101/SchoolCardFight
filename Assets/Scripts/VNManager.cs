using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using DG.Tweening;

public class VNManager : MonoBehaviour
{
    [System.Serializable]
    public class Step
    {
        public Sprite picture;
        public AudioClip voice;                      // optional: voice line for this picture
        public AudioClip ambience;                   // optional: loops until a later step sets a new one
        [Range(0f, 1f)] public float voiceVolume = 1f;
    }

    [Header("Pictures in order. Each tap shows the next one.")]
    public Step[] steps;
    public float fadeTime = 0.35f;

    [Header("Images (both stretched to fill the screen)")]
    public Image baseImage;       // shows the current picture
    public Image overlayImage;    // fades the next picture in on top

    [Header("Buttons")]
    public Button tapArea;
    public Button skipButton;                 // optional: leave empty if you don't want a skip button
    public bool allowSkip = true;             // untick to hide the skip button
    public bool skipOnlyIfSeenBefore = false; // tick to show Skip only on replays, not the first time

    [Header("Audio")]
    public AudioSource voiceSource;      // optional: created automatically if empty
    public AudioSource ambienceSource;   // optional: created automatically if empty
    [Range(0f, 1f)] public float ambienceVolume = 0.5f;

    [Header("Scene to load after the last picture")]
    public string nextScene = "PackOpening";

    private int index = 0;

    void Awake()
    {
        if (voiceSource == null) voiceSource = CreateSource(false);
        if (ambienceSource == null) ambienceSource = CreateSource(true);

        tapArea.onClick.AddListener(OnTap);

        if (skipButton != null)
        {
            bool seenBefore = PlayerPrefs.GetInt("IntroSeen", 0) == 1;
            bool showSkip = allowSkip && (!skipOnlyIfSeenBefore || seenBefore);

            skipButton.gameObject.SetActive(showSkip);
            if (showSkip) skipButton.onClick.AddListener(GoToNextScene);
        }
    }

    void Start()
    {
        if (steps == null || steps.Length == 0)
        {
            Debug.LogWarning("VNManager: no pictures in Steps!");
            GoToNextScene();
            return;
        }

        baseImage.sprite = steps[0].picture;
        baseImage.color = Color.white;

        overlayImage.sprite = null;
        overlayImage.color = new Color(1f, 1f, 1f, 0f);

        PlayAudio(steps[0]);   // the first picture's voice plays right away
    }

    void Update()
    {
        // follow the sliders live
        ambienceSource.volume = ambienceVolume * GameAudio.MusicVolume;
    }

    private AudioSource CreateSource(bool loop)
    {
        AudioSource s = gameObject.AddComponent<AudioSource>();
        s.playOnAwake = false;
        s.loop = loop;
        s.spatialBlend = 0f;
        return s;
    }

    private void OnTap()
    {
        overlayImage.DOComplete();   // if the last fade is still running, finish it first

        index++;
        if (index >= steps.Length)
        {
            GoToNextScene();
            return;
        }

        Step step = steps[index];
        Sprite next = step.picture;
        overlayImage.sprite = next;
        overlayImage.color = new Color(1f, 1f, 1f, 0f);
        overlayImage.DOFade(1f, fadeTime).OnComplete(() =>
        {
            baseImage.sprite = next;
            overlayImage.color = new Color(1f, 1f, 1f, 0f);
        });

        PlayAudio(step);
    }

    private void PlayAudio(Step step)
    {
        // voice: cut the previous line, play the new one (if any)
        voiceSource.Stop();
        if (step.voice != null)
        {
            voiceSource.clip = step.voice;
            voiceSource.volume = step.voiceVolume * GameAudio.SfxVolume;
            voiceSource.Play();
        }

        // ambience: only changes when this step has one; otherwise the old one keeps looping
        if (step.ambience != null && ambienceSource.clip != step.ambience)
        {
            ambienceSource.clip = step.ambience;
            ambienceSource.Play();
        }
    }

    private void GoToNextScene()
    {
        // the Main Menu uses this flag to know the intro has been seen
        PlayerPrefs.SetInt("IntroSeen", 1);
        PlayerPrefs.Save();

        voiceSource.Stop();
        SceneManager.LoadScene(nextScene);
    }
}