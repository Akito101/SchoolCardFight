using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class PackOpener : MonoBehaviour
{
    [Header("Data")]
    public CardDatabase database;
    public int totalPacks = 3;
    public int cardsPerPack = 5;
    public string nextScene = "TestGamePlay";

    [Header("UI")]
    public Button packButton;
    public Button nextPackButton;
    public Button continueButton;
    public TextMeshProUGUI packsLeftText;
    public Transform cardsParent;
    public CardSlotView cardSlotPrefab;

    private int packsOpened = 0;

    void Start()
    {
        if (PlayerPrefs.GetInt("StarterPacksOpened", 0) == 1)
        {
            SceneManager.LoadScene(nextScene);
            return;
        }

        packButton.gameObject.SetActive(true);
        nextPackButton.gameObject.SetActive(false);
        continueButton.gameObject.SetActive(false);
        UpdateText();
    }

    // PackButton's OnClick
    public void OnPackPressed()
    {
        if (packsOpened >= totalPacks) return;

        List<CardData> pulled = RollPack(packsOpened == 0);
        if (pulled.Count == 0) return; // something is wrong, the error shows in the Console

        ClearCards();
        packButton.gameObject.SetActive(false); // hide the pack so it can't cover the cards

        pulled.Sort((a, b) => a.category.CompareTo(b.category));

        for (int i = 0; i < pulled.Count; i++)
        {
            PlayerCollection.AddCard(pulled[i].cardName);
            CardSlotView slot = Instantiate(cardSlotPrefab, cardsParent);
            slot.Setup(pulled[i], i * 0.15f);
        }

        packsOpened++;
        UpdateText();

        if (packsOpened >= totalPacks)
        {
            PlayerPrefs.SetInt("StarterPacksOpened", 1);
            PlayerPrefs.Save();
        }

        StartCoroutine(ShowButtonAfterReveal(pulled.Count));
    }

    // NextPackButton's OnClick
    public void OnNextPackPressed()
    {
        ClearCards();
        nextPackButton.gameObject.SetActive(false);
        packButton.gameObject.SetActive(true);
    }

    // ContinueButton's OnClick
    public void OnContinuePressed()
    {
        SceneManager.LoadScene(nextScene);
    }

    private IEnumerator ShowButtonAfterReveal(int cardCount)
    {
        // wait until all cards have finished popping in
        yield return new WaitForSeconds(cardCount * 0.15f + 0.5f);

        if (packsOpened >= totalPacks) continueButton.gameObject.SetActive(true);
        else nextPackButton.gameObject.SetActive(true);
    }

    private void ClearCards()
    {
        foreach (Transform child in cardsParent)
        {
            child.gameObject.SetActive(false);
            Destroy(child.gameObject);
        }
    }

    private List<CardData> RollPack(bool guaranteeEachCategory)
    {
        List<CardData> result = new List<CardData>();

        if (database == null)
        {
            Debug.LogError("PackOpener: Database slot is empty! Drag the CardDatabase asset in.");
            return result;
        }

        List<CardData> pool = new List<CardData>();
        foreach (CardData c in database.allCards)
        {
            if (c == null) continue;
            if (!c.isArtifact) pool.Add(c);
        }

        if (pool.Count == 0)
        {
            Debug.LogError("PackOpener: no cards found in the database!");
            return result;
        }

        if (guaranteeEachCategory)
        {
            foreach (CardCategory cat in System.Enum.GetValues(typeof(CardCategory)))
            {
                List<CardData> ofCategory = pool.FindAll(c => c.category == cat);
                if (ofCategory.Count > 0 && result.Count < cardsPerPack)
                {
                    result.Add(ofCategory[Random.Range(0, ofCategory.Count)]);
                }
            }
        }

        while (result.Count < cardsPerPack && pool.Count > 0)
        {
            result.Add(pool[Random.Range(0, pool.Count)]);
        }

        return result;
    }

    private void UpdateText()
    {
        packsLeftText.text = $"Packs left: {totalPacks - packsOpened}";
    }
}