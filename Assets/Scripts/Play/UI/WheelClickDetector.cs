using UnityEngine;
using DG.Tweening;

public class WheelClickDetector : MonoBehaviour
{
    private System.Action<CardCategory> onCategoryChosenCallback;

    public void ShowWheel(System.Action<CardCategory> onCategoryChosen)
    {
        onCategoryChosenCallback = onCategoryChosen;
        gameObject.SetActive(true);

        CanvasGroup canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
        canvasGroup.alpha = 1f;

        transform.localScale = Vector3.zero;
        transform.DOScale(Vector3.one, 0.35f).SetEase(Ease.OutBack);
    }

    public void SelectHumans() => CloseAndCallback(CardCategory.Humans);
    public void SelectLegends() => CloseAndCallback(CardCategory.Legends);
    public void SelectSupernatural() => CloseAndCallback(CardCategory.Supernatural);
    public void SelectSpirits() => CloseAndCallback(CardCategory.Spirits);
    public void SelectBeasts() => CloseAndCallback(CardCategory.Beasts);

    private void CloseAndCallback(CardCategory category)
    {
        transform.DOScale(Vector3.zero, 0.25f).SetEase(Ease.InBack).OnComplete(() =>
        {
            gameObject.SetActive(false);
            System.Action<CardCategory> callback = onCategoryChosenCallback;
            onCategoryChosenCallback = null;
            callback?.Invoke(category);
        });
    }
}
