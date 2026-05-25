using System.Collections;
using Dreamteck.Splines;
using K;
using K.DataType;
using UnityEngine;

public enum AIState
{
    Idle,
    Move,
    Won,
    Defeated,
    CrashedOilObstacle,
    CrashedMudObstacle,
    CrashedBarricadeObstacle,
}

public enum AIObstacleIntelligence
{
    Dumb,
    Confused,
    Smart,
}

public class AIController : MonoBehaviour
{
    [SerializeField] private AnimationController animationController;
    [SerializeField] private SplineFollower splineFollower;
    [SerializeField] private AIConfig aIConfig;
    [SerializeField] private float startDistance;

    private AIState aiState;
    private float currentSpeed;
    private MinMax laneOffset = new(-3.5f, 3.5f);
    private LaneSide currentLane;
    private readonly float laneSwitchSpeed = 18f;
    private float maxSpeed;
    private float acceleration;
    private Coroutine laneSwitchRoutine = null;
    private Coroutine oilCrashRoutine = null;
    private Coroutine mudCrashRoutine = null;
    private Coroutine barricadeCrashRoutine = null;
    private float currentRaceCompletePercent;
    private bool alreadyStoppedRace = false;
    private bool registeredRaceComplete = true;

    void Start()
    {
        GameManager.StartRace += StartRace;
        GameManager.StopRace += StopRace;
        GameManager.ResetAll += ResetAll;

        currentLane = aIConfig.startLane;
        maxSpeed = aIConfig.maxSpeed;
        acceleration = aIConfig.acceleration;
        splineFollower.motion.offset = new(currentLane == LaneSide.Left ? laneOffset.min : laneOffset.max, 0);
        aiState = AIState.Idle;
    }

    void Update()
    {
        ContinuesUpdateState();
        if (!registeredRaceComplete && splineFollower.result.percent * 100f >= currentRaceCompletePercent)
        {
            registeredRaceComplete = true;
            bool isFirst = GameManager.Instance.DidAnyoneWonRace();
            GameManager.Instance.RaceCompletedRegister(false, aIConfig.aiName);

            StopRace(false);
            if (isFirst)
            {
                UpdateState(AIState.Won);
            }
        }
    }

    private void StartRace(float raceCompletePercent)
    {
        registeredRaceComplete = false;
        currentRaceCompletePercent = raceCompletePercent;
        UpdateState(AIState.Move);
    }

    private void StopRace(bool isPlayerWon)
    {
        if (alreadyStoppedRace) return;

        alreadyStoppedRace = true;
        if (laneSwitchRoutine != null)
        {
            StopCoroutine(laneSwitchRoutine);
        }
        if (oilCrashRoutine != null)
        {
            StopCoroutine(oilCrashRoutine);
        }
        if (mudCrashRoutine != null)
        {
            StopCoroutine(mudCrashRoutine);
        }
        if (barricadeCrashRoutine != null)
        {
            StopCoroutine(barricadeCrashRoutine);
        }

        UpdateState(AIState.Idle);
        currentSpeed = 0f;
    }

    private void UpdateState(AIState _aiState)
    {
        if (aiState == _aiState)
        {
            return;
        }

        aiState = _aiState;

        switch (aiState)
        {
            case AIState.Idle:
                animationController.UpdateCycleAnimState(CycleAnimState.Idle);
                splineFollower.followSpeed = 0f;
                break;

            case AIState.Move:
                Move();
                break;

            case AIState.Won:
                animationController.UpdateCycleAnimState(CycleAnimState.Celebration);
                break;

            case AIState.CrashedOilObstacle:
                ResetOilCrashRoutine();

                oilCrashRoutine = StartCoroutine(OilCrash());
                break;

            case AIState.CrashedMudObstacle:
                ResetMudCrashRoutine();

                mudCrashRoutine = StartCoroutine(MudCrash());
                break;
            
            case AIState.CrashedBarricadeObstacle:
                ResetBarricadeCrashRoutine();

                barricadeCrashRoutine = StartCoroutine(BarricadeCrash());
                break;
        }
    }

    private void ContinuesUpdateState()
    {
        switch (aiState)
        {
            case AIState.Move:
                Move();
                break;
        }
    }

    private void Move()
    {
        float targetSpeed = maxSpeed;

        currentSpeed = Mathf.MoveTowards(
            currentSpeed,
            targetSpeed,
            acceleration * Time.deltaTime
        );

        splineFollower.followSpeed = currentSpeed;

        animationController.UpdateCycleAnimState(CycleAnimState.Move, currentSpeed / maxSpeed);
    }

    public void CrashOnObstacle(ObstacleType obstacleType)
    {
        switch (obstacleType)
        {
            case ObstacleType.Oil:
                UpdateState(AIState.CrashedOilObstacle);
                break;

            case ObstacleType.Mud:
                UpdateState(AIState.CrashedMudObstacle);
                break;

            case ObstacleType.Barricade:
                UpdateState(AIState.CrashedBarricadeObstacle);
                break;
        }
    }

    public void DetectedObstacle(ObstacleType obstacleType)
    {
        AIObstacleIntelligence intelligence = aIConfig.aIObstacleIntelligence;
        if (aIConfig.aIObstacleIntelligence == AIObstacleIntelligence.Confused)
        {
            intelligence = Random.value < 0.5f ? AIObstacleIntelligence.Dumb : AIObstacleIntelligence.Smart;
        }

        if (intelligence == AIObstacleIntelligence.Smart)
        {
            if (laneSwitchRoutine != null)
                StopCoroutine(laneSwitchRoutine);

            laneSwitchRoutine = StartCoroutine(SwitchLane());
        }
    }

    private IEnumerator SwitchLane()
    {
        currentLane =
            currentLane == LaneSide.Left
                ? LaneSide.Right
                : LaneSide.Left;

        float targetLane =
            currentLane == LaneSide.Left
                ? laneOffset.min
                : laneOffset.max;

        while (Mathf.Abs(splineFollower.motion.offset.x - targetLane) > 0.01f)
        {
            Vector2 offset = splineFollower.motion.offset;

            offset.x = Mathf.MoveTowards(
                offset.x,
                targetLane,
                laneSwitchSpeed * Time.deltaTime
            );

            splineFollower.motion.offset = offset;

            yield return null;
        }

        Vector2 finalOffset = splineFollower.motion.offset;
        finalOffset.x = targetLane;
        splineFollower.motion.offset = finalOffset;
    }

    private IEnumerator OilCrash()
    {
        float deceleration = currentSpeed * 0.9f;
        animationController.UpdateCycleAnimState(CycleAnimState.OilCrash);
        while (currentSpeed > 0.01f)
        {
            float targetSpeed = 0;

            currentSpeed = Mathf.MoveTowards(
                currentSpeed,
                targetSpeed,
                deceleration * Time.deltaTime
            );

            splineFollower.followSpeed = currentSpeed;

            yield return null;
        }

        yield return new WaitForSeconds(1f);

        UpdateState(AIState.Move);
    }

    private void ResetOilCrashRoutine()
    {
        if (oilCrashRoutine != null)
        {
            StopCoroutine(oilCrashRoutine);
            UpdateState(AIState.Move);
        }
    }

    private IEnumerator MudCrash()
    {
        splineFollower.followSpeed = 15f;
        if (currentSpeed > maxSpeed) currentSpeed = maxSpeed;
        yield return new WaitForSeconds(5f);

        UpdateState(AIState.Move);
    }

    private void ResetMudCrashRoutine()
    {
        if (mudCrashRoutine != null)
        {
            StopCoroutine(mudCrashRoutine);
        }
    }

    private IEnumerator BarricadeCrash()
    {
        UpdateState(AIState.Idle);
        currentSpeed = 0f;

        yield return new WaitForSeconds(3.5f);

        UpdateState(AIState.Move);
    }

    private void ResetBarricadeCrashRoutine()
    {
        if (barricadeCrashRoutine != null)
        {
            StopCoroutine(barricadeCrashRoutine);
            UpdateState(AIState.Move);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Oil"))
        {
            CrashOnObstacle(ObstacleType.Oil);
        }
        else if (other.gameObject.CompareTag("Mud"))
        {
            CrashOnObstacle(ObstacleType.Mud);
        }
        else if (other.gameObject.CompareTag("Barr") && other.gameObject.TryGetComponent(out Barricade comp))
        {
            if (comp.IsActive())
            {
                CrashOnObstacle(ObstacleType.Barricade);
                comp.DestroyBarricade();
            }
        }
    }

    private void ResetAll()
    {
        if (laneSwitchRoutine != null)
        {
            StopCoroutine(laneSwitchRoutine);
        }
        if (oilCrashRoutine != null)
        {
            StopCoroutine(oilCrashRoutine);
        }
        if (mudCrashRoutine != null)
        {
            StopCoroutine(mudCrashRoutine);
        }
        if (barricadeCrashRoutine != null)
        {
            StopCoroutine(barricadeCrashRoutine);
        }

        UpdateState(AIState.Idle);
        alreadyStoppedRace = false;
        currentSpeed = 0f;
        maxSpeed = aIConfig.maxSpeed;
        acceleration = aIConfig.acceleration;
        currentLane = aIConfig.startLane;

        splineFollower.motion.offset = new(currentLane == LaneSide.Left ? laneOffset.min : laneOffset.max, 0);
        splineFollower.followSpeed = 0f;

        splineFollower.SetDistance(startDistance);
        splineFollower.RebuildImmediate();
        splineFollower.Evaluate(0f);
    }

    void OnDestroy()
    {
        GameManager.StartRace -= StartRace;
        GameManager.StopRace -= StopRace;
        GameManager.ResetAll -= ResetAll;
    }

    #region Test
    [Header("Testing")]
    [OnValueChange(nameof(TestUpdateState))][SerializeField] private AIState TestAIState;

    public void TestUpdateState() => UpdateState(TestAIState);
    #endregion
}
