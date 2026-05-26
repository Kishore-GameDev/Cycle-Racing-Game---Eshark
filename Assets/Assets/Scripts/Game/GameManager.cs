using System;
using System.Collections.Generic;
using System.Linq;
using K;
using UnityEngine;

public enum GameState
{
    Pause,
    Resume,
}

public class GameManager : MonoBehaviour
{
    public static Action<float> StartRace;
    public static Action<bool> StopRace;
    public static Action ResetAll;

    public static GameManager Instance;

    public Dictionary<RacerID, string> raceCompletedRegistered = new();
    public Dictionary<RacerID, double> raceNotCompletedRegistered = new();

    private float raceStartedTimeSecond;

    void Start()
    {
        if (GameManager.Instance == null)
        {
            GameManager.Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void CountDownDoneStartRace()
    {
        raceStartedTimeSecond = Time.time;
        StartRace?.Invoke(UIManager.Instance.GetRaceCompletionPercentage());
        raceCompletedRegistered.Clear();
        raceNotCompletedRegistered.Clear();
    }

    public void UpdateGameState(GameState gameState)
    {
        switch (gameState)
        {
            case GameState.Pause:
                Time.timeScale = 0f;
                break;
            
            case GameState.Resume:
                Time.timeScale = 1f;
                break;
        }
    }

    public void RaceCompletedRegister(RacerID racerID)
    {
        if (racerID == RacerID.Player)
        {
            bool isPlayerWon = DidAnyoneWonRace();
            raceCompletedRegistered.Add(racerID, GetFormattedTime());
            StopRace?.Invoke(isPlayerWon);
        }
        else
        {
            raceCompletedRegistered.Add(racerID, GetFormattedTime());
        }

        if (raceCompletedRegistered.Count + raceNotCompletedRegistered.Count >= 4)
        {
            FinalizeRankAndScores();
        }
    }
    
    public void RaceNotCompletedRegister(RacerID racerID, double completedPercent)
    {
        raceNotCompletedRegistered.Add(racerID, completedPercent);

        if (raceCompletedRegistered.Count + raceNotCompletedRegistered.Count >= 4)
        {
            var orderedScores = raceNotCompletedRegistered.OrderByDescending(x => x.Value);
            foreach (var item in orderedScores)
            {
                raceCompletedRegistered.Add(item.Key, "DNF");
            }
            FinalizeRankAndScores();
        }
    }

    private void FinalizeRankAndScores()
    {
        var scoresDict = ScoreManager.Instance.Scores;
        List<SingleRankScoreDatas> singleRankScoreDatas = new();

        foreach (var item in raceCompletedRegistered)
        {
            var score = "";
            foreach (var item2 in scoresDict)
            {
                if (item.Key == item2.Key)
                {
                    score = "" + item2.Value;
                    break;
                }
            }
            singleRankScoreDatas.Add(new(item.Key.ToString(), item.Value, score));
        }

        UIManager.Instance.ActivateScoreCardUI(singleRankScoreDatas);
    }

    public bool DidAnyoneWonRace() => raceCompletedRegistered.Count == 0;

    public string GetFormattedTime()
    {
        TimeSpan time = TimeSpan.FromSeconds(Time.time - raceStartedTimeSecond);

        return string.Format("{0:D2}:{1:D2}:{2:D2}", time.Hours, time.Minutes, time.Seconds);
    }

    public void Reset()
    {
        ResetAll?.Invoke();
    }

    #region Test
    [OnValueChange(nameof(TestResetAllF))][SerializeField] private bool TestResetAll;
    [OnValueChange(nameof(TestStartRaceF))][SerializeField] private bool TestStartRace;

    public void TestResetAllF() => Reset();
    public void TestStartRaceF() => CountDownDoneStartRace();
    #endregion
}
