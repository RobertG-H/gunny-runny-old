using UnityEngine;
using Mirror;

public class CustomNetworkManager : NetworkManager
{
    [SerializeField] PowerUpPatchSpawner patchSpawner;

    public override void OnStartServer()
    {
        base.OnStartServer();
        patchSpawner.StartSpawning();
    }

}