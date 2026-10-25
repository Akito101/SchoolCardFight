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

    [Header("UI References")]
    [SerializeField] private SpellUIController spellUIController;

    [Header("Cards Settings")]
    [SerializeField] private List<CardData> playerCards;
    [SerializeField] private List<CardData> aiCards;

    [Header("Artifact Settings")]
    [SerializeField] private ArtifactEffect playerArtifactEffect = ArtifactEffect.Protection;
    [SerializeField] private ArtifactEffect aiArtifactEffect = ArtifactEffect.None;

    private ArtifactController artifactController;
    private CardView playerCCView; // player current card view
    private CardView aiCCView; // ai current card view
    private bool playerHasCard = true;
    private bool aiHasCard = true;
    private bool isBusy;
    private Turn currentTurn = Turn.Player;
    private int initialDeckAnimationsPending;

    private void Awake()
    {
        artifactController = GetComponent<ArtifactController>();
    }

    private void Start()
    {
        isBusy = true;
        boardDeckView.ClearBoard();

        initialDeckAnimationsPending = 2;

        playerDeckView.BuildDeck(
            playerCards,
            playerArtifactEffect,
            OnInitialDeckAnimationComplete);

        aiDeckView.BuildDeck(
            aiCards,
            aiArtifactEffect,
            OnInitialDeckAnimationComplete);

        if (spellUIController != null)
        {
            spellUIController.UpdateSpellUI(playerArtifactEffect);
        }
    }

    private void OnInitialDeckAnimationComplete()
    {
        initialDeckAnimationsPending--;

        if (initialDeckAnimationsPending > 0) return;

        playerCCView = playerDeckView.SpawnCard();
        playerHasCard = true;

        aiCCView = aiDeckView.SpawnCard();
        aiHasCard = true;

        currentTurn = Turn.Player;
        isBusy = false;

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
        PlayerArtifactActivationCheck(drawPlayerCard);
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

    public void PlayerArtifactActivationCheck(CardData drawPlayerCard)
    {
        if (drawPlayerCard.isArtifact && drawPlayerCard.artifactEffect == ArtifactEffect.Protection)
        {
            artifactController.ActivateProtection(BoardCardOwner.Player);
        }
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
        AIArtifactActivationCheck(drawAICard);
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

    public void AIArtifactActivationCheck(CardData drawAICard)
    {
        if (drawAICard.isArtifact && drawAICard.artifactEffect == ArtifactEffect.Protection)
        {
            artifactController.ActivateProtection(BoardCardOwner.AI);
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
            BoardCardOwner defendingOwner = isPlayerWinner ? BoardCardOwner.AI : BoardCardOwner.Player;

            if (artifactController != null && artifactController.ConsumeProtection(defendingOwner))
            {
                onMatchResolved(false);
                return;
            }

            isBusy = true;
            SetSwipe(playerCCView, false);

            RectTransform winnerDeckTransform = isPlayerWinner ? playerDeckView.GetComponent<RectTransform>() : aiDeckView.GetComponent<RectTransform>();
            Vector2 targetDeckPos = winnerDeckTransform.anchoredPosition;

            boardDeckView.AddBoardDeckToWinnerDeck(
            targetDeckPos,
            boardCards =>
            {
                BoardCardOwner winningOwner = isPlayerWinner ? BoardCardOwner.Player : BoardCardOwner.AI;
                artifactController.ResetProtectionOnMatchWinner(winningOwner);

                // Check the loser's deck count to determine if the match is over
                void FinishReshuffle()
                {
                    int loserCardCount = isPlayerWinner
                        ? aiDeckView.GetCardCount()
                        : playerDeckView.GetCardCount();

                    if (loserCardCount <= 4)
                    {
                        string realWinner = isPlayerWinner ? "Player" : "AI";

                        Debug.Log(
                            $"Match Over! Loser dropped to {loserCardCount} cards. " +
                            $"🏆 {realWinner} wins the match!");

                        isBusy = true;
                        SetSwipe(playerCCView, false);
                        return;
                    }

                    onMatchResolved(true);
                }

                // Add the won cards to the winner's remaining deck and reshuffle it.
                if (isPlayerWinner)
                {
                    playerDeckView.AddCardsAndReshuffle(
                    boardCards,
                    FinishReshuffle);
                }
                else
                {
                    aiDeckView.AddCardsAndReshuffle(
                   boardCards,
                   FinishReshuffle);
                }
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
                int reshufflesPending = 2;

                void FinishReshuffle()
                {
                    reshufflesPending--;

                    if (reshufflesPending == 0)
                    {
                        onComplete?.Invoke();
                    }
                }

                playerDeckView.AddCardsAndReshuffle(
                playerCards,
                FinishReshuffle);

                aiDeckView.AddCardsAndReshuffle(
                aiCards,
                FinishReshuffle);


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