using UnityEngine;

public class InteractionCanva : MonoBehaviour
{
    private Camera _camera;
    // La distanza originale o desiderata dalla camera
    [SerializeField] private float _defaultDistance = 5f;
    // Offset per evitare che il canvas si compenetri con la mesh dell'ostacolo
    [SerializeField] private float _surfaceOffset = 0.1f;

    private void OnEnable()
    {
        if (_camera == null)
        {
            _camera = Camera.main;
        }
    }

    private void Update()
    {
        if (_camera == null) return;

        // Calcoliamo la posizione ideale (dove l'oggetto "vorrebbe" stare)
        Vector3 targetPosition = _camera.transform.position + (_camera.transform.forward * _defaultDistance);

        // Direzione dalla camera verso il target
        Vector3 direction = targetPosition - _camera.transform.position;
        float distance = direction.magnitude;

        RaycastHit hit;

        // Lanciamo il raggio dalla camera verso la posizione target
        // NOTA: Usa un LayerMask se vuoi che il raycast ignori certi oggetti (come il player)
        if (Physics.Raycast(_camera.transform.position, direction.normalized, out hit, distance))
        {
            // Se c'è un ostacolo, posizioniamo l'oggetto sul punto di impatto
            // Sottraiamo un piccolo offset per non farlo "affogare" nel muro
            transform.position = hit.point - (direction.normalized * _surfaceOffset);
            // Opzionale: Ruota il canvas per guardare sempre la camera
            transform.LookAt(_camera.transform);
            transform.Rotate(0, 180, 0); // Corregge l'orientamento tipico dei Canvas
        }
        else
        {
            // Se non ci sono ostacoli, l'oggetto sta nella sua posizione ideale
            transform.position = targetPosition;
        }


    }
}