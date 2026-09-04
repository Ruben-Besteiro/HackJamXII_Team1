using System;
using UnityEngine;
using UnityEngine.EventSystems;


public class CardDragger : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private RectTransform _rectTransform;
    private Canvas _canvas;
    
    private Vector2 _initialPosition;
    [SerializeField] private float _limitOffsetPosition = 50f;

    [Header("References")] 
    [SerializeField] private CardManagement cardManagement;
    [SerializeField] private RectTransform RightChoice;
    [SerializeField] private RectTransform LeftChoice;
    private float _initialYPositionChoice;
    [SerializeField] private float positionYOffset = -20f;
    private float dragFactor;

    [Header("Drag Selection")] 
    [SerializeField] private float SelectionOffset = 0.5f;
    
    private void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
        _canvas = GetComponentInParent<Canvas>();

        _initialPosition = _rectTransform.anchoredPosition;
        
        if (RightChoice != null || LeftChoice != null)
            _initialYPositionChoice = RightChoice != null ? RightChoice.anchoredPosition.y :  LeftChoice.anchoredPosition.y;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        _rectTransform.anchoredPosition = _initialPosition;
    }

    public void OnDrag(PointerEventData eventData)
    {
        Vector2 targetPosition = _rectTransform.anchoredPosition + (eventData.delta / _canvas.scaleFactor);
        float clampedX = Mathf.Clamp(targetPosition.x, _initialPosition.x - _limitOffsetPosition,
            _initialPosition.x + _limitOffsetPosition);
        _rectTransform.anchoredPosition =
            new Vector2(Mathf.Clamp(clampedX, -_limitOffsetPosition, _limitOffsetPosition), 0f);

        dragFactor = (clampedX - _initialPosition.x) / _limitOffsetPosition;

        float rightIntensity = Mathf.Max(0f, dragFactor);
        float leftIntensity = Mathf.Max(0f, -dragFactor);

        if (RightChoice != null)
        {
            float targetYRight = _initialYPositionChoice + (positionYOffset * rightIntensity);
            RightChoice.anchoredPosition = new Vector2( _rectTransform.anchoredPosition.x, targetYRight);
        }

        if (LeftChoice != null)
        {
            float targetYLeft = _initialYPositionChoice + (positionYOffset * leftIntensity);
            LeftChoice.anchoredPosition = new Vector2( _rectTransform.anchoredPosition.x, targetYLeft);
        }
}

    public void OnEndDrag(PointerEventData eventData)
    {
        if (dragFactor > SelectionOffset)
            if (cardManagement != null)
                cardManagement.SetChoice(true);
        
        if (dragFactor < -SelectionOffset)
            if (cardManagement != null)
                cardManagement.SetChoice(false);
        
        _rectTransform.anchoredPosition = _initialPosition;
        
        if (RightChoice != null) 
            RightChoice.anchoredPosition = new Vector2( _rectTransform.anchoredPosition.x, _initialYPositionChoice);
        
        if (LeftChoice != null) 
            LeftChoice.anchoredPosition = new Vector2( _rectTransform.anchoredPosition.x, _initialYPositionChoice);
    }
}
