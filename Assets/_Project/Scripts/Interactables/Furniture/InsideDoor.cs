using Unity.VisualScripting;
using UnityEngine;

class InsideDoor : MonoBehaviour
{
    [SerializeField] private AudioClip doorOpenSound;

    private bool isOpen = false;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // AGGIUNTO: guard — se già aperta, non ruotare di nuovo
            if (isOpen) return;
            isOpen = true;

            Debug.Log("I'mma rotate!");
            transform.Rotate(Vector3.left, 90f, Space.Self);
            if (doorOpenSound != null)
            {
                AudioManager.Instance.PlaySfxSoundFromSource(GetComponent<AudioSource>(), doorOpenSound);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isOpen = false;

            transform.Rotate(Vector3.left, -90f, Space.Self);
        }
    }
}
        }
    }
}
