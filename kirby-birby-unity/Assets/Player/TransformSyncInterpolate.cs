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
    [SyncVar] private Vector3 syncPos;
    [SyncVar] private Quaternion syncRot;
    private Vector3 lastPos;
    private Vector3 futurePos;
    private Quaternion lastRot;
    [ReadOnly, SerializeField] private Vector3 currentVelocity;
    [SerializeField] private float lerpRate = 10;
    [SerializeField] private float posThreshold = 0.5f;
    [SerializeField] private float rotThreshold = 5;

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

    void Update()
    {
        if (isClientOnly)
        {
            ApplyMotion();
        }
    }

    void TransmitMotion()
    {
        if (Vector3.Distance(transform.position, lastPos) > posThreshold || Quaternion.Angle(transform.rotation, lastRot) > rotThreshold)
        {
            syncPos = transform.position;
            syncRot = transform.rotation;
            // RpcApplyMotion();
        }
    }

    [ClientRpc]
    void RpcApplyMotion()
    {
        UpdateVelocity();
        CalcDeadReckoning();
        LerpPosition();
        LerpRotation();
    }

    void ApplyMotion()
    {
        UpdateVelocity();
        CalcDeadReckoning();
        LerpPosition();
        LerpRotation();
    }

    void UpdateVelocity()
    {
        Vector3 newVelocity = (transform.position - lastPos) / Time.smoothDeltaTime;
        currentVelocity = Vector3.Lerp(currentVelocity, newVelocity, 0.2f);
    }

    void CalcDeadReckoning()
    {
        futurePos = syncPos + (currentVelocity * Time.smoothDeltaTime);
    }

    void LerpPosition()
    {
        lastPos = transform.position;
        transform.position = Vector3.Lerp(transform.position, futurePos, Time.smoothDeltaTime * lerpRate);
    }

    void LerpRotation()
    {
        lastRot = transform.rotation;
        transform.rotation = Quaternion.Lerp(transform.rotation, syncRot, Time.smoothDeltaTime * lerpRate);
    }

    public Vector3 GetVelocity()
    {
        return currentVelocity;
    }
}
