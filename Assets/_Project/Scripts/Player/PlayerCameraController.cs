using Unity.Cinemachine;
using UnityEngine;

// AGGIUNTO: nuova classe che isola tutta la logica di rotazione della camera,
// prima dentro PlayerMovementController. Estratta com'è (stessi campi, stessi nomi
// di metodo dove possibile) per rispettare la logica originale.
[RequireComponent(typeof(PlayerInputController))]
public class PlayerCameraController : MonoBehaviour
{
    // -------------------------------------------------------------------------
    // Camera
    // -------------------------------------------------------------------------

    [Header("Camera")]
    [SerializeField] private Transform cameraRoot;
    [SerializeField] private CinemachineCamera cinemachineCamera;
    [SerializeField] private float cameraHeight = 1.6f;

    // -------------------------------------------------------------------------
    // Look
    // -------------------------------------------------------------------------

    [Header("Look")]
    [SerializeField] private float sensitivityX = 0.15f;
    [SerializeField] private float sensitivityY = 0.15f;
    [SerializeField] private float topClamp = 80f;
    [SerializeField] private float bottomClamp = -80f;

    // -------------------------------------------------------------------------
    // Stato
    // -------------------------------------------------------------------------

    private PlayerInputController _input;
    private float _cameraPitch;

    public bool CanLook { get; private set; } = true;
    public PlayerMovementController PlayerMovementController { get; private set; }

    // -------------------------------------------------------------------------
    // Unity
    // -------------------------------------------------------------------------

    private void Awake()
    {
        PlayerMovementController = GetComponent<PlayerMovementController>();

        _input = GetComponent<PlayerInputController>();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        SetupCamera();
    }

    private void Update()
    {
        HandleLook();
    }

    // -------------------------------------------------------------------------
    // Camera setup
    // -------------------------------------------------------------------------

    private void SetupCamera()
    {
        cameraRoot.SetParent(transform);
        // MODIFICATO: usa cameraHeight al posto di cameraStandingY (non c'è più il crouch
        // che modificava questo valore nel tempo, quindi viene impostato una sola volta qui).
        cameraRoot.localPosition = new Vector3(0f, cameraHeight, 0f);
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
    // API
    // -------------------------------------------------------------------------

    public void StopLook() => CanLook = false;
    public void StartLook() => CanLook = true;
}