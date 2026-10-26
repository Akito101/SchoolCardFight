using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;

public class CardTilt : MonoBehaviour, IDragHandler, IEndDragHandler
{
    public float sensitivity = 0.3f;
    public float maxAngle = 25f;

    private Vector2 angles;

    public void OnDrag(PointerEventData eventData)
    {
        transform.DOKill();

        angles.y += eventData.delta.x * sensitivity;
        angles.x -= eventData.delta.y * sensitivity;
        angles.x = Mathf.Clamp(angles.x, -maxAngle, maxAngle);
        angles.y = Mathf.Clamp(angles.y, -maxAngle, maxAngle);

        transform.localRotation = Quaternion.Euler(angles.x, -angles.y, 0f);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        angles = Vector2.zero;
        transform.DOLocalRotate(Vector3.zero, 0.4f).SetEase(Ease.OutBack);
    }
}