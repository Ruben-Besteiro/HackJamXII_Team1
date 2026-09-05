using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class GamepadCardController : MonoBehaviour
{
    [SerializeField] private CardDragger player1Dragger;
    [SerializeField] private CardDragger player2Dragger;
    
    bool isDraggingP1 = false;
    bool isDraggingP2 = false;

    private void Update()
    {
        var gamepads = Gamepad.all;

        if (gamepads.Count > 0)
        {
            Vector2 p1Stick = gamepads[0].rightStick.ReadValue();
            if (Mathf.Abs(p1Stick.x) > 0.1f)
            {
                isDraggingP1 = true;
                player1Dragger.DragWithJoysticks(p1Stick.x);
            }
            else if (isDraggingP1)
            {
                player1Dragger.EndDrag();
                isDraggingP1 = false;
            }
        }
        
        if (gamepads.Count > 1)
        {
            Vector2 p2Stick = gamepads[1].rightStick.ReadValue();
            if (Mathf.Abs(p2Stick.x) > 0.1f)
            {
                isDraggingP2 = true;
                player2Dragger.DragWithJoysticks(p2Stick.x);
            }
            else if (isDraggingP2)
            {
                player2Dragger.EndDrag();
                isDraggingP2 = false;
            }
        }
    }
}
