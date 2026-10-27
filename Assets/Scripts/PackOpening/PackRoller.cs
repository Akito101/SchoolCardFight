using System.Collections.Generic;
using UnityEngine;

public static class PackRoller
{
    public static List<CardData> Roll(CardDatabase database, int cardsPerPack, bool guaranteeEachCategory = false)
    {
        List<CardData> result = new List<CardData>();

        if (database == null)
        {
            Debug.LogError("PackRoller: Database is empty!");
            return result;
        }

        List<CardData> pool = new List<CardData>();
        foreach (CardData c in database.allCards)
        {
            if (c == null) continue;
            if (!c.isArtifact) pool.Add(c);   // Artifacts never come out of packs
        }

        if (pool.Count == 0)
        {
            Debug.LogError("PackRoller: no cards found in the database!");
            return result;
        }

        if (guaranteeEachCategory)
        {
            foreach (CardCategory cat in System.Enum.GetValues(typeof(CardCategory)))
            {
                List<CardData> ofCategory = pool.FindAll(c => c.category == cat);
                if (ofCategory.Count > 0 && result.Count < cardsPerPack)
                    result.Add(PickWeighted(database, ofCategory));
            }
        }

        while (result.Count < cardsPerPack)
        {
            result.Add(PickWeighted(database, pool));
        }

        return result;
    }

    private static CardData PickWeighted(CardDatabase database, List<CardData> cards)
    {
        // only star levels that really have cards can be rolled
        float[] weights = new float[4];   // index 1 to 3
        float total = 0f;
        int tier = 1;

        for (int s = 1; s <= 3; s++)
        {
            bool hasCards = cards.Exists(c => Mathf.Clamp(c.stars, 1, 3) == s);
            weights[s] = hasCards ? Mathf.Max(0f, database.WeightFor(s)) : 0f;
            total += weights[s];
            if (weights[s] > 0f) tier = s;
        }

        if (total <= 0f) return cards[Random.Range(0, cards.Count)];   // plain random if the rates are all 0

        float roll = Random.Range(0f, total);
        for (int s = 1; s <= 3; s++)
        {
            if (roll < weights[s]) { tier = s; break; }
            roll -= weights[s];
        }

        List<CardData> inTier = cards.FindAll(c => Mathf.Clamp(c.stars, 1, 3) == tier);
        return inTier[Random.Range(0, inTier.Count)];
    }
}