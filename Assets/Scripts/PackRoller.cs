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
                    result.Add(ofCategory[Random.Range(0, ofCategory.Count)]);
            }
        }

        while (result.Count < cardsPerPack)
        {
            result.Add(pool[Random.Range(0, pool.Count)]);
        }

        return result;
    }
}