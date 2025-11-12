using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMotor : MonoBehaviour
{
    private CharacterController controller;
    private Vector3 playerVelocity;
    public float speed = 5f;
    public float sprintSpeed = 8f;
    public float jumpHeight = 1f;
    public float sprintStaminaCost = 40f;
    private bool isGrounded;
    private bool isSprinting;
    public float gravity = -9.8f;

    void Start()
    {
        controller = GetComponent<CharacterController>();
    }

    void Update()
    {
        isGrounded = controller.isGrounded;
    }

    public void ProcessMove(Vector2 input)
    {
        Vector3 moveDirection = transform.right * input.x + transform.forward * input.y;

        playerVelocity.y += gravity * Time.deltaTime;
        if (isGrounded && playerVelocity.y < 0)
        {
            playerVelocity.y = -2f;
        }

        var playerStats = PlayerStatSystem.Instance;
        if (isSprinting)
        {
            playerStats.ConsumeStamina(sprintStaminaCost * Time.deltaTime);

            if (playerStats.CurrentStamina.CurrentValue <= 0)
            {
                isSprinting = false;
            }
        }
        else
        {
            playerStats.AddStamina(playerStats.StaminaRecoverySpeed.CurrentValue * Time.deltaTime);
        }

        float currentSpeed = isSprinting ? sprintSpeed : speed;
        // Apply speed modifier from SafeZone if player is in one
        currentSpeed *= SafeZone.GetCurrentSpeedModifier();
        Vector3 move = moveDirection * currentSpeed + playerVelocity;
        controller.Move(move * Time.deltaTime);
    }

    public void Jump(InputAction.CallbackContext ctx)
    {
        if (isGrounded)
        {
            playerVelocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }
    }

    public void TrySprint(InputAction.CallbackContext ctx)
    {
        if (isGrounded && PlayerStatSystem.Instance.CurrentStamina.Value > 0)
        {
            isSprinting = true;
        }
        else
        {
            isSprinting = false;
        }
    }

    public void StopSprint(InputAction.CallbackContext ctx)
    {
        isSprinting = false;
    }
}

