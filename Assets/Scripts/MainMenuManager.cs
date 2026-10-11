using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    public GameObject mainPanel;
    public GameObject optionsPanel;
    public GameObject creditsPanel;

    public string introScene = "VNIntro";
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
        panelToShow.SetActive(true);
    }

    public void OnPlayPressed()
    {
        // 1 means the player already finished their first battle (win or lose)
        if (PlayerPrefs.GetInt("FirstBattleDone", 0) == 1)
        {
            SceneManager.LoadScene(mapScene);
        }
        else
        {
            SceneManager.LoadScene(introScene);
        }
    }

    public void OnOptionsPressed() { ShowPanel(optionsPanel); }
    public void OnCreditsPressed() { ShowPanel(creditsPanel); }
    public void OnBackPressed() { ShowPanel(mainPanel); }

    public void OnResetProgressPressed()
    {
        PlayerCollection.Clear();
        PlayerPrefs.DeleteAll();
        Debug.Log("Progress reset");
    }
}