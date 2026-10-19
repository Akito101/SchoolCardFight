using UnityEngine;
using System.Collections.Generic;

public class BattleTestManager : MonoBehaviour
{
    private enum Turn
    {
        Player,
        AI
    }

    [Header("Deck References")]
    [SerializeField] private PlayerDeckView playerDeckView;
    [SerializeField] private AIDeckView aiDeckView;
    [SerializeField] private BoardDeckView boardDeckView;
    [SerializeField] private List<CardData> testCards;

    private CardView playerCCView; // player current card view
    private CardView aiCCView; // ai current card view
    private bool isBusy;
    private Turn currentTurn = Turn.Player;

    private void Start()
    {
        playerDeckView.BuildDeck(testCards, null);
        aiDeckView.BuildDeck(testCards, null);
        boardDeckView.ClearBoard();

        playerCCView = playerDeckView.SpawnCard();
        aiCCView = aiDeckView.SpawnCard();

        SetSwipe(aiCCView, false);
        SetSwipe(playerCCView, true);
    }

    public void DrawNextCard()
    {
        if (currentTurn != Turn.Player || isBusy) return;

        CardData drawnCard = playerDeckView.DrawCard();
        if (drawnCard == null) return;

        isBusy = true;
        SetSwipe(playerCCView, false);

        playerCCView.Setup(drawnCard);
        boardDeckView.AddCardToBoardDeck(drawnCard, playerCCView, FinishPlayerTurn);
        if (playerDeckView.HasCardsRemaining)
        {
            playerCCView = playerCCView.CreateNextDrawCard();
        }

        return;
    }

    private void FinishPlayerTurn()
    {
        CheckAndResolveMatch(isPlayerWinner: true, (isMatch) =>
        {
            if (isMatch)
            {
                isBusy = false;
                SetSwipe(playerCCView, true);
                currentTurn = Turn.Player;
            }
            else
            {
                currentTurn = Turn.AI;
                DrawAICard();
            }
        });
    }

    private void DrawAICard()
    {
        CardData drawnCard = aiDeckView.DrawCard();
        if (drawnCard == null)
        {
            currentTurn = Turn.Player;
            isBusy = false;
            if (playerDeckView.HasCardsRemaining)
            {
                SetSwipe(playerCCView, true);
            }
            return;
        }

        aiCCView.Setup(drawnCard);
        boardDeckView.AddCardToBoardDeck(drawnCard, aiCCView, FinishAITurn);
        if (aiDeckView.HasCardsRemaining)
        {
            aiCCView = aiCCView.CreateNextDrawCard();
        }
    }

    private void FinishAITurn()
    {
        CheckAndResolveMatch(isPlayerWinner: false, (isMatch) =>
         {
             if (isMatch)
             {

                 DrawAICard();
             }
             else
             {

                 currentTurn = Turn.Player;
                 isBusy = false;
                 if (playerDeckView.HasCardsRemaining)
                 {
                     SetSwipe(playerCCView, true);
                 }
             }
         });
    }

    private void CheckAndResolveMatch(bool isPlayerWinner, System.Action<bool> onMatchResolved)
    {
        List<CardData> boardCards = boardDeckView.GetBoardDeck();
        if (boardCards.Count < 2)
        {
            onMatchResolved(false);
            return;
        }

        CardData latestCard = boardCards[boardCards.Count - 1];
        CardData previousCard = boardCards[boardCards.Count - 2];

        if (latestCard.category == previousCard.category)
        {
            isBusy = true;
            SetSwipe(playerCCView, false);

            RectTransform winnerDeckTransform = isPlayerWinner ? playerDeckView.GetComponent<RectTransform>()
                                                           : aiDeckView.GetComponent<RectTransform>();
            Vector2 targetDeckPos = winnerDeckTransform.anchoredPosition;

            boardDeckView.AddBoardDeckToWinnerDeck(
            targetDeckPos,
            boardCards =>
            {
                // 1. Rebuild the winner's deck with the won cards
                if (isPlayerWinner)
                {
                    playerDeckView.BuildDeck(boardCards, null);
                }
                else
                {
                    aiDeckView.BuildDeck(boardCards, null);
                }
                onMatchResolved(true);
            });
        }
        else
        {
            onMatchResolved(false);
        }
    }

    private void SetSwipe(CardView card, bool enable)
    {
        SwipeToPlay swipe = card.GetComponent<SwipeToPlay>();
        if (swipe != null)
        {
            swipe.enabled = enable;
            if (enable) swipe.battleTestManager = this;
        }

    }
}