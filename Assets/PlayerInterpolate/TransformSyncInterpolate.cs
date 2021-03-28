using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class TransformSyncInterpolate : NetworkBehaviour
{
    [SyncVar]
    private Vector3 syncPos;

    [SyncVar]
    private float syncYRot;

    private Vector3 lastPos;
    private Quaternion lastRot;
    private Transform myTransform;
    [SerializeField]
    private float lerpRate = 10;
    [SerializeField]
    private float posThreshold = 0.5f;
    [SerializeField]
    private float rotThreshold = 5;

    // Use this for initialization
    void Start()
    {
        myTransform = transform;
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        TransmitMotion();
        LerpMotion();
    }

    [Command]
    void Cmd_ProvidePositionToServer(Vector3 pos, float rot)
    {
        syncPos = pos;
        syncYRot = rot;
    }

    [ClientCallback]
    void TransmitMotion()
    {
        if (hasAuthority)
        {
            if (Vector3.Distance(myTransform.position, lastPos) > posThreshold || Quaternion.Angle(myTransform.rotation, lastRot) > rotThreshold)
            {
                Cmd_ProvidePositionToServer(myTransform.position, myTransform.localEulerAngles.y);

                lastPos = myTransform.position;
                lastRot = myTransform.rotation;
            }
        }
    }

    void LerpMotion()
    {
        if (!hasAuthority)
        {
            myTransform.position = Vector3.Lerp(myTransform.transform.position, syncPos, Time.deltaTime * lerpRate);

            Vector3 newRot = new Vector3(0, syncYRot, 0);
            myTransform.rotation = Quaternion.Lerp(myTransform.rotation, Quaternion.Euler(newRot), Time.deltaTime * lerpRate);
        }
    }

}
