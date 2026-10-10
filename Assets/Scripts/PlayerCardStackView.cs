using UnityEngine;
using System.Collections.Generic;

public class PlayerCardStackView : MonoBehaviour
{
    public GameObject cardBackPrefab;
    public RectTransform stackParent;
    public Vector2 stackOffset = new Vector2(12f, 0f);
    public int maxVisibleLayers = 4;

    private List<GameObject> playerCardList = new List<GameObject>();

    public void BuildStack(int cardCount)
    {
        ClearStack();
        for (int i = 0; i < cardCount; i++)
        {
            GameObject card = Instantiate(cardBackPrefab, stackParent);
            playerCardList.Add(card);
        }

        RepositionStack();
    }

    private void ClearStack()
    {
        foreach (GameObject card in playerCardList)
        {
            Destroy(card);
        }
        playerCardList.Clear();
    }

    public void RemoveTopCard()
    {
        if (playerCardList.Count == 0) return;

        GameObject topCard = playerCardList[0];
        Destroy(topCard);
        playerCardList.RemoveAt(0);

        RepositionStack();
    }

    private void RepositionStack()
    {
        int visibleLayerCount = Mathf.Max(1, maxVisibleLayers);

        for (int i = 0; i < playerCardList.Count; i++)
        {
            RectTransform rt = playerCardList[i].GetComponent<RectTransform>();
            int layer = Mathf.Min(i, visibleLayerCount - 1);
            rt.anchoredPosition = stackOffset * layer;
            rt.SetSiblingIndex(i);
        }
    }
}