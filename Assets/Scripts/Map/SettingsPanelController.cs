using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class SettingsPanelController : MonoBehaviour
{
    [Header("Volume")]
    public Slider musicSlider;
    public Slider sfxSlider;
    public TextMeshProUGUI musicText;
    public TextMeshProUGUI sfxText;
    public AudioClip sfxPreviewClip;       // optional, plays while you move the SFX slider

    [Header("Reset progress (optional, leave all empty in the battle scene)")]
    public Button resetButton;
    public GameObject confirmPopup;
    public Button confirmYesButton;
    public Button confirmNoButton;
    public string mainMenuScene = "MainMenu";

    [Header("Return to map (optional, Yes/No popup)")]
    public Button returnToMapButton;
    public GameObject returnToMapPopup;    // leave empty to skip the confirmation
    public Button returnToMapYesButton;
    public Button returnToMapNoButton;
    public string mapScene = "Map";        // <-- set this to your map scene's exact name

    [Header("Optional (Map scene only)")]
    public Button mainMenuButton;
    public GameObject debugRoot;           // the group holding the test buttons

    private float lastPreviewTime;

    void Awake()
    {
        SetupSlider(musicSlider);
        SetupSlider(sfxSlider);
        musicSlider.onValueChanged.AddListener(OnMusicChanged);
        sfxSlider.onValueChanged.AddListener(OnSfxChanged);

        if (resetButton != null && confirmPopup != null)
            resetButton.onClick.AddListener(() => confirmPopup.SetActive(true));
        if (confirmNoButton != null && confirmPopup != null)
            confirmNoButton.onClick.AddListener(() => confirmPopup.SetActive(false));
        if (confirmYesButton != null)
            confirmYesButton.onClick.AddListener(OnResetConfirmed);

        // Return to map: ask first if a popup is assigned, otherwise go straight there
        if (returnToMapButton != null)
        {
            returnToMapButton.onClick.AddListener(() =>
            {
                if (returnToMapPopup != null) returnToMapPopup.SetActive(true);
                else GoToMap();
            });
        }
        if (returnToMapNoButton != null && returnToMapPopup != null)
            returnToMapNoButton.onClick.AddListener(() => returnToMapPopup.SetActive(false));
        if (returnToMapYesButton != null)
            returnToMapYesButton.onClick.AddListener(GoToMap);

        if (mainMenuButton != null)
            mainMenuButton.onClick.AddListener(() => SceneManager.LoadScene(mainMenuScene));
    }

    // Runs every time the panel opens
    void OnEnable()
    {
        if (confirmPopup != null) confirmPopup.SetActive(false);
        if (returnToMapPopup != null) returnToMapPopup.SetActive(false);

        // show the saved values without triggering the change events
        musicSlider.SetValueWithoutNotify(GameAudio.MusicVolume);
        sfxSlider.SetValueWithoutNotify(GameAudio.SfxVolume);
        UpdateTexts();

        // test buttons only show in the Editor and Development Builds
        if (debugRoot != null) debugRoot.SetActive(Debug.isDebugBuild);
    }

    // Save when the panel closes
    void OnDisable()
    {
        GameAudio.Save();
    }

    private void SetupSlider(Slider s)
    {
        s.minValue = 0f;
        s.maxValue = 1f;
        s.wholeNumbers = false;
    }

    private void OnMusicChanged(float value)
    {
        GameAudio.MusicVolume = value;
        UpdateTexts();
    }

    private void OnSfxChanged(float value)
    {
        GameAudio.SfxVolume = value;
        UpdateTexts();

        if (sfxPreviewClip != null && Time.unscaledTime - lastPreviewTime > 0.2f)
        {
            lastPreviewTime = Time.unscaledTime;
            GameAudio.PlaySfx(sfxPreviewClip);
        }
    }

    private void OnResetConfirmed()
    {
        ProgressReset.ResetAll();
        SceneManager.LoadScene(mainMenuScene);
    }

    private void GoToMap()
    {
        GameAudio.Save();
        SceneManager.LoadScene(mapScene);
    }

    private void UpdateTexts()
    {
        if (musicText != null) musicText.text = $"Music  {Mathf.RoundToInt(musicSlider.value * 100)}%";
        if (sfxText != null) sfxText.text = $"Sound Effects  {Mathf.RoundToInt(sfxSlider.value * 100)}%";
    }
}