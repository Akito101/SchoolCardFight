using UnityEngine;
using UnityEngine.SceneManagement;

public class BattleSettingsPanel : MonoBehaviour
{
    [Header("Panel")]
    public GameObject settingsPanel;
    public bool pauseWhileOpen = true;   // freezes the fight while the panel is open

    [Header("Scene")]
    public string mainMenuScene = "MainMenu";

    private bool pausedByMe;
    private float savedTimeScale = 1f;

    void Start()
    {
        if (settingsPanel != null) settingsPanel.SetActive(false);
    }

    // The Settings button's OnClick
    public void OpenSettings()
    {
        if (settingsPanel == null) return;

        settingsPanel.SetActive(true);
        settingsPanel.transform.SetAsLastSibling();   // draws on top of the rest of the battle UI

        if (pauseWhileOpen && !pausedByMe)
        {
            savedTimeScale = Time.timeScale;
            Time.timeScale = 0f;
            pausedByMe = true;
        }
    }

    // The panel's Close button's OnClick
    public void CloseSettings()
    {
        if (settingsPanel != null) settingsPanel.SetActive(false);
        ReleasePause();
    }

    // Optional: a Main Menu button inside the panel
    public void OnMainMenuPressed()
    {
        ReleasePause();
        SceneManager.LoadScene(mainMenuScene);
    }

    void OnDisable()
    {
        ReleasePause();
    }

    void OnDestroy()
    {
        ReleasePause();   // never carry a frozen game into the next scene
    }

    private void ReleasePause()
    {
        if (!pausedByMe) return;
        Time.timeScale = savedTimeScale;
        pausedByMe = false;
    }
}