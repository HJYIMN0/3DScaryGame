using UnityEngine;
using Unity.Cinemachine;

/// <summary>
/// Gestisce il movimento del player e della camera FPS.
/// Legge i valori di input da PlayerInputController, che deve essere
/// presente sullo stesso GameObject.
/// </summary>
[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(PlayerInputController))]
public class PlayerMovementController : MonoBehaviour
{
    // -------------------------------------------------------------------------
    // Riferimenti Inspector
    // -------------------------------------------------------------------------

    [Header("Cinemachine")]
    [SerializeField] private Transform _cameraRoot;
    [SerializeField] private CinemachineCamera _cinemachineCamera;

    // -------------------------------------------------------------------------
    // Parametri movimento
    // -------------------------------------------------------------------------

    [Header("Movimento")]
    [SerializeField] private float walkSpeed = 4f;
    [SerializeField] private float sprintSpeed = 7f;
    [SerializeField] private float crouchSpeed = 2f;
    [SerializeField] private float jumpForce = 5f;
    [SerializeField] private float gravity = 20f;

    // -------------------------------------------------------------------------
    // Parametri camera / look
    // -------------------------------------------------------------------------

    [Header("Camera / Look")]
    [SerializeField] private float sensitivityX = 0.15f;
    [SerializeField] private float sensitivityY = 0.15f;
    [SerializeField] private float topClamp = 80f;
    [SerializeField] private float bottomClamp = -80f;

    // -------------------------------------------------------------------------
    // Parametri crouch
    // -------------------------------------------------------------------------

    [Header("Crouch")]
    [SerializeField] private float standingHeight = 2f;
    [SerializeField] private float crouchHeight = 1f;
    [SerializeField] private float cameraStandingY = 1.6f;
    [SerializeField] private float cameraCrouchY = 0.8f;
    [SerializeField] private float crouchTransitionSpeed = 10f;

    // -------------------------------------------------------------------------
    // Stato interno
    // -------------------------------------------------------------------------

    private PlayerInputController _input;
    private CharacterController _characterController;

    private float _verticalVelocity;
    private float _cameraPitch;

    private bool _canMove = true;

    // -------------------------------------------------------------------------
    // Unity Lifecycle
    // -------------------------------------------------------------------------

    private void Awake()
    {
        _characterController = GetComponent<CharacterController>();
        _input = GetComponent<PlayerInputController>();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        SetupCamera();
    }

    private void Update()
    {
        HandleGravityAndJump();
        HandleMovement();
        HandleLook();
        HandleCrouch();
    }

    // -------------------------------------------------------------------------
    // Setup camera
    // -------------------------------------------------------------------------

    private void SetupCamera()
    {
        _cameraRoot.SetParent(transform);
        _cameraRoot.localPosition = new Vector3(0f, cameraStandingY, 0f);
        _cameraRoot.localRotation = Quaternion.identity;

        _cinemachineCamera.transform.SetParent(_cameraRoot);
        _cinemachineCamera.transform.localPosition = Vector3.zero;
        _cinemachineCamera.transform.localRotation = Quaternion.identity;

        _cinemachineCamera.Follow = null;
        _cinemachineCamera.LookAt = null;

        foreach (var component in _cinemachineCamera.GetComponents<CinemachineComponentBase>())
            Destroy(component);

        var mainCam = Camera.main;
        if (mainCam == null)
        {
            Debug.LogError("PlayerMovementController: nessuna Main Camera trovata nella scena.");
            return;
        }

        if (mainCam.GetComponent<CinemachineBrain>() == null)
            mainCam.gameObject.AddComponent<CinemachineBrain>();
    }

    // -------------------------------------------------------------------------
    // Logica movimento
    // -------------------------------------------------------------------------

    private void HandleMovement()
    {
        if (!_canMove) return;

        float currentSpeed = _input.IsCrouching ? crouchSpeed
                           : _input.IsSprinting ? sprintSpeed
                                                 : walkSpeed;

        Vector3 moveDirection = transform.right * _input.MoveInput.x
                              + transform.forward * _input.MoveInput.y;
        moveDirection *= currentSpeed;
        moveDirection.y = _verticalVelocity;

        _characterController.Move(moveDirection * Time.deltaTime);
    }

    private void HandleGravityAndJump()
    {
        if (_characterController.isGrounded)
        {
            _verticalVelocity = -2f;

            if (_input.JumpPressed && !_input.IsCrouching)
                _verticalVelocity = jumpForce;
        }
        else
        {
            _verticalVelocity -= gravity * Time.deltaTime;
        }
    }

    // -------------------------------------------------------------------------
    // Logica camera
    // -------------------------------------------------------------------------

    private void HandleLook()
    {
        if (_input.LookInput == Vector2.zero) return;

        transform.Rotate(Vector3.up, _input.LookInput.x * sensitivityX, Space.Self);

        _cameraPitch -= _input.LookInput.y * sensitivityY;
        _cameraPitch = Mathf.Clamp(_cameraPitch, bottomClamp, topClamp);
        _cameraRoot.localRotation = Quaternion.Euler(_cameraPitch, 0f, 0f);
    }

    private void HandleCrouch()
    {
        float targetHeight = _input.IsCrouching ? crouchHeight : standingHeight;
        float targetCameraY = _input.IsCrouching ? cameraCrouchY : cameraStandingY;

        _characterController.height = Mathf.Lerp(
            _characterController.height,
            targetHeight,
            crouchTransitionSpeed * Time.deltaTime
        );

        Vector3 camLocalPos = _cameraRoot.localPosition;
        camLocalPos.y = Mathf.Lerp(camLocalPos.y, targetCameraY, crouchTransitionSpeed * Time.deltaTime);
        _cameraRoot.localPosition = camLocalPos;
    }

    public void StopMovement() => _canMove = false;

    public void StartMovement() => _canMove = true;
}