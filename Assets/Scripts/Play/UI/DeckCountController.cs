using UnityEngine;
using TMPro;

public class DeckCountController : MonoBehaviour
{
    [Header("Player View")]
    [SerializeField] private PlayerDeckView playerDeckView;
    [SerializeField] private TextMeshProUGUI playerDeckCountText;

    [Header("AI View")]
    [SerializeField] private AIDeckView aiDeckView;
    [SerializeField] private TextMeshProUGUI aiDeckCountText;

    void Update()
    {
        UpdateDeckCounts();
    }

    private void UpdateDeckCounts()
    {
        // Update Player Deck Count
        if (playerDeckView != null && playerDeckCountText != null)
        {
            int count = playerDeckView.GetCardCount();
            playerDeckCountText.text = count.ToString();
        }

        // Update AI Deck Count (if assigned)
        if (aiDeckView != null && aiDeckCountText != null)
        {
            int count = aiDeckView.GetCardCount();
            aiDeckCountText.text = count.ToString();
        }
    }
}
