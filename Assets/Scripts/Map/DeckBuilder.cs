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
        TrimToLimit();   // fixes decks saved before the limit existed
        Refresh();
    }

    // The deck size limit = the cards the next fight needs
    private int MaxDeckSize()
    {
        if (map == null || map.levels == null || map.levels.Length == 0) return int.MaxValue;
        int index = Mathf.Min(PlayerProgress.ClearedLevels, map.levels.Length - 1);   // all cleared: use the last level
        return map.levels[index].minCards;
    }

    private void AddUpToLimit()
    {
        int max = MaxDeckSize();
        foreach (CardCount c in new List<CardCount>(PlayerCollection.GetAll()))
        {
            int benched = PlayerDeck.GetBenched(c.cardName);
            for (int i = 0; i < benched; i++)
            {
                if (PlayerDeck.TotalInDeck() >= max) return;
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
                return;   // no Refresh, so the message stays visible
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

        List<KeyValuePair<CardData, int>> entries = new List<KeyValuePair<CardData, int>>();
        foreach (CardCount c in PlayerCollection.GetAll())
        {
            CardData card = database.GetByName(c.cardName);
            if (card == null) continue;

            int n = showingDeck ? PlayerDeck.GetInDeck(c.cardName) : PlayerDeck.GetBenched(c.cardName);
            if (n > 0) entries.Add(new KeyValuePair<CardData, int>(card, n));
        }

        entries.Sort((a, b) =>
        {
            int byCategory = a.Key.category.CompareTo(b.Key.category);
            return byCategory != 0 ? byCategory : string.CompareOrdinal(a.Key.cardName, b.Key.cardName);
        });

        int totalPages = Mathf.Max(1, Mathf.CeilToInt(entries.Count / (float)cardsPerPage));
        page = Mathf.Clamp(page, 0, totalPages - 1);

        int start = page * cardsPerPage;
        int end = Mathf.Min(start + cardsPerPage, entries.Count);

        for (int i = start; i < end; i++)
        {
            CardData card = entries[i].Key;
            CollectionSlotView slot = Instantiate(slotPrefab, gridParent);
            slot.Setup(card, true, entries[i].Value);

            Button btn = slot.GetComponent<Button>();
            if (btn != null) btn.onClick.AddListener(() => OnCardTapped(card));
        }

        pageText.text = $"{page + 1} / {totalPages}";
        prevButton.interactable = page > 0;
        nextButton.interactable = page < totalPages - 1;

        deckTabButton.interactable = !showingDeck;
        inventoryTabButton.interactable = showingDeck;
        inspectButtonText.text = inspectMode ? "Locked: ON" : "Locked: OFF";

        int inDeck = PlayerDeck.TotalInDeck();
        int max = MaxDeckSize();
        deckCountText.text = max == int.MaxValue ? $"Deck: {inDeck} cards" : $"Deck: {inDeck} / {max} cards";
        addAllButton.interactable = inDeck < max;   // grayed out when the deck is full
        UpdateRequirement(inDeck);
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
        int missing = next.minCards - inDeck;
        requirementText.text = missing <= 0
            ? $"Next fight: {next.enemyName} needs {next.minCards} cards (ready!)"
            : $"Next fight: {next.enemyName} needs {next.minCards} cards ({missing} more needed)";
    }
}