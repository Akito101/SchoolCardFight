using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "CardDatabase", menuName = "Cards/Card Database")]
public class CardDatabase : ScriptableObject
{
    public List<CardData> allCards = new List<CardData>();

    public CardData GetByName(string cardName)
    {
        return allCards.Find(c => c != null && c.cardName == cardName);
    }
}