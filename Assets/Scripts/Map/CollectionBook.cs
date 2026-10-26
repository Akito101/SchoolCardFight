using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CollectionBook : MonoBehaviour
{
    public CardDatabase database;
    public CollectionSlotView slotPrefab;
    public Transform gridParent;
    public TextMeshProUGUI pageText;
    public TextMeshProUGUI progressText;
    public Button prevButton;
    public Button nextButton;
    public CardInspector inspector;
    public int cardsPerPage = 9;
    public bool includeArtifacts = false; // Artifacts aren't in packs, so leave this off

    private List<CardData> cards = new List<CardData>();
    private int page = 0;

    void Awake()
    {
        prevButton.onClick.AddListener(() => ChangePage(-1));
        nextButton.onClick.AddListener(() => ChangePage(1));
    }

    // Runs every time the panel is opened, so the book is always up to date
    void OnEnable()
    {
        BuildList();
        page = 0;
        ShowPage();
    }

    private void BuildList()
    {
        cards.Clear();
        if (database == null)
        {
            Debug.LogError("CollectionBook: Database slot is empty!");
            return;
        }

        foreach (CardData c in database.allCards)
        {
            if (c == null) continue;
            if (c.isArtifact && !includeArtifacts) continue;
            cards.Add(c);
        }
    }

    private void ChangePage(int direction)
    {
        page += direction;
        ShowPage();
    }

    private void ShowPage()
    {
        foreach (Transform child in gridParent)
        {
            child.gameObject.SetActive(false);
            Destroy(child.gameObject);
        }

        int totalPages = Mathf.Max(1, Mathf.CeilToInt(cards.Count / (float)cardsPerPage));
        page = Mathf.Clamp(page, 0, totalPages - 1);

        int start = page * cardsPerPage;
        int end = Mathf.Min(start + cardsPerPage, cards.Count);

        for (int i = start; i < end; i++)
        {
            CardData card = cards[i];
            bool discovered = PlayerCollection.IsDiscovered(card.cardName);

            CollectionSlotView slot = Instantiate(slotPrefab, gridParent);
            slot.Setup(card, discovered, PlayerCollection.GetCount(card.cardName));

            Button btn = slot.GetComponent<Button>();
            if (btn != null && discovered)
            {
                btn.onClick.AddListener(() =>
                    inspector.Show(card, PlayerCollection.GetCount(card.cardName)));
            }
        }

        int found = 0;
        foreach (CardData c in cards)
        {
            if (PlayerCollection.IsDiscovered(c.cardName)) found++;
        }

        pageText.text = $"{page + 1} / {totalPages}";
        progressText.text = $"Collected: {found} / {cards.Count}";
        prevButton.interactable = page > 0;
        nextButton.interactable = page < totalPages - 1;
    }
}