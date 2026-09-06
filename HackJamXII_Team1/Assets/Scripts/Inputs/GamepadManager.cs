using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class GamepadManager : MonoBehaviour
{
    public static GamepadManager Instance;

    public event Action<int> OnButtonAPressed;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        DontDestroyOnLoad(gameObject);
    }

    private void Update()
    {
        if (Gamepad.all.Count == 0)
            return;
        

        for (int i = 0; i < Gamepad.all.Count; i++)
        {
            if (Gamepad.all[i].buttonSouth.wasPressedThisFrame)
                OnButtonAPressed?.Invoke(i);
        }
    }
}
