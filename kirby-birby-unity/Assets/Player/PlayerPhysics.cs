using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
namespace Player
{
    public class PlayerPhysics : MonoBehaviour
    {
        public PlayerController p;
        public float acceleration;
        public float brakingAcceleration;
        public float topSpeed;
        public float turnRate;
        public float velocityTurnRate;
        public float boostDuration;
        public float boostAcceleration;
        public float boostTopSpeedMultiplier;
        public float maxStopAngle;
        public Text velocityDebugText;
        public Text facingDirDebugText;
        public Text turnDirDebugText;
        private bool braking;
        private float charge;
        public float chargeRate, maxCharge;
        private bool boosting;
        private IEnumerator boostTimer;

        [SerializeField]
        private Vector3 velocity = Vector3.zero;

        [SerializeField]
        private Vector3 facingDirection = Vector3.forward; //For debugging
        private int decimals = 4;

        void Update()
        {
            if (braking)
            {
                charge += Mathf.Min(chargeRate * Time.deltaTime, maxCharge);
            }
        }
        void FixedUpdate()
        {
            Debug.Log(velocity.magnitude);
            // Update velocity and apply
            // if (gravityIsActive) ApplyGravity();
            // if (IsGrounded()) ApplyFriction();
            // ApplyFriction();
            ApplyAcceleration();

            Vector3 displacement = velocity * Time.fixedDeltaTime;
            ApplyMovement(displacement);
            Turn(p.iHorz);

            // velocityDebugText.text = string.Format("Velocity: {0:0.00}, {1:0.00}", velocity.x, velocity.z);
            // facingDirDebugText.text = string.Format("Facing Dir: {0:0.00}, {1:0.00}", transform.forward.x, transform.forward.z);

            // if (showDebugGraphs) UpdateLogger();
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
            gameObject.transform.Translate(new Vector3((float)System.Math.Round(displacement.x, decimals),
            (float)System.Math.Round(displacement.y, decimals),
            (float)System.Math.Round(displacement.z, decimals)),
            relativeTo: Space.World);
        }
        public void Turn(float iHorz)
        {
            if (Mathf.Abs(iHorz) > 0)
            {
                Vector3 turnDirection = Mathf.Sign(iHorz) * transform.right;
                // turnDirDebugText.text = string.Format("Turn Dir: {0:0.00}, {1:0.00}", turnDirection.x, turnDirection.z);
                Vector3 rotatedFacingVec = Vector3.RotateTowards(transform.forward, turnDirection, turnRate * Time.fixedDeltaTime * Mathf.Abs(iHorz), 1);


                Debug.DrawLine(transform.position, transform.position + transform.forward, Color.red);
                Debug.DrawLine(transform.position, transform.position + turnDirection, Color.blue);
                Debug.DrawLine(transform.position, transform.position + rotatedFacingVec, Color.yellow);

                // Quaternion rotation = Quaternion.FromToRotation(transform.forward, turnDirection);
                // transform.rotation = Quaternion.RotateTowards(transform.rotation, rotation, );
                transform.rotation = Quaternion.LookRotation(rotatedFacingVec, transform.up);

                Vector3 rotatedVelocityVec = Vector3.RotateTowards(velocity, transform.forward, velocityTurnRate * Time.fixedDeltaTime, 1);
                velocity = Quaternion.FromToRotation(velocity, rotatedVelocityVec) * velocity;
            }
        }
        // public Vector2 XYZToXZ(Vector3 xyz)
        // {
        //     return new Vector2(xyz.x, xyz.z);
        // }
        // public Vector3 XZToXYZ(Vector2 xz)
        // {
        //     return new Vector3(xz.x, 0, xz.y);
        // }
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