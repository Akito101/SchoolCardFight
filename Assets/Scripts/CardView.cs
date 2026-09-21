using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class CardView : MonoBehaviour
{
    public Image artImage;
    public TextMeshProUGUI nameText;
    public RectTransform playPosition;
    public Sprite cardBackSprite;

    private RectTransform rectTransform;
    private Vector2 originalPosition;
    private CardData currentCard;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    public void Setup(CardData data)
    {
        if (data == null)
        {
            gameObject.SetActive(false);
            return;
        }

        gameObject.SetActive(true);
        currentCard = data;
        rectTransform.anchoredPosition = originalPosition;
        rectTransform.localScale = Vector3.one; // reset scale in case a previous flip left it mid-shrink

        ShowFaceDown();
    }

    private void ShowFaceDown()
    {
        artImage.sprite = cardBackSprite;
        artImage.color = Color.white;
        nameText.text = "";
    }

    private void ShowFaceUp()
    {
        if (currentCard.cardArt != null)
        {
            artImage.sprite = currentCard.cardArt;
            artImage.color = Color.white;
        }
        else
        {
            artImage.sprite = null;
            artImage.color = new Color(0.7f, 0.7f, 0.9f);
        }

        nameText.text = $"{currentCard.cardName}\n({currentCard.category})";
    }

    public void SetOriginalPosition()
    {
        originalPosition = rectTransform.anchoredPosition;
    }

    public void PlayCurrentCard()
    {
        rectTransform.DOKill(); // stop any previous animation on this object

        Sequence seq = DOTween.Sequence();

        // Move to center while starting the flip
        seq.Append(rectTransform.DOAnchorPos(playPosition.anchoredPosition, 0.5f).SetEase(Ease.OutQuad));

        // Flip: shrink to edge-on
        seq.Join(rectTransform.DOScaleX(0f, 0.25f).SetEase(Ease.InQuad));

        // At the midpoint, swap the sprite to face-up
        seq.AppendCallback(() => ShowFaceUp());

        // Flip back to full width
        seq.Append(rectTransform.DOScaleX(1f, 0.25f).SetEase(Ease.OutQuad));

        // A little landing bounce for extra juice
        seq.Append(rectTransform.DOPunchScale(new Vector3(0.1f, 0.1f, 0f), 0.2f, 4, 0.5f));

        seq.OnComplete(() => Debug.Log("Card played and revealed!"));
    }
}