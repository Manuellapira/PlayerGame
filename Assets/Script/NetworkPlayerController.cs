using Unity.Netcode;
using UnityEngine;

public class NetworkController : NetworkBehaviour   // Fixed class name spelling
{
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float gravity = -20f;
    [SerializeField] private float groundGravity = -2f;

    private CharacterController characterController;
    private float verticalVelocity;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
    }

    private void Update()
    {
        // Only the owner should read input
        if (!IsOwner) return;

        float horizontalInput = Input.GetAxis("Horizontal");
        float verticalInput = Input.GetAxis("Vertical");

        Vector2 movementInput = new Vector2(horizontalInput, verticalInput);

        // Host / Server can move directly
        if (IsServer)
        {
            MovePlayer(movementInput);
        }
        // Clients send the input to the server
        else
        {
            MovePlayerServerRpc(movementInput);
        }
    }

    [ServerRpc]
    private void MovePlayerServerRpc(Vector2 movementInput)
    {
        MovePlayer(movementInput);
    }

    private void MovePlayer(Vector2 movementInput)
    {
        // Gravity
        if (characterController.isGrounded && verticalVelocity < 0f)
        {
            verticalVelocity = groundGravity;
        }
        else
        {
            verticalVelocity += gravity * Time.deltaTime;
        }

        Vector3 moveDirection = new Vector3(movementInput.x, 0f, movementInput.y).normalized;
        Vector3 horizontalMovement = moveDirection * moveSpeed;
        Vector3 verticalMovement = Vector3.up * verticalVelocity;

        Vector3 finalMovement = horizontalMovement + verticalMovement;
        characterController.Move(finalMovement * Time.deltaTime);
    }
}