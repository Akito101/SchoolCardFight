using UnityEngine;
using System.Collections.Generic;
using DG.Tweening;
using System;

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
    [SerializeField] private List<CardData> playerCards;
    [SerializeField] private List<CardData> aiCards;

    private CardView playerCCView; // player current card view
    private CardView aiCCView; // ai current card view
    private bool playerHasCard = true;
    private bool aiHasCard = true;
    private bool isBusy;
    private Turn currentTurn = Turn.Player;

    private void Start()
    {
        playerDeckView.BuildDeck(playerCards, null);
        aiDeckView.BuildDeck(aiCards, null);
        boardDeckView.ClearBoard();

        playerCCView = playerDeckView.SpawnCard();
        playerHasCard = true;

        aiCCView = aiDeckView.SpawnCard();
        aiHasCard = true;

        SetSwipe(playerCCView, true);
        SetSwipe(aiCCView, false);
    }

    public void DrawNextCard()
    {
        if (currentTurn != Turn.Player || isBusy) return;

        CardData drawPlayerCard = playerDeckView.DrawCard();

        if (drawPlayerCard == null) return;

        isBusy = true;
        SetSwipe(playerCCView, false);

        playerDeckView.PrepareDrawnCard(playerCCView, drawPlayerCard);
        boardDeckView.AddCardToBoardDeck(drawPlayerCard, playerCCView, BoardCardOwner.Player,
        FinishPlayerTurn);

        if (playerDeckView.GetCardCount() > 0)
        {
            playerCCView = playerDeckView.SpawnCard();
            playerHasCard = true;
        }
        else
        {
            playerCCView = null;
            playerHasCard = false;
        }

        return;
    }

    private void FinishPlayerTurn()
    {
        CheckAndResolveMatch(isPlayerWinner: true, (playerWin) =>
        {
            if (playerWin)
            {
                currentTurn = Turn.Player;
                isBusy = false;
                SetSwipe(playerCCView, true);
            }
            else
            {
                currentTurn = Turn.AI;
                DOVirtual.DelayedCall(0.25f, () =>
                {
                    DrawAICard();
                });
            }
        });
    }

    private void DrawAICard()
    {
        CardData drawAICard = aiDeckView.DrawCard();

        if (drawAICard == null)
        {
            isBusy = true;

            ReturnBoardCardsToOwnerDecks(() =>
            {
                if (!playerHasCard && playerDeckView.GetCardCount() > 0)
                {
                    playerCCView = playerDeckView.SpawnCard();
                    playerHasCard = true;
                }

                if (!aiHasCard && aiDeckView.GetCardCount() > 0)
                {
                    aiCCView = aiDeckView.SpawnCard();
                    aiHasCard = true;
                }

                currentTurn = Turn.AI;
                isBusy = false;

                DOVirtual.DelayedCall(0.4f, () =>
               {
                   DrawAICard();
               });
            });

            return;
        }

        aiDeckView.PrepareDrawnCard(aiCCView, drawAICard);
        boardDeckView.AddCardToBoardDeck(drawAICard, aiCCView, BoardCardOwner.AI, FinishAITurn);

        if (aiDeckView.GetCardCount() > 0)
        {
            aiCCView = aiDeckView.SpawnCard();
            aiHasCard = true;
        }
        else
        {
            aiCCView = null;
            aiHasCard = false;
        }
    }

    private void FinishAITurn()
    {
        CheckAndResolveMatch(isPlayerWinner: false, (aiWin) =>
         {
             if (aiWin)
             {
                 DOVirtual.DelayedCall(0.4f, () =>
                {
                    DrawAICard();
                });
             }
             else
             {
                 if (playerDeckView.GetCardCount() == 0)
                 {
                     isBusy = true;
                     SetSwipe(playerCCView, false);

                     ReturnBoardCardsToOwnerDecks(() =>
                     {
                         if (!playerHasCard && playerDeckView.GetCardCount() > 0)
                         {
                             playerCCView = playerDeckView.SpawnCard();
                             playerHasCard = true;
                         }

                         if (!aiHasCard && aiDeckView.GetCardCount() > 0)
                         {
                             aiCCView = aiDeckView.SpawnCard();
                             aiHasCard = true;
                         }

                         currentTurn = Turn.Player;
                         isBusy = false;

                         SetSwipe(playerCCView, true);
                     });
                 }
                 else
                 {
                     currentTurn = Turn.Player;
                     isBusy = false;
                     SetSwipe(playerCCView, true);
                 }
             }
         });
    }

    private void CheckAndResolveMatch(bool isPlayerWinner, Action<bool> onMatchResolved)
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

            RectTransform winnerDeckTransform = isPlayerWinner ? playerDeckView.GetComponent<RectTransform>() : aiDeckView.GetComponent<RectTransform>();
            Vector2 targetDeckPos = winnerDeckTransform.anchoredPosition;

            boardDeckView.AddBoardDeckToWinnerDeck(
            targetDeckPos,
            boardCards =>
            {
                // 1. Add the won cards to the winner's remaining deck and reshuffle it.
                if (isPlayerWinner)
                {
                    playerDeckView.AddCardsAndReshuffle(boardCards);
                }
                else
                {
                    aiDeckView.AddCardsAndReshuffle(boardCards);
                }

                // 2. Check the loser's deck count to determine if the match is over
                int loserCardCount = isPlayerWinner ? aiDeckView.GetCardCount() : playerDeckView.GetCardCount();

                if (loserCardCount <= 4)
                {
                    string realWinner = isPlayerWinner ? "Player" : "AI";
                    Debug.Log($"Match Over! Loser dropped to {loserCardCount} cards. 🏆 {realWinner} wins the match!");

                    isBusy = true;
                    SetSwipe(playerCCView, false);
                    // TODO: Connect with WIN/LOSE UI here 
                    return;
                }

                onMatchResolved(true);
            });
            return;
        }

        onMatchResolved(false);
    }

    private void ReturnBoardCardsToOwnerDecks(Action onComplete)
    {
        Vector2 playerDeckPosition =
            playerDeckView.GetComponent<RectTransform>().anchoredPosition;

        Vector2 aiDeckPosition =
            aiDeckView.GetComponent<RectTransform>().anchoredPosition;

        boardDeckView.ReturnBoardCardsToOwnerDecks(
            playerDeckPosition,
            aiDeckPosition,
            (playerCards, aiCards) =>
            {
                playerDeckView.AddCardsAndReshuffle(playerCards);
                aiDeckView.AddCardsAndReshuffle(aiCards);

                onComplete?.Invoke();
            }
        );
    }

    private void SetSwipe(CardView card, bool enable)
    {
        if (card == null) return;

        SwipeToPlay swipe = card.GetComponent<SwipeToPlay>();
        if (swipe != null)
        {
            swipe.enabled = enable;
            if (enable) swipe.battleTestManager = this;
        }
    }
}