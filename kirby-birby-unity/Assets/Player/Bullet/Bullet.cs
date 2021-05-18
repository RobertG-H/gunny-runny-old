using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class Bullet : NetworkBehaviour
{
    [SerializeField] float speed;

    void FixedUpdate()
    {
        Debug.Log(isServer);
        if (!isServer) { return; }
        Vector3 displacement = transform.forward * speed * Time.fixedDeltaTime;
        transform.Translate(displacement);
    }

}
