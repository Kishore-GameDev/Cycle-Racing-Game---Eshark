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

public class AIController : RacerControllerBase
{
    [Header("References")]
    [SerializeField] private AnimationController animationController;
    [SerializeField] private SplineFollower splineFollower;
    [SerializeField] private AIConfig aIConfig;

    [Header("Settings")]
    [SerializeField] private float startDistance;

    [Header("Lean")]
    [SerializeField] private float maxLeanAngle = 25f;
    [SerializeField] private float leanSmoothSpeed = 5f;

    [SerializeField]
    private Transform leanPivot;


    private readonly MinMax laneOffset = new(-3f, 3f);

    private AIState aiState;

    private LaneSide currentLane;

    private Coroutine laneSwitchRoutine;
    private Coroutine oilCrashRoutine;
    private Coroutine mudCrashRoutine;
    private Coroutine barricadeCrashRoutine;
    private Coroutine shieldRoutine;
    private Coroutine scoreBoosterRoutine;

    private const float laneSwitchSpeed = 18f;
    private float currentSpeed;
    private float maxSpeed;
    private float acceleration;
    private float currentLean;
    private float targetLean;

    private double previousSplinePercent;

    private int completedLaps;
    private int totalLaps;

    private Vector3 previousPosition;

    private bool alreadyStoppedRace;
    private bool registeredRaceComplete = true;

    private float CurrentLaneOffset =>
        currentLane == LaneSide.Left
            ? laneOffset.min
            : laneOffset.max;

    private void Start()
    {
        SubscribeEvents();
        Initialize();
    }

    private void Update()
    {
        if (aiState == AIState.Move)
        {
            Move();
        }

        CheckRaceComplete();
    }

    private void Initialize()
    {
        currentLane = aIConfig.startLane;

        maxSpeed = aIConfig.maxSpeed;
        acceleration = aIConfig.acceleration;

        currentSpeed = 0f;
        previousPosition = transform.position;

        aiState = AIState.Idle;
    }

    #region Events
    private void SubscribeEvents()
    {
        GameManager.CountDownStarted += CountDownStarted;
        GameManager.StartRace += StartRace;
        GameManager.StopRace += StopRace;
        GameManager.ResetAll += ResetAll;
    }

    private void UnsubscribeEvents()
    {
        GameManager.CountDownStarted -= CountDownStarted;
        GameManager.StartRace -= StartRace;
        GameManager.StopRace -= StopRace;
        GameManager.ResetAll -= ResetAll;
    }
    #endregion

    #region Race
    private void CountDownStarted(int totalLapCount)
    {
        completedLaps = 0;
        totalLaps = totalLapCount;
    }

    private void StartRace()
    {
        alreadyStoppedRace = false;

        registeredRaceComplete = false;

        previousSplinePercent = splineFollower.result.percent;

        UpdateState(AIState.Move);
    }

    private void StopRace(bool isPlayerWon)
    {
        if (alreadyStoppedRace)
        {
            return;
        }

        if (!registeredRaceComplete)
        {
            GameManager.Instance.RaceNotCompletedRegister(RacerID, GetCurrentCompletedPercentage());
        }

        alreadyStoppedRace = true;

        ResetCoroutines();

        currentSpeed = 0f;
        
        leanPivot.localRotation = Quaternion.Euler(Vector3.zero);

        UpdateState(AIState.Idle);
    }

    private void CheckRaceComplete()
    {
        if (registeredRaceComplete)
            return;

        double currentPercent = splineFollower.result.percent;

        if (previousSplinePercent > 0.9 && currentPercent < 0.1)
        {
            completedLaps++;

            if (completedLaps >= totalLaps)
            {
                registeredRaceComplete = true;

                bool isFirst = GameManager.Instance.DidAnyoneWonRace();

                GameManager.Instance.RaceCompletedRegister(RacerID);

                StopRace(false);

                if (isFirst)
                {
                    UpdateState(AIState.Won);
                }
            }
        }

        previousSplinePercent = currentPercent;
    }

    private double GetCurrentCompletedPercentage()
    {
        double overallProgress = (completedLaps + splineFollower.result.percent) / totalLaps;

        return overallProgress * 100f;
    }
    #endregion

    #region States
    private void UpdateState(AIState newState)
    {
        if (aiState == newState)
            return;

        aiState = newState;

        switch (aiState)
        {
            case AIState.Idle:
                EnterIdleState();
                break;

            case AIState.Move:
                EnterMoveState();
                break;

            case AIState.Won:
                EnterWonState();
                break;

            case AIState.CrashedOilObstacle:
                EnterOilCrashState();
                break;

            case AIState.CrashedMudObstacle:
                EnterMudCrashState();
                break;

            case AIState.CrashedBarricadeObstacle:
                EnterBarricadeCrashState();
                break;
        }
    }

    private void EnterIdleState()
    {
        splineFollower.followSpeed = 0f;

        animationController.UpdateCycleAnimState(CycleAnimState.Idle);
    }

    private void EnterMoveState()
    {
        Move();
    }

    private void EnterWonState()
    {
        animationController.UpdateCycleAnimState(CycleAnimState.Celebration);
    }

    private void EnterOilCrashState()
    {
        StopRoutine(ref oilCrashRoutine);

        oilCrashRoutine = StartCoroutine(OilCrash());
    }

    private void EnterMudCrashState()
    {
        StopRoutine(ref mudCrashRoutine);

        mudCrashRoutine = StartCoroutine(MudCrash());
    }

    private void EnterBarricadeCrashState()
    {
        StopRoutine(ref barricadeCrashRoutine);

        barricadeCrashRoutine = StartCoroutine(BarricadeCrash());
    }
    #endregion

    private void Move()
    {
        currentSpeed = Mathf.MoveTowards(
            currentSpeed,
            maxSpeed,
            acceleration * Time.deltaTime
        );

        splineFollower.followSpeed = currentSpeed;

        HandleLean();

        float movedDistance =
            Vector3.Distance(
                previousPosition,
                transform.position
            );

        ScoreManager.AddMeterScore(
            RacerID,
            ScoreBoosterActive,
            movedDistance
        );

        previousPosition = transform.position;

        animationController.UpdateCycleAnimState(
            CycleAnimState.Move,
            currentSpeed / maxSpeed
        );
    }

    private void HandleLean()
    {
        Vector3 currentForward =
            splineFollower.result.forward;

        Vector3 futureForward =
            splineFollower.EvaluatePosition(
                splineFollower.result.percent + 0.001
            ) -
            splineFollower.result.position;

        futureForward.Normalize();

        float turnAmount =
            Vector3.SignedAngle(
                currentForward,
                futureForward,
                Vector3.up
            );

        targetLean =
            -turnAmount * 5f;

        targetLean =
            Mathf.Clamp(
                targetLean,
                -maxLeanAngle,
                maxLeanAngle
            );

        currentLean = Mathf.Lerp(
            currentLean,
            targetLean,
            leanSmoothSpeed *
            Time.deltaTime
        );

        leanPivot.localRotation =
            Quaternion.Euler(
                0f,
                0f,
                currentLean
            );
    }

    #region Obstacles
    public void CrashOnObstacle(ObstacleType obstacleType)
    {
        if (ShieldActive)
        {
            return;
        }

        if (aiState == AIState.Won)
        {
            return;
        }

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
        if (aiState == AIState.Won)
            return;

        AIObstacleIntelligence intelligence = aIConfig.aIObstacleIntelligence;

        if (intelligence == AIObstacleIntelligence.Confused)
        {
            intelligence =
                Random.value < 0.5f
                    ? AIObstacleIntelligence.Dumb
                    : AIObstacleIntelligence.Smart;
        }

        if (intelligence != AIObstacleIntelligence.Smart)
            return;

        StopRoutine(ref laneSwitchRoutine);

        laneSwitchRoutine = StartCoroutine(SwitchLane());
    }

    private IEnumerator SwitchLane()
    {
        currentLane =
            currentLane == LaneSide.Left
                ? LaneSide.Right
                : LaneSide.Left;

        float targetLane = CurrentLaneOffset;

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

        splineFollower.motion.offset =
            new Vector2(targetLane, 0f);

        laneSwitchRoutine = null;
    }

    private IEnumerator OilCrash()
    {
        float deceleration = currentSpeed * 0.9f;
        leanPivot.localRotation = Quaternion.Euler(Vector3.zero);

        animationController.UpdateCycleAnimState(
            CycleAnimState.OilCrash
        );

        while (currentSpeed > 0.01f)
        {
            currentSpeed = Mathf.MoveTowards(
                currentSpeed,
                0f,
                deceleration * Time.deltaTime
            );

            splineFollower.followSpeed = currentSpeed;

            yield return null;
        }

        yield return new WaitForSeconds(1f);

        oilCrashRoutine = null;

        UpdateState(AIState.Move);
    }

    private IEnumerator MudCrash()
    {
        splineFollower.followSpeed = 15f;

        if (currentSpeed > maxSpeed)
        {
            currentSpeed = maxSpeed;
        }

        yield return new WaitForSeconds(5f);

        mudCrashRoutine = null;

        UpdateState(AIState.Move);
    }

    private IEnumerator BarricadeCrash()
    {
        leanPivot.localRotation = Quaternion.Euler(Vector3.zero);
        currentSpeed = 0f;

        UpdateState(AIState.Idle);

        yield return new WaitForSeconds(3.5f);

        barricadeCrashRoutine = null;

        UpdateState(AIState.Move);
    }
    #endregion

    #region Coroutines & Reset
    private void ResetCoroutines()
    {
        StopRoutine(ref laneSwitchRoutine);
        StopRoutine(ref oilCrashRoutine);
        StopRoutine(ref mudCrashRoutine);
        StopRoutine(ref barricadeCrashRoutine);
        StopRoutine(ref shieldRoutine);
        StopRoutine(ref scoreBoosterRoutine);
    }

    private void StopRoutine(ref Coroutine routine)
    {
        if (routine == null)
            return;

        StopCoroutine(routine);

        routine = null;
    }

    private void ResetAll()
    {
        ResetCoroutines();

        currentSpeed = 0f;
        completedLaps = 0;
        currentLean = 0f;
        targetLean = 0f;
        leanPivot.localRotation = Quaternion.Euler(Vector3.zero);

        alreadyStoppedRace = false;
        registeredRaceComplete = true;

        shieldActive = false;
        scoreBoosterActive = false;

        currentLane = aIConfig.startLane;

        maxSpeed = aIConfig.maxSpeed;
        acceleration = aIConfig.acceleration;

        //splineFollower.RebuildImmediate();
        splineFollower.Evaluate(0f);

        if (RacerID == RacerID.AI2)
            Debug.Log($"Lane {currentLane}, Offset {CurrentLaneOffset}");
        
        splineFollower.motion.offset = new Vector2(CurrentLaneOffset, 0f);
        splineFollower.followSpeed = 0f;

        splineFollower.SetDistance(startDistance);
        previousSplinePercent = splineFollower.result.percent;

        previousPosition = transform.position;

        UpdateState(AIState.Idle);
    }
    #endregion

    #region PowerUps
    private void ActivateShield()
    {
        shieldActive = true;

        StopRoutine(ref shieldRoutine);
        shieldRoutine = StartCoroutine(ShieldTimer());
    }

    private IEnumerator ShieldTimer()
    {
        yield return new WaitForSeconds(10f);
        shieldActive = false;
    }
    
    private void ActivateScoreBooster()
    {
        scoreBoosterActive = true;

        StopRoutine(ref scoreBoosterRoutine);
        scoreBoosterRoutine = StartCoroutine(ScoreBoosterTimer());
    }

    private IEnumerator ScoreBoosterTimer()
    {
        yield return new WaitForSeconds(10f);
        scoreBoosterActive = false;
    }
    #endregion

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Oil"))
        {
            CrashOnObstacle(ObstacleType.Oil);

            return;
        }

        if (other.CompareTag("Mud"))
        {
            CrashOnObstacle(ObstacleType.Mud);

            return;
        }

        if (other.CompareTag("Shield"))
        {
            if (!other.TryGetComponent(out PowerUp powerUp))
            {
                return;
            }

            if (!powerUp.IsActive)
            {
                return;
            }

            ActivateShield();
            powerUp.Triggered();
            return;
        }
        
        if (other.CompareTag("SB"))
        {
            if (!other.TryGetComponent(out PowerUp powerUp))
            {
                return;
            }

            if (!powerUp.IsActive)
            {
                return;
            }

            ActivateScoreBooster();
            powerUp.Triggered();
            return;
        }

        if (!other.CompareTag("Barr"))
            return;

        if (!other.TryGetComponent(out Barricade barricade))
            return;

        if (!barricade.IsActive())
            return;

        CrashOnObstacle(ObstacleType.Barricade);

        barricade.DestroyBarricade();
    }

    private void OnDestroy()
    {
        UnsubscribeEvents();
    }

    #region Test
    [Header("Testing")]
    [OnValueChange(nameof(TestUpdateState))]
    [SerializeField] private AIState testAIState;

    private void TestUpdateState()
    {
        UpdateState(testAIState);
    }
    #endregion
}