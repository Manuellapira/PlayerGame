using UnityEngine; // Standard Unity engine functions
public class TopDownCameraFollow : MonoBehaviour
{
// The distance and angle the camera maintains from the player
    [SerializeField] private Vector3 offset = new Vector3(0f, 10f, -8f);
// How smoothly the camera catches up to the player
    [SerializeField] private float followSpeed = 10f;
// The specific player the camera is currently tracking
    private Transform target;
// A public method so the player script can tell the camera who to follow
    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }
// LateUpdate runs after all standard Update methods, ensuring player movement finishes first
    private void LateUpdate()
    {
// If there is no target yet, stop reading here
        if (target == null) return;
// Calculate where the camera should be
        Vector3 desiredPosition = target.position + offset;
// Smoothly move the camera toward the desired position
        transform.position = Vector3.Lerp(transform.position, desiredPosition, followSpeed *
                                                                               Time.deltaTime);
// Ensure the camera always points directly at the player
        transform.LookAt(target.position);
    }
}