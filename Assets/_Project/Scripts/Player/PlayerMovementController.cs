using UnityEngine;

// MODIFICA: Rimossa la dipendenza da CharacterController.
// Aggiunte dipendenze a Rigidbody e CapsuleCollider, necessari per un setup fisico standard.
// MODIFICATO: rimossa la dipendenza da PlayerInputController tramite RequireComponent?
// No: PlayerInputController resta richiesto perché HandleMovement legge comunque il Move input.
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CapsuleCollider))]
[RequireComponent(typeof(PlayerInputController))]
public class PlayerMovementController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float walkSpeed = 4f;

    [Header("Ground Check")]
    [SerializeField] private LayerMask walkableLayer;
    [SerializeField] private float groundCheckRadius = 0.3f;
    [SerializeField] private float groundCheckDistance = 0.4f;

    private Rigidbody _rb;
    private CapsuleCollider _collider;
    private PlayerInputController _input;

    private bool _isGrounded;
    public bool CanMove { get; private set; } = true;


    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _collider = GetComponent<CapsuleCollider>();
        _input = GetComponent<PlayerInputController>();
    }

    private void Update()
    {
        UpdateGroundState();
    }

    private void FixedUpdate()
    {
        HandleMovement();
    }

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
    // Movement
    // -------------------------------------------------------------------------

    private void HandleMovement()
    {
        if (!CanMove) return;

        Vector2 moveInput = _input.InputActions.Player.Move.ReadValue<Vector2>();

        Vector3 moveDirection = transform.right * moveInput.x
                              + transform.forward * moveInput.y;

        if (moveDirection.sqrMagnitude > 1f)
            moveDirection.Normalize();

        Vector3 targetVelocity = moveDirection * walkSpeed;

        // Manteniamo la velocità verticale attuale generata dalla gravità del Rigidbody
        targetVelocity.y = _rb.linearVelocity.y;

        // Applicazione finale della velocità al corpo fisico
        _rb.linearVelocity = targetVelocity;
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

    private void OnDrawGizmos()
    {
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