using UnityEngine;

public static class DeckCount
{
    public static int Real()
    {
        return PlayerDeck.TotalInDeck();
    }

    public static void Repair()
    {
        PlayerDeck.Prune();
    }
}