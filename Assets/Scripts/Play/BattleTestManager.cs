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

    public bool DrawNextCard()
    {
        if (currentTurn != Turn.Player || isBusy) return false;

        CardData drawn = playerDeckView.DrawCard();
        if (drawn == null) return false;

        isBusy = true;
        SetSwipe(playerCCView, false);
        playerCCView.Setup(drawn);
        boardDeckView.AddCardToBoardDeck(drawn, playerCCView, FinishPlayerTurn);
        if (playerDeckView.HasCardsRemaining)
        {
            playerCCView = playerCCView.CreateNextDrawCard();
        }
        return true;
    }

    private void FinishPlayerTurn()
    {
        currentTurn = Turn.AI;
        DrawAICard();
    }

    private void DrawAICard()
    {
        CardData drawn = aiDeckView.DrawCard();
        if (drawn == null)
        {
            currentTurn = Turn.Player;
            isBusy = false;
            if (playerDeckView.HasCardsRemaining)
            {
                SetSwipe(playerCCView, true);
            }
            return;
        }

        aiCCView.Setup(drawn);
        boardDeckView.AddCardToBoardDeck(drawn, aiCCView, FinishAITurn);
        if (aiDeckView.HasCardsRemaining)
        {
            aiCCView = aiCCView.CreateNextDrawCard();
        }
    }

    private void FinishAITurn()
    {
        currentTurn = Turn.Player;
        isBusy = false;
        if (playerDeckView.HasCardsRemaining)
        {
            SetSwipe(playerCCView, true);
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