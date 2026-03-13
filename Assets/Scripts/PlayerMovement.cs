using UnityEngine;
using System;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    public float horizontalSpeed = 12f;
    public float acceleration = 8f;
    public float deceleration = 4f;

    [Header("Jumping & Gravity")]
    public float jumpForce = 8f;
    public float gravity = -20f;

    [Header("Track Boundaries")]
    public float xBoundary = 9f;

    [Header("Obstacle Detection")]
    public float detectionRadius = 0.6f;
    public LayerMask obstacleLayer;

    [Header("Ground Detection")]
    public float groundCheckDistance = 0.2f;
    public LayerMask groundLayer;

    [Header("Camera")]
    public Transform mainCamera;
    public Vector3 cameraOffset = new Vector3(0f, 4f, -7f);
    public float cameraSmoothness = 10f;
    [Range(0f, 1f)]
    public float cameraXFollow = 0.85f;

    public static event Action OnPlayerHit;

    private float xVelocity;
    private float yVelocity;
    private float groundedBuffer = 0f;
    private const float groundedGrace = 0.1f;
    private CharacterController controller;
    private bool useGyro = false;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        useGyro = SystemInfo.supportsGyroscope;
        if (useGyro) Input.gyro.enabled = true;
    }

    void Update()
    {
        // Movement input
        float input = useGyro ? Input.gyro.gravity.x : Input.GetAxisRaw("Horizontal");

        if (Mathf.Abs(input) > 0.01f)
            xVelocity = Mathf.MoveTowards(xVelocity, input * horizontalSpeed, acceleration * Time.deltaTime);
        else
            xVelocity = Mathf.MoveTowards(xVelocity, 0f, deceleration * Time.deltaTime);

        // Ground check via raycast — more reliable than controller.isGrounded
        bool isGrounded = Physics.Raycast(
            transform.position,
            Vector3.down,
            controller.height / 2f + groundCheckDistance,
            groundLayer);

        if (isGrounded && yVelocity < 0f)
        {
            yVelocity = -1f;
            groundedBuffer = groundedGrace;
        }
        else if (!isGrounded)
        {
            groundedBuffer -= Time.deltaTime;
        }

        // Jump
        bool jumpPressed;
        if (useGyro)
            jumpPressed = Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began;
        else
            jumpPressed = Input.GetButtonDown("Jump") || Input.GetKeyDown(KeyCode.W);

        if (jumpPressed && groundedBuffer > 0f)
        {
            yVelocity = jumpForce;
            groundedBuffer = 0f;
        }

        yVelocity += gravity * Time.deltaTime;

        // Clamp X inside the delta
        float desiredX = transform.position.x + xVelocity * Time.deltaTime;
        float clampedX = Mathf.Clamp(desiredX, -xBoundary, xBoundary);
        float xDelta = clampedX - transform.position.x;

        // Only correct Z if it has drifted meaningfully
        float zDelta = 0f;
        if (Mathf.Abs(transform.position.z) > 0.01f)
            zDelta = -transform.position.z;

        controller.Move(new Vector3(xDelta, yVelocity * Time.deltaTime, zDelta));

        // Hit detection
        Collider[] hits = Physics.OverlapSphere(transform.position, detectionRadius, obstacleLayer);
        foreach (Collider col in hits)
        {
            if (col.name != "WallSide")
            {
                OnPlayerHit?.Invoke();
                break;
            }
        }
    }

    void LateUpdate()
    {
        if (mainCamera == null) return;
        Vector3 target = new Vector3(
            transform.position.x * cameraXFollow,
            transform.position.y + cameraOffset.y,
            cameraOffset.z);
        mainCamera.position = Vector3.Lerp(mainCamera.position, target, cameraSmoothness * Time.deltaTime);
    }
}
