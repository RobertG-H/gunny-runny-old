using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Player
{


    public class PlayerJuice : MonoBehaviour
    {
        [SerializeField] PlayerController p;
        [SerializeField] Transform modelTransform;
        [SerializeField] float zRotOffsetMax = 15f;
        [SerializeField] float chargeSpeed;
        float zRotOffset = 0f;


        void Update()
        {
            LeanModel();
        }

        void LeanModel()
        {
            float inverseTurnAmount = p.iHorz * -1;
            zRotOffset = Mathf.Lerp(zRotOffset, zRotOffsetMax * inverseTurnAmount, 0.1f);

            Vector3 newRot = modelTransform.localRotation.eulerAngles;
            newRot.z = zRotOffset;
            modelTransform.localRotation = Quaternion.Euler(newRot);
        }
    }
}
