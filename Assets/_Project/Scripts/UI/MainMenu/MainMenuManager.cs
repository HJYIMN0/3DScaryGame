using Unity.VisualScripting;
using UnityEngine;

public class MainMenuManager : MonoBehaviour
{
    [Header("Menu Panels")]
    [SerializeField] private GameObject mainMenu;
    [SerializeField] private GameObject optionsMenu;

    [Header("Fade Settings")]
    [SerializeField] private float fadeDuration = 0.5f;
    public void StartGame()
    {
        //Qui ci vorrebbe qualcosa tipo
        //SaveData data = SaveSystem.LoadGame();
        //if (data != null)
        //int currentDay = data.currentDay;
        //GameFlowManager.Instance.LoadScene(currentDay, fadeDuration);
        //else
        GameFlowManager.Instance.LoadScene(0, fadeDuration);
        //Ma per la build di testing è meglio così
        //Così la demo comincia sempre da capo
    }
    public void QuitGame()
    {
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#endif
        Application.Quit();
    }

    public void ToggleMenuScreen()
    {
        bool isMainMenuActive = mainMenu.activeSelf;
        mainMenu.SetActive(!isMainMenuActive);
        optionsMenu.SetActive(isMainMenuActive);
    }
}
