using UnityEngine;

public class RevertedSpehereController : MonoBehaviour
{
    [SerializeField] private float RotationSpeed = 100f;

    private void FixedUpdate()
    {
        transform.Rotate(Vector3.forward, RotationSpeed * Time.fixedDeltaTime);
    }
}