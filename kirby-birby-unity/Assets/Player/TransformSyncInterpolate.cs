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
    [SerializeField] Transform localBasisVectors;
    [SyncVar] private Vector3 syncPos;
    [SyncVar] private float syncYRot;
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
        lastRot = localBasisVectors.rotation;
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
        if (Vector3.Distance(transform.position, lastPos) > posThreshold || Quaternion.Angle(localBasisVectors.rotation, lastRot) > rotThreshold)
        {
            syncPos = transform.position;
            syncYRot = localBasisVectors.localEulerAngles.y;
            RpcApplyMotion();
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

    void UpdateVelocity()
    {
        Vector3 newVelocity = (transform.position - lastPos) / Time.fixedDeltaTime;
        currentVelocity = Vector3.Lerp(currentVelocity, newVelocity, 0.2f);
    }

    void CalcDeadReckoning()
    {
        futurePos = syncPos + (currentVelocity * Time.fixedDeltaTime);
    }

    void LerpPosition()
    {
        lastPos = transform.position;
        transform.position = Vector3.Lerp(transform.position, futurePos, Time.fixedDeltaTime * lerpRate);
    }

    void LerpRotation()
    {
        lastRot = localBasisVectors.rotation;
        Vector3 newRot = new Vector3(0, syncYRot, 0);
        localBasisVectors.rotation = Quaternion.Lerp(localBasisVectors.rotation, Quaternion.Euler(newRot), Time.fixedDeltaTime * lerpRate);
    }

    public Vector3 GetVelocity()
    {
        return currentVelocity;
    }

}
