using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Player
{
    public class CameraController : MonoBehaviour
    {
        [SerializeField] Transform playerTransfrom;
        [SerializeField] PlayerController pcont;

        [SerializeField] float xOffsetMax = 0.3f;
        [SerializeField] float zRotOffsetMax = 2f;
        [SerializeField] float distance = 8f;

        Camera cam;
        float startFOV;

        public float distanceMult = 1f;

        Vector3 offsetBaseDirection;
        float additionalY = 0f;
        float xOffset = 0f;
        float zRotOffset = 0f;
        float lastFramePlayerYangle;

        void Awake()
        {
            cam = GetComponent<Camera>();
            startFOV = cam.fieldOfView;
        }

        void Start()
        {
            offsetBaseDirection = transform.localPosition.normalized;
            lastFramePlayerYangle = playerTransfrom.eulerAngles.y;
        }

        void Update()
        {
            HandleAngleLag();
            HandleSpeedDip();
            Vector3 newLocal = offsetBaseDirection * distance * distanceMult;
            newLocal.y += additionalY;
            newLocal.x = xOffset;
            this.transform.localPosition = newLocal;

            Vector3 newRot = transform.localRotation.eulerAngles;
            newRot.z = zRotOffset;
            this.transform.localRotation = Quaternion.Euler(newRot);
            HandleFOV();
        }

        void HandleAngleLag()
        {
            float turnAmount = pcont.iHorz;
            xOffset = Mathf.Lerp(xOffset, xOffsetMax * turnAmount, 0.1f);
            float inverseTurnAmount = turnAmount * -1;
            zRotOffset = Mathf.Lerp(zRotOffset, zRotOffsetMax * inverseTurnAmount, 0.1f);
        }

        void HandleSpeedDip()
        {
            float speedDipMult = Mathf.Clamp01(pcont.GetSpeed() * 0.03333f);
            Debug.Log(speedDipMult);
            speedDipMult *= speedDipMult * speedDipMult;

            distanceMult = 1f + speedDipMult * 0.3f;

            // additionalY = -speedDipMult * 4f;
        }

        void HandleFOV()
        {
            float speedMult = Mathf.Clamp01(pcont.GetSpeed() * 0.01f) + 1;
            cam.fieldOfView = startFOV * speedMult;
        }
    }
}
