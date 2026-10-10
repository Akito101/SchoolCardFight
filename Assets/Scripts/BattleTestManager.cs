using UnityEngine;
using System.Collections.Generic;

public class BattleTestManager : MonoBehaviour
{
    public DeckManager deckManager;
    public CardView cardView;
    public PlayerCardStackView playerCardStackView;
    public List<CardData> testCards;

    void Start()
    {
        deckManager.BuildDeck(testCards, null);
        cardView.SetOriginalPosition();
        playerCardStackView.BuildStack(deckManager.CardsRemaining());
    }

    public bool DrawNextCard()
    {
        CardData drawn = deckManager.DrawCard();
        if (drawn == null) return false;

        cardView.Setup(drawn);
        playerCardStackView.RemoveTopCard();
        Debug.Log($"Cards remaining: {deckManager.CardsRemaining()}");
        return true;
    }
}