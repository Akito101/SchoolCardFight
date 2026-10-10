using UnityEngine;
using UnityEngine.EventSystems;

public class SwipeToPlay : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public float swipeThreshold = 150f;
    public CardView cardView;
    public BattleTestManager battleTestManager;

    public void OnBeginDrag(PointerEventData eventData)
    {
        Debug.Log("Begin drag detected!");
    }

    public void OnDrag(PointerEventData eventData)
    {
        // Required for Unity to recognize this object as a valid drag target,
        // even though we don't need to do anything during the drag itself
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        float verticalDistance = eventData.position.y - eventData.pressPosition.y;

        if (verticalDistance > swipeThreshold)
        {
            if (battleTestManager.DrawNextCard())
            {
                cardView.DrawCardAnimation();
            }
        }
    }
}