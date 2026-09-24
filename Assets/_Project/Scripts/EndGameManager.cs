using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class EndGameManager : MonoBehaviour
{
    [SerializeField] private PlayerInputController playerInputController;
    [SerializeField] private float fadeTime = 2f;

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
            Application.Quit();
            Debug.Log("Application.Quit() called. If running in the editor, this won't close the editor.");
        }
    }
}