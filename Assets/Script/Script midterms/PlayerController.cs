using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Owner-only movement + aiming + shoot input for the 3D, angled-camera version.
/// WASD moves on the ground (X/Z plane), the mouse is raycast onto the ground to aim.
/// Position is owner-authoritative (NetworkTransform with Authority Mode = Owner).
/// Uses the legacy Input class: set Project Settings > Player > Active Input Handling to "Both".
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class PlayerController : NetworkBehaviour
{
    [SerializeField] float moveSpeed = 6f;

    Rigidbody rb;
    PlayerStats stats;
    PlayerShooting shooting;
    Camera cam;

    Vector3 moveInput;
    float aimYaw;   // degrees around Y; 0 = facing +Z

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        stats = GetComponent<PlayerStats>();
        shooting = GetComponent<PlayerShooting>();
    }

    public override void OnNetworkSpawn()
    {
        if (IsOwner) cam = Camera.main;
    }

    bool CanAct =>
        GameManager.Instance != null &&
        GameManager.Instance.IsMatchActive.Value &&
        stats.IsAlive.Value;

    void Update()
    {
        if (!IsOwner || cam == null) return;

        if (!CanAct)
        {
            moveInput = Vector3.zero;
            return;
        }

        moveInput = new Vector3(Input.GetAxisRaw("Horizontal"), 0f, Input.GetAxisRaw("Vertical"));
        if (moveInput.sqrMagnitude > 1f) moveInput.Normalize();

        // Aim: cast a ray from the mouse onto a flat plane at the player's height
        Plane ground = new Plane(Vector3.up, new Vector3(0f, rb.position.y, 0f));
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        if (ground.Raycast(ray, out float dist))
        {
            Vector3 toMouse = ray.GetPoint(dist) - rb.position;
            toMouse.y = 0f;
            if (toMouse.sqrMagnitude > 0.04f)
                aimYaw = Mathf.Atan2(toMouse.x, toMouse.z) * Mathf.Rad2Deg;
        }

        if (Input.GetMouseButton(0))
            shooting.TryShoot(aimYaw);
    }

    void FixedUpdate()
    {
        if (!IsOwner) return;

        float speed = moveSpeed;
        if (stats.ActivePowerUp.Value == PowerUpType.SpeedBoost) speed *= 1.5f;

        Vector3 velocity = moveInput * speed;
        velocity.y = rb.linearVelocity.y;   // keep gravity
        rb.linearVelocity = velocity;

        rb.MoveRotation(Quaternion.Euler(0f, aimYaw, 0f));
    }
}