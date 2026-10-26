using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CollectionSlotView : MonoBehaviour
{
    public Image artImage;
    public TextMeshProUGUI nameText;     // optional: leave empty if the card has no name bar
    public TextMeshProUGUI countText;

    [Header("Shown for cards the player hasn't found yet")]
    public Sprite undiscoveredSprite;
    [Range(0f, 1f)] public float undiscoveredBrightness = 0.5f;   // 1 = original picture, 0 = pure black

    public void Setup(CardData data, bool discovered, int ownedCount)
    {
        if (!discovered)
        {
            if (undiscoveredSprite != null)
            {
                // your "not found yet" picture, darkened
                artImage.sprite = undiscoveredSprite;
                float b = undiscoveredBrightness;
                artImage.color = new Color(b, b, b, 1f);
            }
            else if (data.cardArt != null)
            {
                // fallback: the card art as a black silhouette
                artImage.sprite = data.cardArt;
                artImage.color = Color.black;
            }
            else
            {
                artImage.sprite = null;
                artImage.color = new Color(0.15f, 0.15f, 0.2f);
            }

            SetName("???");
            SetCount("");
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

        SetName($"{data.cardName}\n({data.category})");
        SetCount($"x{ownedCount}");   // x0 means you found it before but don't own it now
    }

    private void SetName(string text)
    {
        if (nameText != null) nameText.text = text;
    }

    private void SetCount(string text)
    {
        if (countText != null) countText.text = text;
    }
}