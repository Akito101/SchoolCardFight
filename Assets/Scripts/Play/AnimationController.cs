using UnityEngine;
using System.Collections.Generic;
using DG.Tweening;
using System;

public class AnimationController : MonoBehaviour
{
    [Header("Audio Settings")]
    [SerializeField] private AudioSource deckAudioSource;
    [SerializeField] private AudioClip shuffleAudioClip;

    public static AnimationController Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public Sequence GatherCards(List<CardView> cardViews, Vector2 centerPos, Action onComplete = null)
    {
        Sequence seq = DOTween.Sequence();
        Sequence gatherSeq = DOTween.Sequence();

        foreach (var cardView in cardViews)
        {
            if (cardView == null) continue;
            RectTransform cardTrans = cardView.GetComponent<RectTransform>();
            cardTrans.DOKill();
            cardTrans.SetAsLastSibling();

            gatherSeq.Join(cardTrans.DOAnchorPos(centerPos, 0.3f).SetEase(Ease.OutQuad));
            gatherSeq.Join(cardTrans.DORotate(Vector3.zero, 0.3f).SetEase(Ease.OutQuad));
            gatherSeq.Join(cardTrans.DOScale(Vector3.one, 0.3f).SetEase(Ease.OutQuad));
        }

        seq.Append(gatherSeq);
        if (onComplete != null) seq.OnComplete(() => onComplete());
        return seq;
    }

    public Sequence FlipFaceDown(List<CardView> cardViews, float targetScaleX = 1f, Action onComplete = null)
    {
        Sequence seq = DOTween.Sequence();
        Sequence flipSeq = DOTween.Sequence();

        foreach (var cardView in cardViews)
        {
            if (cardView == null) continue;
            RectTransform cardTrans = cardView.GetComponent<RectTransform>();
            cardTrans.DOKill();

            Sequence cardFlip = DOTween.Sequence();
            cardFlip.Append(cardTrans.DOScaleX(0f, 0.3f).SetEase(Ease.InQuad));
            cardFlip.AppendCallback(() => cardView.ShowFaceDown());
            cardFlip.Append(cardTrans.DOScaleX(targetScaleX, 0.3f).SetEase(Ease.OutQuad));

            flipSeq.Join(cardFlip);
        }

        seq.Append(flipSeq);
        if (onComplete != null) seq.OnComplete(() => onComplete());
        return seq;
    }

    public Sequence FlipFaceUp(List<CardView> cardViews, float targetScaleX = 1f, Action onComplete = null)
    {
        Sequence seq = DOTween.Sequence();
        Sequence flipSeq = DOTween.Sequence();

        foreach (var cardView in cardViews)
        {
            if (cardView == null) continue;
            RectTransform cardTrans = cardView.GetComponent<RectTransform>();
            cardTrans.DOKill();

            Sequence cardFlip = DOTween.Sequence();
            cardFlip.Append(cardTrans.DOScaleX(0f, 0.3f).SetEase(Ease.InQuad));
            cardFlip.AppendCallback(() => cardView.ShowFaceUp());
            cardFlip.Append(cardTrans.DOScaleX(targetScaleX, 0.3f).SetEase(Ease.OutQuad));

            flipSeq.Join(cardFlip);
        }

        seq.Append(flipSeq);
        if (onComplete != null) seq.OnComplete(() => onComplete());
        return seq;
    }

    public Sequence OverhandShuffle(List<CardView> deckView, Vector2 centerPos, float verticalDirection = 100f, Action onComplete = null)
    {
        Sequence seq = DOTween.Sequence();

        if (deckView == null || deckView.Count < 3)
        {
            onComplete?.Invoke();
            return seq;
        }

        if (deckAudioSource != null && shuffleAudioClip != null)
        {
            deckAudioSource.PlayOneShot(shuffleAudioClip);
        }

        Sequence shuffleSeq = DOTween.Sequence();
        int shuffleIterations = 3;

        for (int iter = 0; iter < shuffleIterations; iter++)
        {
            int chunkSize = UnityEngine.Random.Range(2, Mathf.Max(3, deckView.Count / 2));
            int startIndex = UnityEngine.Random.Range(0, deckView.Count - chunkSize);
            int endIndex = startIndex + chunkSize;

            Sequence iterationSeq = DOTween.Sequence();
            List<RectTransform> movingCards = new List<RectTransform>();

            for (int i = startIndex; i < endIndex; i++)
            {
                if (deckView[i] != null)
                {
                    RectTransform cardTrans = deckView[i].GetComponent<RectTransform>();
                    movingCards.Add(cardTrans);
                }
            }

            if (movingCards.Count == 0) continue;

            foreach (var cardTrans in movingCards)
            {

                Vector2 downPosition = centerPos + new Vector2(0f, verticalDirection);

                Sequence cardMoveSeq = DOTween.Sequence();
                cardMoveSeq.Append(cardTrans.DOAnchorPos(downPosition, 0.12f).SetEase(Ease.OutQuad));
                cardMoveSeq.AppendCallback(() => cardTrans.SetAsLastSibling());
                cardMoveSeq.Append(cardTrans.DOAnchorPos(centerPos, 0.12f).SetEase(Ease.InQuad));

                iterationSeq.Join(cardMoveSeq);
            }

            iterationSeq.AppendInterval(0.1f);
            shuffleSeq.Append(iterationSeq);
        }

        seq.Append(shuffleSeq);

        if (onComplete != null) seq.OnComplete(() => onComplete());
        return seq;
    }
}
