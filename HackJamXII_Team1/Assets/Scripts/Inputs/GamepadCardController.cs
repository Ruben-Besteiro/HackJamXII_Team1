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
            Vector2 p1Stick = gamepads[0].leftStick.ReadValue();
            player1Dragger.DragWithJoysticks(p1Stick.x);

            if (gamepads[0].buttonSouth.wasPressedThisFrame)
            {
                player1Dragger.EndDrag();
                {
                    gamepads[0].SetMotorSpeeds(0.2f, 0.8f); // DEBUG
                    gamepads[0].ResetHaptics(); // DEBUG
                }
            }
        }
        
        if (gamepads.Count > 1)
        {
            Vector2 p2Stick = gamepads[1].leftStick.ReadValue();
            player2Dragger.DragWithJoysticks(p2Stick.x);
            
            if (gamepads[1].buttonSouth.wasPressedThisFrame)
                player2Dragger.EndDrag();
        }
    }
}
