using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    public GameObject mainPanel;
    public GameObject optionsPanel;
    public GameObject creditsPanel;
    public GameObject confirmResetPopup;

    public string packScene = "PackOpening";
    public string introScene = "VNIntro";
    public string battleScene = "TestGamePlay";
    public string mapScene = "Map";

    void Start()
    {
        ShowPanel(mainPanel);
    }

    private void ShowPanel(GameObject panelToShow)
    {
        mainPanel.SetActive(false);
        optionsPanel.SetActive(false);
        creditsPanel.SetActive(false);
        if (confirmResetPopup != null) confirmResetPopup.SetActive(false);

        panelToShow.SetActive(true);
    }

    // Sends the player to the right step: packs, then intro, then the first battle, then the Map
    public void OnPlayPressed()
    {
        string target;

        if (PlayerPrefs.GetInt("FirstBattleDone", 0) == 1) target = mapScene;
        else if (PlayerPrefs.GetInt("StarterPacksOpened", 0) == 0) target = packScene;
        else if (PlayerPrefs.GetInt("IntroSeen", 0) == 0) target = introScene;
        else target = battleScene;

        SceneManager.LoadScene(target);
    }

    public void OnOptionsPressed() { ShowPanel(optionsPanel); }
    public void OnCreditsPressed() { ShowPanel(creditsPanel); }
    public void OnBackPressed() { ShowPanel(mainPanel); }

    // The Reset button only ASKS now
    public void OnResetProgressPressed()
    {
        confirmResetPopup.SetActive(true);
    }

    // The popup's "Reset" button does the actual reset
    public void OnConfirmResetPressed()
    {
        ProgressReset.ResetAll();
        confirmResetPopup.SetActive(false);
        Debug.Log("Progress reset");
    }

    public void OnCancelResetPressed()
    {
        confirmResetPopup.SetActive(false);
    }
}