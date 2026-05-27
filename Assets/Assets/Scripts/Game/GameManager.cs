using System;
using System.Collections.Generic;
using System.Linq;
using K;
using NUnit.Framework;
using UnityEngine;

public enum GameState
{
    Pause,
    Resume,
}

public class GameManager : MonoBehaviour
{
    public static Action<int> CountDownStarted;
    public static Action StartRace;
    public static Action<bool> StopRace;
    public static Action ResetAll;

    public static GameManager Instance;

    public Dictionary<RacerID, string> RaceCompletedRegistered =>
        raceCompletedRegistered;

    public Dictionary<RacerID, double> RaceNotCompletedRegistered =>
        raceNotCompletedRegistered;

    private readonly Dictionary<RacerID, string>
        raceCompletedRegistered = new();

    private readonly Dictionary<RacerID, double>
        raceNotCompletedRegistered = new();

    private float raceStartedTimeSecond;

    private const int totalRacers = 4;
    private int totalLaps = 1;

    private void Awake()
    {
        InitializeSingleton();
    }

    #region Initialization
    private void InitializeSingleton()
    {
        if (Instance == null)
        {
            Instance = this;

            return;
        }

        Destroy(gameObject);
    }
    #endregion

    #region Race
    public void CountDownStarts()
    {
        CountDownStarted?.Invoke(totalLaps);
    }

    public void CountDownDoneStartRace()
    {
        raceStartedTimeSecond = Time.time;

        ResetRaceData();

        StartRace?.Invoke();
    }

    private void ResetRaceData()
    {
        raceCompletedRegistered.Clear();

        raceNotCompletedRegistered.Clear();
    }

    public void RaceCompletedRegister(RacerID racerID)
    {
        raceCompletedRegistered.Add(racerID, GetFormattedTime());

        if (racerID == RacerID.Player)
        {
            bool isPlayerWon = raceCompletedRegistered.Count == 1;
            StopRace?.Invoke(isPlayerWon);
        }

        CheckFinalizeRace();
    }

    public void RaceNotCompletedRegister(RacerID racerID, double completedPercent)
    {
        raceNotCompletedRegistered.Add(racerID, completedPercent);

        CheckFinalizeRace();
    }

    private void CheckFinalizeRace()
    {
        int totalRegistered = raceCompletedRegistered.Count + raceNotCompletedRegistered.Count;

        if (totalRegistered < totalRacers)
            return;

        AddDNFRacers();

        FinalizeRankAndScores();
    }

    private void AddDNFRacers()
    {
        IEnumerable<KeyValuePair<RacerID, double>>
            orderedDNF = raceNotCompletedRegistered.OrderByDescending(x => x.Value);

        foreach (var racer in orderedDNF)
        {
            if (raceCompletedRegistered.ContainsKey(racer.Key))
                continue;

            raceCompletedRegistered.Add(racer.Key, "DNF");
        }
    }

    private void FinalizeRankAndScores()
    {
        Dictionary<RacerID, int> scores = ScoreManager.Instance.Scores;

        List<SingleRankScoreDatas> singleRankScoreDatas = new();

        foreach (var racer in raceCompletedRegistered)
        {
            int score = scores.TryGetValue(racer.Key, out int value) ? value : 0;

            singleRankScoreDatas.Add(
                new SingleRankScoreDatas(
                    racer.Key.ToString(),
                    racer.Value,
                    score.ToString()
                )
            );
        }

        UIManager.Instance.ActivateScoreCardUI(singleRankScoreDatas);
    }

    public bool DidAnyoneWonRace()
    {
        return raceCompletedRegistered.Count == 0;
    }

    private string GetFormattedTime()
    {
        TimeSpan time = TimeSpan.FromSeconds(Time.time - raceStartedTimeSecond);

        return string.Format(
            "{0:D2}:{1:D2}:{2:D2}",
            time.Hours,
            time.Minutes,
            time.Seconds
        );
    }

    public void SetTotalLaps(int lapCount)
    {
        totalLaps = lapCount;
    }
    #endregion

    #region Game State
    public void UpdateGameState(GameState gameState)
    {
        switch (gameState)
        {
            case GameState.Pause:
                PauseGame();
                break;

            case GameState.Resume:
                ResumeGame();
                break;
        }
    }

    private void PauseGame()
    {
        Time.timeScale = 0f;
    }

    private void ResumeGame()
    {
        Time.timeScale = 1f;
    }
    #endregion

    #region Reset
    public void Reset()
    {
        ResetRaceData();

        ResetAll?.Invoke();
    }
    #endregion

    #region Test
    [Header("Testing")]
    [OnValueChange(nameof(TestResetAllF))]
    [SerializeField] private bool testResetAll;

    [OnValueChange(nameof(TestStartRaceF))]
    [SerializeField] private bool testStartRace;

    private void TestResetAllF()
    {
        Reset();
    }

    private void TestStartRaceF()
    {
        CountDownDoneStartRace();
    }
    #endregion
}