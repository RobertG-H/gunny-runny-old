using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class PlayerTargetter : NetworkBehaviour
{
    [SerializeField] float searchRadius;
    [SyncVar, SerializeField] Vector3 targetPlayerPos;
    bool isActive = false;

    [SyncVar] bool foundPlayer = false;
    Transform targetPlayer;

    void Start()
    {
        if (!isServer) return;
        StartSearching();
    }

    void LateUpdate()
    {
        if (!isServer) return;
        if (targetPlayer) targetPlayerPos = targetPlayer.position;
    }

    void StartSearching()
    {
        if (isActive) return;
        isActive = true;
        foundPlayer = false;
        InvokeRepeating("SearchForPlayer", 0f, 0.3f);
    }

    void SearchForPlayer()
    {
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, searchRadius);

        foreach (var hitCollider in hitColliders)
        {
            if (hitCollider.CompareTag("Player"))
            {
                // Debug.Log(string.Format("{0} found player: {1}", gameObject.ToString(), hitCollider.gameObject.ToString()));
                if (hitCollider.gameObject == this.gameObject) return;
                foundPlayer = true;
                targetPlayer = hitCollider.transform;
            }
        }
    }

    public Vector3 GetPlayerPosition()
    {
        if (!IsTargetValid()) return Vector3.zero;
        return targetPlayerPos;
    }

    bool IsTargetValid()
    {
        if (!foundPlayer) return false;
        Vector3 toTarget = (targetPlayerPos - transform.position);

        if (toTarget.magnitude > searchRadius) return false;
        // if (Vector3.Dot(toTarget, transform.forward) < 0) return false;

        return true;
    }
}
