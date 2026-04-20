using Unity.VisualScripting;
using UnityEngine;
class InsideDoor : MonoBehaviour
{
    [SerializeField] private AudioClip doorOpenSound;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
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
            transform.Rotate(Vector3.left, -90f, Space.Self);
        }
    }
}
