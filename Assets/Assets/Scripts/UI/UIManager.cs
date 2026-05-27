using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum UIState
{
    None,
    Menu,
    Racing,
    Pause,
    ScoreCard,
}

public class UIManager : MonoBehaviour
{
    private static readonly WaitForSeconds Wait1Second = new(1f);

    public static UIManager Instance;

    [Header("Gameplay UI")]
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI lapText;
    [SerializeField] private TextMeshProUGUI progressText;
    [SerializeField] private GameObject wrongDirectionObject;
    [SerializeField] private TextMeshProUGUI crashedText;
    [SerializeField] private TextMeshProUGUI slowedText;
    [SerializeField] private TextMeshProUGUI countdownText;

    [Header("Powerups")]
    [SerializeField] private Image shieldFG;
    [SerializeField] private Image scoreBoosterFG;

    [Header("Panels")]
    [SerializeField] private GameObject menuUI;
    [SerializeField] private GameObject racingUI;
    [SerializeField] private GameObject pauseUI;
    [SerializeField] private GameObject scoreCardUI;

    [Header("Score Card")]
    [SerializeField] private List<SingleRankScoreData> singleRankScoreDataList;

    // [Header("Race Completion")]
    // [SerializeField] private TextMeshProUGUI raceCompletionPercentageText;
    // [SerializeField] private Slider raceCompletionPercentageSlider;

    private Coroutine crashedTextRoutine;
    private Coroutine slowedTextRoutine;
    private Coroutine shieldFillRoutine;
    private Coroutine scoreBoosterFillRoutine;
    private Coroutine countdownRoutine;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        SubscribeEvents();

        ActivateMenuUI();
    }

    #region Events
    private void SubscribeEvents()
    {
        GameManager.ResetAll += ResetUIEffects;
        GameManager.StopRace += StopRace;
    }

    private void UnsubscribeEvents()
    {
        GameManager.ResetAll -= ResetUIEffects;
        GameManager.StopRace -= StopRace;
    }

    private void StopRace(bool isPlayerWon)
    {
        ResetUIEffects();
    }
    #endregion

    #region Panels
    private void ActivateMenuUI()
    {
        SetActivePanel(UIState.Menu);
    }

    private void ActivateRacingUI()
    {
        SetActivePanel(UIState.Racing);
    }

    private void ActivatePauseUI()
    {
        SetActivePanel(UIState.Pause);
    }

    public void ActivateScoreCardUI(List<SingleRankScoreDatas> singleRankScoreDatas)
    {
        for (int i = 0; i < singleRankScoreDatas.Count; i++)
        {
            SingleRankScoreDatas data = singleRankScoreDatas[i];

            singleRankScoreDataList[i].SetData(data.name, data.time, data.score);
        }

        Debug.Log("Name: " + singleRankScoreDataList[0].Name);
        if (singleRankScoreDataList[0].Name.Equals(RacerID.Player.ToString()))
        {
            Debug.Log("Name: If");
            StartCoroutine(ActivateScoreCardUIWithDelay());
        }
        else
        {
            Debug.Log("Name: Else");
            SetActivePanel(UIState.ScoreCard);
        }
    }

    private IEnumerator ActivateScoreCardUIWithDelay()
    {
        SetActivePanel(UIState.None);
                
        yield return new WaitForSeconds(2f);

        SetActivePanel(UIState.ScoreCard);
    }

    private void SetActivePanel(UIState uiState)
    {
        menuUI.SetActive(uiState == UIState.Menu);

        racingUI.SetActive(uiState == UIState.Racing);

        pauseUI.SetActive(uiState == UIState.Pause);

        scoreCardUI.SetActive(uiState == UIState.ScoreCard);
    }
    #endregion

    #region Gameplay UI
    public void SetActiveWrongDirectionText(bool active)
    {
        wrongDirectionObject.SetActive(active);
    }

    public void UpdateScoreText(int score)
    {
        scoreText.text = $"Score: {score}";
    }

    public void UpdateLapText(int lapCount, int totalLaps)
    {
        lapText.text = $"Lap: {lapCount}/{totalLaps}";
    }

    public void UpdateProgressText(int progress)
    {
        progressText.text = $"{progress}% Progress";
    }
    #endregion

    #region Text Timers
    public void ShowCrashedText(float duration)
    {
        StopRoutine(ref crashedTextRoutine);

        crashedTextRoutine = StartCoroutine(ShowTimerText(crashedText, "Crashed", duration));
    }

    public void ShowSlowedText(float duration)
    {
        StopRoutine(ref slowedTextRoutine);

        slowedTextRoutine = StartCoroutine(ShowTimerText(slowedText,"Slowed", duration));
    }

    private IEnumerator ShowTimerText(TextMeshProUGUI textUI, string label, float duration)
    {
        textUI.gameObject.SetActive(true);

        float timer = duration;

        while (timer > 0f)
        {
            textUI.text = $"{label} {timer:F1}s";

            timer -= Time.deltaTime;

            yield return null;
        }

        textUI.gameObject.SetActive(false);
    }
    #endregion

    #region Fill Timers
    public void StartShieldTimer(float duration)
    {
        StopRoutine(ref shieldFillRoutine);

        shieldFillRoutine = StartCoroutine(ReduceFill(shieldFG, duration));
    }

    public void StartScoreBoosterTimer(float duration)
    {
        StopRoutine(ref scoreBoosterFillRoutine);

        scoreBoosterFillRoutine = StartCoroutine(ReduceFill(scoreBoosterFG, duration));
    }

    private IEnumerator ReduceFill(Image image, float duration)
    {
        float timer = duration;

        image.fillAmount = 1f;

        image.transform.parent.gameObject.SetActive(true);

        while (timer > 0f)
        {
            timer -= Time.deltaTime;

            image.fillAmount = timer / duration;

            yield return null;
        }

        image.fillAmount = 0f;

        image.transform.parent.gameObject.SetActive(false);
    }
    #endregion

    #region Countdown
    private IEnumerator StartCountDown()
    {
        GameManager.Instance.CountDownStarts();
        countdownText.gameObject.SetActive(true);

        countdownText.text = "Ready";
        yield return Wait1Second;

        countdownText.text = "3";
        yield return Wait1Second;

        countdownText.text = "2";
        yield return Wait1Second;

        countdownText.text = "1";
        yield return Wait1Second;

        countdownText.text = "Go";

        GameManager.Instance.CountDownDoneStartRace();

        yield return Wait1Second;

        countdownText.gameObject.SetActive(false);
    }
    #endregion

    #region Race Completion
    // public void RaceCompleteionPercentageSliderValueChange()
    // {
    //     raceCompletionPercentageText.text =
    //         raceCompletionPercentageSlider.value.ToString();
    // }

    // public int GetRaceCompletionPercentage()
    // {
    //     return (int)raceCompletionPercentageSlider.value;
    // }
    #endregion

    #region Buttons
    public void ButtonStartRace()
    {
        ActivateRacingUI();

        StopRoutine(ref countdownRoutine);

        countdownRoutine = StartCoroutine(StartCountDown());
    }

    public void ButtonExit()
    {
        Application.Quit();
    }

    public void ButtonMenuFromScoreUI()
    {
        ActivateMenuUI();

        GameManager.Instance.Reset();
    }

    public void ButtonPause()
    {
        GameManager.Instance.UpdateGameState(GameState.Pause);

        ActivatePauseUI();
    }

    public void ButtonResume()
    {
        GameManager.Instance.UpdateGameState(GameState.Resume);

        ActivateRacingUI();
    }

    public void ButtonRestart()
    {
        GameManager.Instance.UpdateGameState(GameState.Resume);

        GameManager.Instance.Reset();

        ActivateRacingUI();

        StopRoutine(ref countdownRoutine);

        countdownRoutine = StartCoroutine(StartCountDown());
    }

    public void ButtonMenuFromPauseUI()
    {
        GameManager.Instance.UpdateGameState(GameState.Resume);

        ActivateMenuUI();

        GameManager.Instance.Reset();
    }
    #endregion

    #region Utility
    private void ResetUIEffects()
    {
        StopRoutine(ref crashedTextRoutine);
        StopRoutine(ref slowedTextRoutine);
        StopRoutine(ref shieldFillRoutine);
        StopRoutine(ref scoreBoosterFillRoutine);
        StopRoutine(ref countdownRoutine);

        SetActiveWrongDirectionText(false);

        crashedText.gameObject.SetActive(false);

        slowedText.gameObject.SetActive(false);

        shieldFG.transform.parent.gameObject.SetActive(false);

        scoreBoosterFG.transform.parent.gameObject.SetActive(false);

        countdownText.gameObject.SetActive(false);
    }

    private void StopRoutine(ref Coroutine routine)
    {
        if (routine == null)
            return;

        StopCoroutine(routine);

        routine = null;
    }
    #endregion

    private void OnDestroy()
    {
        UnsubscribeEvents();
    }
}