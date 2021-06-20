using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Player {
    public class GlidePointer : MonoBehaviour
    {
        public PlayerController p;
        void Start()
        {
            //TODO: Am I doing this right?
            if(!p.hasAuthority) {
                Destroy(gameObject);
            }
        }

        void Update()
        {            

        }

        public void RotatePointer(float pitch) {
            transform.localRotation = Quaternion.Euler(pitch, transform.localEulerAngles.y, transform.localEulerAngles.z);
        }
    }

}
