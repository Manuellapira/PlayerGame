using Unity.Netcode;
using UnityEngine;

public class LocalPlayerCameraTarget : NetworkBehaviour
{
    public override void OnNetworkSpawn()
    {
        if(IsOwner) return;
        
        TopDownCameraFollow cameraFollow = Camera.main.GetComponent<TopDownCameraFollow>();
        cameraFollow.SetTarget(transform);
    }
}
