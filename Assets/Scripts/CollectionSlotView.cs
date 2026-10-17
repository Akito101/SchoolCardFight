using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CollectionSlotView : MonoBehaviour
{
    public Image artImage;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI countText;

    public void Setup(CardData data, bool discovered, int ownedCount)
    {
        if (!discovered)
        {
            // silhouette: the art in solid black (or dark grey if there's no art yet)
            if (data.cardArt != null)
            {
                artImage.sprite = data.cardArt;
                artImage.color = Color.black;
            }
            else
            {
                artImage.sprite = null;
                artImage.color = new Color(0.15f, 0.15f, 0.2f);
            }
            nameText.text = "???";
            countText.text = "";
            return;
        }

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

        nameText.text = $"{data.cardName}\n({data.category})";
        countText.text = $"x{ownedCount}"; // x0 means you found it before but don't own it now
    }
}