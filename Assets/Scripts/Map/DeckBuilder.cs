using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DeckBuilder : MonoBehaviour
{
    public CardDatabase database;
    public MapManager map;                  // used to read the next level's requirement
    public CollectionSlotView slotPrefab;   // same prefab the book uses
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
        addAllButton.onClick.AddListener(() => { PlayerDeck.AddAllToDeck(); Refresh(); });
        inspectButton.onClick.AddListener(() => { inspectMode = !inspectMode; Refresh(); });
    }

    // Runs every time the panel opens
    void OnEnable()
    {
        showingDeck = true;
        inspectMode = false;
        page = 0;
        Refresh();
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

        if (showingDeck) PlayerDeck.MoveToBench(card.cardName);
        else PlayerDeck.MoveToDeck(card.cardName);

        Refresh();
    }

    private void Refresh()
    {
        if (database == null)
        {
            Debug.LogError("DeckBuilder: Database slot is empty!");
            return;
        }

        // clear the grid
        foreach (Transform child in gridParent)
        {
            child.gameObject.SetActive(false);
            Destroy(child.gameObject);
        }

        // collect the cards for the current tab
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

        // pages
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

        // texts and buttons
        pageText.text = $"{page + 1} / {totalPages}";
        prevButton.interactable = page > 0;
        nextButton.interactable = page < totalPages - 1;

        deckTabButton.interactable = !showingDeck;          // grayed out = the tab you're on
        inventoryTabButton.interactable = showingDeck;
        inspectButtonText.text = inspectMode ? "Locked: ON" : "Locked: OFF";

        int inDeck = PlayerDeck.TotalInDeck();
        deckCountText.text = $"Deck: {inDeck} cards";
        UpdateRequirement(inDeck);
    }

    private void UpdateRequirement(int inDeck)
    {
        int cleared = PlayerProgress.LevelsCleared;
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