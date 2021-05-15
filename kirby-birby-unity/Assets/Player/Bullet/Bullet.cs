using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Player;

public class Bullet : MonoBehaviour
{
    [SerializeField] float speed;
    void FixedUpdate()
    {
        Vector3 displacement = transform.forward * speed * Time.fixedDeltaTime;
        transform.Translate(displacement);
    }

}
