using System;
using System.Collections.Generic;
using UnityEngine;

public class AILevelDeck : MonoBehaviour
{
    [SerializeField] private CardDatabase cardDatabase;
    [SerializeField] private List<CardData> uniqueCardDatabase = new List<CardData>();

    public List<CardData> CreateDeck(BattleTestManager.AIType aiType)
    {
        if (cardDatabase == null || cardDatabase.allCards == null)
        {
            Debug.LogError("AILevelDeck: assign a CardDatabase in the Inspector.");
            return new List<CardData>();
        }

        GetDeckRules(aiType, out int deckSize, out CardCategory preferredCategory, out int preferredCount);

        CardData uniqueCard = null;
        if (aiType != BattleTestManager.AIType.Child)
        {
            uniqueCard = GetUniqueCard(preferredCategory);
            if (uniqueCard == null)
            {
                Debug.LogError(
                    $"AILevelDeck: uniqueCardDatabase has no card in the {preferredCategory} category.");
                return new List<CardData>();
            }
        }

        Dictionary<CardCategory, List<CardData>> cardsByCategory = CreateCategoryPools();
        foreach (CardCategory category in Enum.GetValues(typeof(CardCategory)))
        {
            bool needsNormalCard = category != preferredCategory || preferredCount > 1;
            if (needsNormalCard && cardsByCategory[category].Count == 0)
            {
                Debug.LogError($"AILevelDeck: CardDatabase has no cards in the {category} category.");
                return new List<CardData>();
            }
        }

        List<CardData> deck = new List<CardData>(deckSize);
        if (uniqueCard != null)
        {
            deck.Add(uniqueCard);
            AddRandomCards(deck, cardsByCategory[preferredCategory], preferredCount - 1);
        }
        else
        {
            AddRandomCards(deck, cardsByCategory[preferredCategory], preferredCount);
        }

        foreach (CardCategory category in Enum.GetValues(typeof(CardCategory)))
        {
            if (category != preferredCategory)
            {
                AddRandomCards(deck, cardsByCategory[category], 1);
            }
        }

        List<CardCategory> otherCategories = new List<CardCategory>();
        foreach (CardCategory category in Enum.GetValues(typeof(CardCategory)))
        {
            if (category != preferredCategory)
            {
                otherCategories.Add(category);
            }
        }

        int remainingCards = deckSize - deck.Count;
        for (int i = 0; i < remainingCards; i++)
        {
            CardCategory category = otherCategories[i % otherCategories.Count];
            AddRandomCards(deck, cardsByCategory[category], 1);
        }

        Shuffle(deck);
        return deck;
    }

    private CardData GetUniqueCard(CardCategory preferredCategory)
    {
        List<CardData> matchingCards = uniqueCardDatabase.FindAll(
            card => card != null && card.category == preferredCategory);

        if (matchingCards.Count == 0)
        {
            return null;
        }

        return matchingCards[UnityEngine.Random.Range(0, matchingCards.Count)];
    }

    private Dictionary<CardCategory, List<CardData>> CreateCategoryPools()
    {
        Dictionary<CardCategory, List<CardData>> cardsByCategory = new Dictionary<CardCategory, List<CardData>>();
        foreach (CardCategory category in Enum.GetValues(typeof(CardCategory)))
        {
            cardsByCategory[category] = new List<CardData>();
        }

        foreach (CardData card in cardDatabase.allCards)
        {
            if (card != null && !uniqueCardDatabase.Contains(card))
            {
                cardsByCategory[card.category].Add(card);
            }
        }

        return cardsByCategory;
    }

    private void AddRandomCards(List<CardData> deck, List<CardData> cardPool, int count)
    {
        for (int i = 0; i < count; i++)
        {
            deck.Add(cardPool[UnityEngine.Random.Range(0, cardPool.Count)]);
        }
    }

    private void GetDeckRules(
        BattleTestManager.AIType aiType,
        out int deckSize,
        out CardCategory preferredCategory,
        out int preferredCount)
    {
        switch (aiType)
        {
            case BattleTestManager.AIType.Child:
                deckSize = 10;
                preferredCategory = CardCategory.Beasts;
                preferredCount = 4;
                break;
            case BattleTestManager.AIType.Student:
                deckSize = 10;
                preferredCategory = CardCategory.Supernatural;
                preferredCount = 4;
                break;
            case BattleTestManager.AIType.Monitor:
                deckSize = 15;
                preferredCategory = CardCategory.Spirits;
                preferredCount = 6;
                break;
            case BattleTestManager.AIType.Teacher:
            default:
                deckSize = 20;
                preferredCategory = CardCategory.Legends;
                preferredCount = 8;
                break;
        }
    }

    private void Shuffle(List<CardData> deck)
    {
        for (int i = deck.Count - 1; i > 0; i--)
        {
            int swapIndex = UnityEngine.Random.Range(0, i + 1);
            CardData card = deck[i];
            deck[i] = deck[swapIndex];
            deck[swapIndex] = card;
        }
    }
}
