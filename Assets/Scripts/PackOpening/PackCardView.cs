using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class PackCardView : MonoBehaviour
{
    public Image artImage;               // the Art child
    public Button button;                // the Button on the root
    public TextMeshProUGUI nameText;     // optional
    public GameObject glow;              // optional, shown on 3-star cards

    public bool IsFlipped { get; private set; }
    public System.Action<PackCardView> onFlipped;

    private CardData data;
    private Sprite backSprite;

    void Awake()
    {
        button.onClick.AddListener(Flip);
    }

    public void Setup(CardData card, Sprite back)
    {
        data = card;
        backSprite = back;
        IsFlipped = false;
        transform.localScale = Vector3.one;
        button.interactable = false;   // the pack opener turns tapping on once the cards have popped in
        if (glow != null) glow.SetActive(false);
        if (nameText != null) nameText.text = "";
        ShowBack();
    }

    public void EnableTapping()
    {
        if (!IsFlipped) button.interactable = true;
    }

    public void Flip()
    {
        if (IsFlipped || data == null) return;
        IsFlipped = true;
        button.interactable = false;

        transform.DOKill();
        Sequence s = DOTween.Sequence();
        s.Append(transform.DOScaleX(0f, 0.12f));
        s.AppendCallback(ShowFront);
        s.Append(transform.DOScaleX(1f, 0.12f));

        if (data.stars >= 3)
            s.Append(transform.DOPunchScale(new Vector3(0.25f, 0.25f, 0f), 0.5f, 6, 0.6f));
        else if (data.stars == 2)
            s.Append(transform.DOPunchScale(new Vector3(0.12f, 0.12f, 0f), 0.35f, 6, 0.5f));

        s.OnComplete(() => onFlipped?.Invoke(this));
    }

    private void ShowBack()
    {
        if (backSprite != null)
        {
            artImage.sprite = backSprite;
            artImage.color = Color.white;
        }
        else
        {
            artImage.sprite = null;
            artImage.color = new Color(0.2f, 0.2f, 0.35f);
        }
    }

    private void ShowFront()
    {
        if (data.cardArt != null)
        {
            artImage.sprite = data.cardArt;
            artImage.color = Color.white;
        }
        else
        {
            artImage.sprite = null;
            artImage.color = new Color(0.7f, 0.7f, 0.9f);
        }

        if (nameText != null) nameText.text = data.cardName;
        if (glow != null && data.stars >= 3) glow.SetActive(true);

        GameAudio.PlaySfx(data.signatureSfx);   // plays nothing if the card has no sound
    }
}