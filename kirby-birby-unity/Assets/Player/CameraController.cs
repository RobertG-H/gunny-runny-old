using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Player
{
    public class CameraController : MonoBehaviour
    {
        [SerializeField] Transform playerTransfrom;
        [SerializeField] PlayerController p;
        [SerializeField] float xOffsetMax = 0.3f;
        [SerializeField] float zRotOffsetMax = 2f;
        [SerializeField] float distance = 8f;

        [SerializeField] float followSpeed = 0.2f;

        Camera cam;
        Vector3 camVel = Vector3.zero;
        Vector3 camVelRot = Vector3.zero;
        float startFOV;

        public float distanceMult = 1f;

        Vector3 offsetBasePosition;
        Vector3 offsetBaseRotation;
        float additionalY = 0f;
        float xOffset = 0f;
        float zRotOffset = 0f;
        float lastFramePlayerYangle;

        void Awake()
        {
            cam = GetComponent<Camera>();
            startFOV = cam.fieldOfView;
            offsetBasePosition = transform.localPosition;
            offsetBaseRotation = transform.localEulerAngles;
        }

        void Start()
        {
            lastFramePlayerYangle = playerTransfrom.eulerAngles.y;
        }

        void Update()
        {
            // HandleAngleLag();
            // HandleSpeedDip();
            // Vector3 newLocal = offsetBaseDirection * distance * distanceMult;
            // newLocal.y += additionalY;
            // newLocal.x = xOffset;
            // this.transform.localPosition = newLocal;

            // Vector3 newRot = transform.localRotation.eulerAngles;
            // newRot.z = zRotOffset;
            // this.transform.localRotation = Quaternion.Euler(newRot);
            // HandleFOV();
        }

        void LateUpdate()
        {
            float playerYRot = playerTransfrom.eulerAngles.y;
            Vector3 cameraDistance = Quaternion.AngleAxis(playerYRot, Vector3.up) * offsetBasePosition;

            Vector3 desiredPosition = playerTransfrom.position + cameraDistance;

            Vector3 newPosition = Vector3.SmoothDamp(transform.position, desiredPosition, ref camVel, followSpeed);


            // newPosition.x = desiredPosition.x;
            // newPosition.y = Mathf.Lerp(transform.position.y, desiredPosition.y, followSpeed);
            // newPosition.z = desiredPosition.z;

            transform.position = desiredPosition;

            Vector3 desiredRotation = new Vector3(offsetBaseRotation.x, playerYRot, offsetBaseRotation.z);
            Vector3 newRotation = Vector3.SmoothDamp(transform.eulerAngles, desiredRotation, ref camVelRot, followSpeed);
            // transform.rotation = Quaternion.Euler(newRotation);
            transform.rotation = Quaternion.Euler(desiredRotation);
        }

        void HandleAngleLag()
        {
            float turnAmount = p.iHorz;
            xOffset = Mathf.Lerp(xOffset, xOffsetMax * turnAmount, 0.1f);
            float inverseTurnAmount = turnAmount * -1;
            zRotOffset = Mathf.Lerp(zRotOffset, zRotOffsetMax * inverseTurnAmount, 0.1f);
        }

        void HandleSpeedDip()
        {
            float speedDipMult = Mathf.Clamp01(p.GetSpeed() * 0.03333f);
            Debug.Log(speedDipMult);
            speedDipMult *= speedDipMult * speedDipMult;

            distanceMult = 1f + speedDipMult * 0.3f;

            // additionalY = -speedDipMult * 4f;
        }

        void HandleFOV()
        {
            float speedMult = Mathf.Clamp01(p.GetSpeed() * 0.01f) + 1;
            cam.fieldOfView = startFOV * speedMult;
        }
    }
}
