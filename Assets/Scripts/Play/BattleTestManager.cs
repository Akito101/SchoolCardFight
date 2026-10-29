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
    [SerializeField] private BattleResultPanel resultPanel;
    [SerializeField] private ArtifactContainerController artifactContainerController;
    [SerializeField] private ArtifactUIController artifactUIController;
    [SerializeField] private DialogueController dialogueController;

    [Header("Cards Settings")]
    [SerializeField] private List<CardData> playerCards;
    [SerializeField] private List<CardData> aiCards;

    [Header("Artifact Settings")]
    [SerializeField] private ArtifactEffect playerArtifactEffect = ArtifactEffect.Protection;
    [SerializeField] private ArtifactEffect aiArtifactEffect = ArtifactEffect.None;

    [Header("Real decks")]
    [SerializeField] private CardDatabase database;      // same CardDatabase asset the DeckBuilder uses
    [SerializeField] private bool usePlayerDeck = true;  // untick to keep the placeholder list for testing

    private ArtifactController artifactController;
    private CardView playerCCView; // player current card view
    private CardView aiCCView; // ai current card view
    private bool playerHasCard = true;
    private bool aiHasCard = true;
    private bool isBusy;
    private bool artifactEffectPending;
    private Turn currentTurn = Turn.Player;
    private int initialDeckAnimationsPending;
    private bool isMixAndDistributePendingOrActive;
    private bool playerUsedArtifactFirst;
    private bool aiUsedArtifactFirst;
    private bool monitorDialogue2Triggered;
    private CardData pendingPlayerArtifactCard;
    private List<CardData> pendingTeacherPlayerCards = new List<CardData>();
    private List<CardData> pendingTeacherAICards = new List<CardData>();
    private List<CardData> enemyStartingDeck;   // untouched copy of the enemy deck, used for the reward
    private bool matchOver;

    private void Awake()
    {
        artifactController = GetComponent<ArtifactController>();
    }

    private void Start()
    {
        // 0: Load this level's enemy deck (falls back to the placeholder list if the level has none).
        LevelRewards rewards = Resources.Load<LevelRewards>("LevelRewards");
        LevelRewards.Reward level = rewards != null ? rewards.Get(PlayerProgress.LevelSelected) : null;
        if (level != null && level.enemyDeck != null && level.enemyDeck.Count > 0)
        {
            aiCards = new List<CardData>(level.enemyDeck);
            aiArtifactEffect = level.enemyArtifact;
        }

        enemyStartingDeck = new List<CardData>(aiCards);

        // 0: Load the player's saved deck (falls back to the placeholder list if it is empty).
        if (usePlayerDeck && database != null)
        {
            // The first time a level is played, the deck is exactly this level's size.
            // A replay keeps the current deck, which is already sized for the next level.
            bool isReplay = PlayerProgress.LevelSelected < PlayerProgress.ClearedLevels;
            if (!isReplay && level != null && level.deckSize > 0)
            {
                DeckLimit.TrimTo(level.deckSize, database);

                if (PlayerDeck.TotalInDeck() < level.deckSize)
                    Debug.LogWarning($"BattleTestManager: the deck has {PlayerDeck.TotalInDeck()} cards but this level needs {level.deckSize}.");
            }

            List<CardData> built = BuildPlayerDeckFromSave();
            if (built.Count > 0) playerCards = built;
            else Debug.LogWarning("BattleTestManager: saved deck is empty, using the placeholder deck.");
        }

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

    private List<CardData> BuildPlayerDeckFromSave()
    {
        List<CardData> list = new List<CardData>();
        foreach (CardCount c in PlayerCollection.GetAll())
        {
            CardData card = database.GetByName(c.cardName);
            if (card == null) continue;

            int n = PlayerDeck.GetInDeck(c.cardName);   // copies in the deck, not benched
            for (int i = 0; i < n; i++) list.Add(card);
        }
        return list;
    }

    private void OnInitialDeckAnimationComplete()
    {
        // 1: Condition to ensure animation runs only once for both decks.
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
        if (matchOver || currentTurn != Turn.Player || isBusy) return;

        // 1: Draw and validate the player's next card.
        CardData drawPlayerCard = playerDeckView.DrawCard();
        if (drawPlayerCard == null) return;

        // 2: Disable the player's card interaction.
        isBusy = true;
        SetSwipe(playerCCView, false);

        // 3: Move the drawn card to the board and resolve its artifact effect.
        playerDeckView.PrepareDrawnCard(playerCCView, drawPlayerCard);
        boardDeckView.AddCardToBoardDeck(drawPlayerCard, playerCCView, BoardCardOwner.Player, () =>
        {
            PlayerArtifactActivation(drawPlayerCard);

            if (monitorDialogue2Triggered) return;

            if (dialogueController != null)
            {
                dialogueController.CheckAIDeckCondition(aiDeckView.GetCardCount(), () =>
                {
                    FinishPlayerTurn();
                });
            }
        });

        // 4: Prepare the next card.
        UpdatePlayerCurrentCard();
    }

    public void PlayerArtifactActivation(CardData drawPlayerCard)
    {
        if (drawPlayerCard.isArtifact)
        {
            playerUsedArtifactFirst = true;
            if (!aiUsedArtifactFirst && !monitorDialogue2Triggered)
            {
                pendingPlayerArtifactCard = drawPlayerCard;
                CheckPlayerFirstArtifactUsage();

                if (monitorDialogue2Triggered)
                {
                    Debug.Log("Player used an artifact first, triggering Monitor dialogue 2.");
                    return;
                }
            }

            if (artifactController == null)
            {
                Debug.LogError("Cannot process Player artifact activation because the ArtifactController reference is missing.");
                return;
            }

            if (drawPlayerCard.artifactEffect == ArtifactEffect.Protection)
            {
                artifactController.ActivateProtection(BoardCardOwner.Player);
            }
            else if (drawPlayerCard.artifactEffect == ArtifactEffect.ReshuffleOpponentDeck)
            {
                artifactEffectPending = true;
                artifactUIController.ShowArtifactNotification(ArtifactEffect.ReshuffleOpponentDeck, () =>
                {
                    artifactController.ReshuffleOpponentDeck(
                        BoardCardOwner.Player,
                        () => artifactEffectPending = false);
                });
            }
            else if (drawPlayerCard.artifactEffect == ArtifactEffect.StealCard)
            {
                artifactEffectPending = true;
                artifactUIController.ShowArtifactNotification(ArtifactEffect.StealCard, () =>
                {
                    artifactController.StealOpponentCard(
                        BoardCardOwner.Player, () => artifactEffectPending = false);
                });
            }
        }
    }

    private void FinishPlayerTurn()
    {
        if (matchOver) return;

        if (artifactEffectPending)
        {
            DOVirtual.DelayedCall(0.1f, FinishPlayerTurn);
            return;
        }

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
        if (matchOver) return;

        if (dialogueController != null && dialogueController.IsDialogueActive)
        {
            DOVirtual.DelayedCall(0.1f, DrawAICard);
            return;
        }

        dialogueController?.CheckAIDeckCondition(aiDeckView.GetCardCount(), DrawAICardAfterDialogue);
        if (dialogueController != null)
        {
            return;
        }

        DrawAICardAfterDialogue();
    }

    private void DrawAICardAfterDialogue()
    {
        if (matchOver) return;

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
        boardDeckView.AddCardToBoardDeck(drawAICard, aiCCView, BoardCardOwner.AI, () =>
        {
            AIArtifactActivation(drawAICard);

            FinishAITurn();
        });

        // 3: Prepare the next card for the AI.
        UpdateAICurrentCard();
    }

    public void AIArtifactActivation(CardData drawAICard)
    {
        if (drawAICard.isArtifact)
        {
            aiUsedArtifactFirst = true;

            if (artifactController == null)
            {
                Debug.LogError("Cannot process AI artifact activation because the ArtifactController reference is missing.");
                return;
            }

            if (drawAICard.artifactEffect == ArtifactEffect.Protection)
            {
                artifactController.ActivateProtection(BoardCardOwner.AI);
            }
            else if (drawAICard.artifactEffect == ArtifactEffect.ReshuffleOpponentDeck)
            {
                artifactEffectPending = true;
                artifactUIController.ShowArtifactNotification(ArtifactEffect.ReshuffleOpponentDeck, () =>
                {
                    artifactController.ReshuffleOpponentDeck(
                        BoardCardOwner.AI,
                        () => artifactEffectPending = false);
                });
            }
            else if (drawAICard.artifactEffect == ArtifactEffect.StealCard)
            {
                artifactEffectPending = true;
                artifactUIController.ShowArtifactNotification(ArtifactEffect.StealCard, () =>
                {
                    artifactController.StealOpponentCard(
                        BoardCardOwner.AI, () => artifactEffectPending = false);
                });
            }
        }
    }

    private void FinishAITurn()
    {
        if (matchOver) return;

        if (artifactEffectPending)
        {
            DOVirtual.DelayedCall(0.1f, FinishAITurn);
            return;
        }

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
        if (isMixAndDistributePendingOrActive || monitorDialogue2Triggered)
        {
            onMatchResolved(false);
            return;
        }

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

            if (!isPlayerWinner)
            {
                dialogueController?.CheckTeacherPlayerWinStreak(false);
            }

            // 4: Animate the board cards moving to the winner's deck.
            RectTransform winnerDeckTransform = isPlayerWinner ? playerDeckView.GetComponent<RectTransform>() : aiDeckView.GetComponent<RectTransform>();
            Vector2 targetDeckPos = winnerDeckTransform.anchoredPosition;

            if (isPlayerWinner)
            {
                pendingTeacherPlayerCards.Clear();
                pendingTeacherAICards.Clear();

                for (int i = boardDeck.Count - 2; i < boardDeck.Count; i++)
                {
                    if (!boardDeckView.TryGetCardOwner(boardDeck[i], out BoardCardOwner owner)) continue;

                    if (owner == BoardCardOwner.Player) pendingTeacherPlayerCards.Add(boardDeck[i]);
                    else pendingTeacherAICards.Add(boardDeck[i]);
                }
            }

            if (isPlayerWinner && dialogueController != null &&
                dialogueController.CheckTeacherPlayerWinStreak(true, () => onMatchResolved(true)))
            {
                return;
            }

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
                    if (matchOver) return;

                    int loserCardCount = isPlayerWinner
                        ? aiDeckView.GetCardCount()
                        : playerDeckView.GetCardCount();

                    if (loserCardCount <= 3)
                    {
                        string realWinner = isPlayerWinner ? "Player" : "AI";

                        Debug.Log(
                            $"Match Over! Loser dropped to {loserCardCount} cards. " +
                            $"🏆 {realWinner} wins the match!");

                        isBusy = true;
                        SetSwipe(playerCCView, false);
                        matchOver = true;
                        resultPanel.ShowResult(isPlayerWinner, enemyStartingDeck);
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
        artifactController.ResetAllProtection();

        // 1: Return each board card to its owner's deck.
        Vector2 playerDeckPosition =
            playerDeckView.GetComponent<RectTransform>().anchoredPosition;

        Vector2 aiDeckPosition =
            aiDeckView.GetComponent<RectTransform>().anchoredPosition;

        boardDeckView.ReturnBoardCardsToOwnerDecks(
            playerDeckPosition,
            aiDeckPosition,
            (returnedPlayerCards, returnedAICards) =>
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
                returnedPlayerCards,
                FinishReshuffle);

                aiDeckView.AddCardsAndReshuffle(
                returnedAICards,
                FinishReshuffle);
            }
        );
    }

    public void CheckPlayerFirstArtifactUsage()
    {
        if (monitorDialogue2Triggered ||
            dialogueController == null ||
            !dialogueController.IsMonitorAI)
        {
            return;
        }

        if (playerUsedArtifactFirst && !aiUsedArtifactFirst)
        {
            monitorDialogue2Triggered = true;

            isBusy = true;
            SetSwipe(playerCCView, false);

            dialogueController.TriggerMonitorDialogue2();
        }
    }

    public void HandleMonitorDialogueAgree()
    {
        isMixAndDistributePendingOrActive = true;
        isBusy = true;
        SetSwipe(playerCCView, false);

        artifactController.ResetAllProtection();

        Vector2 playerDeckPosition = playerDeckView.GetComponent<RectTransform>().anchoredPosition;
        Vector2 aiDeckPosition = aiDeckView.GetComponent<RectTransform>().anchoredPosition;

        boardDeckView.MixAndDistributeBoardCards(
            playerDeckPosition,
            aiDeckPosition,
            (distributedPlayerCards, distributedAICards) =>
            {
                int reshufflesPending = 2;

                void FinishReshuffle()
                {
                    reshufflesPending--;

                    if (reshufflesPending == 0)
                    {
                        isMixAndDistributePendingOrActive = false;
                        RestoreMissingCards();
                        SetPlayerTurn();
                    }
                }

                playerDeckView.AddCardsAndReshuffle(distributedPlayerCards, FinishReshuffle);
                aiDeckView.AddCardsAndReshuffle(distributedAICards, FinishReshuffle);
            }
        );
    }

    public void HandleMonitorDialogueDisagree()
    {
        isBusy = true;
        SetSwipe(playerCCView, false);

        ReturnBoardCardsToOwnerDecks(() =>
        {
            RestoreMissingCards();
            SetPlayerTurn();
        });
    }

    public void HandleMonitorDialogue2Agree()
    {
        pendingPlayerArtifactCard = null;
        artifactEffectPending = false;
        monitorDialogue2Triggered = false;

        DOVirtual.DelayedCall(0.25f, () =>
        {
            currentTurn = Turn.AI;
            isBusy = true;
            DrawAICard();
        });
    }

    public void HandleMonitorDialogue2Disagree()
    {
        CardData artifactCard = pendingPlayerArtifactCard;
        pendingPlayerArtifactCard = null;
        monitorDialogue2Triggered = false;
        isBusy = true;
        SetSwipe(playerCCView, false);

        if (artifactCard == null)
        {
            Debug.LogError("Cannot return the pending player artifact because it is missing.");
            SetPlayerTurn();
            return;
        }

        Vector2 playerDeckPosition = playerDeckView.GetComponent<RectTransform>().anchoredPosition;
        boardDeckView.ReturnCardToOwnerDeck(artifactCard, playerDeckPosition, returnedCard =>
        {
            if (returnedCard == null)
            {
                SetPlayerTurn();
                return;
            }

            playerDeckView.AddCardsAndReshuffle(
                new List<CardData> { returnedCard },
                () =>
                {
                    currentTurn = Turn.Player;
                    isBusy = false;
                    DrawNextCard();
                });
        });
    }

    public void HandleTeacherDialogue1Choice(bool playerAgreed)
    {
        int cardsToSteal = playerAgreed ? 5 : 4;
        isBusy = true;
        SetSwipe(playerCCView, false);

        if (artifactController == null)
        {
            Debug.LogError("Cannot process Teacher card theft because the ArtifactController reference is missing.");
            dialogueController?.CompleteTeacherDialogueEffect();
            return;
        }

        artifactController.TeacherTakeCards(
            cardsToSteal,
            () => dialogueController?.CompleteTeacherDialogueEffect());
    }

    public void HandleTeacherDialogue2Choice(bool playerAgreed)
    {
        isBusy = true;
        SetSwipe(playerCCView, false);

        if (!playerAgreed)
        {
            boardDeckView.ReturnBoardCardsToOwnerDecks(
                playerDeckView.GetComponent<RectTransform>().anchoredPosition,
                aiDeckView.GetComponent<RectTransform>().anchoredPosition,
                (returnedPlayerCards, returnedAICards) =>
            {
                if (artifactController == null)
                {
                    Debug.LogError("Cannot steal a Teacher card because the ArtifactController reference is missing.");
                    AddReturnedTeacherDialogueCards(
                        returnedPlayerCards,
                        returnedAICards,
                        () => dialogueController?.CompleteTeacherDialogueEffect());
                    return;
                }

                artifactController.PlayerStealCard(() =>
                {
                    AddReturnedTeacherDialogueCards(
                        returnedPlayerCards,
                        returnedAICards,
                        () => dialogueController?.CompleteTeacherDialogueEffect());
                });
            });
            return;
        }

        ReturnBoardCardsToOwnerDecks(() =>
        {
            dialogueController?.CompleteTeacherDialogueEffect();
        });
    }

    private void AddReturnedTeacherDialogueCards(
        List<CardData> returnedPlayerCards,
        List<CardData> returnedAICards,
        Action onComplete)
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

        playerDeckView.AddCardsAndReshuffle(returnedPlayerCards, FinishReshuffle);
        aiDeckView.AddCardsAndReshuffle(returnedAICards, FinishReshuffle);
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

    // ---- Debug: instant win / lose (remove before release) ----
    // In Play mode: click the three dots (⋮) at the top right of this component in the Inspector,
    // then choose "Debug: Instant Win" or "Debug: Instant Lose".
    [ContextMenu("Debug: Instant Win")]
    private void DebugInstantWin() { ForceResult(true); }

    [ContextMenu("Debug: Instant Lose")]
    private void DebugInstantLose() { ForceResult(false); }

    private void ForceResult(bool playerWon)
    {
        if (!Application.isPlaying) return;
        if (matchOver) return;
        matchOver = true;
        isBusy = true;
        SetSwipe(playerCCView, false);
        DOTween.KillAll();   // stops pending AI draws, card animations and so on
        resultPanel.ShowResult(playerWon, enemyStartingDeck);
    }
}