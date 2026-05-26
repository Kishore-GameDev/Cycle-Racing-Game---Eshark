using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ScoreManager : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_Text playerScoreText;

    [Header("Score Settings")]
    [SerializeField] private int meterScore = 1;

    [SerializeField] private int obstacleClearScore = 50;

    private float perMeter = 10f;

    public static ScoreManager Instance;

    private readonly Dictionary<RacerID, int> scores = new();
    public Dictionary<RacerID, int> Scores => scores;

    private readonly Dictionary<RacerID, float> meterProgress = new();

    private void Awake()
    {
        Instance = this;

        InitializeScores();
    }

    void Start()
    {
        GameManager.ResetAll += ResetScores;
    }

    private void InitializeScores()
    {
        foreach (RacerID racerID in System.Enum.GetValues(typeof(RacerID)))
        {
            scores[racerID] = 0;

            meterProgress[racerID] = 0f;
        }

        UpdateUI();
    }

    public static void AddMeterScore(RacerID racerID, bool boosterActive, float movedDistance)
    {
        Instance.InternalAddMeterScore(racerID, boosterActive, movedDistance);
    }

    public static void AddObstacleClearScore(RacerID racerID, bool boosterActive)
    {
        Instance.AddScore(racerID, Instance.obstacleClearScore, boosterActive);
    }

    private void InternalAddMeterScore(RacerID racerID, bool boosterActive, float movedDistance)
    {
        meterProgress[racerID] += movedDistance;

        while (meterProgress[racerID] >= perMeter)
        {
            meterProgress[racerID] -= perMeter;

            AddScore(racerID, meterScore, boosterActive);
        }
    }

    private void AddScore(RacerID racerID, int amount, bool boosterActive)
    {
        if (boosterActive)
        {
            amount *= 2;
        }

        scores[racerID] += amount;

        UpdateUI();
    }

    public static void ResetScores()
    {
        Instance.InitializeScores();
    }

    private void UpdateUI()
    {
        UIManager.Instance.UpdateScoreText(scores[RacerID.Player]);
    }

    void OnDestroy()
    {
        GameManager.ResetAll -= ResetScores;
    }
}