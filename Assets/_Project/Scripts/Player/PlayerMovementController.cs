using Unity.Cinemachine;
using UnityEngine;

// MODIFICA: Rimossa la dipendenza da CharacterController.
// Aggiunte dipendenze a Rigidbody e CapsuleCollider, necessari per un setup fisico standard.
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CapsuleCollider))]
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

    // MODIFICA: Riferimenti sostituiti per adattarsi al sistema fisico.
    private Rigidbody _rb;
    private CapsuleCollider _collider;
    private PlayerInputController _input;

    private float _cameraPitch;
    private bool _isGrounded;

    // MODIFICA: Introdotta variabile di caching per catturare l'input di salto 
    // in Update e consumarlo in FixedUpdate.
    private bool _jumpRequested;

    public bool CanMove { get; private set; } = true;
    public bool CanLook { get; private set; } = true;

    // -------------------------------------------------------------------------
    // Unity
    // -------------------------------------------------------------------------

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _collider = GetComponent<CapsuleCollider>();
        _input = GetComponent<PlayerInputController>();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        SetupCamera();
    }

    private void Update()
    {
        // La lettura degli input e le modifiche alla telecamera rimangono nell'Update
        // per mantenere la reattività al framerate visivo.
        UpdateGroundState();
        HandleLook();
        HandleCrouch();
        CaptureJumpInput();
    }

    // MODIFICA: Introdotto FixedUpdate per processare esclusivamente la fisica del Rigidbody.
    private void FixedUpdate()
    {
        HandleMovement();
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
        // MODIFICA: Il calcolo dell'origine sfrutta ora i parametri del CapsuleCollider
        Vector3 origin = transform.position
                       + _collider.center
                       + Vector3.down * (_collider.height * 0.5f - groundCheckRadius);

        _isGrounded = Physics.SphereCast(
            origin, groundCheckRadius, Vector3.down,
            out _, groundCheckDistance, walkableLayer,
            QueryTriggerInteraction.Ignore);
    }

    // -------------------------------------------------------------------------
    // Movement & Jump
    // -------------------------------------------------------------------------

    private void CaptureJumpInput()
    {
        if (!CanMove) return;

        bool isCrouching = _input.InputActions.Player.Crouch.IsPressed();

        // Salviamo la richiesta di salto se le condizioni sono soddisfatte
        if (_isGrounded && !isCrouching && _input.InputActions.Player.Jump.WasPressedThisFrame())
        {
            _jumpRequested = true;
        }
    }

    private void HandleMovement()
    {
        if (!CanMove) return;

        Vector2 moveInput = _input.InputActions.Player.Move.ReadValue<Vector2>();
        bool isSprinting = _input.InputActions.Player.Sprint.IsPressed();
        bool isCrouching = _input.InputActions.Player.Crouch.IsPressed();

        float speed = isCrouching ? crouchSpeed :
                      isSprinting ? sprintSpeed :
                      walkSpeed;

        Vector3 moveDirection = transform.right * moveInput.x
                              + transform.forward * moveInput.y;

        if (moveDirection.sqrMagnitude > 1f)
            moveDirection.Normalize();

        // ERRORE RIMOSSO: Eliminato SimpleMove.
        // Calcolo della velocità target orizzontale.
        Vector3 targetVelocity = moveDirection * speed;

        // Manteniamo la velocità verticale attuale generata dalla gravità del Rigidbody
        targetVelocity.y = _rb.linearVelocity.y;

        // Esecuzione del salto consumando la richiesta
        if (_jumpRequested)
        {
            targetVelocity.y = jumpForce;
            _jumpRequested = false;
        }

        // Applicazione finale della velocità al corpo fisico
        _rb.linearVelocity = targetVelocity;
    }

    // -------------------------------------------------------------------------
    // Look
    // -------------------------------------------------------------------------

    private void HandleLook()
    {
        if (!CanLook) return;

        Vector2 lookInput = _input.InputActions.Player.Look.ReadValue<Vector2>();
        if (lookInput == Vector2.zero) return;

        // La rotazione del transform orizzontale funziona correttamente con il Rigidbody
        // purché le rotazioni nel componente Rigidbody siano bloccate (Freeze Rotation X,Y,Z).
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

        // MODIFICA: La transizione ora manipola le proprietà del CapsuleCollider.
        _collider.height = Mathf.Lerp(_collider.height, targetHeight, crouchTransitionSpeed * Time.deltaTime);

        Vector3 center = _collider.center;
        center.y = _collider.height * 0.5f;
        _collider.center = center;

        Vector3 camPos = cameraRoot.localPosition;
        camPos.y = Mathf.Lerp(camPos.y, targetCameraY, crouchTransitionSpeed * Time.deltaTime);
        cameraRoot.localPosition = camPos;
    }

    // -------------------------------------------------------------------------
    // API
    // -------------------------------------------------------------------------

    public void StopMovement()
    {
        CanMove = false;
        // Azzera immediatamente la velocità orizzontale per impedire scivolamenti inerziali
        _rb.linearVelocity = new Vector3(0f, _rb.linearVelocity.y, 0f);
    }

    public void StartMovement() => CanMove = true;
    public void StopLook() => CanLook = false;
    public void StartLook() => CanLook = true;

    // -------------------------------------------------------------------------
    // Gizmos
    // -------------------------------------------------------------------------

    private void OnDrawGizmos()
    {
        // MODIFICA: Gizmos aggiornati per utilizzare i dati del CapsuleCollider.
        if (_collider == null) _collider = GetComponent<CapsuleCollider>();
        if (_collider == null) return;

        Vector3 origin = transform.position
                       + _collider.center
                       + Vector3.down * (_collider.height * 0.5f - groundCheckRadius);

        Gizmos.color = _isGrounded ? Color.green : Color.red;
        Gizmos.DrawWireSphere(origin, groundCheckRadius);
        Gizmos.DrawLine(origin, origin + Vector3.down * groundCheckDistance);
    }
}