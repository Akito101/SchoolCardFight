using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using DG.Tweening;

public class ShopManager : MonoBehaviour
{
    [Header("Pack settings")]
    public int packPrice = 100;
    public string packOpeningScene = "PackOpening";

    [Header("UI")]
    public TextMeshProUGUI coinsText;
    public TextMeshProUGUI messageText;
    public Button buyButton;
    public TextMeshProUGUI buyButtonText;
    public Button ownedPacksButton;
    public TextMeshProUGUI ownedPacksText;

    [Header("Confirm popup")]
    public GameObject confirmPopup;
    public TextMeshProUGUI confirmText;
    public Button confirmYesButton;
    public Button confirmNoButton;

    void Awake()
    {
        buyButton.onClick.AddListener(OnBuyPressed);
        ownedPacksButton.onClick.AddListener(OnOwnedPacksPressed);
        confirmYesButton.onClick.AddListener(OnConfirmYes);
        confirmNoButton.onClick.AddListener(() => confirmPopup.SetActive(false));
    }

    // Runs every time the panel opens
    void OnEnable()
    {
        messageText.text = "";
        confirmPopup.SetActive(false);
        Refresh();
    }

    private void Refresh()
    {
        coinsText.text = $"Coins: {PlayerProgress.Coins}";
        buyButtonText.text = $"Buy Pack\n{packPrice} coins";
        ownedPacksText.text = $"Your Packs\nx{PlayerPacks.Count}\n(tap to open)";
    }

    private void OnBuyPressed()
    {
        if (PlayerProgress.Coins < packPrice)
        {
            messageText.text = "Not enough coins! Play the minigame to earn more.";
            Punch(buyButton.transform);
            return;
        }

        PlayerProgress.SpendCoins(packPrice);
        PlayerPacks.Add(1);

        messageText.text = "Pack bought! Tap \"Your Packs\" to open it.";
        Refresh();
        Punch(ownedPacksButton.transform);
    }

    private void OnOwnedPacksPressed()
    {
        if (PlayerPacks.Count <= 0)
        {
            messageText.text = "You don't have any packs yet. Buy one first!";
            Punch(ownedPacksButton.transform);
            return;
        }

        confirmText.text = $"Open your packs now?\nYou have {PlayerPacks.Count}.";
        confirmPopup.SetActive(true);
    }

    private void OnConfirmYes()
    {
        SceneManager.LoadScene(packOpeningScene);
    }

    private void Punch(Transform t)
    {
        t.DOComplete();
        t.DOPunchScale(new Vector3(0.12f, 0.12f, 0f), 0.3f, 6, 0.5f);
    }
}