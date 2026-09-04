using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Movimiento del jugador para una vista top-down en 3D.
/// El desplazamiento está alineado con los ejes del mundo (X/Z): WASD mueve
/// siempre en esas direcciones fijas, sin depender de la rotación de la cámara
/// ni del propio jugador.
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movimiento")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private bool useGravity = false;
    [SerializeField] private float gravity = -9.81f;

    [Header("Input Actions")]
    [SerializeField] private InputActionAsset inputActions;
    [SerializeField] private string actionMapName = "Player";
    [SerializeField] private string moveActionName = "Move";

    private CharacterController controller;
    private InputAction moveAction;
    private Vector2 moveInput;
    private float verticalVelocity;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();

        if (inputActions == null)
        {
            Debug.LogError($"[{nameof(PlayerMovement)}] No se ha asignado el Input Actions Asset.", this);
            return;
        }

        InputActionMap map = inputActions.FindActionMap(actionMapName, throwIfNotFound: true);
        moveAction = map.FindAction(moveActionName, throwIfNotFound: true);
    }

    private void OnEnable()
    {
        moveAction?.Enable();
    }

    private void OnDisable()
    {
        moveAction?.Disable();
    }

    private void Update()
    {
        if (moveAction == null) return;

        moveInput = moveAction.ReadValue<Vector2>();

        // Se mapea X -> eje X del mundo, Y -> eje Z del mundo: el movimiento
        // queda alineado con los ejes, sin diagonales "libres" fuera de esos ejes.
        Vector3 direction = new Vector3(moveInput.x, 0f, moveInput.y);

        if (useGravity)
        {
            verticalVelocity = controller.isGrounded ? -1f : verticalVelocity + gravity * Time.deltaTime;
        }
        else
        {
            verticalVelocity = 0f;
        }

        Vector3 motion = direction * moveSpeed;
        motion.y = verticalVelocity;

        controller.Move(motion * Time.deltaTime);
    }
}
