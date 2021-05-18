using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Player
{
    public class PlayerPhysicsRigidbody : MonoBehaviour
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
            Turn(p.iHorz);
            ApplyAcceleration();
        }
        private void ApplyAcceleration()
        {
            if (braking)
            {
                rb.AddForce(rb.velocity.normalized * brakingAcceleration, ForceMode.Acceleration);
                if (rb.velocity.sqrMagnitude < 0.1)
                {
                    rb.velocity = Vector3.zero;
                }
            }
            else
            {
                float accelToUse = acceleration;
                float topSpeedToUse = topSpeed;
                if (boosting)
                {
                    accelToUse = charge * boostAcceleration;
                    topSpeedToUse = boostTopSpeed;
                }
                rb.AddForce(localBasisVectors.forward * accelToUse, ForceMode.Acceleration);

                // Decel player towards target topspeed
                if (rb.velocity.magnitude > topSpeedToUse)
                {
                    rb.AddForce(rb.velocity.normalized * naturalDeceleration, ForceMode.Acceleration);
                }
                // clamp total topspeed
                rb.velocity = Vector3.ClampMagnitude(rb.velocity, boostTopSpeed);
            }
        }
        private void Turn(float iHorz)
        {
            if (Mathf.Abs(iHorz) > 0)
            {
                // When braking you have max turn ability
                if (braking)
                {
                    float angularSpeed = topSpeed / turnRadius;
                    rb.angularVelocity = localBasisVectors.up * angularSpeed * iHorz;
                }
                else
                {
                    // Cap the angular speed by the normal topspeed
                    float angularSpeed = Mathf.Min(rb.velocity.magnitude / turnRadius, topSpeed / turnRadius);
                    rb.angularVelocity = localBasisVectors.up * angularSpeed * iHorz;
                }

            }
            else
            {
                rb.angularVelocity = Vector3.zero;
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
        #region Debug
        void OnGUI()
        {

            if (isDebug)
            {
                if (!p.isServer) return;
                Rect rectPos = new Rect(400, 400, 100, 20);
                GUI.Label(rectPos, string.Format("Speed: {0}", rb.velocity.magnitude));
            }
        }
        #endregion
    }


}