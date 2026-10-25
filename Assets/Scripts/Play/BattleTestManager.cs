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
    [SerializeField] private ArtifactContainerController artifactContainerController;
    [SerializeField] private ArtifactUIController artifactUIController;

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
        // 1: Reset the board and wait for both decks to finish their opening animations.
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

        // 2: Display the player's artifact Container.
        artifactContainerController.UpdateSpellUI(playerArtifactEffect);
    }

    private void OnInitialDeckAnimationComplete()
    {
        // 1: Condition to ensure animtion run only once for both decks.
        initialDeckAnimationsPending--;
        if (initialDeckAnimationsPending > 0) return;

        // 2: Spawn the initial cards once.
        playerCCView = playerDeckView.SpawnCard();
        playerHasCard = true;

        aiCCView = aiDeckView.SpawnCard();
        aiHasCard = true;

        // 3: Start the player's turn and enable the player's card interaction.
        currentTurn = Turn.Player;
        isBusy = false;
        SetSwipe(playerCCView, true);
        SetSwipe(aiCCView, false);
    }

    public void DrawNextCard()
    {
        if (currentTurn != Turn.Player || isBusy) return;

        // 1: Draw and validate the player's next card.
        CardData drawPlayerCard = playerDeckView.DrawCard();
        if (drawPlayerCard == null) return;

        // 2: Disable the player's card interaction.
        isBusy = true;
        SetSwipe(playerCCView, false);

        // 3: Move the drawn card to the board and resolve its artifact effect.
        playerDeckView.PrepareDrawnCard(playerCCView, drawPlayerCard);
        PlayerArtifactActivation(drawPlayerCard);
        boardDeckView.AddCardToBoardDeck(drawPlayerCard, playerCCView, BoardCardOwner.Player,
        FinishPlayerTurn);

        // 4: Prepare the next card.
        UpdatePlayerCurrentCard();
    }

    public void PlayerArtifactActivation(CardData drawPlayerCard)
    {
        if (drawPlayerCard.isArtifact)
        {
            if (drawPlayerCard.artifactEffect == ArtifactEffect.Protection)
            {
                artifactController.ActivateProtection(BoardCardOwner.Player);
            }
            else if (drawPlayerCard.artifactEffect == ArtifactEffect.ReshuffleOpponentDeck)
            {
                artifactUIController.ShowArtifactNotification(ArtifactEffect.ReshuffleOpponentDeck, () =>
                {
                    // artifactController.ReshuffleOpponentDeck(BoardCardOwner.Player);
                });
            }
            else if (drawPlayerCard.artifactEffect == ArtifactEffect.StealCard)
            {
                artifactUIController.ShowArtifactNotification(ArtifactEffect.StealCard, () =>
               {
                   // artifactController.StealOpponentCard(BoardCardOwner.Player);
               });
            }
        }
    }

    private void FinishPlayerTurn()
    {
        CheckAndResolveMatch(isPlayerWinner: true, (playerWin) =>
        {
            // 1: If the player won, continue with the player's turn.
            if (playerWin)
            {
                SetPlayerTurn();
            }
            // 2: If player didn't win, continue with the AI's turn.
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
        // 1: Draw the AI's next card and return the board cards if its deck is empty.
        CardData drawAICard = aiDeckView.DrawCard();
        if (drawAICard == null)
        {
            isBusy = true;
            ReturnBoardCardsToOwnerDecks(() =>
            {
                RestoreMissingCards();

                currentTurn = Turn.AI;
                isBusy = false;

                DOVirtual.DelayedCall(0.4f, () =>
               {
                   DrawAICard();
               });
            });

            return;
        }

        // 2: Move the drawn card to the board and resolve its artifact effect.
        aiDeckView.PrepareDrawnCard(aiCCView, drawAICard);
        AIArtifactActivation(drawAICard);
        boardDeckView.AddCardToBoardDeck(drawAICard, aiCCView, BoardCardOwner.AI, FinishAITurn);

        // 3: Prepare the next card for the AI.
        UpdateAICurrentCard();
    }

    public void AIArtifactActivation(CardData drawAICard)
    {
        if (drawAICard.isArtifact)
        {
            if (drawAICard.artifactEffect == ArtifactEffect.Protection)
            {
                artifactController.ActivateProtection(BoardCardOwner.AI);
            }
            else if (drawAICard.artifactEffect == ArtifactEffect.ReshuffleOpponentDeck)
            {
                artifactUIController.ShowArtifactNotification(ArtifactEffect.ReshuffleOpponentDeck, () =>
                {
                    // artifactController.ReshuffleOpponentDeck(BoardCardOwner.AI);
                });
            }
            else if (drawAICard.artifactEffect == ArtifactEffect.StealCard)
            {
                artifactUIController.ShowArtifactNotification(ArtifactEffect.StealCard, () =>
               {
                   // artifactController.StealOpponentCard(BoardCardOwner.AI);
               });
            }
        }
    }

    private void FinishAITurn()
    {
        CheckAndResolveMatch(isPlayerWinner: false, (aiWin) =>
         {
             // 1: If the AI won, continue with the AI's turn.
             if (aiWin)
             {
                 DOVirtual.DelayedCall(0.4f, () =>
                {
                    DrawAICard();
                });
             }
             // 2: If the AI didn't win, check if the player has cards left and continue with the player's turn.
             else
             {
                 if (playerDeckView.GetCardCount() == 0)
                 {
                     isBusy = true;
                     SetSwipe(playerCCView, false);

                     ReturnBoardCardsToOwnerDecks(() =>
                     {
                         RestoreMissingCards();
                         SetPlayerTurn();
                     });
                 }
                 else
                 {
                     SetPlayerTurn();
                 }
             }
         });
    }

    private void CheckAndResolveMatch(bool isPlayerWinner, Action<bool> onMatchResolved)
    {
        // 1: Read the latest two cards and stop when there is no possible match.
        List<CardData> boardDeck = boardDeckView.GetBoardDeck();
        if (boardDeck.Count < 2)
        {
            onMatchResolved(false);
            return;
        }

        CardData latestCard = boardDeck[boardDeck.Count - 1];
        CardData previousCard = boardDeck[boardDeck.Count - 2];

        // 2: Resolve a matching pair or report that no match occurred.
        if (latestCard.category == previousCard.category)
        {
            // 3: Check if the defending party has an active Protection artifact and consume it if available.
            BoardCardOwner defendingOwner = isPlayerWinner ? BoardCardOwner.AI : BoardCardOwner.Player;

            if (artifactController != null && artifactController.ConsumeProtection(defendingOwner, () =>
            {
                isBusy = false;
                onMatchResolved(false);
            }))
            {
                isBusy = true;
                SetSwipe(playerCCView, false);
                return;
            }

            isBusy = true;
            SetSwipe(playerCCView, false);

            // 4: Animate the board cards moving to the winner's deck.
            RectTransform winnerDeckTransform = isPlayerWinner ? playerDeckView.GetComponent<RectTransform>() : aiDeckView.GetComponent<RectTransform>();
            Vector2 targetDeckPos = winnerDeckTransform.anchoredPosition;

            boardDeckView.AddBoardDeckToWinnerDeck(
            targetDeckPos,
            boardDeck =>
            {
                // 5: Reset the Protection active artifact for the winning owner.
                BoardCardOwner winningOwner = isPlayerWinner ? BoardCardOwner.Player : BoardCardOwner.AI;
                artifactController.ResetProtectionOnMatchWinner(winningOwner);

                // 7: Check if the match is truly over.
                void CheckTrueWinner()
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

                // 6: Add the won cards back to the winner's deck.
                if (isPlayerWinner)
                {
                    playerDeckView.AddCardsAndReshuffle(
                    boardDeck,
                    CheckTrueWinner);
                }
                else
                {
                    aiDeckView.AddCardsAndReshuffle(
                   boardDeck,
                   CheckTrueWinner);
                }
            });
            return;
        }

        onMatchResolved(false);
    }

    private void ReturnBoardCardsToOwnerDecks(Action onComplete)
    {
        // 1: Return each board card to its owner's deck.
        Vector2 playerDeckPosition =
            playerDeckView.GetComponent<RectTransform>().anchoredPosition;

        Vector2 aiDeckPosition =
            aiDeckView.GetComponent<RectTransform>().anchoredPosition;

        boardDeckView.ReturnBoardCardsToOwnerDecks(
            playerDeckPosition,
            aiDeckPosition,
            (playerCards, aiCards) =>
            {
                // 2: Reshuffle both decks and continue after both operations finish.
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

    private void UpdatePlayerCurrentCard()
    {
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
    }

    private void UpdateAICurrentCard()
    {
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

    private void RestoreMissingCards()
    {
        if (!playerHasCard && playerDeckView.GetCardCount() > 0)
        {
            UpdatePlayerCurrentCard();
        }

        if (!aiHasCard && aiDeckView.GetCardCount() > 0)
        {
            UpdateAICurrentCard();
        }
    }

    private void SetPlayerTurn()
    {
        currentTurn = Turn.Player;
        isBusy = false;
        SetSwipe(playerCCView, true);
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