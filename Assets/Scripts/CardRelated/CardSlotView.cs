using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class CardSlotView : MonoBehaviour
{
    public Image artImage;
    public TextMeshProUGUI nameText;   // optional: leave empty if the card has no name text

    public void Setup(CardData data, float delay)
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

        if (nameText != null) nameText.text = $"{data.cardName}\n({data.category})";

        transform.localScale = Vector3.zero;
        transform.DOScale(1f, 0.3f).SetDelay(delay).SetEase(Ease.OutBack);
    }
}