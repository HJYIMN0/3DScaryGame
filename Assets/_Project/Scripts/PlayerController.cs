using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;

/// <summary>
/// Controller FPS completamente self-contained.
/// Crea a runtime la gerarchia: Player → CameraRoot → CinemachineCamera
/// e aggiunge CinemachineBrain alla Main Camera se non presente.
/// Non richiede nessun setup da Inspector.
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour, InputSystem_Actions.IPlayerActions
{

    [Header("References")]
    [SerializeField] private CinemachineCamera _cinemachineCamera;
    [SerializeField] private Transform _cameraRoot;
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

    private InputSystem_Actions _inputActions;
    private CharacterController _characterController;

    private Vector2 _moveInput;
    private Vector2 _lookInput;
    private bool _jumpPressed;
    private bool _isSprinting;
    private bool _isCrouching;

    private float _verticalVelocity;
    private float _cameraPitch;

    // -------------------------------------------------------------------------
    // Unity Lifecycle
    // -------------------------------------------------------------------------

    private void Awake()
    {
        _characterController = GetComponent<CharacterController>();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // Crea tutta la gerarchia camera via codice
        SetupCamera();

        _inputActions = new InputSystem_Actions();
        _inputActions.Player.AddCallbacks(this);
    }

    private void OnEnable()
    {
        _inputActions.Player.Enable();
    }

    private void OnDisable()
    {
        _inputActions.Player.Disable();
    }

    private void OnDestroy()
    {
        _inputActions.Player.RemoveCallbacks(this);
        _inputActions.Dispose();
    }

    private void Update()
    {
        HandleGravityAndJump();
        HandleMovement();
        HandleLook();
        HandleCrouch();
    }

    // -------------------------------------------------------------------------
    // Setup camera (chiamato una volta in Awake)
    // -------------------------------------------------------------------------

    private void SetupCamera()
    {
        // CameraRoot e CinemachineCamera sono ora assegnati dall'Inspector.
        // SetupCamera() si limita a posizionare correttamente CameraRoot,
        // reparentare la camera sotto di esso e disabilitare i componenti
        // Cinemachine che sovrascriverebbero la rotazione.

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
            Debug.LogError("PlayerController: nessuna Main Camera trovata nella scena.");
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
        float currentSpeed = _isCrouching ? crouchSpeed
                           : _isSprinting ? sprintSpeed
                                           : walkSpeed;

        Vector3 moveDirection = transform.right * _moveInput.x + transform.forward * _moveInput.y;
        moveDirection *= currentSpeed;
        moveDirection.y = _verticalVelocity;

        _characterController.Move(moveDirection * Time.deltaTime);
    }

    private void HandleGravityAndJump()
    {
        if (_characterController.isGrounded)
        {
            _verticalVelocity = -2f;

            if (_jumpPressed && !_isCrouching)
                _verticalVelocity = jumpForce;
        }
        else
        {
            _verticalVelocity -= gravity * Time.deltaTime;
        }

        _jumpPressed = false;
    }

    // -------------------------------------------------------------------------
    // Logica camera
    // -------------------------------------------------------------------------

    private void HandleLook()
    {
        if (_lookInput == Vector2.zero) return;

        // Yaw: ruota il corpo del player (asse Y)
        transform.Rotate(Vector3.up, _lookInput.x * sensitivityX, Space.Self);

        // Pitch: ruota CameraRoot (asse X locale).
        // Cinemachine con HardLockToTarget copierà questa rotazione direttamente,
        // senza sovrascriverla.
        _cameraPitch -= _lookInput.y * sensitivityY;
        _cameraPitch = Mathf.Clamp(_cameraPitch, bottomClamp, topClamp);
        _cameraRoot.localRotation = Quaternion.Euler(_cameraPitch, 0f, 0f);
    }

    private void HandleCrouch()
    {
        float targetHeight = _isCrouching ? crouchHeight : standingHeight;
        float targetCameraY = _isCrouching ? cameraCrouchY : cameraStandingY;

        _characterController.height = Mathf.Lerp(
            _characterController.height,
            targetHeight,
            crouchTransitionSpeed * Time.deltaTime
        );

        Vector3 camLocalPos = _cameraRoot.localPosition;
        camLocalPos.y = Mathf.Lerp(camLocalPos.y, targetCameraY, crouchTransitionSpeed * Time.deltaTime);
        _cameraRoot.localPosition = camLocalPos;
    }

    // -------------------------------------------------------------------------
    // Implementazione IPlayerActions
    // -------------------------------------------------------------------------

    public void OnMove(InputAction.CallbackContext context)
        => _moveInput = context.ReadValue<Vector2>();

    public void OnLook(InputAction.CallbackContext context)
        => _lookInput = context.ReadValue<Vector2>();

    public void OnJump(InputAction.CallbackContext context)
    {
        if (context.performed) _jumpPressed = true;
    }

    public void OnSprint(InputAction.CallbackContext context)
        => _isSprinting = context.ReadValueAsButton();

    public void OnCrouch(InputAction.CallbackContext context)
        => _isCrouching = context.ReadValueAsButton();

    public void OnAttack(InputAction.CallbackContext context) { }
    public void OnInteract(InputAction.CallbackContext context) { }
    public void OnPrevious(InputAction.CallbackContext context) { }
    public void OnNext(InputAction.CallbackContext context) { }
}