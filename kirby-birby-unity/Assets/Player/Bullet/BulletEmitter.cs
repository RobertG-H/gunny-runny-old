using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class BulletEmitter : MonoBehaviour
{
    // [SerializeField ] NetworkBehaviour networkParent;
    [SerializeField] float bulletsPerSecond;
    [SerializeField] GameObject bulletPrefab;
    [SerializeField] Transform spawnLocation;
    [SerializeField] Transform aimPoint;

    void Awake()
    {
        CustomNetworkManager.OnServerStarted += ServerStartedHandler;
    }


    public void ServerStartedHandler(CustomNetworkManager netMan)
    {
        InvokeRepeating("FireBullet", 0f, 1f / bulletsPerSecond);
    }

    void FireBullet()
    {
        Debug.Log("shooting bullet");
        GameObject bullet = Instantiate(bulletPrefab, spawnLocation.position, Quaternion.identity);
        NetworkServer.Spawn(bullet);
    }

}
