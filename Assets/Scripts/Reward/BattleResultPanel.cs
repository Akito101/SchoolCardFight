using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class BattleResultPanel : MonoBehaviour
{
    [Header("AI result UI")]
    [SerializeField] private WinLoseUIController winLoseUIController;

    [Header("Stamp / level art (optional)")]
    public Image stampImage;
    public bool showStampOnLose = false;

    [Header("Coins")]
    public TextMeshProUGUI coinsAmountText;       // just the number, no row

    [Header("Cards row")]
    public GameObject cardsRow;
    public TextMeshProUGUI cardsLabelText;        // optional
    public TextMeshProUGUI moreCardsText;         // optional, "+3 more"
    public Transform cardsParent;
    public CollectionSlotView cardSlotPrefab;
    public Vector2 cardSlotSize = new Vector2(110f, 150f);
    public int maxCardsShown = 4;

    [Header("Enemy cards")]
    public bool oneCopyOfEach = false;

    [Header("Button")]
    public Button okayButton;

    [Header("Sounds (all optional)")]
    public AudioClip winSfx;
    public AudioClip loseSfx;
    public AudioClip stampSfx;

    private CanvasGroup rootGroup;
    private RectTransform stampRect;
    private Vector3 stampBaseScale;
    private Quaternion stampBaseRot;
    private Vector2 stampBasePos;

    private bool shown;
    private bool won;
    private BattleTestManager.AIType aiType = BattleTestManager.AIType.Teacher;
    private int coinsToGive;
    private ArtifactEffect artifactToUnlock = ArtifactEffect.None;   // still unlocked, just not displayed
    private readonly List<CardData> cardsToGive = new List<CardData>();
    private readonly List<CardData> cardsToLose = new List<CardData>();
    private readonly List<CardData> cardsToShow = new List<CardData>();

    void Awake()
    {
        if (winLoseUIController == null)
        {
            winLoseUIController = GetComponent<WinLoseUIController>();
        }

        if (winLoseUIController == null)
        {
            winLoseUIController = gameObject.AddComponent<WinLoseUIController>();
        }

        rootGroup = GetComponent<CanvasGroup>();
        if (rootGroup == null) rootGroup = gameObject.AddComponent<CanvasGroup>();

        if (stampImage != null)
        {
            stampRect = stampImage.rectTransform;
            stampBaseScale = stampRect.localScale;
            stampBaseRot = stampRect.localRotation;
            stampBasePos = stampRect.anchoredPosition;
        }

        if (okayButton != null) okayButton.onClick.AddListener(OnOkayPressed);
    }

    public void SetAIType(BattleTestManager.AIType type)
    {
        aiType = type;
    }

    public void ShowResult(bool playerWon, List<CardData> wonCards = null, List<CardData> lostCards = null)
    {
        gameObject.SetActive(true);
        if (shown) return;
        shown = true;
        won = playerWon;
        winLoseUIController?.ShowResult(aiType, playerWon);

        LevelRewards rewards = Resources.Load<LevelRewards>("LevelRewards");
        LevelRewards.Reward reward = rewards != null ? rewards.Get(PlayerProgress.LevelSelected) : null;
        if (reward == null) Debug.LogWarning("BattleResultPanel: no reward entry for this level. Check Resources/LevelRewards.");

        bool firstClear = PlayerProgress.LevelSelected == PlayerProgress.ClearedLevels
                          && PlayerProgress.ClearedLevels < PlayerProgress.TotalLevels;

        coinsToGive = 0;
        artifactToUnlock = ArtifactEffect.None;
        cardsToGive.Clear();
        cardsToLose.Clear();
        cardsToShow.Clear();

        if (reward != null)
        {
            if (won)
            {
                coinsToGive = reward.coins;
                if (firstClear && reward.artifact != ArtifactEffect.None &&
                    !PlayerProgress.GetOwnedArtifacts().Contains(reward.artifact.ToString()))
                {
                    artifactToUnlock = reward.artifact;
                }
            }
            else
            {
                coinsToGive = reward.coinsOnLose;
            }
        }

        if (won && wonCards != null)
        {
            HashSet<string> taken = new HashSet<string>();
            foreach (CardData c in wonCards)
            {
                if (c == null || c.isArtifact) continue;
                if (oneCopyOfEach && !taken.Add(c.cardName)) continue;
                cardsToGive.Add(c);
            }

            HashSet<string> listed = new HashSet<string>();
            foreach (CardData c in cardsToGive)
            {
                if (PlayerCollection.GetCount(c.cardName) > 0) continue;
                if (listed.Add(c.cardName)) cardsToShow.Add(c);
            }
        }

        if (!won && lostCards != null)
        {
            foreach (CardData c in lostCards)
            {
                if (c == null) continue;
                cardsToLose.Add(c);
                cardsToShow.Add(c);
            }
        }

        // reset what's on screen (all null-safe)
        if (stampImage != null)
        {
            if (reward != null && reward.stamp != null) stampImage.sprite = reward.stamp;
            stampImage.gameObject.SetActive(false);
        }
        if (coinsAmountText != null) coinsAmountText.gameObject.SetActive(false);
        if (cardsRow != null) cardsRow.SetActive(false);
        if (moreCardsText != null) moreCardsText.gameObject.SetActive(false);
        if (okayButton != null) okayButton.gameObject.SetActive(false);
        ClearCards();

        StartCoroutine(PlayIntro());
    }

    private IEnumerator PlayIntro()
    {
        rootGroup.alpha = 0f;
        rootGroup.DOFade(1f, 0.25f);
        GameAudio.PlaySfx(won ? winSfx : loseSfx);
        MusicManager.StopMusic();

        yield return new WaitForSeconds(0.5f);

        if (stampImage != null && (won || showStampOnLose))
        {
            Tween stamp = PlayStamp();
            yield return stamp.WaitForCompletion();
        }
        yield return new WaitForSeconds(0.2f);

        // coins: only the number
        if (coinsToGive > 0 && coinsAmountText != null)
        {
            Pop(coinsAmountText.gameObject);
            coinsAmountText.text = "+0";
            DOVirtual.Float(0f, coinsToGive, 0.7f, v => coinsAmountText.text = "+" + Mathf.RoundToInt(v))
                .SetEase(Ease.OutQuad);
            yield return new WaitForSeconds(0.8f);
        }

        // cards
        if (cardsToShow.Count > 0 && cardsRow != null)
        {
            if (cardsLabelText != null) cardsLabelText.text = won ? "New cards collected" : "Cards lost";
            Pop(cardsRow);
            yield return new WaitForSeconds(0.3f);
            yield return StartCoroutine(RevealCards());
        }

        if (okayButton != null)
        {
            okayButton.gameObject.SetActive(true);
            okayButton.transform.localScale = Vector3.zero;
            okayButton.transform.DOScale(1f, 0.3f).SetEase(Ease.OutBack);
        }
    }

    private Tween PlayStamp()
    {
        stampRect.DOKill();
        stampImage.gameObject.SetActive(true);
        stampImage.color = new Color(1f, 1f, 1f, 0f);
        stampRect.anchoredPosition = stampBasePos;
        stampRect.localScale = stampBaseScale * 3f;
        stampRect.localRotation = stampBaseRot * Quaternion.Euler(0f, 0f, 25f);

        Sequence s = DOTween.Sequence();
        s.Append(stampImage.DOFade(1f, 0.1f));
        s.Join(stampRect.DOScale(stampBaseScale, 0.18f).SetEase(Ease.InQuad));
        s.Join(stampRect.DOLocalRotateQuaternion(stampBaseRot, 0.18f).SetEase(Ease.InQuad));
        s.AppendCallback(() => GameAudio.PlaySfx(stampSfx));
        s.Append(stampRect.DOShakeAnchorPos(0.4f, 20f, 35, 90f));
        s.Join(stampRect.DOPunchScale(stampBaseScale * 0.15f, 0.4f, 8, 0.6f));
        return s;
    }

    private IEnumerator RevealCards()
    {
        if (cardSlotPrefab == null || cardsParent == null) yield break;

        int count = Mathf.Min(cardsToShow.Count, maxCardsShown);
        for (int i = 0; i < count; i++)
        {
            CardData card = cardsToShow[i];
            CollectionSlotView slot = Instantiate(cardSlotPrefab, cardsParent);
            slot.Setup(card, true, 1);
            if (slot.countText != null) slot.countText.text = "";
            slot.GetComponent<RectTransform>().sizeDelta = cardSlotSize;

            slot.transform.localScale = Vector3.zero;
            slot.transform.DOScale(1f, 0.3f).SetEase(Ease.OutBack);
            GameAudio.PlaySfx(card.signatureSfx);

            yield return new WaitForSeconds(0.2f);
        }

        int extra = cardsToShow.Count - count;
        if (extra > 0 && moreCardsText != null)
        {
            moreCardsText.text = $"And {extra} more...";
            Pop(moreCardsText.gameObject);
        }

        yield return new WaitForSeconds(0.2f);
    }

    private void Pop(GameObject target)
    {
        target.SetActive(true);
        target.transform.localScale = Vector3.zero;
        target.transform.DOScale(1f, 0.3f).SetEase(Ease.OutBack);
    }

    private void ClearCards()
    {
        if (cardsParent == null) return;
        foreach (Transform child in cardsParent)
        {
            child.gameObject.SetActive(false);
            Destroy(child.gameObject);
        }
    }

    private void OnOkayPressed()
    {
        okayButton.interactable = false;
        ApplyRewards();
        PlayerProgress.OnBattleFinished(won);
    }

    private void ApplyRewards()
    {
        if (coinsToGive > 0) PlayerProgress.AddCoins(coinsToGive);

        if (artifactToUnlock != ArtifactEffect.None)
        {
            List<string> owned = PlayerProgress.GetOwnedArtifacts();
            string id = artifactToUnlock.ToString();
            if (!owned.Contains(id))
            {
                owned.Add(id);
                PlayerProgress.SaveOwnedArtifacts(owned);
            }
        }

        foreach (CardData card in cardsToGive)
        {
            // 1. Add card count to the player's general card inventory
            PlayerCollection.AddCard(card.cardName);

            // 2. Automatically register and save the unique card to the Collection Book database!
            SaveNewCardToCollection(card.cardName);
        }

        foreach (CardData card in cardsToLose)
        {
            PlayerCollection.RemoveCard(card.cardName);
        }
    }

    public void SaveNewCardToCollection(string newCardName)
    {
        // 1. Get existing saved collection
        List<string> collection = PlayerProgress.GetCollectionBook();

        // 2. Add the card if it's not already in the collection book
        if (!collection.Contains(newCardName))
        {
            collection.Add(newCardName);

            // 3. Save the updated list to PlayerPrefs & flush IndexedDB for WebGL
            PlayerProgress.SaveCollectionBook(collection);
        }
    }
}