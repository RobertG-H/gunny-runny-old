using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class PlayerTurret : MonoBehaviour
{
    [SerializeField] NetworkBehaviour networkParent;
    [SerializeField] PlayerTargetter playerTargetter;
    [SerializeField] Transform localBasisVectors;
    void Awake()
    {

    }

    // Update is called once per frame
    void Update()
    {
        // if (!networkParent.isServer) return;
        LookAtPlayer();
    }

    void LookAtPlayer()
    {
        Vector3 playerPos = playerTargetter.GetPlayerPosition();
        if (playerPos == Vector3.zero)
        {
            localBasisVectors.localRotation = Quaternion.identity;
            return;
        }
        localBasisVectors.LookAt(playerPos);
    }
}
