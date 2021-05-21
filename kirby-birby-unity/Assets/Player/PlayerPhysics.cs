using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Player
{
    public class PlayerPhysics : MonoBehaviour
    {
        public PlayerController p;
        [SerializeField] Rigidbody rb;
        [SerializeField] Transform localBasisVectors;

        #region Movement properties
        [Header("Movement properties")]
        public float acceleration;
        public float brakingAcceleration;
        public float topSpeed;
        public float turnRadius;
        public float boostDuration;
        public float boostAcceleration;
        public float boostTopSpeed;
        public float naturalDeceleration;
        public float LOW_SPEED_TURN_THRESHOLD;
        #endregion

        #region Local state
        private bool braking;
        private float charge;
        public float chargeRate, maxCharge;
        private bool boosting;
        private IEnumerator boostTimer;
        #endregion

        #region Debug
        [SerializeField] bool isDebug;
        #endregion

        void Start()
        {
            if (!p.isServer)
            {
                Destroy(rb);
                return;
            }
            rb.centerOfMass = Vector3.zero;
            rb.inertiaTensorRotation = Quaternion.identity;
        }
        void Update()
        {
            if (!p.isServer) { return; }
            if (braking)
            {
                charge += chargeRate * Time.deltaTime;
                charge = Mathf.Min(charge, maxCharge);
            }
        }
        void FixedUpdate()
        {
            if (!p.isServer) { return; }
            ApplyAcceleration(p.iHorz);
            LockZRotation();
        }
        #region Private movement methods
        private void ApplyAcceleration(float iHorz)
        {
            float accelToUse = acceleration;
            float topSpeedToUse = topSpeed;
            if (boosting)
            {
                accelToUse = charge * boostAcceleration;
                topSpeedToUse = boostTopSpeed;
            }

            /* TURNING AND MOVING
            * Calculate a circle for the player to follow the path of. The radius of this circle is the "turnRadius".
            * Add a centripetal acceleration to ensure the player follows this path.
            */

            // Formula => (alpha = velocity^2 / radius)
            float centripetalAccelMag = (rb.velocity.sqrMagnitude / turnRadius) * iHorz;
            Vector3 centripetalAccel = localBasisVectors.right * centripetalAccelMag;

            // Standard forward propulsion force
            Vector3 forwardAccel = localBasisVectors.forward * accelToUse;
            if (braking)
            {
                forwardAccel = rb.velocity.normalized * brakingAcceleration;
            }

            rb.AddForce(forwardAccel + centripetalAccel, ForceMode.Acceleration);
            RotateBody(centripetalAccelMag, iHorz);

            // Decel player towards target topspeed
            if (rb.velocity.magnitude > topSpeedToUse)
            {
                rb.AddForce(rb.velocity.normalized * naturalDeceleration, ForceMode.Acceleration);
            }

            // clamp total topspeed
            rb.velocity = Vector3.ClampMagnitude(rb.velocity, boostTopSpeed);

            // Stop player when braking and at low velocity
            if (braking)
            {
                if (rb.velocity.sqrMagnitude < 0.1)
                {
                    rb.velocity = Vector3.zero;
                }
            }

        }
        /// <summary>
        /// Add angular velocity to the rigidbody to ensure the player transform rotates while it follows the 
        /// circular path given by the centripetal acceleration.
        /// </summary>
        /// <param name="centripetalAccelMag"></param>
        /// <param name="iHorz"></param>
        private void RotateBody(float centripetalAccelMag, float iHorz)
        {
            float angularSpeed = GetAngularSpeed(centripetalAccelMag);
            float maxAngularSpeed = GetMaxAngularSpeed();

            // Cap the turn rate by the normal top speed (ie. boosting doesn't make turning better)
            if (angularSpeed > maxAngularSpeed)
            {
                rb.angularVelocity = localBasisVectors.up * maxAngularSpeed * Mathf.Sign(iHorz);
            }
            else
            {
                rb.angularVelocity = localBasisVectors.up * angularSpeed * Mathf.Sign(iHorz);
            }

            // At low speeds override the normal turn rate.
            if (braking && rb.velocity.sqrMagnitude < (LOW_SPEED_TURN_THRESHOLD * LOW_SPEED_TURN_THRESHOLD))
            {
                rb.angularVelocity = localBasisVectors.up * maxAngularSpeed * iHorz;
            }
        }

        private void LockZRotation()
        {
            rb.rotation = Quaternion.Euler(rb.rotation.eulerAngles.x, rb.rotation.eulerAngles.y, 0);
        }

        private float GetMaxCentripetalAcceleration()
        {
            return ((topSpeed * topSpeed) / turnRadius);
        }

        private float GetMaxAngularSpeed()
        {
            return Mathf.Sqrt(GetMaxCentripetalAcceleration() / turnRadius);
        }

        // Formula => (alpha = omega^2 * radius)
        private float GetAngularSpeed(float centripetalAccelMag)
        {
            return Mathf.Sqrt(Mathf.Abs(centripetalAccelMag) / turnRadius);
        }

        #endregion


        #region public Ability methods
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
            charge = 0;
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

        #endregion

        #region Public Getters

        public Vector3 GetVelocity()
        {
            return rb.velocity;
        }
        #endregion

        #region Debug
        void OnGUI()
        {
            if (isDebug)
            {
                if (!p.isServer) return;
                GUIStyle bigStyle = new GUIStyle();
                bigStyle.fontSize = 24;
                bigStyle.fontStyle = FontStyle.Bold;
                GUI.Label(new Rect(800, 600, 100, 20), string.Format("Speed: {0}", rb.velocity.magnitude), bigStyle);
                GUI.Label(new Rect(10, 60, 100, 20), string.Format("Charge: {0}/{1}", charge, maxCharge), bigStyle);
            }
        }

        void OnDrawGizmos()
        {
            if (isDebug)
            {
                // Draws a sphere for the turn radius
                // Gizmos.color = Color.magenta;
                // if (Mathf.Abs(p.iHorz) > 0)
                // {
                //     Vector3 position = new Vector3(localBasisVectors.position.x, localBasisVectors.position.y, localBasisVectors.position.z);
                //     position += localBasisVectors.right * Mathf.Sign(p.iHorz) * turnRadius;
                //     Gizmos.DrawSphere(position, turnRadius);
                // }
            }

        }
        #endregion
    }


}