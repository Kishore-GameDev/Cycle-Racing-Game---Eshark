using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using K;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [Range(5f, 100f)][SerializeField] private float raceCompletePercent;

    public static Action<float> StartRace;
    public static Action<bool> StopRace;
    public static Action ResetAll;

    public static GameManager Instance;

    public Dictionary<string, string> raceCompletedRegistered = new();

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

    private async void CountDownDoneStartRace()
    {
        await Task.Delay(2000);
        raceStartedTimeSecond = Time.time;
        StartRace?.Invoke(raceCompletePercent);
        raceCompletedRegistered.Clear();
    }

    public void RaceCompletedRegister(bool isPlayer = false, string aiName = "")
    {
        if (isPlayer)
        {
            bool isPlayerWon = DidAnyoneWonRace();
            raceCompletedRegistered.Add("YOU", GetFormattedTime());
            StopRace?.Invoke(isPlayerWon);
        }
        else
        {
            raceCompletedRegistered.Add(aiName, GetFormattedTime());
        }
    }

    public bool DidAnyoneWonRace() => raceCompletedRegistered.Count == 0;

    public string GetFormattedTime()
    {
        TimeSpan time = TimeSpan.FromSeconds(Time.time - raceStartedTimeSecond);

        return string.Format("{0:D2}:{1:D2}:{2:D2}", time.Hours, time.Minutes, time.Seconds);
    }

    #region Test
    [OnValueChange(nameof(TestResetAllF))][SerializeField] private bool TestResetAll;
    [OnValueChange(nameof(TestStartRaceF))][SerializeField] private bool TestStartRace;

    public void TestResetAllF() => ResetAll?.Invoke();
    public void TestStartRaceF() => CountDownDoneStartRace();
    #endregion
}
