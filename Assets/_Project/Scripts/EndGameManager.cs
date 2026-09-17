using UnityEngine;
using UnityEngine.InputSystem;

public class EndGameManager : MonoBehaviour
{
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
