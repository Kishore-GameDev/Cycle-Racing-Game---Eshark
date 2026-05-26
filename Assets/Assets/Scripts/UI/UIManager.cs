using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum UIState
{
    Menu,
    Racing,
    Pause,
    ScoreCard,
}

public class UIManager : MonoBehaviour
{
    private static WaitForSeconds _waitForSeconds1 = new WaitForSeconds(1f);
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI progressText;
    [SerializeField] private GameObject wrongDirectionObject;
    [SerializeField] private TextMeshProUGUI crashedText;
    [SerializeField] private TextMeshProUGUI slowedText;
    [SerializeField] private TextMeshProUGUI countdownText;
    [SerializeField] private Image shieldFG;
    [SerializeField] private Image scoreBoosterFG;
    [SerializeField] private List<SingleRankScoreData> singleRankScoreDataList;
    [SerializeField] private GameObject menuUI;
    [SerializeField] private GameObject racingUI;
    [SerializeField] private GameObject pauseUI;
    [SerializeField] private GameObject scoreCardUI;
    [SerializeField] private TextMeshProUGUI raceCompletionPercentageText;
    [SerializeField] private Slider raceCompletionPercentageSlider;

    private Coroutine crashedTextRoutine;
    private Coroutine slowedTextRoutine;
    private Coroutine shieldFillRoutine;
    private Coroutine scoreBoosterFillRoutine;

    public static UIManager Instance;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        GameManager.StopRace += StopRace;
    }

    private void StopRace(bool isPlayerWon)
    {
        StopRoutine(ref crashedTextRoutine);
        StopRoutine(ref slowedTextRoutine);
        StopRoutine(ref shieldFillRoutine);
        StopRoutine(ref scoreBoosterFillRoutine);

        SetActiveWrongDirectionText(false);

        crashedText.gameObject.SetActive(false);
        slowedText.gameObject.SetActive(false);

        shieldFG.transform.parent.gameObject.SetActive(false);
        scoreBoosterFG.transform.parent.gameObject.SetActive(false);
    }

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
        SetActivePanel(UIState.ScoreCard);
        for (int i = 0; i < singleRankScoreDatas.Count; i++)
        {
            var data = singleRankScoreDatas[i];
            singleRankScoreDataList[i].SetData(data.name, data.time, data.score);
        }
    }

    private void SetActivePanel(UIState uiState)
    {
        menuUI.SetActive(uiState == UIState.Menu);
        racingUI.SetActive(uiState == UIState.Racing);
        pauseUI.SetActive(uiState == UIState.Pause);
        scoreCardUI.SetActive(uiState == UIState.ScoreCard);
    }

    public void SetActiveWrongDirectionText(bool active)
    {
        wrongDirectionObject.SetActive(active);
    }

    public void UpdateScoreText(int score)
    {
        scoreText.text = "Score: " + score;
    }

    public void UpdateProgressText(int progress)
    {
        progressText.text = progress + "% to Complete";
    }

    public void ShowCrashedText(float duration)
    {
        StopRoutine(ref crashedTextRoutine);

        crashedTextRoutine = StartCoroutine(ShowCrashTimer(duration));
    }

    private IEnumerator ShowCrashTimer(float duration)
    {
        crashedText.gameObject.SetActive(true);

        float timer = duration;

        while (timer > 0f)
        {
            crashedText.text =
                $"Crashed {timer:F1}s";

            timer -= Time.deltaTime;

            yield return null;
        }

        crashedText.gameObject.SetActive(false);
    }
    
    public void ShowSlowedText(float duration)
    {
        StopRoutine(ref slowedTextRoutine);
        
        slowedTextRoutine = StartCoroutine(ShowSlowedTimer(duration));
    }

    private IEnumerator ShowSlowedTimer(float duration)
    {
        slowedText.gameObject.SetActive(true);

        float timer = duration;

        while (timer > 0f)
        {
            slowedText.text =
                $"Slowed {timer:F1}s";

            timer -= Time.deltaTime;

            yield return null;
        }

        slowedText.gameObject.SetActive(false);
    }

    public void StartShieldTimer(float duration)
    {
        StopRoutine(ref shieldFillRoutine);

        shieldFillRoutine = StartCoroutine(ShieldReduceFill(duration));
    }

    private IEnumerator ShieldReduceFill(float duration)
    {
        float timer = duration;

        shieldFG.fillAmount = 1f;
        shieldFG.transform.parent.gameObject.SetActive(true);

        while (timer > 0f)
        {
            timer -= Time.deltaTime;

            shieldFG.fillAmount = timer / duration;

            yield return null;
        }

        shieldFG.fillAmount = 0f;
        shieldFG.transform.parent.gameObject.SetActive(false);
    }

    public void StartScoreBoosterTimer(float duration)
    {
        StopRoutine(ref scoreBoosterFillRoutine);

        scoreBoosterFillRoutine = StartCoroutine(ScoreBoosterReduceFill(duration));
    }

    private IEnumerator ScoreBoosterReduceFill(float duration)
    {
        float timer = duration;

        scoreBoosterFG.fillAmount = 1f;
        scoreBoosterFG.transform.parent.gameObject.SetActive(true);

        while (timer > 0f)
        {
            timer -= Time.deltaTime;

            scoreBoosterFG.fillAmount = timer / duration;

            yield return null;
        }

        scoreBoosterFG.fillAmount = 0f;
        scoreBoosterFG.transform.parent.gameObject.SetActive(false);
    }

    private void StopRoutine(ref Coroutine routine)
    {
        if (routine == null)
            return;

        StopCoroutine(routine);

        routine = null;
    }

    public void RaceCompleteionPercentageSliderValueChange()
    {
        raceCompletionPercentageText.text = "" + raceCompletionPercentageSlider.value;
    }

    public int GetRaceCompletionPercentage()
    {
        return (int)raceCompletionPercentageSlider.value;
    }

    public void ButtonStartRace()
    {
        ActivateRacingUI();
        StartCoroutine(StartCountDown());
    }

    private IEnumerator StartCountDown()
    {
        countdownText.gameObject.SetActive(true);
        countdownText.text = "Ready";
        yield return _waitForSeconds1;
        countdownText.text = "3";
        yield return _waitForSeconds1;
        countdownText.text = "2";
        yield return _waitForSeconds1;
        countdownText.text = "1";
        yield return _waitForSeconds1;
        countdownText.text = "Go";
        GameManager.Instance.CountDownDoneStartRace();
        yield return _waitForSeconds1;
        countdownText.gameObject.SetActive(false);
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
        StartCoroutine(StartCountDown());
    }
    
    public void ButtonMenuFromPauseUI()
    {
        GameManager.Instance.UpdateGameState(GameState.Resume);
        ActivateMenuUI();
        GameManager.Instance.Reset();
    }

    void OnDestroy()
    {
        GameManager.StopRace -= StopRace;
    }
}
