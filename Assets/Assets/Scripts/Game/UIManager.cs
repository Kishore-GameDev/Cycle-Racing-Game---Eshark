using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    [SerializeField] private GameObject wrongDirectionObject;
    [SerializeField] private TextMeshProUGUI crashedText;
    [SerializeField] private TextMeshProUGUI slowedText;
    [SerializeField] private Image shieldFG;
    [SerializeField] private Image scoreBoosterFG;

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

    public void SetActiveWrongDirectionText(bool active)
    {
        wrongDirectionObject.SetActive(active);
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

    void OnDestroy()
    {
        GameManager.StopRace -= StopRace;
    }
}
