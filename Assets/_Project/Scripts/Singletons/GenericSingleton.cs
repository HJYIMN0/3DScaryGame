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
            if (instance == null)
            {
                FindAnyObjectByType(typeof(T));
                if (instance == null && !isApplicationQuitting)
                {
                    GameObject gameObj = new GameObject(typeof(T).Name + "_Singleton");
                    instance = gameObj.AddComponent<T>();
                    Debug.Log($"Generating new Singleton: {gameObj.name}");
                }
            }
            return instance;
        }
    }

    public virtual void Awake()
    {
        if (instance == null)
        {

            if (ShouldDetatchFromParent())
            {
                transform.parent = null;
                //Debug.Log($"Removing {gameObject.name} from its Parents!");
            }

            instance = GetComponent<T>();
            if (!IsDestroyedOnLoad())
            {
                DontDestroyOnLoad(gameObject);
                //Debug.Log("Creating Singleton: " + gameObject.name);
            }

        }
        else if (instance != null && instance != this)
        {
            Destroy(gameObject);
        }
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }

    private void OnApplicationQuit()
    {
        isApplicationQuitting = true;
    }
}
