using UnityEngine;
using Unity.Netcode;
public class PlayerSpawnManager : NetworkBehaviour
{
    //stores pawn points.
    //Static = means all player objects shares this value;
    private static int nextSpawnIndex;


    //Runs when the player object spawns by NetCode
    public override void OnNetworkSpawn()
    {
        //Check if this instance is not the server
        if (!IsServer)
        {
            return;
        }
        //Find all spwnpoint
        GameObject[] spawnPointObjects = GameObject.FindGameObjectsWithTag("SpawnPoint");

        //Selects the next spawn point
        Transform selectedSpawnPoint = spawnPointObjects[nextSpawnIndex].transform;
        //gets the character controller from the payer.
        CharacterController characterController = GetComponent<CharacterController>();
        //temporary disbale the character controller
        characterController.enabled = false;    

        //moves the player to the spawn point
        transform.position = selectedSpawnPoint.position;
        characterController.enabled = true;
        nextSpawnIndex++;

        if(nextSpawnIndex >= spawnPointObjects.Length)
        {
            nextSpawnIndex = 0;
        }
    }

}
