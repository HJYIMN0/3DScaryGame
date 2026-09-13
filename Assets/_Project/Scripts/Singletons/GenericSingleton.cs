using UnityEngine;

public abstract class GenericSingleton<T> : MonoBehaviour where T : MonoBehaviour
{
    private static T instance;
    private static bool isApplicationQuitting = false;

    public abstract bool IsDestroyedOnLoad();
    public abstract bool ShouldDetatchFromParent();

    public static T Instance
    {
        get
        {
            if (instance != null) return instance;
            if (isApplicationQuitting) return null;

            instance = FindAnyObjectByType(typeof(T)) as T;

            if (instance == null)
            {
                GameObject gameObj = new GameObject(typeof(T).Name + "_Singleton");
                instance = gameObj.AddComponent<T>();

                // Se questo log appare durante un cambio scena, qualcuno sta
                // accedendo a Instance in OnDisable/OnDestroy e sta creando
                // un GameObject che Unity non riuscirà a pulire.
                // Cerca lo stack qui sotto per capire chi è il colpevole.
                Debug.Log($"Generating new Singleton: {gameObj.name}\n{StackTraceUtility.ExtractStackTrace()}");
            }

            return instance;
        }
    }

    public virtual void Awake()
    {
        if (instance == null)
        {
            if (ShouldDetatchFromParent()) transform.parent = null;

            instance = GetComponent<T>();
            if (!IsDestroyedOnLoad()) DontDestroyOnLoad(gameObject);
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }

    private void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    private void OnApplicationQuit()
    {
        isApplicationQuitting = true;
    }
}