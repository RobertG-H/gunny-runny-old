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
        CapsuleCollider ECB;

        [SerializeField]
        Transform localBasisVectors;

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

        #region Raycasting Properties
        private float SKIN_WIDTH = 0.02f;
        private float MINIMUM_MOVE_THRESHOLD = 0.001f;
        #endregion

        #region Local state
        private bool braking;
        private float charge;
        public float chargeRate, maxCharge;
        private bool boosting;
        private IEnumerator boostTimer;

        [ReadOnly, SerializeField]
        private Vector3 velocity = Vector3.zero;
        #endregion

        #region Raycasting and Collision Variables
        public RaycastOrigins raycastOrigins;

        //From perspective of looking FROM the player's perspective
        public struct RaycastOrigins
        {
            public Vector3 center;
        }
        #endregion
        private int decimals = 8;

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
            Turn(p.iHorz);
            ApplyMovement(displacement);
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
                Vector3 deltaVel = localBasisVectors.forward * accelToUse * Time.fixedDeltaTime;
                velocity += deltaVel;

                Vector3 xzVel = new Vector3(velocity.x, 0, velocity.z);
                if (xzVel.magnitude > topSpeedToUse)
                {
                    velocity = xzVel.normalized * topSpeedToUse + new Vector3(0, velocity.y, 0);
                }
            }
        }
        private void ApplyMovement(Vector3 displacement)
        {
            UpdateRaycastOrigins();
            displacement = HorizontalCollisions(displacement);
            // Debug.Log(string.Format("Moving: {0}", displacement.z));
            gameObject.transform.position += new Vector3((float)System.Math.Round(displacement.x, decimals),
            (float)System.Math.Round(displacement.y, decimals),
            (float)System.Math.Round(displacement.z, decimals));
        }

        #region Raycasting

        void UpdateRaycastOrigins()
        {
            Bounds bounds = ECB.bounds;
            raycastOrigins.center = bounds.center;
        }

        Vector3 HorizontalCollisions(Vector3 displacement)
        {
            // check in 3 directions
            // Move the smallest distance
            Vector3 newDisplacement;
            Vector3[] hitNormals = new Vector3[3];
            if (CheckRaycast(raycastOrigins.center, displacement.normalized, displacement.magnitude, out newDisplacement, out hitNormals[0], 0))
            {
                if (newDisplacement.magnitude < displacement.magnitude)
                    displacement = newDisplacement;
            }

            float theta = 45;
            Vector3 newDirection = Quaternion.AngleAxis(theta, localBasisVectors.up) * displacement.normalized;
            float newMag = displacement.magnitude * Mathf.Cos(Mathf.Deg2Rad * theta);
            if (CheckRaycast(raycastOrigins.center, newDirection, newMag, out newDisplacement, out hitNormals[0], theta))
            {
                if (newDisplacement.magnitude < displacement.magnitude)
                    displacement = newDisplacement;
            }

            theta = -45;
            newDirection = Quaternion.AngleAxis(theta, localBasisVectors.up) * displacement.normalized;
            newMag = displacement.magnitude * Mathf.Cos(Mathf.Deg2Rad * theta);
            if (CheckRaycast(raycastOrigins.center, newDirection, newMag, out newDisplacement, out hitNormals[0], theta))
            {
                if (newDisplacement.magnitude < displacement.magnitude)
                    displacement = newDisplacement;
            }

            // Check sliding based on normals of all detected collisions
            for (int i = 0; i < hitNormals.Length; i++)
            {
                // Remove all normal to the wall components of newDisplacement
                displacement = displacement - Vector3.Project(displacement, hitNormals[i]);
                Debug.DrawRay(ECB.bounds.center, displacement, Color.blue);
            }



            return displacement;
        }

        bool CheckRaycast(Vector3 rayOrigin, Vector3 rayDir, float rayMag, out Vector3 newDisplacement, out Vector3 hitNormal, float theta = 0)
        {
            Vector3 displacement = rayDir * rayMag;
            rayMag += SKIN_WIDTH;
            rayMag += ECB.radius;
            RaycastHit hit;
            if (Physics.Raycast(rayOrigin, rayDir, out hit, rayMag, LayerMaskConfig.DEFAULT))
            {
                hitNormal = hit.normal;
                Debug.DrawRay(rayOrigin, rayDir * rayMag, Color.red);
                //Debug.Log(string.Format("Hit distance: {0} and displacement: {1} new movement: {2}", hit.distance, rayDir * rayMag, rayDir * rayMag - Vector3.Project(rayDir * rayMag, hit.normal)));

                // Player is very close to the wall
                if (hit.distance - SKIN_WIDTH - ECB.radius < MINIMUM_MOVE_THRESHOLD)
                {
                    newDisplacement = Vector3.zero;
                    return true;
                }

                // Find the component of the ray in the displacement vector. 
                float directionalDistance = (hit.distance - SKIN_WIDTH - ECB.radius);
                newDisplacement = (directionalDistance / Mathf.Cos(Mathf.Deg2Rad * theta)) * rayDir;

                //applyingSliding

                return true;
            }

            Debug.DrawRay(rayOrigin, rayDir * rayMag, Color.green);

            newDisplacement = displacement;
            hitNormal = Vector3.zero;
            return false;
        }

        #endregion

        public void Turn(float iHorz)
        {
            if (Mathf.Abs(iHorz) > 0)
            {
                Vector3 turnDirection = Mathf.Sign(iHorz) * localBasisVectors.right;
                Vector3 rotatedFacingVec = Vector3.RotateTowards(localBasisVectors.forward, turnDirection, turnRate * Time.fixedDeltaTime * Mathf.Abs(iHorz), 1);


                // Debug.DrawLine(transform.position, transform.position + localBasisVectors.forward, Color.red);
                // Debug.DrawLine(transform.position, transform.position + turnDirection, Color.blue);
                // Debug.DrawLine(transform.position, transform.position + rotatedFacingVec, Color.yellow);

                localBasisVectors.rotation = Quaternion.LookRotation(rotatedFacingVec, localBasisVectors.up);

                Vector3 rotatedVelocityVec = Vector3.RotateTowards(velocity, localBasisVectors.forward, velocityTurnRate * Time.fixedDeltaTime, 1);
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