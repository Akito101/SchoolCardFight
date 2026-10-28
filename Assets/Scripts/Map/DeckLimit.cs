using UnityEngine;

public static class DeckLimit
{
    // Moves cards from the deck to the inventory until the deck has at most 'max' cards.
    // It takes a card from the category the deck has the most of, and the fewest stars first,
    // so the trimmed deck stays balanced and keeps its best cards.
    public static void TrimTo(int max, CardDatabase database)
    {
        if (database == null) return;

        int categoryCount = System.Enum.GetValues(typeof(CardCategory)).Length;
        int safety = 1000;

        while (PlayerDeck.TotalInDeck() > max && safety-- > 0)
        {
            // how many cards the deck has in each category
            int[] perCategory = new int[categoryCount];
            foreach (CardCount c in PlayerCollection.GetAll())
            {
                CardData card = database.GetByName(c.cardName);
                if (card == null) continue;
                perCategory[(int)card.category] += PlayerDeck.GetInDeck(c.cardName);
            }

            // the fullest category
            int biggest = 0;
            for (int i = 1; i < categoryCount; i++)
            {
                if (perCategory[i] > perCategory[biggest]) biggest = i;
            }

            // inside it, the card with the fewest stars
            CardData pick = null;
            foreach (CardCount c in PlayerCollection.GetAll())
            {
                CardData card = database.GetByName(c.cardName);
                if (card == null || (int)card.category != biggest) continue;
                if (PlayerDeck.GetInDeck(c.cardName) <= 0) continue;
                if (pick == null || card.stars < pick.stars) pick = card;
            }

            if (pick == null || !PlayerDeck.MoveToBench(pick.cardName)) break;
        }
    }
}