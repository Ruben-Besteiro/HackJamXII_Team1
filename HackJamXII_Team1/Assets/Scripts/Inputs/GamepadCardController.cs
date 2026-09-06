using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class GamepadCardController : MonoBehaviour
{
    [SerializeField] private CardDragger player1Dragger;
    [SerializeField] private CardDragger player2Dragger;
    
    private void Update()
    {
        var gamepads = Gamepad.all;

        if (gamepads.Count > 0)
        {
            Vector2 p1Stick = gamepads[0].rightStick.ReadValue();
            player1Dragger.DragWithJoysticks(p1Stick.x);

            if (gamepads[0].buttonSouth.wasPressedThisFrame)
                player1Dragger.EndDrag();
        }
        
        if (gamepads.Count > 1)
        {
            Vector2 p2Stick = gamepads[1].rightStick.ReadValue();
            player2Dragger.DragWithJoysticks(p2Stick.x);
            
            if (gamepads[1].buttonSouth.wasPressedThisFrame)
                player2Dragger.EndDrag();
        }
    }
}
