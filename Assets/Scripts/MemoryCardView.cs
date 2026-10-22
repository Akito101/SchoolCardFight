using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class MemoryCardView : MonoBehaviour
{
    public Image image;
    public TextMeshProUGUI nameText;
    public Button button;

    [HideInInspector] public int pairId;
    [HideInInspector] public bool isFaceUp;
    [HideInInspector] public bool isMatched;
    public System.Action<MemoryCardView> onClicked;

    private CardData data;
    private Sprite backSprite;
    private const float HalfFlip = 0.12f;

    void Awake()
    {
        button.onClick.AddListener(() => onClicked?.Invoke(this));
    }

    void OnDestroy()
    {
        transform.DOKill();
    }

    public void Setup(CardData cardData, int id, Sprite back)
    {
        data = cardData;
        pairId = id;
        backSprite = back;
        isFaceUp = false;
        isMatched = false;
        transform.localScale = Vector3.one;
        ShowBack();
    }

    public void FlipUp()
    {
        isFaceUp = true;
        Flip(ShowFace);
    }

    public void FlipDown()
    {
        isFaceUp = false;
        Flip(ShowBack);
    }

    public void SetMatched()
    {
        isMatched = true;
        image.color = new Color(0.85f, 1f, 0.85f);   // slight green tint
        transform.DOPunchScale(new Vector3(0.15f, 0.15f, 0f), 0.3f, 6, 0.5f);
    }

    // squash the card to a thin line, swap the picture, then open it again
    private void Flip(System.Action swap)
    {
        transform.DOKill();
        Sequence s = DOTween.Sequence();
        s.Append(transform.DOScaleX(0f, HalfFlip));
        s.AppendCallback(() => swap());
        s.Append(transform.DOScaleX(1f, HalfFlip));
    }

    private void ShowFace()
    {
        if (data.cardArt != null)
        {
            image.sprite = data.cardArt;
            image.color = Color.white;
            nameText.text = "";
        }
        else
        {
            image.sprite = null;
            image.color = new Color(0.7f, 0.7f, 0.9f);
            nameText.text = data.cardName;
        }
    }

    private void ShowBack()
    {
        if (backSprite != null)
        {
            image.sprite = backSprite;
            image.color = Color.white;
        }
        else
        {
            image.sprite = null;
            image.color = new Color(0.2f, 0.2f, 0.35f);
        }
        nameText.text = "";
    }
}