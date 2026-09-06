using UnityEngine;
using UnityEngine.EventSystems;


public class CardDragger : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private RectTransform _rectTransform;
    private Canvas _canvas;
    
    private Vector2 _initialPosition;
    private Quaternion _initialRotation;
    [SerializeField] private float _limitOffsetPosition = 50f;
    [SerializeField] private float _limitOffsetRotation = 8f;

    [Header("References")] 
    [SerializeField] private CardManagement cardManagement;
    [SerializeField] private RectTransform RightChoice;
    [SerializeField] private RectTransform LeftChoice;
    private float _initialYPositionChoice;
    [SerializeField] private float positionYOffset = -20f;
    [HideInInspector] public float dragFactor;

    [Header("Drag Selection")] 
    [SerializeField] private float SelectionOffset = 0.5f;
    
    private void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
        _canvas = GetComponentInParent<Canvas>();

        _initialPosition = _rectTransform.anchoredPosition;
        _initialRotation = _rectTransform.rotation;
        
        if (RightChoice != null || LeftChoice != null)
            _initialYPositionChoice = RightChoice != null ? RightChoice.anchoredPosition.y :  LeftChoice.anchoredPosition.y;
    }

    // Mientras dure la cuenta atrás inicial del RaceManager, la carta no
    // debe reaccionar a ningún input (ratón ni mando).
    private bool IsInputAllowed()
    {
        return RaceManager.Instance == null || RaceManager.Instance.RaceStarted;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        // _rectTransform.anchoredPosition = _initialPosition;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!IsInputAllowed())
            return;

        Vector2 targetPosition = _rectTransform.anchoredPosition + (eventData.delta / _canvas.scaleFactor);
        float clampedX = Mathf.Clamp(targetPosition.x, _initialPosition.x - _limitOffsetPosition,
            _initialPosition.x + _limitOffsetPosition);
        _rectTransform.anchoredPosition =
            new Vector2(Mathf.Clamp(clampedX, -_limitOffsetPosition, _limitOffsetPosition), 0f);

        float normalizedX = (clampedX - _initialPosition.x) / _limitOffsetPosition;
        ApplyDragPosition(normalizedX);
    }

    public void DragWithJoysticks(float joystick)
    {
        if (!IsInputAllowed())
            return;

        ApplyDragPosition(joystick);
    }

    private void ApplyDragPosition(float _normalizedFactor)
    {
        dragFactor = Mathf.Clamp(_normalizedFactor, -1f, 1f);
        
        float targetX = _initialPosition.x + (dragFactor * _limitOffsetPosition);
        _rectTransform.anchoredPosition = new Vector2(targetX, _initialPosition.y);
        
        float targetRotX = _initialRotation.x + (dragFactor * _limitOffsetRotation);
        _rectTransform.localRotation = Quaternion.Euler(0f, 0f, targetRotX);
        
        float rightIntensity = Mathf.Max(0f, dragFactor);
        float leftIntensity = Mathf.Max(0f, -dragFactor);
        
        if (RightChoice != null)
        {
            float targetYRight = _initialYPositionChoice + (positionYOffset * rightIntensity);
            RightChoice.anchoredPosition = new Vector2(_rectTransform.anchoredPosition.x, targetYRight);
        }

        if (LeftChoice != null)
        {
            float targetYLeft = _initialYPositionChoice + (positionYOffset * leftIntensity);
            LeftChoice.anchoredPosition = new Vector2(_rectTransform.anchoredPosition.x, targetYLeft);
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        EndDrag();
    }

    public void EndDrag()
    {
        if (!IsInputAllowed())
            return;

        if (dragFactor > SelectionOffset)
            if (cardManagement != null)
                cardManagement.SetChoice(true);
        
        if (dragFactor < -SelectionOffset)
            if (cardManagement != null)
                cardManagement.SetChoice(false);
        
        _rectTransform.anchoredPosition = _initialPosition;
        _rectTransform.rotation = _initialRotation;
        
        if (RightChoice != null) 
            RightChoice.anchoredPosition = new Vector2( _rectTransform.anchoredPosition.x, _initialYPositionChoice);
        
        if (LeftChoice != null) 
            LeftChoice.anchoredPosition = new Vector2( _rectTransform.anchoredPosition.x, _initialYPositionChoice);
    }
}
