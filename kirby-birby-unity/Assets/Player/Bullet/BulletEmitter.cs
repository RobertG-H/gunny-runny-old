using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class BulletEmitter : NetworkBehaviour
{
    // [SerializeField ] NetworkBehaviour 
    [SerializeField] float bulletsPerSecond;
    [SerializeField] GameObject bulletPrefab;
    [SerializeField] Transform spawnLocation;

    public override void OnStartServer()
    {
        if (!isServer) return;
        InvokeRepeating("FireBullet", 0f, 1f / bulletsPerSecond);
    }

    void FireBullet()
    {
        GameObject bullet = Instantiate(bulletPrefab, spawnLocation.position, Quaternion.identity);
        NetworkServer.Spawn(bullet);

    }

}
