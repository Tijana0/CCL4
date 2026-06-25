using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Fills the Hogwarts-style report-card results screen (landscape).
/// Stars use RawImage (Texture), so no sprite conversion needed.
/// Call Show(...) from GameManager when the round ends (win OR loss).
/// </summary>
public class ReportCard : MonoBehaviour
{
    [System.Serializable]
    public class ClassEntry
    {
        public string displayName;     // "Potions", "Divination", ...
        public int buildIndex = -1;    // scene build index for star lookup. -1 = flavour class.
    }

    [Header("Centre-left — the class just played")]
    public TMP_Text subjectTitle;
    public RawImage[] starSlots;       // your 3 star RawImages (left to right)
    public Texture starFilled;         // star_filled texture
    public Texture starEmpty;          // star_empty texture
    public TMP_Text verdictText;       // "PASSED" / "FAILED"

    [Header("Score")]
    public TMP_Text scoreText;

    [Header("Class list (right side)")]
    public Transform rowsParent;
    public GameObject rowPrefab;
    public List<ClassEntry> classes = new List<ClassEntry>();

    [Header("Colours")]
    public Color highlightColor = new Color(0.37f, 0.17f, 0.17f);
    public Color passColor = new Color(0.18f, 0.42f, 0.23f);
    public Color failColor = new Color(0.62f, 0.13f, 0.13f);

    public void Show(string playedClassName, int starsEarned, int score, bool won)
    {
        gameObject.SetActive(true);

        if (subjectTitle) subjectTitle.text = playedClassName.ToUpper();
        SetStars(starsEarned);
        if (scoreText) scoreText.text = score.ToString("N0");

        if (verdictText)
        {
            verdictText.text = won ? "PASSED" : "FAILED";
            verdictText.color = won ? passColor : failColor;
        }

        BuildClassList(playedClassName, starsEarned, won);
    }

    void SetStars(int stars)
    {
        if (starSlots == null) return;
        for (int i = 0; i < starSlots.Length; i++)
            if (starSlots[i]) starSlots[i].texture = (i < stars) ? starFilled : starEmpty;
    }

    void BuildClassList(string playedName, int playedStars, bool won)
    {
        if (rowsParent == null || rowPrefab == null) return;

        for (int i = rowsParent.childCount - 1; i >= 0; i--)
            Destroy(rowsParent.GetChild(i).gameObject);

        foreach (var entry in classes)
        {
            bool isPlayed = entry.displayName == playedName;
            int stars = isPlayed ? playedStars : GetStarsForLevel(entry.buildIndex);

            string grade = (isPlayed && !won) ? "Failed" : StarsToGrade(stars, entry.buildIndex);

            GameObject row = Instantiate(rowPrefab, rowsParent);
            TMP_Text[] t = row.GetComponentsInChildren<TMP_Text>();
            if (t.Length >= 2)
            {
                t[0].text = entry.displayName;
                t[1].text = grade;
                if (isPlayed)
                {
                    t[0].fontStyle |= FontStyles.Bold;
                    t[1].fontStyle |= FontStyles.Bold;
                    Color c = won ? highlightColor : failColor;
                    t[0].color = c; t[1].color = c;
                }
            }
        }
    }

    int GetStarsForLevel(int buildIndex)
    {
        if (buildIndex < 0) return -1;
        if (PersistentGameState.Instance == null) return -1;

        if (PersistentGameState.Instance.starsPerLevel.TryGetValue(buildIndex, out int stars))
            return stars;     // played before -> real stars
        return -1;            // never played -> "Not yet attempted"
    }

    string StarsToGrade(int stars, int buildIndex)
    {
        if (buildIndex < 0) return "\u2014";
        switch (stars)
        {
            case 3:  return "Outstanding";
            case 2:  return "Exceeds Expectations";
            case 1:  return "Acceptable";
            case 0:  return "Poor";
            default: return "Not yet attempted";
        }
    }
}