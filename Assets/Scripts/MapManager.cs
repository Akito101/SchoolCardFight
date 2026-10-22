using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class MapManager : MonoBehaviour
{
    [Header("Levels (in order, tutorial first)")]
    public LevelNode[] levels;
    public string battleScene = "TestGamePlay";
    public string minigameScene = "Minigame"; // not built yet, handled safely

    [Header("UI")]
    public TextMeshProUGUI coinsText;
    public GameObject collectionPanel;
    public GameObject deckPanel;
    public GameObject shopPanel;
    public GameObject settingsPanel;
    public GameObject minigamePanel;
    public GameObject messagePanel;
    public TextMeshProUGUI messageText;

    void Start()
    {
        for (int i = 0; i < levels.Length; i++)
        {
            int index = i; // copy so each button remembers its own number
            levels[i].button.onClick.AddListener(() => OnLevelPressed(index));
        }

        CloseAllPanels();
        RefreshAll();
    }

    public void RefreshAll()
    {
        coinsText.text = PlayerProgress.Coins.ToString();

        int cleared = PlayerProgress.LevelsCleared;
        for (int i = 0; i < levels.Length; i++)
        {
            bool unlocked = i <= cleared;
            bool beaten = i < cleared;

            levels[i].button.interactable = unlocked;
            if (levels[i].label != null) levels[i].label.text = unlocked ? levels[i].enemyName : "???";
            if (levels[i].lockIcon != null) levels[i].lockIcon.SetActive(!unlocked);
            if (levels[i].clearedMark != null) levels[i].clearedMark.SetActive(beaten);
        }
    }

    private void OnLevelPressed(int index)
    {
        int inDeck = PlayerDeck.TotalInDeck();
        int needed = levels[index].minCards;

        if (inDeck < needed)
        {
            ShowMessage($"Your deck needs at least {needed} cards to fight {levels[index].enemyName}.\nYour deck has {inDeck}. Add cards in the Deck tab, or earn coins in the minigame and buy more packs!");
            return;
        }

        PlayerProgress.SelectedLevel = index;
        SceneManager.LoadScene(battleScene);
    }

    private int TotalOwnedCards()
    {
        int total = 0;
        foreach (CardCount c in PlayerCollection.GetAll()) total += c.count;
        return total;
    }

    // ---- Buttons ----
    public void OnCollectionPressed() { OpenPanel(collectionPanel); }
    public void OnDeckPressed() { OpenPanel(deckPanel); }
    public void OnShopPressed() { OpenPanel(shopPanel); }
    public void OnSettingsPressed() { OpenPanel(settingsPanel); }
    public void OnClosePanel() { CloseAllPanels(); RefreshAll(); }

    public void OnPlusPressed()
    {
        ShowMessage("Need more coins?\nPlay the minigame to earn some!");
    }

    public void OnMinigamePressed()
    {
        OpenPanel(minigamePanel);
    }

    public void OnMainMenuPressed() { SceneManager.LoadScene("MainMenu"); }

    // ---- Test buttons (remove before the final build) ----
    public void OnDebugCoinsPressed()
    {
        PlayerProgress.AddCoins(100);
        RefreshAll();
    }

    public void OnDebugWinPressed()
    {
        PlayerProgress.SelectedLevel = PlayerProgress.LevelsCleared;
        PlayerProgress.OnBattleFinished(true);
    }

    // ---- Helpers ----
    private void OpenPanel(GameObject panel)
    {
        CloseAllPanels();
        panel.SetActive(true);
    }

    private void CloseAllPanels()
    {
        collectionPanel.SetActive(false);
        deckPanel.SetActive(false);
        shopPanel.SetActive(false);
        settingsPanel.SetActive(false);
        messagePanel.SetActive(false);
        minigamePanel.SetActive(false);
    }

    private void ShowMessage(string text)
    {
        CloseAllPanels();
        messageText.text = text;
        messagePanel.SetActive(true);
    }
}