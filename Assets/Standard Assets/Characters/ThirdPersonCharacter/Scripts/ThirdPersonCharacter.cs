using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace UnityStandardAssets.Characters.ThirdPerson
{
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(CapsuleCollider))]
    [RequireComponent(typeof(Animator))]
    public class ThirdPersonCharacter : MonoBehaviour
    {
        [SerializeField] float m_MovingTurnSpeed = 360;
        [SerializeField] float m_StationaryTurnSpeed = 180;
        [SerializeField] float m_JumpPower = 12f;
        [UnityEngine.Range(1f, 4f)] [SerializeField] float m_GravityMultiplier = 2f;

        [SerializeField] float
            m_RunCycleLegOffset =
                0.2f; //specific to the character in sample assets, will need to be modified to work with others

        [SerializeField] float m_MoveSpeedMultiplier = 1f;
        [SerializeField] float m_AnimSpeedMultiplier = 1f;
        [SerializeField] float m_GroundCheckDistance = 0.1f;
        [SerializeField] private Transform downRayPointer;
        [SerializeField] private Transform forwardHeadRayPointer;
        [SerializeField] private Transform forwardEndClimbRayPointer;
        [SerializeField] private float forwardHeadRayDistance = 0.2f;
        [SerializeField] private float distanceFromWall = 0.1f;
        [SerializeField] private float pushAfterClimb = 0.5f; 

        private AnimatorStateInfo stateInfo;

        Rigidbody m_Rigidbody;
        Animator m_Animator;
        [SerializeField]bool m_IsGrounded;
        float m_OrigGroundCheckDistance;
        const float k_Half = 0.5f;
        float m_TurnAmount;
        float m_ForwardAmount;
        Vector3 m_GroundNormal;
        float m_CapsuleHeight;
        Vector3 m_CapsuleCenter;
        CapsuleCollider m_Capsule;
        bool m_Crouching;


        void Start()
        {
            m_Animator = GetComponent<Animator>();
            m_Rigidbody = GetComponent<Rigidbody>();
            m_Capsule = GetComponent<CapsuleCollider>();
            m_CapsuleHeight = m_Capsule.height;
            m_CapsuleCenter = m_Capsule.center;

            stateInfo = m_Animator.GetCurrentAnimatorStateInfo(0);
            m_Rigidbody.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationY |
                                      RigidbodyConstraints.FreezeRotationZ;
            m_OrigGroundCheckDistance = m_GroundCheckDistance;
        }


        public void Move(Vector3 move, bool crouch, bool jump)
        {
            // convert the world relative moveInput vector into a local-relative
            // turn amount and forward amount required to head in the desired
            // direction.
            if (move.magnitude > 1f) move.Normalize();
            move = transform.InverseTransformDirection(move);
            CheckGroundStatus();
            move = Vector3.ProjectOnPlane(move, m_GroundNormal);
            m_TurnAmount = Mathf.Atan2(move.x, move.z);
            m_ForwardAmount = move.z;

            ApplyExtraTurnRotation();

            // control and velocity handling is different when grounded and airborne:
            if (m_IsGrounded)
            {
                HandleGroundedMovement(crouch, jump);
            }
            else
            {
                HandleAirborneMovement();
            }

            ScaleCapsuleForCrouching(crouch);
            PreventStandingInLowHeadroom();

            // send input and other state parameters to the animator
            UpdateAnimator(move);
        }


        [SerializeField]bool isClimbing = false;
        [SerializeField]bool isFinalizingClimb = false;

        public void Move(Vector3 move, bool crouch, bool jump, bool climb)
        {
            //check finishing animation


            // convert the world relative moveInput vector into a local-relative
            // turn amount and forward amount required to head in the desired
            // direction.
            if (climb || isClimbing)
            {
                bool forwardheadObstacle = CheckForRayCast(forwardHeadRayPointer, forwardHeadRayDistance);
                bool forwardEndClimb = CheckForRayCast(forwardEndClimbRayPointer, forwardHeadRayDistance);
                if (!isFinalizingClimb && !stateInfo.IsName("Finalise climb") && forwardheadObstacle &&
                    !forwardEndClimb)
                {
                    AlignWithWall();
                    // Debug.Log(m_Animator.applyRootMotion);
                    isClimbing = true;
                    Debug.Log("Climb is calling");
                    m_Animator.SetTrigger("Climb_End");
                    m_Animator.applyRootMotion = true;
                    m_Rigidbody.useGravity = false;
                    isFinalizingClimb = true;
                }
                else if (!isClimbing && forwardheadObstacle)
                {
                    // Debug.Log(m_Animator.applyRootMotion);
                    AlignWithWall();
                    isFinalizingClimb = false;
                    isClimbing = true;
                    Debug.Log("Climb is calling");
                    m_Animator.SetTrigger("Climb");
                    m_Animator.applyRootMotion = true;
                    m_Rigidbody.useGravity = false;
                }


                //handle cancelation
                m_ForwardAmount = 0;
                m_TurnAmount = 0;
                // if (move.z < 0)
                // {
                //     isClimbing = false;
                // }

                UpdateAnimator(Vector3.zero);
                return;
            }

            if (move.magnitude > 1f) move.Normalize();
            move = transform.InverseTransformDirection(move);
            CheckGroundStatus();
            move = Vector3.ProjectOnPlane(move, m_GroundNormal);
            m_TurnAmount = Mathf.Atan2(move.x, move.z);
            m_ForwardAmount = move.z;

            ApplyExtraTurnRotation();

            // control and velocity handling is different when grounded and airborne:
            if (m_IsGrounded)
            {
                HandleGroundedMovement(crouch, jump);
            }
            else
            {
                HandleAirborneMovement();
            }

            ScaleCapsuleForCrouching(crouch);
            PreventStandingInLowHeadroom();

            // send input and other state parameters to the animator
            UpdateAnimator(move);
        }


        void ScaleCapsuleForCrouching(bool crouch)
        {
            if (m_IsGrounded && crouch)
            {
                if (m_Crouching) return;
                m_Capsule.height = m_Capsule.height / 2f;
                m_Capsule.center = m_Capsule.center / 2f;
                m_Crouching = true;
                // m_Rigidbody.constraints = RigidbodyConstraints.FreezePositionY | RigidbodyConstraints.FreezeRotation;
            }
            else
            {
                Ray crouchRay = new Ray(downRayPointer.position + Vector3.up * m_Capsule.radius * k_Half, Vector3.up);
                float crouchRayLength = m_CapsuleHeight - m_Capsule.radius * k_Half;
                if (Physics.SphereCast(crouchRay, m_Capsule.radius * k_Half, crouchRayLength, Physics.AllLayers,
                        QueryTriggerInteraction.Ignore))
                {
                    m_Crouching = true;
                    return;
                }

                m_Capsule.height = m_CapsuleHeight;
                m_Capsule.center = m_CapsuleCenter;
                m_Crouching = false;
                m_Rigidbody.constraints = RigidbodyConstraints.FreezeRotation;
            }
        }
        
        

        void PreventStandingInLowHeadroom()
        {
            // prevent standing up in crouch-only zones
            if (!m_Crouching)
            {
                Ray crouchRay = new Ray(m_Rigidbody.position + Vector3.up * m_Capsule.radius * k_Half, Vector3.up);
                float crouchRayLength = m_CapsuleHeight - m_Capsule.radius * k_Half;
                if (Physics.SphereCast(crouchRay, m_Capsule.radius * k_Half, crouchRayLength, Physics.AllLayers,
                        QueryTriggerInteraction.Ignore))
                {
                    m_Crouching = true;
                }
            }
        }


        void UpdateAnimator(Vector3 move)
        {
            // update the animator parameters
            m_Animator.SetFloat("Forward", m_ForwardAmount, 0.1f, Time.deltaTime);
            m_Animator.SetFloat("Turn", m_TurnAmount, 0.1f, Time.deltaTime);
            m_Animator.SetBool("Crouch", m_Crouching);
            m_Animator.SetBool("OnGround", m_IsGrounded);
            if (!m_IsGrounded)
            {
                m_Animator.SetFloat("Jump", m_Rigidbody.linearVelocity.y);
            }

            // calculate which leg is behind, so as to leave that leg trailing in the jump animation
            // (This code is reliant on the specific run cycle offset in our animations,
            // and assumes one leg passes the other at the normalized clip times of 0.0 and 0.5)
            float runCycle =
                Mathf.Repeat(
                    m_Animator.GetCurrentAnimatorStateInfo(0).normalizedTime + m_RunCycleLegOffset, 1);
            float jumpLeg = (runCycle < k_Half ? 1 : -1) * m_ForwardAmount;
            if (m_IsGrounded)
            {
                m_Animator.SetFloat("JumpLeg", jumpLeg);
            }

            // the anim speed multiplier allows the overall speed of walking/running to be tweaked in the inspector,
            // which affects the movement speed because of the root motion.
            if (m_IsGrounded && move.magnitude > 0)
            {
                m_Animator.speed = m_AnimSpeedMultiplier;
            }
            else
            {
                // don't use that while airborne
                m_Animator.speed = 1;
            }
        }


        void HandleAirborneMovement()
        {
            // apply extra gravity from multiplier:
            // Vector3 extraGravityForce = (Physics.gravity * m_GravityMultiplier) - Physics.gravity;
            // m_Rigidbody.AddForce(extraGravityForce);
            //
            m_GroundCheckDistance = m_Rigidbody.linearVelocity.y < 0 ? m_OrigGroundCheckDistance : 0.01f;
        }


        void HandleGroundedMovement(bool crouch, bool jump)
        {
            // check whether conditions are right to allow a jump:
            if (jump && !crouch && m_Animator.GetCurrentAnimatorStateInfo(0).IsName("Grounded"))
            {
                // jump!
                m_Rigidbody.linearVelocity =
                    new Vector3(m_Rigidbody.linearVelocity.x, m_JumpPower, m_Rigidbody.linearVelocity.z);
                m_IsGrounded = false;
                m_Animator.applyRootMotion = false;
                m_GroundCheckDistance = 0.1f;
            }
        }

        void ApplyExtraTurnRotation()
        {
            // help the character turn faster (this is in addition to root rotation in the animation)
            float turnSpeed = Mathf.Lerp(m_StationaryTurnSpeed, m_MovingTurnSpeed, m_ForwardAmount);
            transform.Rotate(0, m_TurnAmount * turnSpeed * Time.deltaTime, 0);
        }

        public bool checkForListRay(List<Transform> raycastPoints, float distance)
        {
            for (int i = 0; i < raycastPoints.Count; i++)
            {
                if (CheckForRayCast(raycastPoints[i], distance))
                {
                    return true;
                }
            }

            return false;
        }

        public bool CheckForRayCast(Transform objectTransform, float rayLength)
        {
            // Get the forward direction of the object
            Vector3 rayDirection = objectTransform.forward;

            // Create a ray starting from the object's position and pointing forward
            Ray ray = new Ray(objectTransform.position, rayDirection);


            // Store the hit information
            RaycastHit hit;

            // Perform the raycast
            if (Physics.Raycast(ray, out hit, rayLength))
            {
                // Check if the hit object has the "Ground" or "Building" tag
                if (hit.collider.CompareTag("Ground"))
                {
                    // Return true if the ray hits ground or building
                    Debug.DrawRay(objectTransform.position, rayDirection * rayLength, Color.green,
                        1f); // 1f is the duration the line stays visible

                    return true;
                }
            }

            // Draw the debug ray in the Scene view (visible only in the editor)
            Debug.DrawRay(objectTransform.position, rayDirection * rayLength, Color.red,
                1f); // 1f is the duration the line stays visible

            // Return false if nothing is hit or the hit object is not ground or building
            return false;
        }

        // public void OnAnimatorMove()
        // {
        // we implement this function to override the default root motion.
        // this allows us to modify the positional speed before it's applied.
        // if (!m_Animator.applyRootMotion &&m_IsGrounded && Time.deltaTime > 0)
        // {
        // 	Vector3 v = (m_Animator.deltaPosition * m_MoveSpeedMultiplier) / Time.deltaTime;
        //
        // 	// we preserve the existing y part of the current velocity.
        // 	v.y = m_Rigidbody.linearVelocity.y;
        // 	m_Rigidbody.linearVelocity = v;
        // }
        // }


        void CheckGroundStatus()
        {
            RaycastHit hitInfo;
#if UNITY_EDITOR
            // helper to visualise the ground check ray in the scene view
            Debug.DrawLine(transform.position + (Vector3.up * 0.1f),
                transform.position + (Vector3.up * 0.1f) + (Vector3.down * m_GroundCheckDistance));
#endif
            // 0.1f is a small offset to start the ray from inside the character
            // it is also good to note that the transform position in the sample assets is at the base of the character
            if (Physics.Raycast(downRayPointer.position + (Vector3.up * 0.3f), Vector3.down, out hitInfo,
                    m_GroundCheckDistance) )
            {
                m_GroundNormal = hitInfo.normal;
                m_IsGrounded = true;
                m_Animator.applyRootMotion = true;
            }
            else
            {
                m_IsGrounded = false;
                m_GroundNormal = Vector3.up;
                m_Animator.applyRootMotion = false;
            }
        }


        private void AlignWithWall()
        {
            // Cast a ray forward from the character's position
            Ray ray = new Ray(forwardHeadRayPointer.transform.position, transform.forward);
            RaycastHit hit;
            Vector3 wallNormal;


            if (Physics.Raycast(ray, out hit, forwardHeadRayDistance))
            {
                // If the ray hits a wall, align the character with the wall
                wallNormal = hit.normal;


                // Calculate the target position 0.3 meters away from the wall
                Debug.Log(hit.point);
                Vector3 targetPosition = hit.point + hit.normal * distanceFromWall;

                // Keep the current Y position to avoid vertical jumps
                targetPosition.y = transform.position.y;
                
                // Move the character to the target position
                transform.position = targetPosition;


                // Calculate the target rotation to face the wall
                Vector3 targetDirection = -wallNormal; // Face the opposite of the wall's normal
                Quaternion targetRotation = Quaternion.LookRotation(targetDirection, Vector3.up);

                // Smoothly rotate the character toward the target rotation
                transform.rotation = targetRotation;
            }
        }


        // private IEnumerator TurnOffVariable(string name , float time)
        // {
        //     yield return new WaitForSeconds(time);
        //     switch (name)
        //     {
        //         case "FinalizingClimb":
        //             isFinalizingClimb = false;
        //             isClimbing = false;
        //             break;
        //     }
        //     
        // }

        public void FinsihClimbing()
        {
            isFinalizingClimb = false;
            isClimbing = false;
            m_Rigidbody.useGravity = true;
            m_Rigidbody.Move(transform.position + transform.forward * pushAfterClimb + transform.up* pushAfterClimb,transform.rotation );
            m_Capsule.height *= 2;

        }


        public void TurnOffIsTriggerCollider()
        {
            m_Capsule.isTrigger = false;
        }

        public void TurnOnIsTriggerCollider()
        {
            m_Capsule.isTrigger = true;
            m_Capsule.height /= 2;

        }
    }
}