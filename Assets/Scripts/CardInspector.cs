using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class CardInspector : MonoBehaviour
{
    public RectTransform cardRoot;      // the big card image
    public Image artImage;              // the same Image on CardRoot
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI categoryText;
    public TextMeshProUGUI countText;

    public void Show(CardData data, int ownedCount)
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

        nameText.text = data.cardName;
        categoryText.text = data.category.ToString();
        countText.text = $"Owned: {ownedCount}";

        gameObject.SetActive(true);

        cardRoot.DOKill();
        cardRoot.localRotation = Quaternion.identity;
        cardRoot.localScale = Vector3.zero;
        cardRoot.DOScale(1f, 0.25f).SetEase(Ease.OutBack);
    }

    // Hook to the dark background and the Close button
    public void Close()
    {
        cardRoot.DOKill();
        gameObject.SetActive(false);
    }
}