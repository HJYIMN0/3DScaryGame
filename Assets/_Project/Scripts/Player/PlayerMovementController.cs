using Unity.Cinemachine;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(PlayerInputController))]
public class PlayerMovementController : MonoBehaviour
{
    // -------------------------------------------------------------------------
    // Camera
    // -------------------------------------------------------------------------

    [Header("Camera")]
    [SerializeField] private Transform cameraRoot;
    [SerializeField] private CinemachineCamera cinemachineCamera;

    // -------------------------------------------------------------------------
    // Movimento
    // -------------------------------------------------------------------------

    [Header("Movement")]
    [SerializeField] private float walkSpeed = 4f;
    [SerializeField] private float sprintSpeed = 7f;
    [SerializeField] private float crouchSpeed = 2f;
    [SerializeField] private float jumpForce = 5f;

    // -------------------------------------------------------------------------
    // Look
    // -------------------------------------------------------------------------

    [Header("Look")]
    [SerializeField] private float sensitivityX = 0.15f;
    [SerializeField] private float sensitivityY = 0.15f;
    [SerializeField] private float topClamp = 80f;
    [SerializeField] private float bottomClamp = -80f;

    // -------------------------------------------------------------------------
    // Ground
    // -------------------------------------------------------------------------

    [Header("Ground Check")]
    [SerializeField] private LayerMask walkableLayer;
    [SerializeField] private float groundCheckRadius = 0.3f;
    [SerializeField] private float groundCheckDistance = 0.4f;

    // -------------------------------------------------------------------------
    // Crouch
    // -------------------------------------------------------------------------

    [Header("Crouch")]
    [SerializeField] private float standingHeight = 2f;
    [SerializeField] private float crouchHeight = 1f;
    [SerializeField] private float cameraStandingY = 1.6f;
    [SerializeField] private float cameraCrouchY = 0.8f;
    [SerializeField] private float crouchTransitionSpeed = 10f;

    // -------------------------------------------------------------------------
    // Stato
    // -------------------------------------------------------------------------

    private CharacterController _controller;
    private PlayerInputController _input;

    private float _cameraPitch;
    private bool _isGrounded;

    // Velocità verticale usata SOLO per il salto — SimpleMove gestisce la gravità da solo
    private float _verticalVelocity;

    public bool CanMove { get; private set; } = true;
    public bool CanLook { get; private set; } = true;

    // -------------------------------------------------------------------------
    // Unity
    // -------------------------------------------------------------------------

    private void Awake()
    {
        _controller = GetComponent<CharacterController>();
        _input = GetComponent<PlayerInputController>();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        SetupCamera();
    }

    private void Update()
    {
        UpdateGroundState();
        HandleMovement();
        HandleLook();
        HandleCrouch();
    }

    // -------------------------------------------------------------------------
    // Camera setup
    // -------------------------------------------------------------------------

    private void SetupCamera()
    {
        cameraRoot.SetParent(transform);
        cameraRoot.localPosition = new Vector3(0f, cameraStandingY, 0f);
        cameraRoot.localRotation = Quaternion.identity;

        cinemachineCamera.transform.SetParent(cameraRoot);
        cinemachineCamera.transform.localPosition = Vector3.zero;
        cinemachineCamera.transform.localRotation = Quaternion.identity;

        cinemachineCamera.Follow = null;
        cinemachineCamera.LookAt = null;

        var cinemachineComponents = cinemachineCamera.GetComponents<CinemachineComponentBase>();
        for (int i = cinemachineComponents.Length - 1; i >= 0; i--)
            Destroy(cinemachineComponents[i]);

        Camera mainCam = Camera.main;
        if (mainCam != null && mainCam.GetComponent<CinemachineBrain>() == null)
            mainCam.gameObject.AddComponent<CinemachineBrain>();
    }

    // -------------------------------------------------------------------------
    // Ground
    // -------------------------------------------------------------------------

    private void UpdateGroundState()
    {
        Vector3 origin = transform.position
                       + _controller.center
                       + Vector3.down * (_controller.height * 0.5f - groundCheckRadius);

        _isGrounded = Physics.SphereCast(
            origin, groundCheckRadius, Vector3.down,
            out _, groundCheckDistance, walkableLayer,
            QueryTriggerInteraction.Ignore);
    }

    // -------------------------------------------------------------------------
    // Movement
    // -------------------------------------------------------------------------

    private void HandleMovement()
    {
        if (!CanMove) return;

        Vector2 moveInput = _input.InputActions.Player.Move.ReadValue<Vector2>();
        bool isSprinting = _input.InputActions.Player.Sprint.IsPressed();
        bool isCrouching = _input.InputActions.Player.Crouch.IsPressed();
        bool jumpPressed = _input.InputActions.Player.Jump.WasPressedThisFrame();

        float speed = isCrouching ? crouchSpeed :
                      isSprinting ? sprintSpeed :
                      walkSpeed;

        Vector3 horizontalMove = transform.right * moveInput.x
                               + transform.forward * moveInput.y;

        if (horizontalMove.sqrMagnitude > 1f)
            horizontalMove.Normalize();

        // SimpleMove applica gravità internamente — passiamo solo la componente orizzontale
        _controller.SimpleMove(horizontalMove * speed);

        // Salto: impulso verticale manuale applicato con Move() solo nel frame della pressione.
        // SimpleMove ignora la Y, quindi usiamo Move() esclusivamente per l'impulso verticale.
        if (_isGrounded && !isCrouching && jumpPressed)
        {
            _verticalVelocity = jumpForce;
        }

        if (_verticalVelocity > 0f)
        {
            _controller.Move(Vector3.up * _verticalVelocity * Time.deltaTime);
            // Decadimento dell'impulso — si esaurisce naturalmente frame per frame
            _verticalVelocity -= 20f * Time.deltaTime;
        }
    }

    // -------------------------------------------------------------------------
    // Look
    // -------------------------------------------------------------------------

    private void HandleLook()
    {
        if (!CanLook) return;

        Vector2 lookInput = _input.InputActions.Player.Look.ReadValue<Vector2>();
        if (lookInput == Vector2.zero) return;

        transform.Rotate(Vector3.up, lookInput.x * sensitivityX, Space.Self);

        _cameraPitch -= lookInput.y * sensitivityY;
        _cameraPitch = Mathf.Clamp(_cameraPitch, bottomClamp, topClamp);

        cameraRoot.localRotation = Quaternion.Euler(_cameraPitch, 0f, 0f);
    }

    // -------------------------------------------------------------------------
    // Crouch
    // -------------------------------------------------------------------------

    private void HandleCrouch()
    {
        bool isCrouching = _input.InputActions.Player.Crouch.IsPressed();

        float targetHeight = isCrouching ? crouchHeight : standingHeight;
        float targetCameraY = isCrouching ? cameraCrouchY : cameraStandingY;

        _controller.height = Mathf.Lerp(_controller.height, targetHeight, crouchTransitionSpeed * Time.deltaTime);

        Vector3 center = _controller.center;
        center.y = _controller.height * 0.5f;
        _controller.center = center;

        Vector3 camPos = cameraRoot.localPosition;
        camPos.y = Mathf.Lerp(camPos.y, targetCameraY, crouchTransitionSpeed * Time.deltaTime);
        cameraRoot.localPosition = camPos;
    }

    // -------------------------------------------------------------------------
    // API
    // -------------------------------------------------------------------------

    public void StopMovement() => CanMove = false;
    public void StartMovement() => CanMove = true;
    public void StopLook() => CanLook = false;
    public void StartLook() => CanLook = true;

    // -------------------------------------------------------------------------
    // Gizmos
    // -------------------------------------------------------------------------

    private void OnDrawGizmos()
    {
        if (_controller == null) return;

        Vector3 origin = transform.position
                       + _controller.center
                       + Vector3.down * (_controller.height * 0.5f - groundCheckRadius);

        Gizmos.color = _isGrounded ? Color.green : Color.red;
        Gizmos.DrawWireSphere(origin, groundCheckRadius);
        Gizmos.DrawLine(origin, origin + Vector3.down * groundCheckDistance);
    }
}