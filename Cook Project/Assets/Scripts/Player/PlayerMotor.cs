using UnityEngine;

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

        if (isSprinting)
        {
            PlayerStatSystem.Instance.CurrentStamina.Value -= sprintStaminaCost * Time.deltaTime;
            if (PlayerStatSystem.Instance.CurrentStamina.Value <= 0)
            {
                PlayerStatSystem.Instance.CurrentStamina.Value = 0;
                isSprinting = false;
            }
        }
        else
        {
            PlayerStatSystem.Instance.CurrentStamina.Value = Mathf.Clamp(
                PlayerStatSystem.Instance.CurrentStamina.Value + PlayerStatSystem.Instance.StaminaRecoverySpeed.Value * Time.deltaTime,
                0,
                PlayerStatSystem.Instance.MaxStamina.Value);
        }

        float currentSpeed = isSprinting ? sprintSpeed : speed;
        Vector3 move = moveDirection * currentSpeed + playerVelocity;
        controller.Move(move * Time.deltaTime);
    }

    public void Jump()
    {
        if (isGrounded)
        {
            playerVelocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }
    }

    public void TrySprint()
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

    public void StopSprint()
    {
        isSprinting = false;
    }
}
