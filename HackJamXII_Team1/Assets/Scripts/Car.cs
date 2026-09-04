using UnityEngine;

/// <summary>
/// Mueve un icono de UI (uno de los "Square") hacia la posición de otro
/// elemento de UI (la esfera), sin salirse nunca de los límites del Canvas
/// que los contiene.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class Car : MonoBehaviour
{
    [Header("Objetivo")]
    [Tooltip("RectTransform hacia el que se desplaza este icono (la esfera).")]
    [SerializeField] private RectTransform target;

    [Header("Movimiento")]
    [SerializeField] private float speed = 150f; // píxeles por segundo

    private RectTransform rect;
    private RectTransform bounds; // el Canvas que limita el movimiento

    private void Awake()
    {
        rect = (RectTransform)transform;
        bounds = (RectTransform)rect.parent; // el Square es hijo directo del Canvas
    }

    private void Update()
    {
        if (target == null) return;

        Vector2 newPos = Vector2.MoveTowards(rect.anchoredPosition, target.anchoredPosition, speed * Time.deltaTime);
        rect.anchoredPosition = ClampToBounds(newPos);
    }

    private Vector2 ClampToBounds(Vector2 pos)
    {
        if (bounds == null) return pos;

        Vector2 halfArea = bounds.rect.size * 0.5f;
        Vector2 halfIcon = rect.rect.size * 0.5f;

        pos.x = Mathf.Clamp(pos.x, -halfArea.x + halfIcon.x, halfArea.x - halfIcon.x);
        pos.y = Mathf.Clamp(pos.y, -halfArea.y + halfIcon.y, halfArea.y - halfIcon.y);
        return pos;
    }
}