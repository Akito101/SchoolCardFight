using UnityEngine;
using System.Collections.Generic;

public class BattleTestManager : MonoBehaviour
{
    public DeckManager deckManager;
    public CardView cardView;
    public CardStackView cardStackView;
    public List<CardData> testCards;

    void Start()
    {
        deckManager.BuildDeck(testCards, null);
        cardView.SetOriginalPosition();
        cardStackView.BuildStack(deckManager.CardsRemaining());
    }

    public void OnDrawButtonPressed()
    {
        CardData drawn = deckManager.DrawCard();
        cardView.Setup(drawn);
        cardStackView.RemoveTopCard();
        Debug.Log($"Cards remaining: {deckManager.CardsRemaining()}");
    }
}