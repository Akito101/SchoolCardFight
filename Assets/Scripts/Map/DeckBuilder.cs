using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DeckBuilder : MonoBehaviour
{
    public CardDatabase database;
    public MapManager map;
    public CollectionSlotView slotPrefab;
    public Transform gridParent;
    public CardInspector inspector;

    public TextMeshProUGUI deckCountText;
    public TextMeshProUGUI requirementText;
    public TextMeshProUGUI pageText;
    public TextMeshProUGUI inspectButtonText;

    public Button prevButton;
    public Button nextButton;
    public Button deckTabButton;
    public Button inventoryTabButton;
    public Button addAllButton;
    public Button inspectButton;

    public int cardsPerPage = 9;

    private bool showingDeck = true;
    private bool inspectMode = false;
    private int page = 0;

    void Awake()
    {
        prevButton.onClick.AddListener(() => { page--; Refresh(); });
        nextButton.onClick.AddListener(() => { page++; Refresh(); });
        deckTabButton.onClick.AddListener(() => SetTab(true));
        inventoryTabButton.onClick.AddListener(() => SetTab(false));
        addAllButton.onClick.AddListener(() => { AddUpToLimit(); Refresh(); });
        inspectButton.onClick.AddListener(() => { inspectMode = !inspectMode; Refresh(); });
    }

    void OnEnable()
    {
        showingDeck = true;
        inspectMode = false;
        page = 0;
        TrimToLimit();
        Refresh();
    }

    // The deck size limit = the cards the next fight needs
    private int MaxDeckSize()
    {
        if (map == null || map.levels == null || map.levels.Length == 0)
            return int.MaxValue;

        int index = Mathf.Min(PlayerProgress.ClearedLevels, map.levels.Length - 1);
        int size = NeededFor(index, map.levels[index].minCards);

        return size > 0 ? size : int.MaxValue;
    }

    private void AddUpToLimit()
    {
        int max = MaxDeckSize();

        foreach (CardCount c in new List<CardCount>(PlayerCollection.GetAll()))
        {
            int benched = PlayerDeck.GetBenched(c.cardName);

            for (int i = 0; i < benched; i++)
            {
                if (PlayerDeck.TotalInDeck() >= max)
                    return;

                PlayerDeck.MoveToDeck(c.cardName);
            }
        }
    }

    private void TrimToLimit()
    {
        DeckLimit.TrimTo(MaxDeckSize(), database);
    }

    private void SetTab(bool deckTab)
    {
        showingDeck = deckTab;
        page = 0;
        Refresh();
    }

    private void OnCardTapped(CardData card)
    {
        if (inspectMode)
        {
            inspector.Show(card, PlayerCollection.GetCount(card.cardName));
            return;
        }

        if (showingDeck)
        {
            PlayerDeck.MoveToBench(card.cardName);
        }
        else
        {
            if (PlayerDeck.TotalInDeck() >= MaxDeckSize())
            {
                requirementText.text = $"Deck is full ({MaxDeckSize()} cards max). Remove a card first.";
                return;
            }

            PlayerDeck.MoveToDeck(card.cardName);
        }

        Refresh();
    }

    private void Refresh()
    {
        if (database == null)
        {
            Debug.LogError("DeckBuilder: Database slot is empty!");
            return;
        }

        foreach (Transform child in gridParent)
        {
            child.gameObject.SetActive(false);
            Destroy(child.gameObject);
        }

        List<KeyValuePair<CardData, int>> entries =
            new List<KeyValuePair<CardData, int>>();

        foreach (CardCount c in PlayerCollection.GetAll())
        {
            CardData card = database.GetByName(c.cardName);

            if (card == null)
                continue;

            int n = showingDeck
                ? PlayerDeck.GetInDeck(c.cardName)
                : PlayerDeck.GetBenched(c.cardName);

            if (n > 0)
            {
                entries.Add(new KeyValuePair<CardData, int>(card, n));
            }
        }

        entries.Sort((a, b) =>
        {
            int byCategory = a.Key.category.CompareTo(b.Key.category);

            return byCategory != 0
                ? byCategory
                : string.CompareOrdinal(a.Key.cardName, b.Key.cardName);
        });

        int totalPages = Mathf.Max(
            1,
            Mathf.CeilToInt(entries.Count / (float)cardsPerPage)
        );

        page = Mathf.Clamp(page, 0, totalPages - 1);

        int start = page * cardsPerPage;
        int end = Mathf.Min(start + cardsPerPage, entries.Count);

        for (int i = start; i < end; i++)
        {
            CardData card = entries[i].Key;

            CollectionSlotView slot = Instantiate(slotPrefab, gridParent);
            slot.opensInspector = false;   // this screen decides what a tap does (move the card or inspect it)
            slot.Setup(card, true, entries[i].Value);

            Button btn = slot.GetComponent<Button>();

            if (btn != null)
            {
                btn.onClick.AddListener(() => OnCardTapped(card));
            }
        }

        pageText.text = $"{page + 1} / {totalPages}";

        prevButton.interactable = page > 0;
        nextButton.interactable = page < totalPages - 1;

        deckTabButton.interactable = !showingDeck;
        inventoryTabButton.interactable = showingDeck;

        inspectButtonText.text = inspectMode
            ? "Inspect: ON"
            : "Inspect: OFF";

        int inDeck = PlayerDeck.TotalInDeck();
        int max = MaxDeckSize();

        if (showingDeck)
        {
            // Deck tab: display the current deck size.
            deckCountText.text = max == int.MaxValue
                ? $"Deck: {inDeck} cards"
                : $"Deck: {inDeck} / {max} cards";
        }
        else
        {
            // Inventory tab: count all cards outside the deck.
            int remainingCards = 0;

            foreach (CardCount c in PlayerCollection.GetAll())
            {
                remainingCards += PlayerDeck.GetBenched(c.cardName);
            }

            deckCountText.text = $"Inventory: {remainingCards} cards";
        }

        addAllButton.interactable = inDeck < max;

        UpdateRequirement(inDeck);
    }

    // The deck size for a level, read from the LevelRewards asset (0 = no limit).
    private int NeededFor(int levelIndex, int fallback)
    {
        LevelRewards rewards = Resources.Load<LevelRewards>("LevelRewards");

        LevelRewards.Reward reward = rewards != null
            ? rewards.Get(levelIndex)
            : null;

        return reward != null ? reward.deckSize : fallback;
    }

    private void UpdateRequirement(int inDeck)
    {
        int cleared = PlayerProgress.ClearedLevels;

        if (map == null || cleared >= map.levels.Length)
        {
            requirementText.text = "";
            return;
        }

        LevelNode next = map.levels[cleared];
        int needed = NeededFor(cleared, next.minCards);

        if (needed <= 0)
        {
            requirementText.text =
                $"Next fight: {next.enemyName} (no deck limit)";

            return;
        }

        int missing = needed - inDeck;

        requirementText.text = missing <= 0
            ? $"Next fight: {next.enemyName} needs {needed} cards (ready!)"
            : $"Next fight: {next.enemyName} needs {needed} cards ({missing} more needed)";
    }
}