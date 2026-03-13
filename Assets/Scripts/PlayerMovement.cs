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

    [Header("Camera")]
    public Transform mainCamera;
    public Vector3 cameraOffset = new Vector3(0f, 4f, -7f);
    public float cameraSmoothness = 10f;
    [Range(0f, 1f)]
    public float cameraXFollow = 0.85f;

    public static event Action OnPlayerHit;

    private float xVelocity;
    private float yVelocity;
    private CharacterController controller;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        Input.gyro.enabled = true;
    }

    void Update()
    {
        // Ice movement
        float input = 0f;
        if (SystemInfo.supportsGyroscope && Input.gyro.enabled)
            input = Input.gyro.gravity.x;
        else
            input = Input.GetAxisRaw("Horizontal");

        if (Mathf.Abs(input) > 0.01f)
            xVelocity = Mathf.MoveTowards(xVelocity, input * horizontalSpeed, acceleration * Time.deltaTime);
        else
            xVelocity = Mathf.MoveTowards(xVelocity, 0f, deceleration * Time.deltaTime);

        // Jump & gravity
        if (controller.isGrounded)
        {
            yVelocity = -1f;

            bool jumpPressed = SystemInfo.supportsGyroscope && Input.gyro.enabled
                ? Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began
                : Input.GetButtonDown("Jump") || Input.GetKeyDown(KeyCode.W);

            if (jumpPressed)
                yVelocity = jumpForce;
        }
        yVelocity += gravity * Time.deltaTime;

        controller.Move(new Vector3(xVelocity * Time.deltaTime, yVelocity * Time.deltaTime, 0));

        // Clamp X
        Vector3 pos = transform.position;
        pos.x = Mathf.Clamp(pos.x, -xBoundary, xBoundary);
        pos.z = 0f;
        controller.enabled = false;
        transform.position = pos;
        controller.enabled = true;

        // Hit detection — catches anything on obstacleLayer (obstacles AND wall colliders)
        if (Physics.OverlapSphere(transform.position, detectionRadius, obstacleLayer).Length > 0)
        {
            OnPlayerHit?.Invoke();
            Debug.Log("se invocó OnPlayerHit");
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