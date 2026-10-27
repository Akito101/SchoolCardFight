using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using DG.Tweening;

public class PackOpener : MonoBehaviour
{
    [Header("Data")]
    public CardDatabase database;
    public int starterPacks = 3;
    public int cardsPerPack = 5;
    public string afterStarterScene = "VNIntro";
    public string mapScene = "Map";

    [Header("Cards")]
    public PackCardView cardPrefab;
    public Sprite cardBackSprite;
    public Transform cardsParent;

    [Header("UI")]
    public Button packButton;
    public Button flipAllButton;
    public Button nextPackButton;
    public Button continueButton;
    public TextMeshProUGUI continueButtonText;
    public Button backButton;
    public TextMeshProUGUI packsLeftText;

    private bool firstTime;
    private int openedThisVisit = 0;
    private bool busy = false;
    private int flippedCount;
    private int totalCards;

    private CanvasGroup packGroup;
    private Vector3 packStartPos;
    private Vector3 packStartScale;
    private Quaternion packStartRot;
    private Transform packTransform;

    void Awake()
    {
        packGroup = packButton.GetComponent<CanvasGroup>();
        if (packGroup == null) packGroup = packButton.gameObject.AddComponent<CanvasGroup>();
        packTransform = packButton.transform;
        packStartPos = packButton.transform.localPosition;
        packStartScale = packButton.transform.localScale;
        packStartRot = packButton.transform.localRotation;

        flipAllButton.onClick.AddListener(FlipAll);
    }

    void Start()
    {
        firstTime = PlayerPrefs.GetInt("StarterPacksOpened", 0) == 0;

        // The first time ever, give the player their starter packs (only once)
        if (firstTime && PlayerPrefs.GetInt("StarterPacksGranted", 0) == 0)
        {
            PlayerPacks.Add(starterPacks);
            PlayerPrefs.SetInt("StarterPacksGranted", 1);
            PlayerPrefs.Save();
        }

        ShowIdle();
    }

    void OnDestroy()
    {
        // stops the floating tween without touching the (maybe already destroyed) button
        DOTween.Kill(packTransform);
    }

    // The pack floats and waits to be tapped
    private void ShowIdle()
    {
        busy = false;
        ClearCards();
        nextPackButton.gameObject.SetActive(false);
        flipAllButton.gameObject.SetActive(false);

        bool hasPacks = PlayerPacks.Count > 0;
        backButton.gameObject.SetActive(!firstTime && hasPacks);
        continueButton.gameObject.SetActive(!hasPacks);
        continueButtonText.text = firstTime ? "Continue" : "Back";

        Transform pack = packButton.transform;
        pack.DOKill();
        pack.localScale = packStartScale;
        pack.localRotation = packStartRot;
        pack.localPosition = packStartPos;
        packGroup.alpha = 1f;

        packButton.gameObject.SetActive(hasPacks);
        packButton.interactable = hasPacks;

        if (hasPacks)
        {
            pack.DOLocalMoveY(packStartPos.y + 20f, 1.2f)
                .SetLoops(-1, LoopType.Yoyo)
                .SetEase(Ease.InOutSine);
        }

        UpdateText();
    }

    // PackButton's OnClick
    public void OnPackPressed()
    {
        if (busy || PlayerPacks.Count <= 0) return;

        // the very first starter pack always has one card of each category
        List<CardData> pulled = PackRoller.Roll(database, cardsPerPack, firstTime && openedThisVisit == 0);
        if (pulled.Count == 0) return;

        PlayerPacks.Use();
        openedThisVisit++;

        // the cards are yours right away, so nothing is lost if the player leaves mid-animation
        foreach (CardData c in pulled) PlayerCollection.AddCard(c.cardName);

        UpdateText();
        StartCoroutine(RipAndReveal(pulled));
    }

    private IEnumerator RipAndReveal(List<CardData> pulled)
    {
        busy = true;
        packButton.interactable = false;
        backButton.gameObject.SetActive(false);

        Transform pack = packButton.transform;
        pack.DOKill();   // stop the floating

        // shake and swell, then burst and fade
        Sequence rip = DOTween.Sequence();
        rip.Append(pack.DOShakeRotation(0.7f, new Vector3(0f, 0f, 20f), 30, 90f));
        rip.Join(pack.DOScale(packStartScale * 1.15f, 0.7f).SetEase(Ease.InQuad));
        rip.Append(pack.DOScale(packStartScale * 1.5f, 0.2f).SetEase(Ease.OutQuad));
        rip.Join(packGroup.DOFade(0f, 0.2f));
        yield return rip.WaitForCompletion();

        packButton.gameObject.SetActive(false);

        // face-down cards pop out one by one
        ClearCards();
        flippedCount = 0;
        totalCards = pulled.Count;

        List<PackCardView> cards = new List<PackCardView>();
        for (int i = 0; i < pulled.Count; i++)
        {
            PackCardView card = Instantiate(cardPrefab, cardsParent);
            card.Setup(pulled[i], cardBackSprite);
            card.onFlipped = OnCardFlipped;
            card.transform.localScale = Vector3.zero;
            card.transform.DOScale(1f, 0.3f).SetDelay(i * 0.12f).SetEase(Ease.OutBack);
            cards.Add(card);
        }

        yield return new WaitForSeconds(pulled.Count * 0.12f + 0.4f);

        foreach (PackCardView card in cards) card.EnableTapping();
        flipAllButton.gameObject.SetActive(true);

        // wait until every card has been flipped
        yield return new WaitUntil(() => flippedCount >= totalCards);
        flipAllButton.gameObject.SetActive(false);
        yield return new WaitForSeconds(0.6f);

        ShowEndButtons();
        busy = false;
    }

    private void OnCardFlipped(PackCardView card)
    {
        flippedCount++;
    }

    private void FlipAll()
    {
        flipAllButton.gameObject.SetActive(false);
        StartCoroutine(FlipAllRoutine());
    }

    private IEnumerator FlipAllRoutine()
    {
        foreach (Transform child in cardsParent)
        {
            PackCardView card = child.GetComponent<PackCardView>();
            if (card != null && !card.IsFlipped)
            {
                card.Flip();
                yield return new WaitForSeconds(0.25f);
            }
        }
    }

    private void ShowEndButtons()
    {
        int left = PlayerPacks.Count;
        nextPackButton.gameObject.SetActive(left > 0);

        // on the first visit the player must open all starter packs
        bool showContinue = left == 0 || !firstTime;
        continueButton.gameObject.SetActive(showContinue);
        continueButtonText.text = firstTime ? "Continue" : "Done";
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
            PlayerPrefs.SetInt("StarterPacksOpened", 1);
            PlayerPrefs.Save();
            SceneManager.LoadScene(afterStarterScene);
        }
        else
        {
            SceneManager.LoadScene(mapScene);
        }
    }

    // BackButton's OnClick
    public void OnBackPressed()
    {
        SceneManager.LoadScene(mapScene);
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
        int left = PlayerPacks.Count;
        if (left > 0) packsLeftText.text = $"Packs left: {left}";
        else if (firstTime) packsLeftText.text = "All packs opened!";
        else packsLeftText.text = "No packs to open.\nBuy more in the shop!";
    }
}