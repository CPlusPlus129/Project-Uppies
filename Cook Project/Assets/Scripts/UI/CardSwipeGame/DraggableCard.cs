using DG.Tweening;
using R3;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DraggableCard : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField] private RectTransform cardRectTransform;
    [SerializeField] private Image cardImage;
    [SerializeField] private CanvasGroup canvasGroup;

    public Subject<(float, bool)> OnSwipeComplete = new Subject<(float, bool)>();
    public Subject<Unit> OnSwipeStart = new Subject<Unit>();

    private Vector2 startPosition;
    private Vector2 endPosition;
    private float startTime;
    private float swipeDistance;
    private bool canDrag = true;
    private bool isDragging = false;

    public void Initialize(Vector2 startPos, Vector2 endPos, float distance)
    {
        startPosition = startPos;
        endPosition = endPos;
        swipeDistance = distance;
        ResetPosition();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!canDrag) return;
        isDragging = true;
        startTime = Time.time;
        canvasGroup.alpha = 0.8f;
        OnSwipeStart.OnNext(Unit.Default);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!canDrag) return;
        if (!isDragging) return;

        Vector2 currentPos = cardRectTransform.anchoredPosition;
        Vector2 newPos = new Vector2(
            Mathf.Clamp(currentPos.x + eventData.delta.x, startPosition.x, endPosition.x),
            startPosition.y
        );

        cardRectTransform.anchoredPosition = newPos;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!canDrag) return;
        if (!isDragging) return;

        isDragging = false;
        canvasGroup.alpha = 1f;

        float swipeTime = Time.time - startTime;
        float distanceTraveled = cardRectTransform.anchoredPosition.x - startPosition.x;
        bool reachedEnd = distanceTraveled >= swipeDistance * 0.8f;

        float speed = swipeTime > 0 ? distanceTraveled / swipeTime : 0;

        OnSwipeComplete.OnNext((speed, reachedEnd));
    }

    public void MoveToResetPosition(float duration)
    {
        canDrag = false;
        DOTween.Kill(cardRectTransform);
        cardRectTransform.DOAnchorPos(startPosition, duration).SetEase(Ease.OutQuad).OnComplete(() => ResetPosition());
    }

    public void ResetPosition()
    {
        cardRectTransform.anchoredPosition = startPosition;
        canvasGroup.alpha = 1f;
        canDrag = true;
        isDragging = false;
    }

    public void SetCardEnabled(bool enabled)
    {
        cardImage.raycastTarget = enabled;
    }
}