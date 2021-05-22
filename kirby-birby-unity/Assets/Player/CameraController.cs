using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Player
{
    public class CameraController : MonoBehaviour
    {
        [SerializeField] Transform playerTransfrom;
        [SerializeField] PlayerController p;
        [SerializeField] bool enableJuice = false;
        [SerializeField] float xOffsetMax = 0.3f;
        [SerializeField] float zRotOffsetMax = 2f;
        [SerializeField] bool doLagBehindTarget = false;
        [SerializeField] float followSpeed = 0.2f;
        Camera cam;
        Vector3 camVel = Vector3.zero;
        Vector3 camVelRot = Vector3.zero;
        float startFOV;
        Vector3 offsetBasePosition;
        Vector3 offsetBaseRotation;
        float xOffset = 0f;
        float zRotOffset = 0f;

        void Awake()
        {
            cam = GetComponent<Camera>();
            startFOV = cam.fieldOfView;
            offsetBasePosition = transform.localPosition;
            offsetBaseRotation = transform.localEulerAngles;
        }

        void LateUpdate()
        {
            FollowTarget();
            if (enableJuice)
            {
                HandleAngleLag();
                HandleFOV();

                Vector3 newPosition = transform.position;
                Vector3 horizontalMove = transform.right * xOffset;
                newPosition += horizontalMove;
                transform.position = newPosition;

                Vector3 newRot = transform.localRotation.eulerAngles;
                newRot.z = zRotOffset;
                this.transform.localRotation = Quaternion.Euler(newRot);
            }

        }
        void FollowTarget()
        {
            float playerXRot = playerTransfrom.eulerAngles.x;
            float playerYRot = playerTransfrom.eulerAngles.y;
            Vector3 cameraDistance = Quaternion.AngleAxis(playerYRot, Vector3.up) * offsetBasePosition;

            Vector3 desiredPosition = playerTransfrom.position + cameraDistance;
            if (doLagBehindTarget)
            {
                Vector3 newPosition = Vector3.SmoothDamp(transform.position, desiredPosition, ref camVel, followSpeed);
                transform.position = newPosition;
            }
            else
            {
                transform.position = desiredPosition;
            }

            Vector3 desiredRotation = new Vector3(playerXRot + offsetBaseRotation.x, playerYRot, offsetBaseRotation.z);
            if (doLagBehindTarget)
            {
                Vector3 newRotation = Vector3.SmoothDamp(transform.eulerAngles, desiredRotation, ref camVelRot, followSpeed);
                transform.rotation = Quaternion.Euler(newRotation);

            }
            else
            {
                transform.rotation = Quaternion.Euler(desiredRotation);
            }
        }
        void HandleAngleLag()
        {
            float turnAmount = p.iHorz;
            xOffset = Mathf.Lerp(xOffset, xOffsetMax * turnAmount, 0.1f);
            float inverseTurnAmount = turnAmount * -1;
            zRotOffset = Mathf.Lerp(zRotOffset, zRotOffsetMax * inverseTurnAmount, 0.1f);
        }

        void HandleFOV()
        {
            float speedMult = Mathf.Clamp01(p.GetSpeed() * 0.01f) + 1;
            float newFov = startFOV * speedMult;
            float fovChangeThreshold = 3f;
            if (Mathf.Abs(newFov - cam.fieldOfView) > fovChangeThreshold)
                cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, newFov, 0.02f);
        }
    }
}
