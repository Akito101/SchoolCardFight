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
    public int starterPacks = 3;
    public int cardsPerPack = 5;
    public string introScene = "VNIntro";
    public string battleScene = "TestGamePlay";
    public string mapScene = "Map";

    [Header("UI")]
    public Button packButton;
    public Button nextPackButton;
    public Button continueButton;
    public TextMeshProUGUI continueButtonText;
    public Button backButton;
    public TextMeshProUGUI packsLeftText;
    public Transform cardsParent;
    public CardSlotView cardSlotPrefab;

    private bool firstTime;
    private int openedThisVisit = 0;

    void Start()
    {
        firstTime = PlayerPrefs.GetInt("StarterPacksOpened", 0) == 0;

        // The first time ever, give the player their 3 starter packs (only once)
        if (firstTime && PlayerPrefs.GetInt("StarterPacksGranted", 0) == 0)
        {
            PlayerPacks.Add(starterPacks);
            PlayerPrefs.SetInt("StarterPacksGranted", 1);
            PlayerPrefs.Save();
        }

        ShowIdle();
    }

    // The pack is waiting to be ripped
    private void ShowIdle()
    {
        ClearCards();
        nextPackButton.gameObject.SetActive(false);

        bool hasPacks = PlayerPacks.Count > 0;
        packButton.gameObject.SetActive(hasPacks);
        backButton.gameObject.SetActive(!firstTime && hasPacks);

        continueButton.gameObject.SetActive(!hasPacks);
        continueButtonText.text = firstTime ? "Start Battle" : "Back";

        UpdateText();
    }

    // PackButton's OnClick
    public void OnPackPressed()
    {
        if (PlayerPacks.Count <= 0) return;

        // the very first starter pack always has one card of each category
        List<CardData> pulled = PackRoller.Roll(database, cardsPerPack, firstTime && openedThisVisit == 0);
        if (pulled.Count == 0) return;   // something is misconfigured, the error shows in the Console

        PlayerPacks.Use();
        openedThisVisit++;

        ClearCards();
        packButton.gameObject.SetActive(false);
        backButton.gameObject.SetActive(false);

        pulled.Sort((a, b) => a.category.CompareTo(b.category));
        for (int i = 0; i < pulled.Count; i++)
        {
            PlayerCollection.AddCard(pulled[i].cardName);
            CardSlotView slot = Instantiate(cardSlotPrefab, cardsParent);
            slot.Setup(pulled[i], i * 0.15f);
        }

        UpdateText();
        StartCoroutine(ShowButtonsAfterReveal(pulled.Count));
    }

    // NextPackButton's OnClick
    public void OnNextPackPressed()
    {
        ShowIdle();
    }

    // ContinueButton's OnClick
    public void OnContinuePressed()
    {
        if (firstTime)
        {
            // Mark the starter pack sequence as completed.
            PlayerPrefs.SetInt("StarterPacksOpened", 1);
            PlayerPrefs.Save();

            // Play the VN intro before the first battle.
            SceneManager.LoadScene(introScene);
        }
        else
        {
            // Shop packs return to the map.
            SceneManager.LoadScene(mapScene);
        }
    }

    // BackButton's OnClick (only shown for packs bought in the shop)
    public void OnBackPressed()
    {
        SceneManager.LoadScene(mapScene);
    }

    private IEnumerator ShowButtonsAfterReveal(int cardCount)
    {
        // wait until the last card has finished popping in
        yield return new WaitForSeconds(cardCount * 0.15f + 0.5f);

        int left = PlayerPacks.Count;
        nextPackButton.gameObject.SetActive(left > 0);

        // On the first visit the player must open all starter packs before the battle
        bool showContinue = left == 0 || !firstTime;
        continueButton.gameObject.SetActive(showContinue);
        continueButtonText.text = firstTime ? "Start Battle" : "Done";
    }

    private void ClearCards()
    {
        foreach (Transform child in cardsParent)
        {
            child.gameObject.SetActive(false);
            Destroy(child.gameObject);
        }
    }

    private void UpdateText()
    {
        packsLeftText.text = $"Packs left: {PlayerPacks.Count}";
    }
}