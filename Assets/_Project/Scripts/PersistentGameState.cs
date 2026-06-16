using UnityEngine;
using System.Collections.Generic;

public class PersistentGameState : MonoBehaviour
{
    public static PersistentGameState Instance { get; private set; }

    public int currentLevelIndex;
    public int housePointsTotal;
    public int sessionScore;
    public Dictionary<int, int> starsPerLevel = new Dictionary<int, int>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        if (Instance == null)
        {
            GameObject pgsObj = new GameObject("PersistentGameState (Auto-Bootstrapped)");
            pgsObj.AddComponent<PersistentGameState>();
        }

        if (SceneLoader.Instance == null)
        {
            GameObject slObj = new GameObject("SceneLoader (Auto-Bootstrapped)");
            slObj.AddComponent<SceneLoader>();
        }
    }

    public void AddPoints(int points)
    {
        sessionScore += points;
        housePointsTotal += points;
    }

    public void AwardStars(int levelIndex, int starCount)
    {
        if (starsPerLevel.ContainsKey(levelIndex))
        {
            if (starCount > starsPerLevel[levelIndex])
            {
                starsPerLevel[levelIndex] = starCount;
            }
        }
        else
        {
            starsPerLevel.Add(levelIndex, starCount);
        }
    }

    public void ResetRun()
    {
        sessionScore = 0;
        currentLevelIndex = 0;
        starsPerLevel.Clear();
    }
}
