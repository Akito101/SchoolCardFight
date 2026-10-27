using UnityEngine;
using DG.Tweening;

public class ArtifactUIController : MonoBehaviour
{
    [Header("Artifact UI Effect References")]
    [SerializeField] private GameObject protectionActivationObj;
    [SerializeField] private GameObject reshuffleActivationObj;
    [SerializeField] private GameObject stealActivationObj;
    [SerializeField] private WheelClickDetector wheelClickDetector;

    public bool IsPlayingNotification { get; private set; } = false;
    private Sequence currentSequence;

    private void Awake()
    {
        HideAll();
    }

    public void ShowStealArtifactWheel(System.Action<CardCategory> onCategoryChosen)
    {
        if (wheelClickDetector == null)
        {
            Debug.LogError("Cannot show steal artifact wheel because the WheelClickDetector reference is missing.");
            onCategoryChosen?.Invoke(CardCategory.Humans);
            return;
        }

        wheelClickDetector.ShowWheel(onCategoryChosen);
    }

    public void ShowArtifactNotification(ArtifactEffect effect, System.Action onComplete = null)
    {
        // 1: Stop the previous notification and reset all notification objects.
        StopCurrentSequence();
        HideAll();

        // 2: Find and prepare the notification object for the selected artifact.
        GameObject targetObj = GetNotificationObject(effect);
        if (targetObj == null)
        {
            onComplete?.Invoke();
            return;
        }

        CanvasGroup canvasGroup = PrepareNotificationObject(targetObj);

        // 3: Animate the prepared notification and complete the request when it finishes.
        currentSequence = CreateNotificationSequence(targetObj, canvasGroup, onComplete);
    }

    private GameObject GetNotificationObject(ArtifactEffect effect)
    {
        switch (effect)
        {
            case ArtifactEffect.Protection:
                return protectionActivationObj;
            case ArtifactEffect.ReshuffleOpponentDeck:
                return reshuffleActivationObj;
            case ArtifactEffect.StealCard:
                return stealActivationObj;
            default:
                return null;
        }
    }

    private CanvasGroup PrepareNotificationObject(GameObject targetObj)
    {
        IsPlayingNotification = true;
        targetObj.SetActive(true);

        CanvasGroup canvasGroup = targetObj.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = targetObj.AddComponent<CanvasGroup>();
        }

        canvasGroup.alpha = 1f;
        targetObj.transform.localScale = Vector3.zero;
        return canvasGroup;
    }

    private Sequence CreateNotificationSequence(
        GameObject targetObj,
        CanvasGroup canvasGroup,
        System.Action onComplete)
    {
        Sequence sequence = DOTween.Sequence();
        sequence.Append(targetObj.transform.DOScale(Vector3.one, 0.35f).SetEase(Ease.OutBack));
        sequence.AppendInterval(1f);
        sequence.Append(canvasGroup.DOFade(0f, 0.5f));
        sequence.OnComplete(() =>
        {
            targetObj.SetActive(false);
            IsPlayingNotification = false;
            onComplete?.Invoke();
        });
        return sequence;
    }

    private void StopCurrentSequence()
    {
        if (currentSequence != null && currentSequence.IsActive())
        {
            currentSequence.Kill();
        }
    }

    private void HideAll()
    {
        HideNotificationObject(protectionActivationObj);
        HideNotificationObject(reshuffleActivationObj);
        HideNotificationObject(stealActivationObj);
        IsPlayingNotification = false;
    }

    private void HideNotificationObject(GameObject notificationObject)
    {
        if (notificationObject != null)
        {
            notificationObject.SetActive(false);
        }
    }
}
