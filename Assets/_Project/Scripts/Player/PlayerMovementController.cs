using Unity.Cinemachine;
using Unity.VisualScripting;
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
    [SerializeField] private float gravity = 20f;

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

    private CharacterController controller;
    private PlayerInputController input;

    private float verticalVelocity;
    private float cameraPitch;

    private bool isGrounded;
    public bool CanMove { get; private set; } = true;

    // MODIFICA: Rimosse le variabili lastValidPosition e hasValidPosition
    // in quanto funzionali unicamente a un sistema di rollback difettoso.

    // -------------------------------------------------------------------------
    // Unity
    // -------------------------------------------------------------------------

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        input = GetComponent<PlayerInputController>();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        SetupCamera();
    }

    private void Update()
    {
        UpdateGroundState();

        HandleGravity();
        HandleJump();
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

        // Si mantiene questa logica distruttiva per fedeltà alla richiesta, 
        // sebbene in un ambiente di produzione andrebbe evitata configurando il prefab correttamente.
        var cinemachineComponents = cinemachineCamera.GetComponents<CinemachineComponentBase>();
        for (int i = cinemachineComponents.Length - 1; i >= 0; i--)
        {
            Destroy(cinemachineComponents[i]);
        }

        Camera mainCam = Camera.main;
        if (mainCam != null && mainCam.GetComponent<CinemachineBrain>() == null)
        {
            mainCam.gameObject.AddComponent<CinemachineBrain>();
        }
    }

    // -------------------------------------------------------------------------
    // Ground
    // -------------------------------------------------------------------------

    private void UpdateGroundState()
    {
        Vector3 origin = transform.position
                       + controller.center
                       + Vector3.down * (controller.height * 0.5f - groundCheckRadius);

        // MODIFICA: Questo è ora l'unico punto in cui viene eseguito lo SphereCast.
        isGrounded = Physics.SphereCast(
            origin,
            groundCheckRadius,
            Vector3.down,
            out _,
            groundCheckDistance,
            walkableLayer,
            QueryTriggerInteraction.Ignore
        );
    }

    // -------------------------------------------------------------------------
    // Gravity
    // -------------------------------------------------------------------------

    private void HandleGravity()
    {
        if (isGrounded && verticalVelocity < 0f)
        {
            // Reset della velocità verticale se si è a terra
            verticalVelocity = -2f;
        }
        else
        {
            // Applicazione costante della gravità
            verticalVelocity -= gravity * Time.deltaTime;
        }
    }

    // -------------------------------------------------------------------------
    // Jump
    // -------------------------------------------------------------------------

    private void HandleJump()
    {
        if (!CanMove || !isGrounded || input.IsCrouching)
            return;

        if (input.JumpPressed)
        {
            verticalVelocity = jumpForce;
        }
    }

    // -------------------------------------------------------------------------
    // Movement
    // -------------------------------------------------------------------------

    private void HandleMovement()
    {
        if (!CanMove)
            return;

        float speed = input.IsCrouching ? crouchSpeed :
                      input.IsSprinting ? sprintSpeed :
                      walkSpeed;

        Vector3 horizontalMove = transform.right * input.MoveInput.x +
                                 transform.forward * input.MoveInput.y;

        if (horizontalMove.sqrMagnitude > 1f)
            horizontalMove.Normalize();

        horizontalMove *= speed;

        Vector3 finalMove = horizontalMove + Vector3.up * verticalVelocity;

        // Esecuzione dello spostamento
        CollisionFlags flags = controller.Move(finalMove * Time.deltaTime);

        // Correzione della velocità in caso di impatto col soffitto
        if ((flags & CollisionFlags.Above) != 0 && verticalVelocity > 0f)
        {
            verticalVelocity = 0f;
        }

        // Correzione in caso di impatto col pavimento per non accumulare gravità residua
        if ((flags & CollisionFlags.Below) != 0 && verticalVelocity < -2f)
        {
            verticalVelocity = -2f;
        }

        // MODIFICA: Tutta la logica di rollback (lastValidPosition) che causava
        // l'incastro ai margini del walkableLayer è stata sradicata. Il CharacterController
        // è ora libero di cadere e muoversi, fermato unicamente dalla geometria dei collider.
    }

    // -------------------------------------------------------------------------
    // Look
    // -------------------------------------------------------------------------

    private void HandleLook()
    {
        if (input.LookInput == Vector2.zero)
            return;

        transform.Rotate(Vector3.up, input.LookInput.x * sensitivityX, Space.Self);

        cameraPitch -= input.LookInput.y * sensitivityY;
        cameraPitch = Mathf.Clamp(cameraPitch, bottomClamp, topClamp);

        cameraRoot.localRotation = Quaternion.Euler(cameraPitch, 0f, 0f);
    }

    // -------------------------------------------------------------------------
    // Crouch
    // -------------------------------------------------------------------------

    private void HandleCrouch()
    {
        float targetHeight = input.IsCrouching ? crouchHeight : standingHeight;
        float targetCameraY = input.IsCrouching ? cameraCrouchY : cameraStandingY;

        controller.height = Mathf.Lerp(controller.height, targetHeight, crouchTransitionSpeed * Time.deltaTime);

        Vector3 center = controller.center;
        center.y = controller.height * 0.5f;
        controller.center = center;

        Vector3 camPos = cameraRoot.localPosition;
        camPos.y = Mathf.Lerp(camPos.y, targetCameraY, crouchTransitionSpeed * Time.deltaTime);
        cameraRoot.localPosition = camPos;
    }

    // -------------------------------------------------------------------------
    // API
    // -------------------------------------------------------------------------

    public void StopMovement() => CanMove = false;
    public void StartMovement() => CanMove = true;

    // -------------------------------------------------------------------------
    // Gizmos
    // -------------------------------------------------------------------------

    private void OnDrawGizmos()
    {
        if (controller == null)
            return;

        Vector3 origin = transform.position
                       + controller.center
                       + Vector3.down * (controller.height * 0.5f - groundCheckRadius);

        Gizmos.color = isGrounded ? Color.green : Color.red;
        Gizmos.DrawWireSphere(origin, groundCheckRadius);
        Gizmos.DrawLine(origin, origin + Vector3.down * groundCheckDistance);
    }

    // MODIFICA: Rimosso HasWalkableGroundBelow() poiché totalmente ridondante
    // rispetto a UpdateGroundState() e asservito unicamente alla logica di rollback fallata.
}