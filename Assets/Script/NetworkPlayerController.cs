using System;
using Unity.Netcode;
using UnityEngine;

public class NetworkPlayerController : NetworkBehaviour   // Fixed class name spelling
{
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float gravity = -20f;
    [SerializeField] private float groundGravity = -2f;
    

    [SerializeField] private CharacterController characterController;
    [SerializeField] private PlayerStats stats;
    [SerializeField] private PlayerShooting shooting;
    private float verticalVelocity;
    
    private Camera cam;
    private Plane groundPlane;
    
    bool CanAct =>
        GameManager.Instance != null &&
        GameManager.Instance.IsMatchActive.Value &&
        stats.IsAlive.Value;

    private void Awake()
    {
        
    }

    private void Start()
    {
        groundPlane =  new Plane(Vector3.up, Vector3.zero);
        if(!characterController)
        {
            Debug.LogWarning("Warning: CharacterController is not set, setting to attached character controller.");
            characterController = GetComponent<CharacterController>();
        }
        if (!stats)
        {
            Debug.LogWarning("Warning: Stats is not set, setting to attached stats.");
            stats = GetComponent<PlayerStats>();
        }

        if (!shooting)
        {
            Debug.LogWarning("Warning: Shooting is not set, setting to attached component.");
            shooting = GetComponent<PlayerShooting>();
        }
        cam = Camera.main;
    }

    private void Update()
    {
        // Only the owner should read input
        if (!IsOwner || !CanAct) return;

        float horizontalInput = Input.GetAxis("Horizontal");
        float verticalInput = Input.GetAxis("Vertical");

        Vector2 movementInput = new Vector2(horizontalInput, verticalInput);
        
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);

        float aimYaw = 0;
        if (groundPlane.Raycast(ray, out var enter))
        {
            Vector3 toMouse = ray.GetPoint(enter) - transform.position;
            toMouse.y = 0f;
            if (toMouse.sqrMagnitude > 0.04f)
                aimYaw = Mathf.Atan2(toMouse.x, toMouse.z) * Mathf.Rad2Deg;
        }
        
        // if (IsServer) {
        MovePlayer(movementInput);
        RotatePlayer(aimYaw);
        if (Input.GetMouseButton(0)) 
            shooting.TryShoot(aimYaw);
        //}
        //else{
            //MovePlayerServerRpc(movementInput);
            //RotatePlayerRpc(aimYaw);
        //}
    }

    /*
    [Rpc(SendTo.Server)]
    private void MovePlayerServerRpc(Vector2 movementInput)
    {
        MovePlayer(movementInput);
    }

    [Rpc(SendTo.Server)]
    private void RotatePlayerRpc(float angle)
    {
        RotatePlayer(angle);
    }
    */

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

    private void RotatePlayer(float angle)
    {
        characterController.transform.rotation = Quaternion.AngleAxis(angle, Vector3.up);
    }
}