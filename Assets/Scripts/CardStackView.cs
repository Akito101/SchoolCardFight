using UnityEngine;
using System.Collections.Generic;

public class CardStackView : MonoBehaviour
{
    public GameObject cardBackPrefab;
    public RectTransform stackParent;
    public Vector2 stackOffset = new Vector2(0f, 2f);

    private List<GameObject> stackVisuals = new List<GameObject>();

    public void BuildStack(int cardCount)
    {
        ClearStack();
        for (int i = cardCount - 1; i >= 0; i--)
        {
            GameObject card = Instantiate(cardBackPrefab, stackParent);
            RectTransform rt = card.GetComponent<RectTransform>();
            rt.anchoredPosition = stackOffset * i;
            stackVisuals.Add(card);
        }
    }

    private void ClearStack()
    {
        foreach (GameObject card in stackVisuals)
        {
            Destroy(card);
        }
        stackVisuals.Clear();
    }

    public void RemoveTopCard()
    {
        if (stackVisuals.Count == 0) return;

        int lastIndex = stackVisuals.Count - 1;
        Destroy(stackVisuals[lastIndex]);
        stackVisuals.RemoveAt(lastIndex);
    }
}