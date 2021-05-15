using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProjectileCollider : MonoBehaviour
{
    [SerializeField] GameObject objectToDestroy;
    public void OnTriggerEnter(Collider other)
    {
        Debug.Log("col");
        if (other.CompareTag("Player"))
        {
            Damageable damageable = other.GetComponent<Damageable>();
            damageable.TakeDamage(1);
        }
        Destroy(objectToDestroy);
    }
}
