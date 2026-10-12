using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

[RequireComponent(typeof(RectTransform), typeof(Image))]
public class CardView : MonoBehaviour
{
    [SerializeField] private Sprite cardBackSprite;

    private Image cardView;
    private RectTransform currentCardTransform;
    private Vector2 playerDeckPosition;
    private CardData currentCard;

    void Awake()
    {
        cardView = GetComponent<Image>();
        currentCardTransform = GetComponent<RectTransform>();
    }

    public void SetOriginalPosition(Vector2 position)
    {
        playerDeckPosition = position;
        currentCardTransform.anchoredPosition = position;
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
        currentCardTransform.anchoredPosition = playerDeckPosition;
        currentCardTransform.localScale = Vector3.one; // reset scale in case a previous flip left it mid-shrink

        ShowFaceDown();
    }

    private void ShowFaceDown()
    {
        cardView.sprite = cardBackSprite;
    }

    private void ShowFaceUp()
    {
        if (currentCard.cardFrontSprite != null)
        {
            cardView.sprite = currentCard.cardFrontSprite;
        }
        else
        {
            cardView.sprite = null;
        }
    }

    public CardView CreateNextDrawCard()
    {
        CardView nextCard = Instantiate(this, transform.parent);
        nextCard.playerDeckPosition = playerDeckPosition;
        return nextCard;
    }

    public void DrawCardAnimation(Vector2 destination)
    {
        currentCardTransform.DOKill(); // stop any previous animation on this object

        Sequence seq = DOTween.Sequence();

        // Move to center while starting the flip
        seq.Append(currentCardTransform.DOAnchorPos(destination, 0.5f).SetEase(Ease.OutQuad));

        // Flip: shrink to edge-on
        seq.Join(currentCardTransform.DOScaleX(0f, 0.25f).SetEase(Ease.InQuad));

        // At the midpoint, swap the sprite to face-up
        seq.AppendCallback(() => ShowFaceUp());

        // Flip back to full width
        seq.Append(currentCardTransform.DOScaleX(1f, 0.25f).SetEase(Ease.OutQuad));

        // A little landing bounce for extra juice
        seq.Append(currentCardTransform.DOPunchScale(new Vector3(0.1f, 0.1f, 0f), 0.2f, 4, 0.5f));
    }
}