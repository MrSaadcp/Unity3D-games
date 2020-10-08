﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Character : MonoBehaviour
{
    private CharacterController characterController;

    [Tooltip("Horizontal XZ plane speed multiplier")] public float speed = 8f;
    [Tooltip("Smoothing for rotating the character to their movement direction")] public float turnSmoothTime = 0.2f;
    [Tooltip("General multiplier for gravity (affects jump and freefall)")] public float gravityMultiplier = 5f;
    [Tooltip("The initial upwards push when pressing jump. This is injected into verticalMovement, and gradually cancelled by gravity")] public float initialJumpForce = 10f;
    [Tooltip("How long can the player hold the jump button")] public float jumpInputDuration = .4f;
    [Tooltip("Represents how fast gravityContributionMultiplier will go back to 1f. The higher, the faster")] public float gravityComebackMultiplier = 15f;
    [Tooltip("The maximum speed reached when falling (in units/frame)")] public float maxFallSpeed = 50f;
    [Tooltip("Each frame while jumping, gravity will be multiplied by this amount in an attempt to 'cancel it' (= jump higher)")] public float gravityDivider = .6f;
    [Tooltip("Starting vertical movement when falling from a platform")] public float fallingVerticalMovement = -5f;
    [Tooltip("Friction that will be applied when there is a slide movement")] public float slideFriction = .25f;

    private float gravityContributionMultiplier = 0f; //The factor which determines how much gravity is affecting verticalMovement
    private bool isJumping = false; //If true, a jump is in effect and the player is holding the jump button
    private float jumpBeginTime = -Mathf.Infinity; //Time of the last jump
    private float turnSmoothSpeed; //Used by Mathf.SmoothDampAngle to smoothly rotate the character to their movement direction
    private float verticalMovement = 0f; //Represents how much a player will move vertically in a frame. Affected by gravity * gravityContributionMultiplier
    private Vector3 inputVector; //Initial input horizontal movement (y == 0f)
    private Vector3 movementVector; //Final movement vector
    private bool isUnderneathStable = false; //If true, it is stable enough to be able to jump and to not slide if there is no input

    private const float ROTATION_TRESHOLD = .02f; // Used to prevent NaN result causing rotation in a non direction

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
    }

    private void Update()
    {
        //Raises the multiplier to how much gravity will affect vertical movement when in mid-air
        //This is 0f at the beginning of a jump and will raise to maximum 1f
        if (!characterController.isGrounded)
        {
            gravityContributionMultiplier += Time.deltaTime * gravityComebackMultiplier;
        }

        //Reduce the influence of the gravity while holding the Jump button
        if (isJumping)
        {
            //The player can only hold the Jump button for so long
            if (Time.time >= jumpBeginTime + jumpInputDuration)
            {
                isJumping = false;
                gravityContributionMultiplier = 1f; //Gravity influence is reset to full effect
            }
            else
            {
                gravityContributionMultiplier *= gravityDivider; //Reduce the gravity effect
            }
        }

        //Calculate the final verticalMovement
        if (!characterController.isGrounded)
        {
            //Less control in mid-air, conserving momentum from previous frame
            movementVector = inputVector * speed;

            //The character is either jumping or in freefall, so gravity will add up
            gravityContributionMultiplier = Mathf.Clamp01(gravityContributionMultiplier);
            verticalMovement += Physics.gravity.y * gravityMultiplier * Time.deltaTime * gravityContributionMultiplier; //Add gravity contribution
                                                                                                                        //Note that even if it's added, the above value is negative due to Physics.gravity.y

            //Cap the maximum so the player doesn't reach incredible speeds when freefalling from high positions
            verticalMovement = Mathf.Clamp(verticalMovement, -maxFallSpeed, 100f);
        }
        else
        {
            //Full speed ground movement
            movementVector = inputVector * speed;

            //Resets the verticalMovement while on the ground,
            //so that regardless of whether the player landed from a high fall or not,
            //if they drop off a platform they will always start with the same verticalMovement.
            //-5f is a good value to make it so the player also sticks to uneven terrain/bumps without floating.
            if (!isJumping)
            {
                verticalMovement = fallingVerticalMovement;
                gravityContributionMultiplier = 0f;
            }
        }

        //Apply the result and move the character in space
        movementVector.y = verticalMovement;
        characterController.Move(movementVector * Time.deltaTime);

        //Check if underneath is stable and slide accordingly
        CheckUnderneathStabilityAndSlide();

        //Rotate to the movement direction
        movementVector.y = 0f;
        if (movementVector.sqrMagnitude >= ROTATION_TRESHOLD)
        {
            float targetRotation = Mathf.Atan2(movementVector.x, movementVector.z) * Mathf.Rad2Deg;
            transform.eulerAngles = Vector3.up * Mathf.SmoothDampAngle(
                transform.eulerAngles.y,
                targetRotation,
                ref turnSmoothSpeed,
                turnSmoothTime);
        }
    }
    
    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        bool isMovingUpwards = verticalMovement > 0f;

        if (isMovingUpwards)
        {
            // Making sure the collision is near the top of the head
            float permittedDistance = characterController.radius / 2f;
            float topPositionY = transform.position.y + characterController.height;
            float distance = Mathf.Abs(hit.point.y - topPositionY);

            if (distance <= permittedDistance)
            {
                // Stopping any upwards movement
                // and having the player fall back down

                isJumping = false;
                gravityContributionMultiplier = 1f;

                verticalMovement = 0f;
            }
        }
    }

    private void CheckUnderneathStabilityAndSlide()
    {
        isUnderneathStable = true;

        if (characterController.isGrounded) {
            //Perform multiple downward spherecast to check underneath the character every slope
            Ray ray = new Ray(transform.position + characterController.center, Vector3.down);
            var hits = Physics.SphereCastAll(ray, characterController.radius, characterController.height / 2 + characterController.skinWidth);
            Vector3 underneathAllSlopesDirection = Vector3.zero;
            foreach (var hit in hits) { 
                float underneathSlopeAngle = Vector3.Angle(transform.up, hit.normal);
                if (underneathSlopeAngle > characterController.slopeLimit) {
                    //Add a slide movement along the underneath slope direction
                    underneathAllSlopesDirection += Vector3.Cross(hit.normal, Vector3.Cross(hit.normal, Vector3.up)).normalized;
                }
            }

            if (underneathAllSlopesDirection != Vector3.zero) {
                //Project the gravity on slopes direction
                //The character will fall faster if the total slopes angle is steep
                float gravityMagnitude = Vector3.Dot(-Physics.gravity.y * gravityMultiplier * Vector3.down, underneathAllSlopesDirection);
                //Move along every unstable slopes direction
                characterController.Move(underneathAllSlopesDirection * gravityMagnitude * slideFriction * Time.deltaTime);
                isUnderneathStable = false;
            }
        }
    }

    //---- COMMANDS ISSUED BY OTHER SCRIPTS ----

    public void Move(Vector3 movement)
    {
        inputVector = movement;
    }

    public void Jump()
    {
        if (characterController.isGrounded && isUnderneathStable)
        {
            isJumping = true;
            jumpBeginTime = Time.time;
            verticalMovement = initialJumpForce; //This is the only place where verticalMovement is set to a positive value
            gravityContributionMultiplier = 0f;
        }
    }

    public void CancelJump()
    {
        isJumping = false; //This will stop the reduction to the gravity, which will then quickly pull down the character
    }
}