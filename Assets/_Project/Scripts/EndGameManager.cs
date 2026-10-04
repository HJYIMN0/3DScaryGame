using UnityEngine;

public class EndGameManager : MonoBehaviour
{
    [SerializeField] private PlayerInputController playerInputController;
    [SerializeField] private float fadeTime = 2f;

    private void Update()
    {
        if (playerInputController.InputActions.Player.Quit.WasPressedThisFrame())
        { Application.Quit();
        }
    }
}