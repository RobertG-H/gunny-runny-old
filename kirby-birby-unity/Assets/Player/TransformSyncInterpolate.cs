using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

/// <summary>
/// Server sends pose of player to all clients
/// Players update their pose from server
/// </summary>
public class TransformSyncInterpolate : NetworkBehaviour
{
    [SyncVar]
    private Vector3 syncPos;

    [SyncVar]
    private float syncYRot;

    private Vector3 lastPos;
    private Vector3 futurePos;
    private Quaternion lastRot;

    [ReadOnly]
    [SerializeField]
    private Vector3 currentVelocity;
    [SerializeField]
    private float lerpRate = 10;
    [SerializeField]
    private float posThreshold = 0.5f;
    [SerializeField]
    private float rotThreshold = 5;

    void Start()
    {
        lastPos = transform.position;
        lastRot = transform.rotation;
    }

    void FixedUpdate()
    {
        if (isServer)
        {
            TransmitMotion();
        }
    }

    void TransmitMotion()
    {
        if (Vector3.Distance(transform.position, lastPos) > posThreshold || Quaternion.Angle(transform.rotation, lastRot) > rotThreshold)
        {
            syncPos = transform.position;
            syncYRot = transform.localEulerAngles.y;
            RpcApplyMotion();
        }
    }

    [ClientRpc]
    void RpcApplyMotion()
    {
        UpdateVelocity();
        CalcDeadReckoning();
        LerpMotion();
    }

    void UpdateVelocity()
    {
        currentVelocity = (transform.position - lastPos) / Time.fixedDeltaTime;
    }

    void CalcDeadReckoning()
    {
        futurePos = syncPos + (currentVelocity * Time.fixedDeltaTime);
    }

    void LerpMotion()
    {
        // Save previous pose first.
        lastPos = transform.position;
        lastRot = transform.rotation;

        transform.position = Vector3.Lerp(transform.position, futurePos, Time.fixedDeltaTime * lerpRate);
        Vector3 newRot = new Vector3(0, syncYRot, 0);
        transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.Euler(newRot), Time.fixedDeltaTime * lerpRate);

    }

}
