using UnityEngine;
using Unity.Netcode;
public class NetworkPlayerController : NetworkBehaviour
{
    public float moveSpeed;
    public float gravity;
    public float groundGravity;

    private CharacterController characterController;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
    }

    private void Update()
    {
        if (!IsOwner)
        {
            return;
        }

        float horizontalInput = Input.GetAxisRaw("Horizontal");
        float verticalInput = Input.GetAxisRaw("Vertical");

        Vector2 movementInput = new Vector2(horizontalInput, verticalInput);

    }

}
