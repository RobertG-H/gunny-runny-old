using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

namespace Player
{
    /// <summary>
    /// A spring/damper model for suspension
    /// Src: https://www.youtube.com/watch?v=x0LUiE0dxP0
    /// </summary>
    public class Suspension : MonoBehaviour
    {
        [SerializeField] Rigidbody rb;
        [SerializeField] Transform localBasisVectors;
        [SerializeField] NetworkBehaviour networkParent;
        [SerializeField] float restLength;
        [SerializeField] float springTravel;
        [SerializeField] float springStiffness;
        [SerializeField] float damperStiffness;
        float minLength;
        float maxLength;
        float lastLength;
        float springLength;
        float springVelocity;
        float springForce;
        float damperForce;

        Vector3 suspensionForce;

        [Header("Wheel")]
        [SerializeField] float wheelRadius;

        private bool isGrounded;

        void Awake()
        {
            minLength = restLength - springTravel;
            maxLength = restLength + springTravel;
        }
        void FixedUpdate()
        {
            if (!networkParent.isServer) return;
            Debug.DrawRay(transform.position, -localBasisVectors.up * (maxLength + wheelRadius), Color.yellow);
            Debug.DrawRay(transform.position, -localBasisVectors.up * (restLength + wheelRadius), Color.green);

            if (Physics.Raycast(transform.position, -localBasisVectors.up, out RaycastHit hit, maxLength + wheelRadius))
            {
                lastLength = springLength;
                springLength = hit.distance - wheelRadius;
                springLength = Mathf.Clamp(springLength, minLength, maxLength);
                springVelocity = (lastLength - springLength) / Time.fixedDeltaTime;
                springForce = springStiffness * (restLength - springLength);
                damperForce = damperStiffness * springVelocity;

                suspensionForce = (springForce + damperForce) * hit.normal;

                rb.AddForceAtPosition(suspensionForce, hit.point);
                Debug.DrawRay(transform.position, -localBasisVectors.up * (hit.distance), Color.red);
                isGrounded = true;
            }
            else 
            {
                isGrounded = false;
            }

        }

        public bool IsGrounded()
        {
            return isGrounded;
        }
    }
}

