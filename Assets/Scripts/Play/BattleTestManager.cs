using UnityEngine;
using System.Collections.Generic;
using DG.Tweening;
using System;

public class BattleTestManager : MonoBehaviour
{
    public enum AIType
    {
        Child,
        Student,
        Monitor,
        Teacher
    }

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
    [SerializeField] private BackgroundController backgroundController;
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

    [Header("AI Settings")]
    [SerializeField] private AIType currentAIType = AIType.Teacher;
    [SerializeField] private AILevelDeck aiLevelDeck;

    [System.Serializable]
    public class BattleMusic
    {
        public AIType aiType;
        public AudioClip clip;
        [Range(0f, 1f)] public float volume = 1f;
    }

    [Header("Battle music override (leave empty to use the MusicLibrary track)")]
    [SerializeField] private List<BattleMusic> battleMusic = new List<BattleMusic>();


    private ArtifactController artifactController;
    private CardView playerCCView; // player current card view
    private CardView aiCCView; // ai current card view
    private bool playerHasCard = true;
    private bool aiHasCard = true;
    private bool isBusy;
    private bool artifactEffectPending;
    private Turn currentTurn = Turn.AI;

    private int initialDeckAnimationsPending;
    private bool isMixAndDistributePendingOrActive;
    private bool playerUsedArtifactFirst;
    private bool aiUsedArtifactFirst;
    private bool monitorDialogue2Triggered;
    private bool autoDrawAfterTeacherDialogue2;
    private CardData pendingPlayerArtifactCard;
    private List<CardData> pendingTeacherPlayerCards = new List<CardData>();
    private List<CardData> pendingTeacherAICards = new List<CardData>();
    private List<CardData> playerWonCards = new List<CardData>();
    private bool matchOver;

    private void Awake()
    {
        ApplyBattleMusicOverride();   // before the scene-loaded step, so the song isn't swapped

        artifactController = GetComponent<ArtifactController>();
        aiLevelDeck ??= GetComponent<AILevelDeck>();
    }

    private void Start()
    {
        // Load Battle Scene
        currentAIType = GetAITypeForLevel(PlayerProgress.LevelSelected);
        // currentAIType = GetAITypeForLevel(1);
        dialogueController?.SetAIType(currentAIType);
        backgroundController?.SetBackgroundForAI(currentAIType);
        resultPanel?.SetAIType(currentAIType);

        // Load AI Artifact
        LevelRewards rewards = Resources.Load<LevelRewards>("LevelRewards");
        LevelRewards.Reward level = rewards != null ? rewards.Get(PlayerProgress.LevelSelected) : null;
        if (level != null)
        {
            aiArtifactEffect = level.enemyArtifact;
        }

        // Load AI Deck
        List<CardData> generatedAIDeck = aiLevelDeck?.CreateDeck(currentAIType);
        if (generatedAIDeck != null && generatedAIDeck.Count > 0)
        {
            aiCards = generatedAIDeck;
        }

        // Player's artifact: only what was chosen on the Map. No choice (tutorial) = no artifact.
        playerArtifactEffect = PlayerArtifactChoice.HasChoice
            ? PlayerArtifactChoice.Effect
            : ArtifactEffect.None;

        playerWonCards.Clear();

        // 2: Load the player's saved deck
        {
            bool isReplay = PlayerProgress.LevelSelected < PlayerProgress.ClearedLevels;
            if (!isReplay && level != null && level.deckSize > 0)
            {
                DeckLimit.TrimTo(level.deckSize, database);

                // Cleanly delegate auto-population logic to PlayerDeck
                PlayerDeck.EnsureDeckPopulatedUpTo(level.deckSize);

                if (PlayerDeck.TotalInDeck() < level.deckSize)
                    Debug.LogWarning($"BattleTestManager: the deck has {PlayerDeck.TotalInDeck()} cards but this level needs {level.deckSize}.");
            }

            List<CardData> built = BuildPlayerDeckFromSave();
            if (built.Count > 0) playerCards = built;
            else Debug.LogWarning("BattleTestManager: saved deck is empty, using the placeholder deck.");
        }

        // 3: Reset the board and wait for both decks to finish their opening animations.
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

        // 4: Display the player's artifact Container.
        artifactContainerController.UpdateSpellUI(playerArtifactEffect);
    }

    // Picks the final boss song before MusicManager looks at the library
    private void ApplyBattleMusicOverride()
    {
        AIType type = GetAITypeForLevel(PlayerProgress.LevelSelected);
        Debug.Log($"Battle music check: level {PlayerProgress.LevelSelected} = {type}, entries = {battleMusic.Count}");

        foreach (BattleMusic m in battleMusic)
        {
            if (m.aiType == type && m.clip != null)
            {
                Debug.Log($"Battle music override set: {m.clip.name}");
                MusicManager.SetSceneOverride(m.clip, m.volume);
                return;
            }
        }

        Debug.Log("Battle music: no override found, using the library track.");
    }

    public AIType GetAITypeForLevel(int level)
    {
        switch (level)
        {
            case 0:
                return AIType.Child;
            case 1:
                return AIType.Student;
            case 2:
                return AIType.Monitor;
            case 3:
                return AIType.Teacher;
            default:
                Debug.LogWarning($"BattleTestManager: level {level} has no AI type mapping. Using Teacher.");
                return AIType.Teacher;
        }
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

        // 3: Always start the battle with the AI's turn.
        currentTurn = Turn.AI;
        isBusy = true;
        SetSwipe(playerCCView, false);
        SetSwipe(aiCCView, false);
        DOVirtual.DelayedCall(0.1f, DrawAICard);
    }

    public void DrawNextCard()
    {
        if (matchOver || currentTurn != Turn.Player || isBusy) return;

        EnsureCurrentCards();

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
                if (autoDrawAfterTeacherDialogue2)
                {
                    autoDrawAfterTeacherDialogue2 = false;
                    currentTurn = Turn.Player;
                    isBusy = false;
                    DOVirtual.DelayedCall(0.1f, DrawNextCard);
                }
                else
                {
                    SetPlayerTurn();
                }
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

                DOVirtual.DelayedCall(0.4f, DrawAICard);
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
                DOVirtual.DelayedCall(0.4f, DrawAICard);
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
                dialogueController?.CheckPlayerWinStreak(false);
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
                dialogueController.CheckPlayerWinStreak(true, () => onMatchResolved(true)))
            {
                return;
            }

            List<CardData> cardsWonByPlayer = isPlayerWinner
                ? GetCardsOwnedByAI(boardDeck)
                : new List<CardData>();

            boardDeckView.AddBoardDeckToWinnerDeck(
            targetDeckPos,
            boardDeck =>
            {
                if (isPlayerWinner)
                {
                    playerWonCards.AddRange(cardsWonByPlayer);
                }

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

                        isBusy = true;
                        SetSwipe(playerCCView, false);
                        matchOver = true;

                        List<CardData> lostCards = new List<CardData>();
                        if (!isPlayerWinner)
                        {
                            List<CardData> survivingCards = playerDeckView.GetRemainingDeckCards();

                            // Find cards that started in playerCards but are missing from survivingCards
                            List<CardData> tempSurviving = new List<CardData>(survivingCards);
                            foreach (CardData card in playerCards)
                            {
                                if (card == null) continue;
                                int index = tempSurviving.FindIndex(c => c != null && c.cardName == card.cardName);
                                if (index >= 0)
                                {
                                    tempSurviving.RemoveAt(index); // Match found, card survived
                                }
                                else
                                {
                                    lostCards.Add(card); // Card is missing, it was lost!
                                }
                            }
                        }

                        resultPanel.ShowResult(isPlayerWinner, playerWonCards, lostCards);
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
                ReshuffleBothDecks(
                    returnedPlayerCards,
                    returnedAICards,
                    onComplete);
            }
        );
    }

    private void ReshuffleBothDecks(
        List<CardData> playerCards,
        List<CardData> aiCards,
        Action onComplete)
    {
        int pendingAnimations = 2;

        void CompleteWhenBothDecksFinish()
        {
            pendingAnimations--;
            if (pendingAnimations == 0)
            {
                onComplete?.Invoke();
            }
        }

        playerDeckView.AddCardsAndReshuffle(playerCards, CompleteWhenBothDecksFinish);
        aiDeckView.AddCardsAndReshuffle(aiCards, CompleteWhenBothDecksFinish);
    }

    private List<CardData> GetCardsOwnedByAI(List<CardData> cards)
    {
        List<CardData> ownedCards = new List<CardData>();

        foreach (CardData card in cards)
        {
            if (card != null &&
                boardDeckView.TryGetCardOwner(card, out BoardCardOwner owner) &&
                owner == BoardCardOwner.AI)
            {
                ownedCards.Add(card);
            }
        }

        return ownedCards;
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
                ReshuffleBothDecks(
                    distributedPlayerCards,
                    distributedAICards,
                    () =>
                    {
                        isMixAndDistributePendingOrActive = false;
                        RestoreMissingCards();
                        SetPlayerTurn();
                    });
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
        autoDrawAfterTeacherDialogue2 = true;

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
                    TeacherCancelPlayerAritifact(
                        returnedPlayerCards,
                        returnedAICards,
                        () => dialogueController?.CompleteTeacherDialogueEffect());
                    return;
                }

                artifactController.PlayerStealCard(() =>
                {
                    TeacherCancelPlayerAritifact(
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

    private void TeacherCancelPlayerAritifact(
        List<CardData> returnedPlayerCards,
        List<CardData> returnedAICards,
        Action onComplete)
    {
        ReshuffleBothDecks(returnedPlayerCards, returnedAICards, onComplete);
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

    private void EnsureCurrentCards()
    {
        if (playerCCView == null && playerDeckView.GetCardCount() > 0)
            UpdatePlayerCurrentCard();

        if (aiCCView == null && aiDeckView.GetCardCount() > 0)
            UpdateAICurrentCard();
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
        EnsureCurrentCards();

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
        DOTween.KillAll();

        List<CardData> lostCards = playerWon ? null : new List<CardData>(playerCards);
        resultPanel.ShowResult(playerWon, playerWonCards, lostCards);
    }
}