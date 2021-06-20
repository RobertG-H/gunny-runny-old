using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

namespace Player
{
    public class PlayerPhysics : MonoBehaviour, IReceivePowerUpPatches
    {
        public PlayerController p;
        [SerializeField] Rigidbody rb;
        [SerializeField] Transform localBasisVectors;
        [SerializeField] GlidePointer glidePointer;

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
        public float glideAcceleration;
        public float glideBreakAcceleration;
        public float maxGlideSpeed;
        public float pitchRate;
        public float GROUNDED_CHECK_DIST;
        public float MIN_PITCH, MAX_PITCH;
        public float LOW_SPEED_TURN_THRESHOLD;
        #endregion

        #region Local state
        private bool braking;
        private bool isGrounded;
        private float charge;
        public float chargeRate, maxCharge;
        private bool boosting;
        private Vector3 eulerAngles;
        private IEnumerator boostTimer;
        private float targetPitch;
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

            eulerAngles = GetEulerAngles();
            targetPitch = CalculateTargetPitch(p.iVert);
            glidePointer.RotatePointer(targetPitch);

            CheckGrounded();
            if(!isGrounded) {
                Glide(p.iHorz, p.iVert);
            }

            ApplyAcceleration(p.iHorz);
            
            LockZRotation();

            ApplyRotation();
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
            if (braking && isGrounded)
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
            if (braking && isGrounded)
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

        private void Glide(float iHorz, float iVert)
        {
            //Rotate player towards the target pitch angle\
            GlidePitch();

            if(braking) {
                rb.AddForce(Vector3.down * glideBreakAcceleration, ForceMode.Acceleration);
            }
            else {
                //Create glide vector by rotating the forward vector by the pitch to apply force in the direction of the pitch
                Vector3 glideVec = (Quaternion.AngleAxis(eulerAngles.x, transform.right) * transform.forward).normalized;
                Debug.DrawRay(transform.position, glideVec, Color.magenta);

                rb.AddForce(glideVec * glideAcceleration, ForceMode.Acceleration);
                if (rb.velocity.magnitude > maxGlideSpeed)
                {
                    rb.AddForce(rb.velocity.normalized * naturalDeceleration, ForceMode.Acceleration);
                }
            }


        }

        private void GlidePitch()
        {
            float angleX = eulerAngles.x;
            if(angleX > 180) {
                angleX -= 360;
            }

            angleX = Mathf.Lerp(angleX, targetPitch, Time.fixedDeltaTime * pitchRate);
            eulerAngles.x = Mathf.Clamp(angleX, MIN_PITCH, MAX_PITCH);
        }

        private void CheckGrounded()
        {
            isGrounded = Physics.Raycast(transform.position, Vector3.down, GROUNDED_CHECK_DIST);
            Debug.DrawRay(transform.position, Vector3.down * GROUNDED_CHECK_DIST, isGrounded ? Color.red : Color.yellow);
        }

        private void LockZRotation()
        {
            eulerAngles.z = 0;
        }
        private Vector3 GetEulerAngles()
        {
            return rb.rotation.eulerAngles;
        }

        private float CalculateTargetPitch(float iVert) 
        {
            if(braking)
                return 0;
            else 
                return Mathf.Lerp(0, iVert > 0 ? MAX_PITCH : MIN_PITCH, Mathf.Abs(iVert));    
        }

        private void ApplyRotation()
        {
            rb.rotation = Quaternion.Euler(eulerAngles);
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
        
        [Server]        
        public void Boost()
        {
            if(isGrounded) {
                boosting = true;
                if (boostTimer != null)
                    StopCoroutine(boostTimer);
                boostTimer = BoostTimer();
                StartCoroutine(boostTimer);
            }
        }
        private IEnumerator BoostTimer()
        {
            yield return new WaitForSeconds(boostDuration);
            boosting = false;
            charge = 0;
        }

        [Server]        
        public void Brake()
        {
            braking = true;
            charge = 0;
            if (boostTimer != null)
                StopCoroutine(boostTimer);
        }

        [Server]        
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

        #region PowerUp Patches

        void IReceivePowerUpPatches.TopSpeedIncrease()
        {
            Debug.Log("Got topspeed");
            topSpeed += 1;
            boostTopSpeed += 1;
            acceleration += 0.5f;
        }

        void IReceivePowerUpPatches.TurnIncrease()
        {
            if (turnRadius > 2)
                turnRadius -= 1;
        }

        void IReceivePowerUpPatches.ChargeRateIncrease()
        {
            chargeRate += 1;
        }

        void IReceivePowerUpPatches.BoostIncrease()
        {
            boostTopSpeed += 1;
            boostAcceleration += 1;
        }

        void IReceivePowerUpPatches.HPIncrease()
        {

        }

        void IReceivePowerUpPatches.DamageIncrease()
        {

        }

        #endregion

        #region Debug
        void OnGUI()
        {
            if (isDebug)
            {
                if (!p.isServer) return;
                GUIStyle bigStyle = new GUIStyle();
                bigStyle.fontSize = 12;
                bigStyle.fontStyle = FontStyle.Bold;
                GUI.Label(new Rect(10, 30, 100, 20), string.Format("Speed: {0}", rb.velocity.magnitude), bigStyle);
                GUI.Label(new Rect(10, 60, 100, 20), string.Format("Charge: {0}/{1}", charge, maxCharge), bigStyle);
                GUI.Label(new Rect(10, 90, 100, 20), string.Format("Grounded: {0}", isGrounded), bigStyle);
                GUI.Label(new Rect(10, 120, 100, 20), string.Format("Angle: {0}", eulerAngles), bigStyle);
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