using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DeckView : MonoBehaviour
{
    public CardDatabase database;
    public MapManager map;                  // used to read the next level's requirement
    public CollectionSlotView slotPrefab;   // same prefab the book uses
    public Transform gridParent;
    public CardInspector inspector;

    public TextMeshProUGUI totalText;
    public TextMeshProUGUI categoryText;
    public TextMeshProUGUI requirementText;
    public TextMeshProUGUI pageText;
    public Button prevButton;
    public Button nextButton;
    public int cardsPerPage = 9;

    private List<CardData> owned = new List<CardData>();
    private int page = 0;

    void Awake()
    {
        prevButton.onClick.AddListener(() => ChangePage(-1));
        nextButton.onClick.AddListener(() => ChangePage(1));
    }

    // Runs every time the panel opens, so it's always up to date
    void OnEnable()
    {
        BuildList();
        page = 0;
        ShowPage();
    }

    private void BuildList()
    {
        owned.Clear();
        if (database == null)
        {
            Debug.LogError("DeckView: Database slot is empty!");
            return;
        }

        foreach (CardCount c in PlayerCollection.GetAll())
        {
            if (c.count <= 0) continue;
            CardData data = database.GetByName(c.cardName);
            if (data != null) owned.Add(data);
        }

        owned.Sort((a, b) =>
        {
            int byCategory = a.category.CompareTo(b.category);
            return byCategory != 0 ? byCategory : string.CompareOrdinal(a.cardName, b.cardName);
        });
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

        int totalPages = Mathf.Max(1, Mathf.CeilToInt(owned.Count / (float)cardsPerPage));
        page = Mathf.Clamp(page, 0, totalPages - 1);

        int start = page * cardsPerPage;
        int end = Mathf.Min(start + cardsPerPage, owned.Count);

        for (int i = start; i < end; i++)
        {
            CardData card = owned[i];
            CollectionSlotView slot = Instantiate(slotPrefab, gridParent);
            slot.Setup(card, true, PlayerCollection.GetCount(card.cardName));

            Button btn = slot.GetComponent<Button>();
            if (btn != null)
            {
                btn.onClick.AddListener(() =>
                    inspector.Show(card, PlayerCollection.GetCount(card.cardName)));
            }
        }

        UpdateSummary();

        pageText.text = $"{page + 1} / {totalPages}";
        prevButton.interactable = page > 0;
        nextButton.interactable = page < totalPages - 1;
    }

    private void UpdateSummary()
    {
        // total cards, and how many of each category
        int total = 0;
        Dictionary<CardCategory, int> perCategory = new Dictionary<CardCategory, int>();
        foreach (CardCategory cat in System.Enum.GetValues(typeof(CardCategory)))
            perCategory[cat] = 0;

        foreach (CardData card in owned)
        {
            int count = PlayerCollection.GetCount(card.cardName);
            total += count;
            perCategory[card.category] += count;
        }

        totalText.text = $"Cards: {total}";

        List<string> parts = new List<string>();
        foreach (var pair in perCategory)
            parts.Add($"{pair.Key} {pair.Value}");
        categoryText.text = string.Join("  |  ", parts);

        // what the next fight needs
        int cleared = PlayerProgress.ClearedLevels;
        if (map != null && cleared < map.levels.Length)
        {
            LevelNode next = map.levels[cleared];
            int missing = next.minCards - total;
            requirementText.text = missing <= 0
                ? $"Next fight: {next.enemyName} needs {next.minCards} cards (ready!)"
                : $"Next fight: {next.enemyName} needs {next.minCards} cards (need {missing} more)";
        }
        else
        {
            requirementText.text = "";
        }
    }
}