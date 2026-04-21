using UnityEngine;

class InsideDoor : MonoBehaviour
{
    [SerializeField] private AudioClip doorOpenSound;
    [SerializeField] private GameObject[] doorMeshes;
    [SerializeField] private Collider doorCollider;
    [SerializeField] private Directions doorDirection;

    private Vector3 initialRot;
    private AudioSource audioSource;   

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();

        if (doorMeshes.Length > 0)
        {
            initialRot = doorMeshes[0].transform.localEulerAngles;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            doorCollider.enabled = false;

            foreach (GameObject go in doorMeshes)
            {
                RotateDoor(go, doorDirection, initialRot);
            }

            if (doorOpenSound != null && audioSource != null)
            {
                AudioManager.Instance.PlaySfxSoundFromSource(audioSource, doorOpenSound);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            doorCollider.enabled = true;

            foreach (GameObject go in doorMeshes)
            {
                go.transform.localEulerAngles = initialRot;
            }

            if (doorOpenSound != null && audioSource != null)
            {
                AudioManager.Instance.PlaySfxSoundFromSource(audioSource, doorOpenSound);
            }
        }
    }

    private void RotateDoor(GameObject go, Directions directions, Vector3 originalRot)
    {
        // Calcoliamo la rotazione finale desiderata partendo da quella originale
        Vector3 targetRotation = originalRot;

        switch (directions)
        {
            case Directions.Up:
                targetRotation += new Vector3(-90, 0, 0);
                break;
            case Directions.Down:
                targetRotation += new Vector3(90, 0, 0);
                break;
            case Directions.Left:
                targetRotation += new Vector3(0, -90, 0);
                break;
            case Directions.Right:
                targetRotation += new Vector3(0, 90, 0);
                break;
        }

        // Applichiamo la rotazione in modo assoluto e locale, non relativo
        go.transform.localEulerAngles = targetRotation;
    }
}