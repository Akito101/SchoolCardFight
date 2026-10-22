using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class MemoryMatchGame : MonoBehaviour
{
    [Header("Rules")]
    public int pairs = 6;                 // 6 pairs = 12 cards, fits a 3 x 4 grid
    public int baseReward = 25;
    public int mistakeBudget = 6;         // mistakes below this earn the bonus
    public int bonusPerSavedMistake = 0;  // 0 = no bonus. Try 8 for up to about 73 coins.
    public float mismatchShowTime = 0.9f;
    public string mapScene = "Map";

    [Header("Data")]
    public CardDatabase database;
    public Sprite cardBackSprite;
    public MemoryCardView cardPrefab;

    [Header("UI")]
    public Transform gridParent;
    public TextMeshProUGUI statusText;
    public Button leaveButton;
    public GameObject endPanel;
    public TextMeshProUGUI endText;
    public Button playAgainButton;
    public Button backToMapButton;

    private MemoryCardView first;
    private bool busy;
    private bool ended;
    private int pairCount;
    private int matchedPairs;
    private int mistakes;

    void Awake()
    {
        leaveButton.onClick.AddListener(GoToMap);
        backToMapButton.onClick.AddListener(GoToMap);
        playAgainButton.onClick.AddListener(StartGame);
    }

    void Start()
    {
        StartGame();
    }

    private void StartGame()
    {
        StopAllCoroutines();
        first = null;
        busy = false;
        ended = false;
        matchedPairs = 0;
        mistakes = 0;
        endPanel.SetActive(false);

        foreach (Transform child in gridParent)
        {
            child.gameObject.SetActive(false);
            Destroy(child.gameObject);
        }

        if (database == null)
        {
            Debug.LogError("MemoryMatchGame: Database slot is empty!");
            return;
        }

        // pick distinct cards for the pairs
        List<CardData> pool = new List<CardData>();
        foreach (CardData c in database.allCards)
        {
            if (c != null && !c.isArtifact) pool.Add(c);
        }
        Shuffle(pool);

        pairCount = Mathf.Min(pairs, pool.Count);
        if (pairCount < pairs)
            Debug.LogWarning($"MemoryMatchGame: only {pool.Count} cards in the database, using {pairCount} pairs.");
        if (pairCount == 0)
        {
            Debug.LogError("MemoryMatchGame: no cards in the database!");
            return;
        }

        // two of each pair id, shuffled
        List<int> order = new List<int>();
        for (int i = 0; i < pairCount; i++)
        {
            order.Add(i);
            order.Add(i);
        }
        Shuffle(order);

        foreach (int id in order)
        {
            MemoryCardView card = Instantiate(cardPrefab, gridParent);
            card.Setup(pool[id], id, cardBackSprite);
            card.onClicked = OnCardClicked;
        }

        UpdateStatus();
    }

    private void OnCardClicked(MemoryCardView card)
    {
        if (busy || ended) return;
        if (card.isFaceUp || card.isMatched) return;

        card.FlipUp();

        if (first == null)
        {
            first = card;
            return;
        }

        MemoryMatchGame_Resolve(first, card);
        first = null;
    }

    private void MemoryMatchGame_Resolve(MemoryCardView a, MemoryCardView b)
    {
        StartCoroutine(ResolvePair(a, b));
    }

    private IEnumerator ResolvePair(MemoryCardView a, MemoryCardView b)
    {
        busy = true;
        yield return new WaitForSeconds(0.35f);   // let the flip finish

        if (a.pairId == b.pairId)
        {
            a.SetMatched();
            b.SetMatched();
            matchedPairs++;
            UpdateStatus();
            yield return new WaitForSeconds(0.4f);

            if (matchedPairs >= pairCount)
            {
                EndGame();
                yield break;
            }
        }
        else
        {
            mistakes++;
            UpdateStatus();
            yield return new WaitForSeconds(mismatchShowTime);
            a.FlipDown();
            b.FlipDown();
            yield return new WaitForSeconds(0.3f);
        }

        busy = false;
    }

    private void EndGame()
    {
        ended = true;

        int saved = Mathf.Max(0, mistakeBudget - mistakes);
        int reward = baseReward + saved * bonusPerSavedMistake;
        PlayerProgress.AddCoins(reward);

        endText.text = bonusPerSavedMistake > 0
            ? $"Cleared!\nMistakes: {mistakes}\n+{reward} coins"
            : $"Cleared!\n+{reward} coins";
        endPanel.SetActive(true);
    }

    private void UpdateStatus()
    {
        statusText.text = $"Pairs: {matchedPairs} / {pairCount}      Mistakes: {mistakes}";
    }

    private void Shuffle<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    private void GoToMap()
    {
        SceneManager.LoadScene(mapScene);
    }
}