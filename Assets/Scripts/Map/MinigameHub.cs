using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class MinigameHub : MonoBehaviour
{
    public string memoryMatchScene = "MemoryMatch";
    public string rpsScene = "RockPaperScissors";
    public TextMeshProUGUI messageText;

    void OnEnable()
    {
        messageText.text = "";
    }

    public void OnMemoryMatchPressed() { Open(memoryMatchScene); }
    public void OnRpsPressed() { Open(rpsScene); }

    private void Open(string sceneName)
    {
        if (Application.CanStreamedLevelBeLoaded(sceneName))
            SceneManager.LoadScene(sceneName);
        else
            messageText.text = "Coming soon!";
    }
}