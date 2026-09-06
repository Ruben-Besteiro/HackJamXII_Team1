using System;
using UnityEngine;
using UnityEngine.UI;

public class MenuStartButton : MonoBehaviour
{
    [SerializeField] private Button buttonReference;

    private void Start()
    {
        if (GamepadManager.Instance != null)
            GamepadManager.Instance.OnButtonAPressed += HandlePressButton;
    }

    private void OnDisable()
    {
        if (GamepadManager.Instance != null)
            GamepadManager.Instance.OnButtonAPressed -= HandlePressButton;
    }

    private void HandlePressButton(int _player)
    {
        if (buttonReference != null)
        {
            buttonReference.onClick.Invoke();
            buttonReference.onClick.RemoveAllListeners();
            Debug.Log("Llamada A");
        }
    }
}
