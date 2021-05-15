using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BulletEmitter : MonoBehaviour
{
    [SerializeField] float bulletsPerSecond;
    [SerializeField] bool isActive;

    [SerializeField] GameObject bulletPrefab;

    void Start()
    {
        InvokeRepeating("FireBullet", 0f, 1f / bulletsPerSecond);
    }

    void FireBullet()
    {
        GameObject.Instantiate(bulletPrefab, transform.position, Quaternion.identity);
    }

}
