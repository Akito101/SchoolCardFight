using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "CardDatabase", menuName = "Cards/Card Database")]
public class CardDatabase : ScriptableObject
{
    public List<CardData> allCards = new List<CardData>();

    [Header("Pack drop rates (any numbers, they are shares of the total)")]
    public float oneStarWeight = 70f;
    public float twoStarWeight = 25f;
    public float threeStarWeight = 5f;

    public CardData GetByName(string cardName)
    {
        return allCards.Find(c => c != null && c.cardName == cardName);
    }

    public float WeightFor(int stars)
    {
        if (stars >= 3) return threeStarWeight;
        if (stars == 2) return twoStarWeight;
        return oneStarWeight;
    }

    void OnValidate()
    {
        HashSet<string> seen = new HashSet<string>();
        foreach (CardData c in allCards)
        {
            if (c == null) continue;
            if (!seen.Add(c.cardName))
                Debug.LogWarning($"CardDatabase: duplicate Card Name '{c.cardName}'. Every card needs a unique name!");
        }
    }
}