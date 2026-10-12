using UnityEngine;
using System.Collections.Generic;

public class BattleTestManager : MonoBehaviour
{
    [Header("Deck References")]
    [SerializeField] private DeckManager deckManager;
    [SerializeField] private PlayerCardStackView playerCardStackView;
    [SerializeField] private BoardCardStackView boardCardStackView;
    [SerializeField] private List<CardData> testCards;

    private CardView currentCardView;
    private bool hasDrawnCard;

    private void Start()
    {
        deckManager.BuildDeck(testCards, null);
        boardCardStackView.ClearBoard();
        currentCardView = playerCardStackView.SpawnCard();
        playerCardStackView.BuildDeck(deckManager.CardsRemaining());
        EnableCurrentCardSwipe();
    }

    public bool DrawNextCard()
    {
        CardData drawn = deckManager.DrawCard();
        if (drawn == null) return false;

        PrepareCardViewForDraw();
        currentCardView.Setup(drawn);
        playerCardStackView.RemoveTopCard();
        boardCardStackView.AddCardToBoardDeck(drawn, currentCardView);

        hasDrawnCard = true;
        Debug.Log($"Cards remaining: {deckManager.CardsRemaining()}");
        return true;
    }

    private void PrepareCardViewForDraw()
    {
        if (!hasDrawnCard) return;

        DisableCurrentCardSwipe();
        currentCardView = currentCardView.CreateNextDrawCard();
        EnableCurrentCardSwipe();
    }

    private void DisableCurrentCardSwipe()
    {
        SwipeToPlay swipe = currentCardView.GetComponent<SwipeToPlay>();
        if (swipe != null)
        {
            swipe.enabled = false;
        }
    }

    private void EnableCurrentCardSwipe()
    {
        SwipeToPlay swipe = currentCardView.GetComponent<SwipeToPlay>();
        if (swipe != null)
        {
            swipe.battleTestManager = this;
            swipe.enabled = true;
        }
    }

}