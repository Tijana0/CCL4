using UnityEngine;

/// Keeps WwiseGlobal alive across scene loads, and ensures only one exists.
public class PersistentWwiseGlobal : MonoBehaviour
{
    private static PersistentWwiseGlobal instance;

    void Awake()
    {
        if (instance != null)
        {
            // A WwiseGlobal already persisted from a previous scene — destroy this duplicate
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);
    }
}