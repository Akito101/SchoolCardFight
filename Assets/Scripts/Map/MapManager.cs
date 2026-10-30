using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections.Generic;

public class MapManager : MonoBehaviour
{
    [Header("Levels (in order, tutorial first)")]
    public LevelNode[] levels;
    public string battleScene = "TestGamePlay";
    public string minigameScene = "Minigame"; // not built yet, handled safely

    // NEW
    [Header("Artifact choice + VN before battle")]
    public ArtifactChoicePanel artifactChoicePanel;
    public int firstLevelWithArtifactChoice = 1;   // 0 = tutorial (no choice), 1/2/3 = levels 2/3/4
    [Tooltip("Optional VN scene per level (same order as Levels). Leave an entry empty to go straight to battle.")]
    public string[] levelVNScenes;

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

        int cleared = PlayerProgress.ClearedLevels;
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
        // the deck size comes from the LevelRewards asset (Min Cards on the button is only a backup)
        LevelRewards rewards = Resources.Load<LevelRewards>("LevelRewards");
        LevelRewards.Reward reward = rewards != null ? rewards.Get(index) : null;
        int needed = reward != null ? reward.deckSize : levels[index].minCards;

        int inDeck = PlayerDeck.TotalInDeck();

        if (needed > 0 && inDeck < needed)
        {
            ShowMessage($"Your deck needs at least {needed} cards to fight {levels[index].enemyName}.\nYour deck has {inDeck}. Add cards in the Deck tab, or earn coins in the minigame and buy more packs!");
            return;
        }

        PlayerProgress.LevelSelected = index;

        List<ArtifactEffect> owned = GetOwnedArtifacts();

        // no panel for the tutorial, or if the player owns no artifact yet
        if (artifactChoicePanel == null || index < firstLevelWithArtifactChoice || owned.Count == 0)
        {
            PlayerArtifactChoice.Clear();
            SceneManager.LoadScene(GetSceneAfterChoice(index));
            return;
        }

        // one or more owned: always show the panel
        CloseAllPanels();
        artifactChoicePanel.Show(owned, effect =>
        {
            PlayerArtifactChoice.Set(effect);
            SceneManager.LoadScene(GetSceneAfterChoice(index));
        });
    }

    // the level's VN scene if one is set, otherwise straight to battle
    private string GetSceneAfterChoice(int index)
    {
        if (levelVNScenes != null && index < levelVNScenes.Length && !string.IsNullOrEmpty(levelVNScenes[index]))
            return levelVNScenes[index];

        return battleScene;
    }

    // artifacts earned from the first clear of every beaten level
    private List<ArtifactEffect> GetOwnedArtifacts()
    {
        List<ArtifactEffect> owned = new List<ArtifactEffect>();

        LevelRewards rewards = Resources.Load<LevelRewards>("LevelRewards");
        if (rewards == null) return owned;

        int cleared = PlayerProgress.ClearedLevels;
        for (int i = 0; i < cleared; i++)
        {
            LevelRewards.Reward r = rewards.Get(i);
            if (r == null || r.artifact == ArtifactEffect.None) continue;
            if (!owned.Contains(r.artifact)) owned.Add(r.artifact);
        }
        return owned;
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
        PlayerProgress.LevelSelected = PlayerProgress.ClearedLevels; // ensure the next level is selected
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
        if (artifactChoicePanel != null) artifactChoicePanel.gameObject.SetActive(false);   // NEW
    }

    private void ShowMessage(string text)
    {
        CloseAllPanels();
        messageText.text = text;
        messagePanel.SetActive(true);
    }
}