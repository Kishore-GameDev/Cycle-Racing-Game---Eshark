using System.Collections;
using Dreamteck.Splines;
using UnityEngine;

public enum PlayerState
{
    Idle,
    Move,
    Celebration,
    OilCrash,
    MudCrash,
    BarricadeCrash
}

public class PlayerController : RacerControllerBase
{
    [Header("References")]
    [SerializeField] private AnimationController animationController;
    [SerializeField] private InputManager inputManager;
    [SerializeField] private SplineComputer spline;

    [Header("Movement")]
    [SerializeField] private float maxSpeed = 30f;
    [SerializeField] private float accelerationRate = 18f;
    [SerializeField] private float decelerationRate = 8f;
    [SerializeField] private float rotationSpeed = 100f;

    [Header("Lean")]
    [SerializeField] private float maxLeanAngle = 25f;
    [SerializeField] private float leanSmoothSpeed = 5f;

    private PlayerState playerState;

    private Coroutine oilCrashRoutine;
    private Coroutine mudCrashRoutine;
    private Coroutine barricadeCrashRoutine;
    private Coroutine shieldRoutine;
    private Coroutine scoreBoosterRoutine;

    private float currentYaw;
    private float currentSpeed;

    private float currentLean;
    private float targetLean;

    private float finalSpeed;

    private float actualMaxSpeed;
    private float actualDecelerationRate;

    private float currentRaceCompletePercent;

    private bool oilCrash;
    private bool playerCanMove;
    private bool registeredRaceComplete = true;

    private Vector3 startPos;
    private Quaternion startRot;
    private Vector3 previousPosition;

    private void Start()
    {
        SubscribeEvents();
        Initialize();
    }

    private void Update()
    {
        CheckWrongWay();
        CheckRaceComplete();
        UpdateRaceProgress();
    }

    private void Initialize()
    {
        actualMaxSpeed = maxSpeed;
        actualDecelerationRate = decelerationRate;

        startPos = transform.position;
        startRot = transform.rotation;
        previousPosition = transform.position;

        UpdateSplineYaw();

        playerState = PlayerState.Idle;
        playerCanMove = false;
    }

    #region Events
    private void SubscribeEvents()
    {
        inputManager.OnMoveContinues += MovementManagement;

        GameManager.StartRace += StartRace;
        GameManager.StopRace += StopRace;
        GameManager.ResetAll += ResetAll;
    }

    private void UnsubscribeEvents()
    {
        inputManager.OnMoveContinues -= MovementManagement;

        GameManager.StartRace -= StartRace;
        GameManager.StopRace -= StopRace;
        GameManager.ResetAll -= ResetAll;
    }
    #endregion

    #region Race
    private void StartRace(float raceCompletePercent)
    {
        registeredRaceComplete = false;

        currentRaceCompletePercent = raceCompletePercent;

        UpdateState(PlayerState.Move);
    }

    private void StopRace(bool isPlayerWon)
    {
        ResetCoroutines();

        if (isPlayerWon)
        {
            UpdateState(PlayerState.Celebration);
        }
        else
        {
            UpdateState(PlayerState.Idle);
        }
    }
    #endregion

    #region States
    private void UpdateState(PlayerState newState)
    {
        if (playerState == newState)
            return;

        playerState = newState;

        switch (playerState)
        {
            case PlayerState.Idle:
                EnterIdleState();
                break;

            case PlayerState.Move:
                EnterMoveState();
                break;

            case PlayerState.Celebration:
                EnterCelebrationState();
                break;

            case PlayerState.OilCrash:
                EnterOilCrashState();
                break;

            case PlayerState.MudCrash:
                EnterMudCrashState();
                break;

            case PlayerState.BarricadeCrash:
                EnterBarricadeCrashState();
                break;
        }
    }

    private void EnterIdleState()
    {
        playerCanMove = false;

        animationController.UpdateCycleAnimState(CycleAnimState.Idle);
    }

    private void EnterMoveState()
    {
        playerCanMove = true;
    }

    private void EnterCelebrationState()
    {
        playerCanMove = false;

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

    #region Movement
    private void MovementManagement(Vector2 moveInput)
    {
        if (!playerCanMove)
            return;

        if (oilCrash)
        {
            moveInput = Vector2.zero;
        }

        HandleAcceleration(moveInput);

        HandleMovement();

        HandleRotation(moveInput);

        HandleLean(moveInput);

        HandleAnimation();

        FollowSplineHeight();

        RestrictInsideTrack();
    }

    private void HandleAcceleration(Vector2 moveInput)
    {
        if (moveInput.y > 0)
        {
            currentSpeed += accelerationRate * Time.deltaTime;
        }
        else
        {
            currentSpeed -= decelerationRate * Time.deltaTime;
        }

        currentSpeed = Mathf.Clamp(
            currentSpeed,
            0f,
            maxSpeed
        );

        finalSpeed = currentSpeed;
    }

    private void HandleMovement()
    {
        transform.position += finalSpeed * Time.deltaTime * transform.forward;

        float movedDistance =
            Vector3.Distance(
                previousPosition,
                transform.position
            );

        if (!IsMovingOppositeDirection())
            ScoreManager.AddMeterScore(RacerID, ScoreBoosterActive, movedDistance);

        previousPosition = transform.position;
    }

    private void HandleRotation(Vector2 moveInput)
    {
        if (currentSpeed <= 0.1f)
            return;

        currentYaw += moveInput.x * rotationSpeed * Time.deltaTime;
    }

    private void HandleLean(Vector2 moveInput)
    {
        if (Mathf.Abs(moveInput.x) > 0.01f && currentSpeed > 1f)
        {
            targetLean = -moveInput.x * maxLeanAngle;
        }
        else
        {
            targetLean = 0f;
        }

        currentLean = Mathf.Lerp(
            currentLean,
            targetLean,
            leanSmoothSpeed *
            Time.deltaTime
        );
    }

    private void HandleAnimation()
    {
        if (oilCrash)
            return;

        if (currentSpeed <= 0f)
        {
            animationController.UpdateCycleAnimState(CycleAnimState.Idle);

            return;
        }

        animationController.UpdateCycleAnimState(CycleAnimState.Move, currentSpeed / maxSpeed);
    }

    private void FollowSplineHeight()
    {
        SplineSample sample = new();

        spline.Project(transform.position, ref sample);

        Vector3 pos = transform.position;

        pos.y = sample.position.y;

        transform.position = pos;

        Quaternion slopeRotation = Quaternion.LookRotation(sample.forward, sample.up);

        Quaternion yawRotation = Quaternion.Euler(0f, currentYaw, 0f);

        transform.rotation =
            yawRotation *
            Quaternion.Euler(
                slopeRotation.eulerAngles.x,
                0f,
                currentLean
            );
    }

    private void RestrictInsideTrack()
    {
        SplineSample sample = new();

        spline.Project(transform.position, ref sample);

        Vector3 offset = transform.position - sample.position;

        float horizontalOffset = Vector3.Dot(offset, sample.right);

        float absOffset = Mathf.Abs(horizontalOffset);

        float edgePercent =
            Mathf.InverseLerp(
                3f,
                4f,
                absOffset
            );

        float dragMultiplier =
            Mathf.Lerp(
                1f,
                0.000001f,
                edgePercent
            );

        finalSpeed = currentSpeed * dragMultiplier;

        horizontalOffset = Mathf.Clamp(horizontalOffset, -4f, 4f);

        Vector3 finalPosition = sample.position + sample.right * horizontalOffset;

        finalPosition.y = sample.position.y;

        transform.position = finalPosition;
    }
    #endregion

    #region Obstacles
    private void RanOverObstacle(ObstacleType obstacleType)
    {
        if (ShieldActive)
        {
            return;
        }

        switch (obstacleType)
        {
            case ObstacleType.Oil:
                UpdateState(PlayerState.OilCrash);
                break;

            case ObstacleType.Mud:
                UpdateState(PlayerState.MudCrash);
                break;

            case ObstacleType.Barricade:
                UpdateState(PlayerState.BarricadeCrash);
                break;
        }
    }

    private IEnumerator OilCrash()
    {
        oilCrash = true;

        decelerationRate = currentSpeed * 0.9f;

        animationController.UpdateCycleAnimState(CycleAnimState.OilCrash);

        UIManager.Instance.ShowCrashedText(2f);
        yield return new WaitForSeconds(2f);

        oilCrash = false;

        decelerationRate = actualDecelerationRate;

        oilCrashRoutine = null;

        UpdateState(PlayerState.Move);
    }

    private IEnumerator MudCrash()
    {
        maxSpeed = 15f;

        UIManager.Instance.ShowSlowedText(5f);
        yield return new WaitForSeconds(5f);

        maxSpeed = actualMaxSpeed;

        mudCrashRoutine = null;

        UpdateState(PlayerState.Move);
    }

    private IEnumerator BarricadeCrash()
    {
        playerCanMove = false;

        currentSpeed = 0f;

        UpdateSplineYaw();

        animationController.UpdateCycleAnimState(CycleAnimState.Idle);

        UIManager.Instance.ShowCrashedText(3.5f);
        yield return new WaitForSeconds(3.5f);

        barricadeCrashRoutine = null;

        UpdateState(PlayerState.Move);
    }
    #endregion

    #region Race
    private void CheckWrongWay()
    {
        if (!IsMovingOppositeDirection())
        {
            UIManager.Instance.SetActiveWrongDirectionText(false);
            return;
        }

        UIManager.Instance.SetActiveWrongDirectionText(true);
    }

    private void CheckRaceComplete()
    {
        if (registeredRaceComplete)
            return;

        if (GetPlayerSplinePercent() < currentRaceCompletePercent)
            return;

        registeredRaceComplete = true;

        GameManager.Instance.RaceCompletedRegister(RacerID);
    }

    private void UpdateRaceProgress()
    {
        float raceProgress = GetRaceProgress();
        UIManager.Instance.UpdateProgressText((int)raceProgress);
    }

    private float GetRaceProgress()
    {
        SplineSample sample = new();

        spline.Project(transform.position, ref sample);

        float currentPercent = (float)sample.percent * 100f;

        return Mathf.Clamp01(currentPercent / currentRaceCompletePercent) * 100f;
    }

    private float GetPlayerSplinePercent()
    {
        SplineSample sample = new();

        spline.Project(transform.position, ref sample);

        return (float)sample.percent * 100f;
    }

    private bool IsMovingOppositeDirection()
    {
        SplineSample sample = new();

        spline.Project(transform.position, ref sample);

        float dot = Vector3.Dot(transform.forward, sample.forward);

        return dot < 0f;
    }
    #endregion

    private void UpdateSplineYaw()
    {
        SplineSample sample = new();

        spline.Project(transform.position, ref sample);

        currentYaw = Quaternion.LookRotation(sample.forward).eulerAngles.y;
    }

    #region Coroutines & Reset
    private void ResetCoroutines()
    {
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

        currentLean = 0f;
        targetLean = 0f;

        finalSpeed = 0f;

        maxSpeed = actualMaxSpeed;
        decelerationRate = actualDecelerationRate;

        oilCrash = false;
        playerCanMove = false;

        registeredRaceComplete = true;

        shieldActive = false;
        scoreBoosterActive = false;

        transform.SetPositionAndRotation(startPos, startRot);
        previousPosition = transform.position;

        UpdateSplineYaw();

        UpdateState(PlayerState.Idle);
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
        UIManager.Instance.StartShieldTimer(10f);
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
        UIManager.Instance.StartScoreBoosterTimer(10f);
        yield return new WaitForSeconds(10f);
        scoreBoosterActive = false;
    }
    #endregion

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Oil"))
        {
            RanOverObstacle(ObstacleType.Oil);

            return;
        }

        if (other.CompareTag("Mud"))
        {
            RanOverObstacle(ObstacleType.Mud);

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

        RanOverObstacle(ObstacleType.Barricade);

        barricade.DestroyBarricade();
    }

    private void OnDestroy()
    {
        UnsubscribeEvents();
    }
}