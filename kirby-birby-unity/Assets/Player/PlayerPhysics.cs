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
            DEFAULT += 1 << LayerMask.NameToLayer("Projectile");
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
        private float MINIMUM_MOVE_THRESHOLD = 0.01f;
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
            Debug.DrawRay(raycastOrigins.center, displacement, Color.blue);
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
            Vector3 originalDisplacement = displacement;
            CheckRaycast(raycastOrigins.center, displacement.normalized, displacement.magnitude, ref displacement, 0);

            Debug.DrawRay(raycastOrigins.center, displacement, Color.yellow);

            float theta = 45;
            Vector3 newDirection = Quaternion.AngleAxis(theta, localBasisVectors.up) * originalDisplacement.normalized;
            float newMag = displacement.magnitude * Mathf.Cos(Mathf.Deg2Rad * theta);
            CheckRaycast(raycastOrigins.center, newDirection, newMag, ref displacement, theta);

            Debug.DrawRay(raycastOrigins.center, displacement, Color.yellow);

            theta = -45;
            newDirection = Quaternion.AngleAxis(theta, localBasisVectors.up) * originalDisplacement.normalized;
            newMag = displacement.magnitude * Mathf.Cos(Mathf.Deg2Rad * theta);
            CheckRaycast(raycastOrigins.center, newDirection, newMag, ref displacement, theta);

            theta = 75;
            newDirection = Quaternion.AngleAxis(theta, localBasisVectors.up) * originalDisplacement.normalized;
            newMag = displacement.magnitude * Mathf.Cos(Mathf.Deg2Rad * theta);
            CheckRaycast(raycastOrigins.center, newDirection, newMag, ref displacement, theta);

            Debug.DrawRay(raycastOrigins.center, displacement, Color.yellow);

            theta = -75;
            newDirection = Quaternion.AngleAxis(theta, localBasisVectors.up) * originalDisplacement.normalized;
            newMag = displacement.magnitude * Mathf.Cos(Mathf.Deg2Rad * theta);
            CheckRaycast(raycastOrigins.center, newDirection, newMag, ref displacement, theta);

            theta = 25;
            newDirection = Quaternion.AngleAxis(theta, localBasisVectors.up) * originalDisplacement.normalized;
            newMag = displacement.magnitude * Mathf.Cos(Mathf.Deg2Rad * theta);
            CheckRaycast(raycastOrigins.center, newDirection, newMag, ref displacement, theta);

            Debug.DrawRay(raycastOrigins.center, displacement, Color.yellow);

            theta = -25;
            newDirection = Quaternion.AngleAxis(theta, localBasisVectors.up) * originalDisplacement.normalized;
            newMag = displacement.magnitude * Mathf.Cos(Mathf.Deg2Rad * theta);
            CheckRaycast(raycastOrigins.center, newDirection, newMag, ref displacement, theta);

            if (displacement.magnitude < MINIMUM_MOVE_THRESHOLD)
                return Vector3.zero;
            return displacement;
        }

        void CheckRaycast(Vector3 rayOrigin, Vector3 rayDir, float rayMag, ref Vector3 displacement, float theta = 0)
        {
            rayMag += SKIN_WIDTH;
            rayMag += ECB.radius;
            RaycastHit hit;
            if (Physics.Raycast(rayOrigin, rayDir, out hit, rayMag, LayerMaskConfig.DEFAULT))
            {
                Debug.DrawRay(rayOrigin, rayDir * rayMag, Color.red);
                //Debug.Log(string.Format("Hit distance: {0} and displacement: {1} new movement: {2}", hit.distance, rayDir * rayMag, rayDir * rayMag - Vector3.Project(rayDir * rayMag, hit.normal)));

                // Calculate the how much of the displacement vector is in the wall.
                float colliderToWall = hit.distance - SKIN_WIDTH - ECB.radius;
                float colliderToWall_DisplacementComponent = (colliderToWall / Mathf.Cos(Mathf.Deg2Rad * theta));
                float displacementInTheWall = displacement.magnitude - colliderToWall_DisplacementComponent;

                // Remove how much the player would move into the wall.
                Vector3 inWallMovement = Vector3.Project(displacementInTheWall * rayDir, hit.normal);
                // displacement -= inWallMovement;
                RemoveVectorComponent(ref displacement, inWallMovement);
            }
            else
            {
                Debug.DrawRay(rayOrigin, rayDir * rayMag, Color.green);
            }
        }

        void RemoveVectorComponent(ref Vector3 target, Vector3 componentToRemove)
        {
            if (Mathf.Abs(target.x - componentToRemove.x) < Mathf.Abs(target.x))
            {
                target.x -= componentToRemove.x;
            }
            if (Mathf.Abs(target.y - componentToRemove.y) < Mathf.Abs(target.y))
            {
                target.y -= componentToRemove.y;
            }
            if (Mathf.Abs(target.z - componentToRemove.z) < Mathf.Abs(target.z))
            {
                target.z -= componentToRemove.z;
            }
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