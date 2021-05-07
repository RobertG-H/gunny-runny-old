using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Player
{
    public static class LayerMaskConfig
    {
        public static int DEFAULT;

        public static void initConfig()
        {
            //Layers that the player should ignore
            DEFAULT = 1 << LayerMask.NameToLayer("Player");
            DEFAULT += 1 << LayerMask.NameToLayer("Ignore Raycast");
            DEFAULT = ~DEFAULT;
        }

    }

    /// <summary>
    /// Physics is SERVER-ONLY
    /// </summary>
    public class PlayerPhysics : MonoBehaviour
    {
        public PlayerController p;
        [SerializeField]
        BoxCollider ECB;

        #region Movement properties
        [Header("Movement properties")]
        public float acceleration;
        public float brakingAcceleration;
        public float topSpeed;
        public float turnRate;
        public float velocityTurnRate;
        public float boostDuration;
        public float boostAcceleration;
        public float boostTopSpeedMultiplier;
        public float maxStopAngle;
        #endregion

        #region Local state
        private bool braking;
        private float charge;
        public float chargeRate, maxCharge;
        private bool boosting;
        private IEnumerator boostTimer;

        [ReadOnly, SerializeField]
        private Vector3 velocity = Vector3.zero;

        [ReadOnly, SerializeField]
        private Vector3 facingDirection = Vector3.forward; //For debugging
        #endregion

        #region Raycasting and Collision Variables
        public RaycastOrigins raycastOrigins;

        //From perspective of looking AT the player from the front
        public struct RaycastOrigins
        {
            public Vector3 frontBottomLeft, frontBottomRight;
        }
        #endregion
        private int decimals = 4;

        void Awake()
        {
            LayerMaskConfig.initConfig();
        }

        void Update()
        {
            if (!p.isServer) { return; }
            if (braking)
            {
                charge += Mathf.Min(chargeRate * Time.deltaTime, maxCharge);
            }
        }
        void FixedUpdate()
        {
            if (!p.isServer) { return; }
            ApplyAcceleration();

            Vector3 displacement = velocity * Time.fixedDeltaTime;
            ApplyMovement(displacement);
            Turn(p.iHorz);
        }
        private void ApplyAcceleration()
        {
            if (braking)
            {
                Vector3 deltaVel = velocity.normalized * brakingAcceleration * Time.fixedDeltaTime;
                velocity += deltaVel;
            }
            else
            {
                float accelToUse = acceleration;
                float topSpeedToUse = topSpeed;
                if (boosting)
                {
                    accelToUse = charge * boostAcceleration;
                    topSpeedToUse = topSpeed * boostTopSpeedMultiplier;
                }
                Vector3 deltaVel = transform.forward * accelToUse * Time.fixedDeltaTime;
                velocity += deltaVel;

                Vector3 xzVel = new Vector3(velocity.x, 0, velocity.z);
                if (xzVel.magnitude > topSpeedToUse)
                {
                    velocity = xzVel.normalized * topSpeedToUse + new Vector3(0, velocity.y, 0);
                }
            }

            facingDirection = transform.forward;
        }
        private void ApplyMovement(Vector3 displacement)
        {
            UpdateRaycastOrigins();
            HorizontalCollisions(ref displacement);

            gameObject.transform.Translate(new Vector3((float)System.Math.Round(displacement.x, decimals),
            (float)System.Math.Round(displacement.y, decimals),
            (float)System.Math.Round(displacement.z, decimals)),
            relativeTo: Space.World);
        }

        #region Raycasting

        void UpdateRaycastOrigins()
        {

            Bounds bounds = ECB.bounds;
            Vector3[] vertices = GetColliderVertexPositions();
            raycastOrigins.frontBottomLeft = vertices[2];
            raycastOrigins.frontBottomRight = vertices[3];
        }

        /// <summary>
        /// Get box collider vertex positions in global coordinates.
        /// Order starts in the front-top-right corner
        /// Proceeds in counter-clockwise direction on the front face.
        /// Point 5 starts in the back-top-right corner
        /// Proceeds in counter-clockwise direction on the back face.
        /// </summary>
        /// <returns>Vector3 array of size 8</returns>
        Vector3[] GetColliderVertexPositions()
        {
            Vector3[] vertices = new Vector3[8];
            Vector3 size = ECB.size * 0.5f;
            Matrix4x4 thisMatrix = Matrix4x4.TRS(ECB.bounds.center, ECB.transform.localRotation, ECB.transform.localScale);
            vertices[0] = thisMatrix.MultiplyPoint3x4(new Vector3(size.x, size.y, size.z));
            vertices[1] = thisMatrix.MultiplyPoint3x4(new Vector3(-size.x, size.y, size.z));
            vertices[2] = thisMatrix.MultiplyPoint3x4(new Vector3(size.x, -size.y, size.z));
            vertices[3] = thisMatrix.MultiplyPoint3x4(new Vector3(-size.x, -size.y, size.z));
            vertices[4] = thisMatrix.MultiplyPoint3x4(new Vector3(size.x, size.y, -size.z));
            vertices[5] = thisMatrix.MultiplyPoint3x4(new Vector3(-size.x, size.y, -size.z));
            vertices[6] = thisMatrix.MultiplyPoint3x4(new Vector3(size.x, -size.y, -size.z));
            vertices[7] = thisMatrix.MultiplyPoint3x4(new Vector3(-size.x, -size.y, -size.z));

            return vertices;
        }

        // void OnDrawGizmos()
        // {
        //     Vector3[] vertices = new Vector3[8];
        //     Vector3 size = ECB.size * 0.5f;
        //     Matrix4x4 thisMatrix = Matrix4x4.TRS(ECB.bounds.center, ECB.transform.localRotation, ECB.transform.localScale);
        //     vertices[0] = thisMatrix.MultiplyPoint3x4(new Vector3(size.x, size.y, size.z));
        //     vertices[1] = thisMatrix.MultiplyPoint3x4(new Vector3(-size.x, size.y, size.z));
        //     vertices[2] = thisMatrix.MultiplyPoint3x4(new Vector3(size.x, -size.y, size.z));
        //     vertices[3] = thisMatrix.MultiplyPoint3x4(new Vector3(-size.x, -size.y, size.z));
        //     vertices[4] = thisMatrix.MultiplyPoint3x4(new Vector3(size.x, size.y, -size.z));
        //     vertices[5] = thisMatrix.MultiplyPoint3x4(new Vector3(-size.x, size.y, -size.z));
        //     vertices[6] = thisMatrix.MultiplyPoint3x4(new Vector3(size.x, -size.y, -size.z));
        //     vertices[7] = thisMatrix.MultiplyPoint3x4(new Vector3(-size.x, -size.y, -size.z));

        //     Gizmos.color = Color.green;
        //     Gizmos.DrawSphere(vertices[2], 0.03f);
        //     Gizmos.DrawLine(vertices[2], vertices[2] + velocity * Time.fixedDeltaTime);

        //     Gizmos.DrawSphere(vertices[3], 0.03f);
        //     Gizmos.DrawLine(vertices[3], vertices[3] + velocity * Time.fixedDeltaTime);
        // }

        void HorizontalCollisions(ref Vector3 displacement)
        {
            Vector3 rayOrigin = raycastOrigins.frontBottomLeft;
            Debug.DrawRay(rayOrigin, displacement, Color.red);
            RaycastHit hit;

            if (Physics.Raycast(rayOrigin, displacement.normalized, out hit, displacement.magnitude, LayerMaskConfig.DEFAULT))
            {
                Vector3 displacementToSubtract = hit.normal * displacement.magnitude;
                // displacement += displacementToSubtract;
                displacement = Vector3.zero;
                Debug.Log("L HIT");
            }
            rayOrigin = raycastOrigins.frontBottomRight;
            Debug.DrawRay(rayOrigin, displacement, Color.red);

            if (Physics.Raycast(rayOrigin, displacement.normalized, out hit, displacement.magnitude, LayerMaskConfig.DEFAULT))
            {
                Vector3 displacementToSubtract = hit.normal * displacement.magnitude;
                // displacement += displacementToSubtract;
                displacement = Vector3.zero;
                Debug.Log("R HIT");
            }
        }

        #endregion

        public void Turn(float iHorz)
        {
            if (Mathf.Abs(iHorz) > 0)
            {
                Vector3 turnDirection = Mathf.Sign(iHorz) * transform.right;
                Vector3 rotatedFacingVec = Vector3.RotateTowards(transform.forward, turnDirection, turnRate * Time.fixedDeltaTime * Mathf.Abs(iHorz), 1);


                Debug.DrawLine(transform.position, transform.position + transform.forward, Color.red);
                Debug.DrawLine(transform.position, transform.position + turnDirection, Color.blue);
                Debug.DrawLine(transform.position, transform.position + rotatedFacingVec, Color.yellow);

                transform.rotation = Quaternion.LookRotation(rotatedFacingVec, transform.up);

                Vector3 rotatedVelocityVec = Vector3.RotateTowards(velocity, transform.forward, velocityTurnRate * Time.fixedDeltaTime, 1);
                velocity = Quaternion.FromToRotation(velocity, rotatedVelocityVec) * velocity;
            }
        }
        public void Boost()
        {
            boosting = true;
            if (boostTimer != null)
                StopCoroutine(boostTimer);
            boostTimer = BoostTimer();
            StartCoroutine(boostTimer);
        }
        private IEnumerator BoostTimer()
        {
            yield return new WaitForSeconds(boostDuration);
            boosting = false;
        }
        public void Brake()
        {
            braking = true;
            charge = 0;
            if (boostTimer != null)
                StopCoroutine(boostTimer);
        }

        public void StopBraking()
        {
            braking = false;
            Boost();
        }

        public void Stop()
        {
            SetVel(new Vector3(0, velocity.y, 0));
        }
        public void AddVel(float x = 0, float y = 0, float z = 0)
        {
            velocity = new Vector3(velocity.x + x, velocity.y + y, velocity.z + z);
        }
        public void AddVel(Vector3 vel)
        {
            velocity += vel;
        }
        public void SetVel(Vector3 vel)
        {
            velocity = vel;
        }
    }
}